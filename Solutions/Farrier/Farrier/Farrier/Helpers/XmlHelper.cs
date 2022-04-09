using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

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
                if (!String.IsNullOrWhiteSpace(lines[lines.Length - 1]))
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
    }
}
