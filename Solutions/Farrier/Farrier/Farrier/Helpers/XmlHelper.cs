using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Farrier.Helpers
{
    static class XmlHelper
    {
        public static string XmlAttributeToString(XmlAttribute attr)
        {
            if (attr == null)
                return String.Empty;
            return attr.Value;
        }

        public static bool XmlAttributeToBool(XmlAttribute attr)
        {
            if (attr == null)
                return false;
            return attr.Value.ToLower() == "true";
        }

        public static string CleanXMLFormatting(string innerText, int leadingSpacesToTrim, bool skipFormattingFix = true)
        {
            if (!skipFormattingFix)
            {
                StringBuilder sb = new StringBuilder();
                var lines = innerText.Split(Environment.NewLine);

                string trimSequence = new String(' ', leadingSpacesToTrim);

                //remove 1st and last lines if empty
                if (!String.IsNullOrWhiteSpace(lines[0]))
                    sb.AppendLine(lines[0]);
                for (int i = 1; i < lines.Length - 1; i++)
                {
                    if (lines[i].StartsWith(trimSequence))
                        lines[i] = lines[i].Substring(leadingSpacesToTrim);
                    sb.AppendLine(lines[i]);
                }
                if (lines.Length > 1 && !String.IsNullOrWhiteSpace(lines[lines.Length - 1]))
                    sb.AppendLine(lines[lines.Length - 1]);

                // if an element exists, it will at least generate a new line
                string content = sb.ToString();
                if (content.Length == 0)
                    return Environment.NewLine;
                else
                    return content;
            }
            return innerText;
        }

        public static List<string> ValidateSchema(string xmlLocation, string schemaNamespace, string schemaLocation)
        {
            var messages = new List<string>();
            var settings = new XmlReaderSettings();
            settings.ValidationType = ValidationType.Schema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessInlineSchema;
            settings.ValidationFlags |= XmlSchemaValidationFlags.ProcessSchemaLocation;
            //settings.ValidationFlags |= XmlSchemaValidationFlags.ReportValidationWarnings;
            settings.Schemas.Add(schemaNamespace, schemaLocation);
            settings.ValidationEventHandler += (o, args) =>
            {
                messages.Add($"{args.Message} (Line: {args.Exception.LineNumber}, Position: {args.Exception.LinePosition})");
            };

            XmlReader reader = XmlReader.Create(xmlLocation, settings);
            while (reader.Read()) ;
            return messages;
        }

        public static string djb2(string text)
        {
            int r = 5381;
            foreach(char c in text)
            {
                r = (r * 33) + (int)c;
            }
            return r.ToString();
        }
    }
}
