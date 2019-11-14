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

namespace ChatWeb.Models
{
    /// <summary>
    /// See https://developers.google.com/actions/reference/conversation
    /// </summary>

    public sealed class GoogleActionConversationType
    {
        public const string Unspecified = "TYPE_UNSPECIFIED";
        public const string New = "NEW";
        public const string Active = "ACTIVE";
        public const string Expired = "EXPIRED";
        public const string Archived = "ARCHIVED";
    }

    public sealed class GoogleActionInputType
    {
        public const string Unspecific = "UNSPECIFIC_INPUT_TYPE";
        public const string Touch = "TOUCH";
        public const string Voice = "VOICE";
    }

    public class GoogleActionRequest
    {
        /// <summary>
        /// Describes the user that initiated this conversation.
        /// </summary>
        public GoogleActionUser User { get; set; }
        /// <summary>
        /// Information associated with the device from which the conversation was initiated.
        /// </summary>
        public GoogleActionDevice Device { get; set; }
        /// <summary>
        /// Information about the surface the user is interacting with, e.g. whether it can output audio or has a screen.
        /// </summary>
        public GoogleActionSurface Surface { get; set; }
        /// <summary>
        /// Holds session data like the conversation ID and token.
        /// </summary>
        public GoogleActionConversation Conversation { get; set; }
        /// <summary>
        /// List of inputs corresponding to developer-required expected input.
        /// The input could be the query semantics for initial query, or assistant-provided response for developer required input.
        /// We ensure 1:1 mapping for all the required inputs from developer. 
        /// Note that currently only one expected input is supported.
        /// </summary>
        public GoogleActionInput[] Inputs { get; set; }
        /// <summary>
        /// Indicates whether the request should be handled in sandbox mode.
        /// </summary>
        public bool IsInSandbox { get; set; }
    }

    /// <summary>
    /// The user object contains information about the user. 
    /// </summary>
    public class GoogleActionUser
    {
        /// <summary>
        /// A random string identifier for the Google user. The user_id can be used to track a user across multiple sessions and devices.
        /// </summary>
        /// <remarks>
        /// Users can reset their user_id at any time. Do not use this field as a key to store valuable information about the user, use account linking instead.
        /// </remarks>
        public string UserId { get; set; }
        /// <summary>
        /// Information about the user.
        /// </summary>
        /// <remarks>
        /// This object will only be available in the request after requesting and being granted user's consent to share.
        /// </remarks>
        public GoogleUserProfile Profile { get; set; }
        /// <summary>
        /// A unique OAuth2 token that identifies the user in your system. Only available if Account Linking configuration is defined in the Action Package and the user links their account.
        /// </summary>
        public string AccessToken { get; set; }

        /// <summary>
        /// Contains permissions granted by user to this app.
        /// </summary>
        public int[] Permissions { get; set; }

        /// <summary>
        /// Primary locale setting of the user making the request. Follows IETF BCP-47 language code http://www.rfc-editor.org/rfc/bcp/bcp47.txt However, the script subtag is not included.
        /// </summary>
        public string Locale { get; set; }
    }

    /// <summary>
    /// Stores user's personal info. It's accessible only after user grants the permission to the agent.
    /// </summary>
    public class GoogleUserProfile
    {
        /// <summary>
        /// The user's first name as specified in their Google account.
        /// </summary>
        /// <remarks>
        /// Requires permission NAME
        /// </remarks>
        public string GivenName { get; set; }
        /// <summary>
        /// The user's last name as specified in their Google account. Note that this field could be empty.
        /// </summary>
        /// <remarks>
        /// Requires permission NAME
        /// </remarks>
        public string FamilyName { get; set; }
        /// <summary>
        /// The user's full name as specified in their Google account.
        /// </summary>
        /// <remarks>
        /// Requires permission NAME
        /// </remarks>
        public string DisplayName { get; set; }
    }

    /// <summary>
    /// The device object contains information about the device through which the conversation is taking place.
    /// </summary>
    public class GoogleActionDevice
    {
        /// <summary>
        /// Representation of the device location.
        /// </summary>
        /// <remarks>
        /// Requires permission DEVICE_PRECISE_LOCATION or DEVICE_COARSE_LOCATION
        /// </remarks>
        public GoogleActionLocation Location { get; set; }
    }

    /// <summary>
    /// Information specific to the Google Assistant client surface the user is interacting with. Surface is distinguished from Device by the fact that multiple Assistant surfaces may live on the same device.
    /// </summary>
    public class GoogleActionSurface
    {
        /// <summary>
        /// A list of capabilities the surface supports at the time of the request e.g. actions.capability.AUDIO_OUTPUT
        /// </summary>
        public GoogleActionCapability[] Capabilities { get; set; }
    }

    /// <summary>
    /// Represents a unit of functionality that the surface is capable of supporting.
    /// </summary>
    public class GoogleActionCapability
    {
        /// <summary>
        /// The name of the capability, e.g. actions.capabililty.AUDIO_OUTPUT
        /// </summary>
        public string Name { get; set; }
    }

