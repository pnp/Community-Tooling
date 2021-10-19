using System;
using System.Collections.Generic;
using System.Text;
using Farrier.Parser;
using Farrier.Helpers;
using System.Xml;
using System.IO;
using Farrier.Models;

namespace Farrier.Forge
{
    class Forger
    {
        private LogRouter _log;

        private ForgeOptions _options;
        private FunctionResolver _functionResolver;
        private TokenManager _rootTokens;
        private XmlHelper _xmlHelper;

        private Dictionary<string, string> _templates;

        public Forger(ForgeOptions options, LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            _templates = new Dictionary<string, string>();

            _options = options;

            _functionResolver = new FunctionResolver(log:_log);
            _rootTokens = newTokenManager();
            _xmlHelper = new XmlHelper(_options.SkipXMLFormattingFix);

            // Add any tokens passed in the options
            _rootTokens.AddTokens(options.Tokens);
        }

        public void Forge()
        {
            try
            {
                var doc = new XmlDocument();
                doc.Load(_options.Blueprint);

                _rootTokens.AddTokens(doc.SelectSingleNode("//tokens"));
                if (_options.ListTokens)
                    _rootTokens.LogTokens();

                LoadTemplates(doc.SelectSingleNode("//templates"));

                XmlNodeList fileNodes = doc.SelectNodes("//file");
                if (fileNodes != null && fileNodes.Count > 0)
                {
                    _log.Info($"Processing {fileNodes.Count} files");

                    foreach (XmlNode fileNode in fileNodes)
                    {
                        BuildFile(fileNode);
                    }
                }
                else
                {
                    _log.Warn("No file entries found in config!");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error Forging Files using blueprint: \"{_options.Blueprint}\"");
            }
        }


        private TokenManager newTokenManager()
        {
            return new TokenManager(_functionResolver, log: _log);
        }
        
        private void LoadTemplates(XmlNode templatesNode)
        {
            if (templatesNode != null && templatesNode.ChildNodes.Count > 0)
            {
                _log.Info($"Loading {templatesNode.ChildNodes.Count} templates from the blueprint");

                foreach (XmlNode templateNode in templatesNode)
                {
                    if (templateNode.NodeType == XmlNodeType.Element)
                    {
                        string templateName = _xmlHelper.XmlAttributeToString(templateNode.Attributes["name"]);
                        string template = _xmlHelper.CleanXMLFormatting(templateNode.InnerText, 6);

                        if (!String.IsNullOrEmpty(templateName))
                        {
                            _templates.Add(_rootTokens.DecodeString(templateName), template);
                        }
                    }
                }
            }
        }

        private void BuildFile(XmlNode fileNode)
        {
            if (fileNode != null)
            {
                XmlAttribute outputAttribute = fileNode.Attributes["output"];
                if (outputAttribute != null && !String.IsNullOrEmpty(outputAttribute.Value))
                {
                    string outputpath = Path.Combine(_options.OutputPath, _rootTokens.DecodeString(outputAttribute.Value));
                    string outputfilename = Path.GetFileName(outputpath);
                    string outputfilenamewithoutextension = Path.GetFileNameWithoutExtension(outputfilename);

                    var fileTokens = newTokenManager();

                    fileTokens.AddToken("OutputPath", outputpath);
                    fileTokens.AddToken("OutputFilename", outputfilename);
                    fileTokens.AddToken("OutputFilenameNoExtension", outputfilenamewithoutextension);

                    _log.Info($"Processing file: {outputpath}...");
                    if (_options.ListTokens)
                        fileTokens.LogTokens();

                    StringBuilder fileContent = new StringBuilder();

                    foreach (XmlNode contentNode in fileNode)
                    {
                        if (contentNode.NodeType == XmlNodeType.Element)
                        {
                            string contentType = contentNode.LocalName;
                            switch (contentType)
                            {
                                case "section":
                                    fileContent.Append(BuildSection(contentNode, fileTokens));
                                    break;
                                case "loop":
                                    fileContent.Append(BuildLoop(contentNode, fileTokens));
                                    break;
                                default:
                                    _log.Warn($"Unknown content element: {contentType}");
                                    break;
                            }
                        }
                    }

                    string finalContent = fileContent.ToString();

                    if (!_options.SkipXMLFormattingFix)
                    {
                        //remove final newline
                        finalContent = finalContent.TrimEnd();
                    }

                    File.WriteAllText(outputpath, finalContent);
                }
                else
                {
                    _log.Warn("No ouput found for file, skipping entry!");
                }
            }
        }

