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
using System.Threading.Tasks;
using System.Web;
using ChatWeb.Models;
using ChatWeb.Helpers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Cors;

namespace ChatWeb.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        static readonly log4net.ILog logger = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        readonly ChatConfiguration chatConfiguration;

        public ChatController(ChatConfiguration chatConfiguration)
        {
            this.chatConfiguration = chatConfiguration;
        }

        // POST: api/Chat/
        /// <summary>
        /// Processes chat input and provides a response to the text message.
        /// </summary>
        /// <param name="chatRequest">Input text from user.  Set ChatId to null or empty string for initial request 
        /// for each conversation.  The Server will provide a ChatId in the response which should be used here for 
        /// future requests in the same conversation.</param>
        /// <param name="partner">Partner</param>
        /// <returns>Conversation object with details for response.</returns>
        [HttpPost("{partner?}")]
        [EnableCors("AllowAny")]
        public async Task<ActionResult<ChatResponse>> Post([FromServices]ChatWeb.Services.ChatEngine chatEngine, [FromBody]ChatRequest chatRequest, string partner = null)
        {
            // NOTE: We are requiring the partner in the URL so an ALB can easily parse the partner for proper routing through proxies.
            if (chatRequest == null)
            {
                logger.Debug("Chat request is null");
                return BadRequest();
            }

            if (chatRequest.ClientData == null)
                chatRequest.ClientData = new ChatClientData();

            chatRequest.ClientData.ClientIp = NetworkHelpers.GetClientIP(HttpContext);
            chatRequest.ClientData.UserAgent = HttpContext.Request.Headers["User-Agent"];

            // Ignore debug data if not allowed
            if (!chatConfiguration.AllowDebugMode)
                chatRequest.DebugData = null;

            bool trustedClient = (!String.IsNullOrEmpty(chatConfiguration.TrustedClientKey) && (HttpContext.Request.Headers["x-api-key"] == chatConfiguration.TrustedClientKey));

            if ((chatRequest.TrustedClientData != null) && !trustedClient)
            {
                logger.WarnFormat("TrustedClientData was present but stripped since the API key did not match.");
                chatRequest.TrustedClientData = null;
            }

            log4net.LogicalThreadContext.Properties["interactionId"] = Guid.NewGuid().ToString();
            log4net.LogicalThreadContext.Properties["clientIp"] = chatRequest.ClientData.ClientIp;

            if (String.IsNullOrEmpty(partner) || partner != chatRequest.Partner)
            {
                logger.Warn("Request does not contain the partner in URL parameter");
                //return BadRequest("partner is required in URL");
            }

            try
            {
                ChatResponse response = null;

                // Make initial call to get chat Id if missing and userinput was sent
                if (String.IsNullOrEmpty(chatRequest.ChatId) && !String.IsNullOrEmpty(chatRequest.UserInput))
                {
                    response = await chatEngine.HandleRequest(chatRequest);
                    chatRequest.ChatId = response.ChatId;
                }

                if (response == null || String.IsNullOrEmpty(response.Status.Error))
                    response = await chatEngine.HandleRequest(chatRequest);

                if (!trustedClient)
                    response.TrustedClientData = null;

                return Ok(response);
            }
            catch (Exception ex)
            {
                logger.Error("Post failed.", ex);
            }

            return BadRequest();
        }
    }
}