    public class GoogleActionLocation
    {
        public GoogleActionCoordinates Coordinates { get; set; }
        /// <summary>
        /// The device's display address.
        /// </summary>
        /// <example>1600 Amphitheatre Pkwy, Mountain View, CA 94043</example>
        public string FormattedAddress { get; set; }
        /// <summary>
        /// The ZIP code in which the device is located.
        /// </summary>
        public string ZipCode { get; set; }
        /// <summary>
        /// The city in which the device is located.
        /// </summary>
        public string City { get; set; }
        public object PostalAddress { get; set; }
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
        public string Notes { get; set; }
    }

    public class GoogleActionCoordinates
    {
        /// <summary>
        /// The device's latitude, in degrees. It must be in the range [-90.0, +90.0].
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// The device's longitude, in degrees. It must be in the range [-180.0, +180.0].
        /// </summary>
        public double Longitude { get; set; }
    }

    /// <summary>
    /// The conversationobject defines session data about the ongoing conversation.
    /// </summary>
    public class GoogleActionConversation
    {
        /// <summary>
        /// Unique ID for the multi-step conversation, it's assigned for the first step, after that it remains the same for subsequent user's queries until the conversation is terminated.
        /// </summary>
        public string ConversationId { get; set; }
        /// <summary>
        /// Type indicates the state of the conversation in its life cycle.
        /// </summary>
        public string Type { get; set; }
        /// <summary>
        /// Opaque token specified by the action endpoint in a previous response; mainly used by the agent to maintain the current conversation state.
        /// </summary>
        public string ConversationToken { get; set; }
    }

    /// <summary>
    /// The inputs object contains useful data about the request. The input could be the query semantics for the initial query, or the assistant-provided response for developer required input.
    /// </summary>
    /// <remarks>
    /// Currently, only one expected input is supported.
    /// </remarks>
    public class GoogleActionInput
    {
        /// <summary>
        /// Indicates the user's intent; will be one of the possible_intents specified in the developer request.
        /// </summary>
        public string Intent { get; set; }
        /// <summary>
        /// Raw input transcription from each turn of conversation in the dialog that resulted from the previous expected input.
        /// </summary>
        public GoogleActionRawInputs[] RawInputs { get; set; }
        /// <summary>
        /// Semantically annotated values extracted from the user's inputs.
        /// </summary>
        public GoogleActionArguments[] Arguments { get; set; }
    }

    public class GoogleActionRawInputs
    {
        public GoogleActionCreateTime CreateTime { get; set; }
        /// <summary>
        /// Indicates the kind of input: a typed query, a voice query, or unspecified.
        /// </summary>
        public string InputType { get; set; }
        /// <summary>
        /// Keyboard input or spoken input from end user.
        /// </summary>
        public string Query { get; set; }
    }

    public class GoogleActionCreateTime
    {
        /// <summary>
        /// Represents seconds of UTC time since Unix epoch 1970-01-01T00:00:00Z. Must be from 0001-01-01T00:00:00Z to 9999-12-31T23:59:59Z inclusive.
        /// </summary>
        public int Seconds { get; set; }
        /// <summary>
        /// Non-negative fractions of a second at nanosecond resolution. Negative second values with fractions must still have non-negative nanos values that count forward in time. Must be from 0 to 999,999,999 inclusive.
        /// </summary>
        public int Nanos { get; set; }
    }

    public class GoogleActionArguments
    {
        /// <summary>
        /// Name of the payload in the query.
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// raw text value for the argument.
        /// </summary>
        public string RawText { get; set; }
        /// <summary>
        /// Specified when the user input had a $SchemaOrg_YesNo argument.
        /// </summary>
        public bool BoolValue { get; set; }
        /// <summary>
        /// Specified when the user input had a $SchemaOrg_Text argument.
        /// </summary>
        public string TextValue { get; set; }
        /// <summary>
        /// Specified when the user input had a $SchemaOrg_Date argument
        /// </summary>
        public GoogleActionDateTime DatetimeValue { get; set; }
        /// <summary>
        /// Extension whose type depends on the argument. For example, if the argument name is SIGN_IN for the actions.intent.SIGN_IN intent, then this extension will contain a SignInValue value.
        ///
        /// An object containing fields of an arbitrary type.An additional field "@type" contains a URI identifying the type.Example: { "id": 1234, "@type": "types.example.com/standard/id" }.
        /// </summary>
        public object Extension { get; set; }
    }

    /// <summary>
    /// Date and time argument value parsed from user input. Does not include time zone information.
    /// </summary>
    public class GoogleActionDateTime
    {
        /// <summary>
        /// Date value
        /// </summary>
        public GoogleActionDate Date { get; set; }
        /// <summary>
        /// Time value
        /// </summary>
        public GoogleActionTimeOfDay Time { get; set; }
    }

    public class GoogleActionDate
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int Day { get; set; }
    }

    public class GoogleActionTimeOfDay
    {
        public int Hours { get; set; }
        public int Minutes { get; set; }
        public int Seconds { get; set; }
        public int Nanos { get; set; }
    }

}