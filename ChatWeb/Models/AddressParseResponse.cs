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
using System.Text;

namespace ChatWeb.Models
{
    public class AddressParseResponse
    {
        public ParseAddressDTO parseAddressDTO { get; set; }
    }

    public class ParseAddressDTO
    {
        public AddressPart[] addresses { get; set; }
        public string response { get; set; }
    }

    [Serializable]
    public class AddressPart
    {
        public string address1 { get; set; }
        public string address2 { get; set; }
        public string city { get; set; }
        public string region { get; set; }
        public string postalCode { get; set; }
        public string recordType { get; set; }

        public string formattedAddress
        {
            get
            {
                StringBuilder sbAddress = new StringBuilder();
                sbAddress.Append(address1);
                sbAddress.Append(" ");

                if (String.IsNullOrEmpty(address2))
                    sbAddress.Append(address2);

                sbAddress.Append(city);
                sbAddress.Append(", ");
                sbAddress.Append(region);
                sbAddress.Append(" ");
                sbAddress.Append(postalCode);

                return sbAddress.ToString();
            }
        }
    }

}