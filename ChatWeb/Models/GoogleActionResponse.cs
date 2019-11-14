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
using System.Collections.Generic;

namespace ChatWeb.Models
{
    public sealed class GoogleActionInputPermission
    {
        public const string Name = "NAME";
        public const string DevicePreciseLocation = "DEVICE_PRECISE_LOCATION";
        public const string DeviceCoarseLocation = "DEVICE_COARSE_LOCATION";
    }

    public class GoogleActionResponse
    {
        /// <summary>
        /// An opaque token that is recirculated to the app every conversation turn.
        /// </summary>
        public string ConversationToken { get; set; }
        /// <summary>
        /// Indicates whether the app is expecting a user response. This is true when the conversation is ongoing, false when the conversation is done.
        /// </summary>
        public bool ExpectUserResponse { get; set; }
        /// <summary>
        /// List of inputs the app expects, each input can be a built-in intent, or an input taking list of possible intents. Only one input is supported for now.
        /// </summary>
        public GoogleActionExpectedInput[] ExpectedInputs { get; set; }
        /// <summary>
        /// Final response when the app does not expect user's input.
        /// </summary>
        public GoogleActionFinalResponse FinalResponse { get; set; }

        /// <summary>
        /// Custom Push Message allows developers to send structured data to Google for interactions on the Assistant.
        /// </summary>
        public GoogleActionCustomPushMessage CustomPushMessage { get; set; }

        /// <summary>
        /// Metadata passed from bot builder platforms to Google. This metadata can contain error and logging information that bot building platforms want to expose to the app developer.
        /// </summary>
        public GoogleActionResponseMetadata ResponseMetadata { get; set; }

        /// <summary>
        /// Indicates whether the response should be handled in sandbox mode. This bit is needed to push structured data to Google in sandbox mode.
        /// </summary>
        public bool IsInSandbox { get; set; }
    }

    public class GoogleActionExpectedInput
    {
        /// <summary>
        /// The customized prompt used to ask user for input.
        /// </summary>
        public GoogleActionInputPrompt InputPrompt { get; set; }
        /// <summary>
        /// List of intents that can be used to fulfill this input. To have the Google Assistant just return the raw user input, the app should ask for the actions.intent.TEXT intent.
        /// </summary>
        public GoogleActionExpectedIntent[] PossibleIntents { get; set; }

        /// <summary>
        /// List of phrases the app wants Google to use for speech biasing. Up to 1000 phrases are allowed.
        /// </summary>
        public string[] SpeechBiasingHints { get; set; }
    }

    /// <summary>
    /// The input prompt used for assistant to guide user to provide an input for the app's question.
    /// </summary>
    public class GoogleActionInputPrompt
    {
        /// <summary>
        /// Prompt payload.
        /// </summary>
        public GoogleActionRichResponse RichInitialPrompt { get; set; }
        /// <summary>
        /// Prompt used to ask user when there is no input from user.
        /// </summary>
        public GoogleActionSimpleResponse[] NoInputPrompts { get; set; }
    }

    /// <summary>
    /// A rich response that can include audio, text, cards, suggestions and structured data.
    /// </summary>
    public class GoogleActionRichResponse
    {
        /// <summary>
        /// A list of UI elements which compose the response The items must meet the following requirements:
        /// 1. The first item must be a google.actions.v2.SimpleResponse 
        /// 2. At most two google.actions.v2.SimpleResponse 
        /// 3. At most one card (e.g. google.actions.v2.ui_elements.BasicCard or google.actions.v2.StructuredResponse 
        /// 4. Cards may not be used if an actions.intent.OPTION intent is used ie google.actions.v2.ui_elements.ListSelect or google.actions.v2.ui_elements.CarouselSelect
        /// </summary>
        public GoogleActionItem[] Items { get; set; }
        /// <summary>
        /// A list of suggested replies. These will always appear at the end of the response. If used in a FinalResponse, they will be ignored.
        /// </summary>
        public GoogleActionSuggestion[] Suggestions { get; set; }
        /// <summary>
        /// An additional suggestion chip that can link out to the associated app or site.
        /// </summary>
        public GoogleActionLinkOutSuggestion LinkOutSuggestion { get; set; }
    }

