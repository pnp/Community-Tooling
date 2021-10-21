﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Json.Path;

namespace Farrier.Models
{
    class MappedColumn
    {
        public MappedColumn() { }

        public MappedColumn(XmlNode columnNode)
        {
            if (columnNode != null)
            {
                var xmlHelper = new XmlHelper();
                Name = xmlHelper.XmlAttributeToString(columnNode.Attributes["name"]);
                Path = xmlHelper.XmlAttributeToString(columnNode.Attributes["path"]);
                Transform = xmlHelper.XmlAttributeToString(columnNode.Attributes["transform"]);
            }
        }

        public static List<MappedColumn> FromXmlNodeList(XmlNodeList columnNodes)
        {
            var mappedColumns = new List<MappedColumn>();

            foreach (XmlNode columnNode in columnNodes)
            {
                mappedColumns.Add(new MappedColumn(columnNode));
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
