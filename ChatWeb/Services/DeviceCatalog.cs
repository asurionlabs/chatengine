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

using System.Collections.Generic;
using System.Linq;
using ChatWeb.Models;
using System.IO;

namespace ChatWeb.Services
{
    public class DeviceCatalog
    {
        public List<DeviceModel> MakeModelList = new List<DeviceModel>();
        public char[][] MatchCharacters;

        public void LoadDeviceListFromPath(string modelPath)
        {
            LoadDeviceList(Path.Combine(modelPath, "make_model_common.csv"), false);
            LoadDeviceList(Path.Combine(modelPath, "make_model_uncommon.csv"), true);

            // Remove duplicates
            MakeModelList = MakeModelList.Distinct().ToList();
            MatchCharacters = MakeModelList.Select(c => { return c.MakeModelChars; }).ToArray();
        }

        void LoadDeviceList(string path, bool uncommon)
        {
            foreach (var line in File.ReadLines(path))
            {
                // Skip header
                if (line.StartsWith("Model_Code"))
                    continue;

                var fields = line.Split(',');
                if (fields.Length != 4)
                    continue;

                // Add match with full make model
                var device = new DeviceModel(fields[0], fields[1], fields[2], $"{fields[1]} {fields[2]}".ToLower())
                {
                    DisplayName = fields[3],
                    IsUncommon = uncommon
                };
                MakeModelList.Add(device);

                // Add match with just model
                device = new DeviceModel(fields[0], fields[1], fields[2], fields[2].ToLower())
                {
                    DisplayName = fields[3],
                    IsUncommon = uncommon
                };
                MakeModelList.Add(device);
            }

        }
    }
}