    /// <summary>
    /// Items of the response.
    /// </summary>
    public class GoogleActionItem
    {
        // Union field item can be only one of the following:
        /// <summary>
        /// Voice and text-only response.
        /// </summary>
        public GoogleActionSimpleResponse SimpleResponse { get; set; }
        /// <summary>
        /// A basic card.
        /// </summary>
        public GoogleActionBasicCard BasicCard { get; set; }
        /// <summary>
        /// Structured payload to be processed by Google.
        /// </summary>
        public GoogleActionStructuredResponse StructuredResponse { get; set; }
    }

    /// <summary>
    /// The final response when the user input is not expected.
    /// </summary>
    public class GoogleActionFinalResponse
    {
        /// <summary>
        /// Rich response when user is not required to provide an input.
        /// </summary>
        public GoogleActionRichResponse RichResponse { get; set; }
    }

    /// <summary>
    /// A simple response containing speech or text to show the user.
    /// </summary>
    public class GoogleActionSimpleResponse
    {
        /// <summary>
        /// Plain text of the speech output, e.g., "where do you want to go?" Mutually exclusive with ssml.
        /// </summary>
        public string TextToSpeech { get; set; }
        /// <summary>
        /// Structured spoken response to the user in the SSML format, e.g. <speak> Say animal name after the sound. <audio src = 'https://www.pullstring.com/moo.mps' />, what’s the animal? </speak>. Mutually exclusive with textToSpeech.
        /// </summary>
        public string Ssml { get; set; }
        /// <summary>
        /// Optional text to display in the chat bubble. If not given, a display rendering of the textToSpeech or ssml above will be used. Limited to 640 chars.
        /// </summary>
        public string DisplayText { get; set; }
    }

    /// <summary>
    /// A basic card for displaying some information, e.g. an image and/or text.
    /// </summary>
    public class GoogleActionBasicCard
    {
        /// <summary>
        /// Overall title of the card. Optional.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Optional.
        /// </summary>
        public string Subtitle { get; set; }
        /// <summary>
        /// Body text of the card. Supports a limited set of markdown syntax for formatting. Required, unless image is present.
        /// </summary>
        public string FormattedText { get; set; }
        /// <summary>
        /// A hero image for the card. The height is fixed to 192dp. Optional.
        /// </summary>
        public GoogleActionImage Image { get; set; }
        /// <summary>
        /// Buttons. Currently at most 1 button is supported. Optional.
        /// </summary>
        public GoogleActionButton[] Buttons { get; set; }
    }

    /// <summary>
    /// An image displayed in the card.
    /// </summary>
    public class GoogleActionImage
    {
        /// <summary>
        /// The source url of the image. Images can be JPG, PNG and GIF (animated and non-animated). For example, https://www.agentx.com/logo.png. Required.
        /// </summary>
        public string Url { get; set; }
        /// <summary>
        /// A text description of the image to be used for accessibility, e.g. screen readers. Required.
        /// </summary>
        public string AccessibilityText { get; set; }
        /// <summary>
        /// The height of the image in pixels. Optional.
        /// </summary>
        public int Height { get; set; }
        /// <summary>
        /// The width of the image in pixels. Optional.
        /// </summary>
        public int Width { get; set; }
    }

    /// <summary>
    /// A button object that usually appears at the bottom of a card.
    /// </summary>
    public class GoogleActionButton
    {
        /// <summary>
        /// Title of the button. Required.
        /// </summary>
        public string Title { get; set; }
        /// <summary>
        /// Action to take when a user taps on the button. Required.
        /// </summary>
        public GoogleActionTimeOfDay OpenUrlAction { get; set; }
    }

    /// <summary>
    /// Opens the given url.
    /// </summary>
    public class GoogleActionOpenUrlAction
    {
        /// <summary>
        /// http or https scheme url. Required.
        /// </summary>
        public string Url { get; set; }
    }

