using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Json.Path;
using Farrier.Parser;

namespace Farrier.Models
{
    class MappedColumn
    {
        public MappedColumn() { }

        public MappedColumn(XmlNode columnNode, TokenManager tokens)
        {
            if (columnNode != null)
            {
                //var xmlHelper = new XmlHelper();
                var rawName = XmlHelper.XmlAttributeToString(columnNode.Attributes["name"]);
                Name = tokens.DecodeString(rawName);
                Path = XmlHelper.XmlAttributeToString(columnNode.Attributes["path"]);
                Transform = XmlHelper.XmlAttributeToString(columnNode.Attributes["transform"]);
            }
        }

        public static List<MappedColumn> FromXmlNodeList(XmlNodeList columnNodes, TokenManager tokens)
        {
            var mappedColumns = new List<MappedColumn>();

            foreach (XmlNode columnNode in columnNodes)
            {
                mappedColumns.Add(new MappedColumn(columnNode, tokens));
            }

            return mappedColumns;
        }

        public string Name { get; set; }

        private string _transform;
        public string Transform
        {
            get
            {
                return _transform;
            }

            set
            {
                if(String.IsNullOrEmpty(value))
                {
                    _transform = "@@currentvalue@@";
                }
                else
                {
                    _transform = value;
                }
            }
        }

        private string _path;
        public string Path
        { 
            get
            {
                return _path;
            }
            set
            {
                _path = value;
                _successfulPathParse = JsonPath.TryParse(_path, out _parsedPath);
            }
        }

        private JsonPath _parsedPath;
        private bool _successfulPathParse;
        public bool ValidPath
        { 
            get
            {
                return _successfulPathParse;
            }
        }

        public JsonPath ParsedPath
        {
            get
            {
                if (_successfulPathParse)
                    return _parsedPath;
                else
                    return null;
            }
        }
    }
}
