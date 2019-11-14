/*
AVA Chat Engine is a chat bot API.

Copyright (C) 2015-2019  Asurion, LLC

AVA Chat Engine is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

AVA Chat Engine is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with AVA Chat Engine.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Bot.Connector;
using Newtonsoft.Json;
using ChatWeb.Models;
using System.Dynamic;
using ChatWeb.Services;
using System.Web;
using ChatWeb.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using ChatWeb;
using System.Text;
using Microsoft.AspNetCore.SignalR;
using ChatEngine.Hubs;
using Microsoft.AspNetCore.NodeServices;
using System.Collections.Generic;

namespace ChatEngine.Controllers
{
    /// <summary>
    /// Exposes Microsoft Bot Connector API.
    /// See http://docs.botframework.com/connector/getstarted/#navtitle
    /// </summary>
    [BotAuthentication]
    [Route("api/[controller]")]
    [ApiController]
    public class MessagesController : ControllerBase
    {
        const string HelpText = @"The following commands are available.
-help : This help message
-startover : Reset the chat as a new conversation";
        const int SessionTimeout = 20;

        readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly MemCacheService memCacheService;

        public MessagesController(MemCacheService memCacheService)
        {
            this.memCacheService = memCacheService;
        }

        /// <summary>
        /// POST: api/Messages
        /// Receive a message from a user and reply to it
        /// </summary>
        [HttpPost()]
        public async Task<ActionResult> Post([FromBody]Activity activity, string partner, string context, string channel, string userId, bool dummyMode, [FromServices]ChatWeb.Services.ChatEngine chatEngine)
        {
            //set the property to our new object
            log4net.LogicalThreadContext.Properties["interactionId"] = Guid.NewGuid().ToString();
            log4net.LogicalThreadContext.Properties["clientIp"] = NetworkHelpers.GetClientIP(HttpContext);
            log4net.LogicalThreadContext.Properties["SessionData"] = null;

            if (String.IsNullOrEmpty(channel))
                channel = activity.ChannelId;

            try
            {
                logger.DebugFormat("Internal: MessagesController Request: {0}", JsonConvert.SerializeObject(activity));

                Activity reply = null;

                switch (activity.Type)
                {
                    case ActivityTypes.Message:
                    {
                        if (String.Equals(activity.Text, "-help", StringComparison.OrdinalIgnoreCase))
                            reply = activity.CreateReply(HelpText, "en");
                        else
                            reply = await ProcessUserMessage(activity, partner, context, channel, userId, dummyMode, chatEngine);
                        break;
                    }
                    case ActivityTypes.ConversationUpdate:
                        if (!activity.MembersAdded.Any(m => m.Id != activity.Recipient.Id))
                            break;

                        // Start new chat
                        reply = await ProcessUserMessage(activity, partner, context, channel, userId, dummyMode, chatEngine);
                        break;
                    default:
                        HandleSystemMessage(activity);
                        break;
                }

                if (reply != null)
                {
                    ConnectorClient connectorClient = new ConnectorClient(new Uri(activity.ServiceUrl));
                    await connectorClient.Conversations.ReplyToActivityAsync(reply);
                }
            }
            catch (Exception ex)
            {
                logger.Error("Internal: MessagesController.Post", ex);
            }

            return Ok();
        }

        private async Task<Activity> ProcessUserMessage(Activity activity, string partner, string context, string channel, string userId, bool dummyMode, ChatWeb.Services.ChatEngine chatEngine)
        {
            //set the property to our new object
            log4net.LogicalThreadContext.Properties["interactionId"] = Guid.NewGuid().ToString();

            var sessionRawData = memCacheService.GetObject<string>(activity.Conversation.Id);
            ChatRequest request = null;
            BotFrameworkSession sessionData = null;

            if ((sessionRawData == null) || ((activity.Text != null) && (activity.Text.StartsWith("-new"))))
            {
                sessionData = new BotFrameworkSession();
                request = CreateNewChat(activity, partner, context, channel, userId, dummyMode);

                if (request == null)
                    return activity.CreateReply("Unsupported partner", "en");
            }
            else
            {
                sessionData = JsonConvert.DeserializeObject<BotFrameworkSession>(sessionRawData);

                request = new ChatRequest()
                {
                    ChatId = sessionData.ChatId,
                    UserInput = activity.Text,
                    Channel = channel,
                    UserName = activity.From.Name,
                    Partner = sessionData.Partner,
                    Context = sessionData.Context,
                    DebugData = sessionData.DebugData,
                    ClientData = new ChatClientData { ClientName = "Bot Connector", ClientVersion = "0.0.0.", ClientIp = "0.0.0.0" }
                };
            }

            ChatResponse chatResponse = await chatEngine.HandleRequest(request);

            SaveSession(activity, sessionData, request, chatResponse);

            return CreateReply(activity, chatResponse);
        }

        private void SaveSession(Activity activity, BotFrameworkSession sessionData, ChatRequest request, ChatResponse chatResponse)
        {
            sessionData.ChatId = chatResponse.ChatId;
            sessionData.Partner = request.Partner;
            sessionData.Context = request.Context;
            sessionData.DebugData = request.DebugData;

            memCacheService.SaveObject(activity.Conversation.Id, JsonConvert.SerializeObject(sessionData), SessionTimeout);
        }

        private Activity CreateReply(Activity activity, ChatResponse chatResponse)
        {
            string replyText;
            HeroCard heroCard = null;
            if (!String.IsNullOrEmpty(chatResponse.Status.Error) && IsLocalTesting(activity))
            {
                replyText = chatResponse.Status.Error;
            }
            else
            {
                //string replyText = String.Join(" ", chatResponse.UiMessages);

                StringBuilder replyTextBuilder = new StringBuilder();
                foreach (var uiMessage in chatResponse.UiMessages)
                {
                    if (String.IsNullOrEmpty(uiMessage.UiTextMarkup))
                        replyTextBuilder.Append(uiMessage.TextMessage);
                    else
                    {
                        try
                        {
                            dynamic uiText = JsonConvert.DeserializeObject<dynamic>(uiMessage.UiTextMarkup);
                            if (uiText.type == "bot card")
                                heroCard = new HeroCard((string)uiText.profilePanel.title);
                            else
                                replyTextBuilder.Append(uiMessage.UiTextMarkup ?? uiMessage.TextMessage);
                        }
                        catch (JsonReaderException)
                        {
                            replyTextBuilder.Append(uiMessage.UiTextMarkup ?? uiMessage.TextMessage);
                        }
                    }
                    replyTextBuilder.AppendLine();
                }


                replyText = replyTextBuilder.ToString();
            }

            var reply = activity.CreateReply(replyText, "en");

            if (activity.ChannelId == "facebook")
                ProcessFacebookResponse(chatResponse, reply);

            if (heroCard != null)
                reply.Attachments.Add(heroCard.ToAttachment());
            return reply;
        }

        ChatRequest CreateNewChat(Activity activity, string partner, string context, string channel, string userId, bool dummyMode)
        {
            var request = CreateNewDefaultRequest(activity);
            if (IsLocalTesting(activity))
            {
                request.Partner = partner;
                request.Context = context;
            }

            request.Channel = channel;

            request.DebugData.UserId = userId;
            request.DebugData.DummyMode = dummyMode;

            return request;
        }

        ChatRequest CreateNewDefaultRequest(Activity activity)
        {
            return new ChatRequest()
            {
                UserInput = activity.Text,
                Channel = activity.ChannelId,
                UserName = activity.From.Name,
                Partner = "Partner",
                Context = "Test",
                DebugData = new ChatDebugData { },
                ClientData = new ChatClientData { ClientName = "Bot Connector", ClientVersion = "0.0.0.", ClientIp = "0.0.0.0" }
            };
        }

        private void ProcessFacebookResponse(ChatResponse response, Activity reply)
        {
            if (response?.PossibleUserAnswers != null)
                reply.ChannelData = MakeFakeFacebookButtonsData(reply.Text, response.PossibleUserAnswers);
        }

        bool IsLocalTesting(Activity activity)
        {
            Uri uri = new Uri(activity.ServiceUrl);
            return uri.IsLoopback;
        }

        dynamic MakeFakeFacebookButtonsData(string text, string[] options)
        {
            dynamic data = new ExpandoObject();
            data.attachment = new ExpandoObject();
            data.attachment.type = "template";
            data.attachment.payload = new ExpandoObject();
            data.attachment.payload.template_type = "button";
            data.attachment.payload.text = text;

            data.attachment.payload.buttons = new ExpandoObject[options.Length];
            for (int i = 0; i < options.Length; i++)
            {
                data.attachment.payload.buttons[i] = new ExpandoObject();
                data.attachment.payload.buttons[i].type = "postback";
                data.attachment.payload.buttons[i].title = options[i];
                data.attachment.payload.buttons[i].payload = options[i];
            }

            return data;
        }

        private Activity HandleSystemMessage(Activity message)
        {

            if (message.Type == "Ping")
            {
                Activity reply = message.CreateReply();
                reply.Type = "Ping";
                return reply;
            }
            else if (message.Type == "DeleteUserData")
            {
            }
            else if (message.Type == "BotAddedToConversation")
            {
            }
            else if (message.Type == "BotRemovedFromConversation")
            {
            }
            else if (message.Type == "UserAddedToConversation")
            {
            }
            else if (message.Type == "UserRemovedFromConversation")
            {
            }
            else if (message.Type == "EndOfConversation")
            {
            }

            return null;
        }
    }
}