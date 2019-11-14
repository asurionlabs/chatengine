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
using System.Linq;
using System.Web;
using System.Configuration;

namespace ChatWeb.Models
{
    public class ClassifierConfigSection : ConfigurationElement
    {
        private const string PropertyId = "id";
        private const string PropertyUrl = "url";
        private const string PropertyKey = "key";
        private const string PropertyMethod = "method";

        [ConfigurationProperty(PropertyId, IsKey = true, IsRequired = true)]
        public string Id
        {
            get
            {
                return ((string)(base[PropertyId]));
            }
            set
            {
                base[PropertyId] = value;
            }
        }

        [ConfigurationProperty(PropertyUrl, IsKey = false, IsRequired = true)]
        public string Url
        {
            get
            {
                return ((string)(base[PropertyUrl]));
            }
            set
            {
                base[PropertyUrl] = value;
            }
        }

        [ConfigurationProperty(PropertyKey, IsKey = false, IsRequired = true)]
        public string Key
        {
            get
            {
                return ((string)(base[PropertyKey]));
            }
            set
            {
                base[PropertyKey] = value;
            }
        }


        [ConfigurationProperty(PropertyMethod, IsKey = false, IsRequired = false)]
        public string Method
        {
            get
            {
                return ((string)(base[PropertyMethod]));
            }
            set
            {
                base[PropertyMethod] = value;
            }
        }
    }

    //-----------------------------------------------------------------------

    //-----------------------------------------------------------------------

    [ConfigurationCollection(typeof(ClassifierConfigSection))]
    public class ClassifierConfigSectionCollection : ConfigurationElementCollection
    {

        protected override ConfigurationElement CreateNewElement()
        {
            return new ClassifierConfigSection();
        }

        protected override object GetElementKey(ConfigurationElement element)
        {
            return ((ClassifierConfigSection)(element)).Id;
        }


        public ClassifierConfigSection this[int idx]
        {
            get
            {
                return (ClassifierConfigSection)BaseGet(idx);
            }
        }

        new public ClassifierConfigSection this[string key]
        {
            get
            {
                return (ClassifierConfigSection)BaseGet(key);
            }
        }
    }

    //-----------------------------------------------------------------------

    //-----------------------------------------------------------------------

    public class ClassifierConfigSectionMappingConfigSection : ConfigurationSection
    {
        private const string TextClassifierListName = "TextClassifierList";
        private const string TextClassifierSectionName = "TextClassifier";

        [ConfigurationProperty(TextClassifierListName)]
        public ClassifierConfigSectionCollection TextClassifierList
        {
            get { return ((ClassifierConfigSectionCollection)(base[TextClassifierListName])); }
        }

        public static ClassifierConfigSectionCollection GetClassifierConfigs(string fileName)
        {
            var map = new ExeConfigurationFileMap(fileName);
            map.ExeConfigFilename = map.MachineConfigFilename;

            var config = ConfigurationManager.OpenMappedExeConfiguration(map, ConfigurationUserLevel.None);
            //var config = WebConfigurationManager.OpenWebConfiguration(fileName);
            if (config == null)
                return null;

            var t = config.GetSection(TextClassifierSectionName);
            var mappingSection = config.GetSection(TextClassifierSectionName) as ClassifierConfigSectionMappingConfigSection;
            if (mappingSection == null)
                return null;

            return mappingSection.TextClassifierList;
        }
    }
}