using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Farrier.Helpers;
using Farrier.Parser;

namespace Farrier.Models
{
    class Suppression
    {
        private Dictionary<string, string> _propertyMap;
        private string _rawMessage;
        private string _rawMust;

        public Suppression(XmlNode suppressNode)
        {
            _propertyMap = new Dictionary<string, string>();

            ConditionName = XmlHelper.XmlAttributeToString(suppressNode.Attributes["conditionname"]);
            _rawMessage = XmlHelper.XmlAttributeToString(suppressNode.Attributes["message"]);
            _rawMust = XmlHelper.XmlAttributeToString(suppressNode.Attributes["must"]);

            // Preapproved property values that may or may not be used by conditions (only added if they are specified)
            addToMap(suppressNode, "path");
        }

        private void addToMap(XmlNode suppressNode, string key)
        {
            var value = XmlHelper.XmlAttributeToString(suppressNode.Attributes[$"{key}endswith"]);
            if (!string.IsNullOrEmpty(value))
            {
                if (key == "path")
                {
                    _propertyMap.Add(key, PathNormalizer.Normalize(value));
                }
                else
                {
                    _propertyMap.Add(key, value);
                }
            }
        }

        public bool IsSuppressed(Dictionary<string, string> mappedProperties, TokenManager tokens, string message = "")
        {
            // If the message is different, it's not suppressed
            string _message = tokens.DecodeString(_rawMessage);
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

            string decodedMust = String.IsNullOrEmpty(_rawMust) ? "true" : tokens.DecodeString(_rawMust);
            if (decodedMust != "true" && decodedMust != "false")
            {
                throw new Exception($"Suppression must attribute must evaluate to 'true' or 'false' but got '{decodedMust}'");
            }
            bool must = decodedMust == "true";
            if (!must)
                return false;

            return true;
        }

        public string ConditionName { get; }
    }
}