    /// <summary>
    /// A suggestion chip that the user can tap to quickly post a reply to the conversation.
    /// </summary>
    public class GoogleActionSuggestion
    {
        /// <summary>
        /// The text shown the in the suggestion chip. When tapped, this text will be posted back to the conversation verbatim as if the user had typed it. Each title must be unique among the set of suggestion chips. Max 25 chars Required
        /// </summary>
        public string Title { get; set; }
    }

    /// <summary>
    /// Creates a suggestion chip that allows the user to jump out to the App or Website associated with this agent.
    /// </summary>
    public class GoogleActionLinkOutSuggestion
    {
        /// <summary>
        /// The name of the app or site this chip is linking to. The chip will be rendered with the title "Open ". Max 20 chars. Required.
        /// </summary>
        public string DestinationName { get; set; }
        /// <summary>
        /// The URL of the App or Site to open when the user taps the suggestion chip. Ownership of this URL must be validated in the Actions on Google developer console, or the suggestion will not be shown to the user.
        /// </summary>
        public string Url { get; set; }
    }

    /// <summary>
    /// The expected intent the app is asking the assistant to provide.
    /// </summary>
    public class GoogleActionExpectedIntent
    {
        /// <summary>
        /// The built-in intent name, e.g. actions.intent.TEXT, or intents defined in the action package. If the intent specified is not a built-in intent, it is only used for speech biasing and the input provided by the Google Assistant will be the actions.intent.TEXT intent.
        /// </summary>
        public string Intent { get; set; }
        /// <summary>
        /// Additional configuration data required by a built-in intent. Possible values for the built-in intents: actions.intent.OPTION -> google.actions.v2.OptionValueSpec, 
        /// actions.intent.CONFIRMATION -> google.actions.v2.ConfirmationValueSpec, actions.intent.TRANSACTION_REQUIREMENTS_CHECK -> google.actions.v2.TransactionRequirementsCheckSpec,
        /// actions.intent.DELIVERY_ADDRESS -> google.actions.v2.DeliveryAddressValueSpec, actions.intent.TRANSACTION_DECISION -> google.actions.v2.TransactionDecisionValueSpec
        /// An object containing fields of an arbitrary type.An additional field "@type" contains a URI identifying the type.
        /// Example: { "id": 1234, "@type": "types.example.com/standard/id" }.
        /// </summary>
        public object InputValueData { get; set; }
    }

    /// <summary>
    /// A custom push message that holds structured data to push for the Actions Fulfillment API.
    /// </summary>
    public class GoogleActionCustomPushMessage
    {
        /// <summary>
        /// An order update updating orders placed through transaction APIs.
        /// </summary>
        public GoogleActionOrderUpdate OrderUpdate { get; set; }
    }

    /// <summary>
    /// The response defined for app to respond with structured data.
    /// </summary>
    public class GoogleActionStructuredResponse
    {
        /// <summary>
        /// App provides an order update (e.g. Receipt) after receiving the order.
        /// </summary>
        public GoogleActionOrderUpdate OrderUpdate { get; set; }
    }

    public class GoogleActionOrderUpdate
    {
        /*
        public GoogleActionOrderState orderState { get; set; }
        public GoogleActionAction[] orderManagementActions { get; set; }
        public string updateTime { get; set; }
        public GoogleActionPrice totalPrice { get; set; }
        public Dictionary<string, GoogleActionLineItemUpdate> lineItemUpdates { get; set; }
        public GoogleActionUserNotification userNotification { get; set; }
        public object infoExtension { get; set; }
        public string googleOrderId { get; set; }
        public string actionOrderId { get; set; }
        public GoogleActionRejectionInfo rejectionInfo { get; set; }
        public GoogleActionReceipt receipt { get; set; }
        public GoogleActionCancellationInfo cancellationInfo { get; set; }
        public GoogleActionInTransitInfo inTransitInfo { get; set; }
        public GoogleActionFulfillmentInfo fulfillmentInfo { get; set; }
        public GoogleActionReturnInfo returnInfo { get; set; }
        */
    }

    public class GoogleActionResponseMetadata
    {
        public GoogleActionStatus Status { get; set; }
    }

    public class GoogleActionStatus
    {
        public int Code { get; set; }
        public string Message { get; set; }
        public object[] Details { get; set; }
    }
}