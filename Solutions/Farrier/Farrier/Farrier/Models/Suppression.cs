using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Farrier.Helpers;

namespace Farrier.Models
{
    class Suppression
    {
        private Dictionary<string, string> _propertyMap;
        private string _message;
        public Suppression(XmlNode suppressNode)
        {
            _propertyMap = new Dictionary<string, string>();

            ConditionName = XmlHelper.XmlAttributeToString(suppressNode.Attributes["conditionname"]);
            _message = XmlHelper.XmlAttributeToString(suppressNode.Attributes["message"]);

            // Preapprove values that may or may not be used by conditions (up to them)
            addToMap(suppressNode, "path");
        }

        private void addToMap(XmlNode suppressNode, string key)
        {
            var value = XmlHelper.XmlAttributeToString(suppressNode.Attributes[$"{key}endswith"]);
            if (!string.IsNullOrEmpty(value))
            {
                _propertyMap.Add(key, value);
            }
        }

        public bool IsSuppressed(Dictionary<string, string> mappedProperties, string message = "")
        {
            // If the message is different, it's not suppressed
            if (!String.IsNullOrEmpty(_message) && _message != message)
                return false;
            
            foreach (var key in _propertyMap.Keys)
            {
                // If they key is in the suppression, but not in the mapped properties, it's not suppressed (doesn't match)
                if (!mappedProperties.ContainsKey(key))
                    return false;

                if (!mappedProperties[key].EndsWith(_propertyMap[key], StringComparison.CurrentCultureIgnoreCase))
                    return false;
            }

            return true;
        }

        public string ConditionName { get; }
    }
}