        private string BuildSection(XmlNode sectionNode, TokenManager fileTokens)
        {
            try
            {
                if (sectionNode != null)
                {
                    string sectionTemplate = GetContentTemplate(sectionNode, 6, fileTokens);
                    return _rootTokens.DecodeString(sectionTemplate, false, fileTokens);
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to create section");
            }
            return String.Empty;
        }

        private string BuildLoop(XmlNode loopNode, TokenManager fileTokens)
        {
            try
            {
                if (loopNode != null)
                {
                    StringBuilder loopContent = new StringBuilder();

                    XmlAttribute csvAttribute = loopNode.Attributes["csv"];
                    if (csvAttribute == null || String.IsNullOrEmpty(csvAttribute.Value))
                        throw new Exception("csv attribute is missing from loop!");

                    string csvPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_options.Blueprint), csvAttribute.Value));
                    if (!File.Exists(csvPath))
                        throw new Exception("Unable to find csv file at " + csvPath);

                    string orderBy = _xmlHelper.XmlAttributeToString(loopNode.Attributes["orderBy"]);
                    bool orderDesc = _xmlHelper.XmlAttributeToBool(loopNode.Attributes["orderDesc"]);
                    string groupBy = _xmlHelper.XmlAttributeToString(loopNode.Attributes["groupBy"]);
                    bool groupOrderBySize = _xmlHelper.XmlAttributeToString(loopNode.Attributes["groupOrder"]) == "size";
                    bool groupDesc = _xmlHelper.XmlAttributeToBool(loopNode.Attributes["groupDesc"]);
                    string filter = _xmlHelper.XmlAttributeToString(loopNode.Attributes["filter"]);
                    string groupSeparator = _xmlHelper.XmlAttributeToString(loopNode.Attributes["groupSeparator"]);

                    LoopData loopData = new LoopData(csvPath, orderBy, orderDesc, groupBy, groupOrderBySize, groupDesc, filter, groupSeparator, log: _log);

                    XmlNode itemNode = loopNode.SelectSingleNode("item");
                    if (itemNode == null)
                        throw new Exception("No item element found in loop!");

                    var loopTokens = newTokenManager();
                    loopTokens.AddToken("TotalItems", loopData.Data.Rows.Count.ToString());
                    loopTokens.AddToken("TotalGroups", loopData.IsGrouped ? loopData.GroupedData.Count.ToString() : "0");

                    if (!loopData.IsGrouped)
                    {
                        string itemTemplate = GetContentTemplate(itemNode, 8, loopTokens);
                        var rowTokens = TokenManager.TokenizeRows(loopData.Data, _functionResolver,log: _log);
                        foreach (var rowT in rowTokens)
                        {
                            loopContent.Append(_rootTokens.DecodeString(itemTemplate, false, fileTokens, loopTokens, rowT));
                        }
                    }
                    else
                    {
                        XmlNode groupStartNode = loopNode.SelectSingleNode("groupStart");
                        XmlNode groupEndNode = loopNode.SelectSingleNode("groupEnd");

                        int groupIndex = 0;
                        foreach (var group in loopData.GroupedData)
                        {
                            var groupTokens = newTokenManager();
                            groupTokens.AddToken("GroupSize", group.Count.ToString());
                            groupTokens.AddToken("GroupValue", group[0][groupBy].ToString());
                            groupTokens.AddToken("GroupIndex", groupIndex.ToString());
                            groupIndex += 1;

                            string itemTemplate = GetContentTemplate(itemNode, 8, fileTokens, loopTokens, groupTokens);
                            string groupStartTemplate = GetContentTemplate(groupStartNode, 8, fileTokens, loopTokens, groupTokens);
                            string groupEndTemplate = GetContentTemplate(groupEndNode, 8, fileTokens, loopTokens, groupTokens);

                            if (groupStartNode != null)
                            {
                                loopContent.Append(_rootTokens.DecodeString(groupStartTemplate, false, fileTokens, loopTokens, groupTokens));
                            }

                            var rowTokens = TokenManager.TokenizeRows(group, loopData.Data.Columns, _functionResolver, log: _log);
                            foreach (var rowT in rowTokens)
                            {
                                loopContent.Append(_rootTokens.DecodeString(itemTemplate, false, fileTokens, loopTokens, groupTokens, rowT));
                            }

                            if (groupEndNode != null)
                            {
                                loopContent.Append(_rootTokens.DecodeString(groupEndTemplate, false, fileTokens, loopTokens, groupTokens));
                            }
                        }
                    }

                    return loopContent.ToString();
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Unable to create loop: " + ex.Message);
            }
            return String.Empty;
        }

        private string GetContentTemplate(XmlNode contentNode, int leadingSpacesToTrim, params TokenManager[] additionalTokens)
        {
            if (contentNode != null)
            {
                string templateName = _xmlHelper.XmlAttributeToString(contentNode.Attributes["template"]);
                if (!String.IsNullOrEmpty(templateName))
                {
                    templateName = _rootTokens.DecodeString(templateName, false, additionalTokens);
                    if (_templates.ContainsKey(templateName))
                        return _templates[templateName];
                    else
                        throw new Exception("Template \"" + templateName + "\" not found!");
                }
                else
                {
                    return _xmlHelper.CleanXMLFormatting(contentNode.InnerText, leadingSpacesToTrim);
                }
            }
            return "";
        }
    }
}
