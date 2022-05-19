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

        private FunctionResolver _functionResolver;
        private TokenManager _rootTokens;

        private string _blueprint;
        private string _outputpath;
        private bool _listTokens;
        private bool _skipXMLFormattingFix;
        private bool _skipXMLValidation;

        private Dictionary<string, string> _templates;

        private XmlNamespaceManager nsmgr;

        public Forger(string blueprint, string outputpath = "", Dictionary<string, string> tokens = null, bool ListTokens = false, bool SkipXMLFormattingFix = false, bool SkipXMLValidation = false, LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            _templates = new Dictionary<string, string>();

            _blueprint = blueprint;
            _outputpath = outputpath;
            _listTokens = ListTokens;
            _skipXMLFormattingFix = SkipXMLFormattingFix;
            _skipXMLValidation = SkipXMLValidation;

            _functionResolver = new FunctionResolver(log:_log);
            _rootTokens = newTokenManager();

            // Add any tokens passed in the options
            _rootTokens.AddTokens(tokens);
        }

        public void Forge()
        {
            try
            {
                var doc = new XmlDocument();
                try
                {
                    doc.Load(_blueprint);
                    nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("f", "https://pnp.github.io/forge");
                    if (!_skipXMLValidation)
                    {
                        var validationMessages = XmlHelper.ValidateSchema(_blueprint, "https://pnp.github.io/forge", @"XML\Forge.xsd");
                        if (validationMessages.Count > 0)
                        {
                            _log.Error("Invalid Forger Blueprint XML:");
                            foreach (var message in validationMessages)
                            {
                                _log.Error($"  {message}");
                            }
                            _log.Warn("If you are convinced the validation errors above will not affect the actual forge, run again with --skipxmlvalidation to ignore these messages");
                            return;
                        }
                    }
                    else
                    {
                        _log.Warn("Skipping XML Validation of Forge Blueprint file (Proceed at your own risk)");
                    }
                }
                catch (XmlException ex)
                {
                    _log.Error($"Unable to read {_blueprint}, likely bad XML. Details: {ex.Message}");
                    return;
                }


                _rootTokens.AddTokens(doc.SelectSingleNode("//f:tokens", nsmgr));
                if (_listTokens)
                    _rootTokens.LogTokens();

                LoadTemplates(doc.SelectSingleNode("//f:templates", nsmgr));

                XmlNodeList fileNodes = doc.SelectNodes("//f:file", nsmgr);
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
                _log.Error(ex, $"Error Forging Files using blueprint: \"{_blueprint}\"");
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
                        string templateName = XmlHelper.XmlAttributeToString(templateNode.Attributes["name"]);
                        string template = XmlHelper.CleanXMLFormatting(templateNode.InnerText, 6, _skipXMLFormattingFix);

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
                string rawPath = XmlHelper.XmlAttributeToString(fileNode.Attributes["path"]);
                string outputpath = Path.Combine(_outputpath, _rootTokens.DecodeString(rawPath));
                string outputfilename = Path.GetFileName(outputpath);
                string outputfilenamewithoutextension = Path.GetFileNameWithoutExtension(outputfilename);

                var fileTokens = newTokenManager();

                fileTokens.AddToken("OutputPath", outputpath);
                fileTokens.AddToken("OutputFilename", outputfilename);
                fileTokens.AddToken("OutputFilenameNoExtension", outputfilenamewithoutextension);

                _log.Info($"Processing file: {outputpath}...");
                if (_listTokens)
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

                if (!_skipXMLFormattingFix)
                {
                    //remove final newline
                    finalContent = finalContent.TrimEnd();
                }

                File.WriteAllText(outputpath, finalContent);
            }
        }

        private string BuildSection(XmlNode sectionNode, TokenManager fileTokens)
        {
            try
            {
                if (sectionNode != null)
                {
                    string sectionTemplate = GetContentTemplate(sectionNode, 8, fileTokens);
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

                    string rawCsv = XmlHelper.XmlAttributeToString(loopNode.Attributes["csv"]);
                    string csvPath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(_blueprint), _rootTokens.DecodeString(rawCsv, false, fileTokens)));
                    if (!File.Exists(csvPath))
                        throw new Exception("Unable to find csv file at " + csvPath);

                    var rawOrderBy = XmlHelper.XmlAttributeToString(loopNode.Attributes["orderBy"]);
                    var rawOrderDesc = XmlHelper.XmlAttributeToString(loopNode.Attributes["orderDesc"]);
                    var rawGroupBy = XmlHelper.XmlAttributeToString(loopNode.Attributes["groupBy"]);
                    bool groupOrderBySize = XmlHelper.XmlAttributeToString(loopNode.Attributes["groupOrder"]) == "size";
                    var rawGroupDesc = XmlHelper.XmlAttributeToString(loopNode.Attributes["groupDesc"]);
                    var rawFilter = XmlHelper.XmlAttributeToString(loopNode.Attributes["filter"]);
                    var rawGroupSeparator = XmlHelper.XmlAttributeToString(loopNode.Attributes["groupSeparator"]);
                    var rawgroupValues = XmlHelper.XmlAttributeToString(loopNode.Attributes["groupValues"]);
                    var rawgroupValuesSeparator = XmlHelper.XmlAttributeToString(loopNode.Attributes["groupValuesSeparator"]);
                    var rawgroupValuesRestrict = XmlHelper.XmlAttributeToString(loopNode.Attributes["groupValuesRestrict"]);
                    var rawgroupValuesIncludeMissing = XmlHelper.XmlAttributeToString(loopNode.Attributes["groupValuesIncludeMissing"]);

                    var orderBy = _rootTokens.DecodeString(rawOrderBy, false, fileTokens);
                    var orderDesc = _rootTokens.DecodeString(rawOrderDesc, false, fileTokens) == "true";
                    var groupBy = _rootTokens.DecodeString(rawGroupBy, false, fileTokens);
                    var groupDesc = _rootTokens.DecodeString(rawGroupDesc, false, fileTokens) == "true";
                    var filter = _rootTokens.DecodeString(rawFilter, false, fileTokens);
                    var groupSeparator = _rootTokens.DecodeString(rawGroupSeparator, false, fileTokens);
                    var groupValues = _rootTokens.DecodeString(rawgroupValues, false, fileTokens);
                    var groupValuesSeparator = _rootTokens.DecodeString(rawgroupValuesSeparator, false, fileTokens);
                    if (String.IsNullOrEmpty(groupValuesSeparator))
                        groupValuesSeparator = ",";
                    var groupValuesRestrict = _rootTokens.DecodeString(rawgroupValuesRestrict, false, fileTokens) == "true";
                    var groupValuesIncludeMissing = _rootTokens.DecodeString(rawgroupValuesIncludeMissing, false, fileTokens) == "true";


                    LoopData loopData = new LoopData(csvPath, orderBy, orderDesc, groupBy, groupOrderBySize, groupDesc, filter, groupSeparator, groupValues, groupValuesSeparator, groupValuesRestrict, groupValuesIncludeMissing, log: _log);

                    XmlNode itemNode = loopNode.SelectSingleNode("f:item", nsmgr);
                    if (itemNode == null)
                        throw new Exception("No item element found in loop!");

                    var loopTokens = newTokenManager();
                    loopTokens.AddToken("TotalItems", loopData.Data.Rows.Count.ToString());
                    loopTokens.AddToken("TotalGroups", loopData.IsGrouped ? loopData.GroupedData.Count.ToString() : "0");

                    if (!loopData.IsGrouped)
                    {
                        string itemTemplate = GetContentTemplate(itemNode, 10, loopTokens);
                        var rowTokens = TokenManager.TokenizeRows(loopData.Data, _functionResolver,log: _log);
                        foreach (var rowT in rowTokens)
                        {
                            loopContent.Append(_rootTokens.DecodeString(itemTemplate, false, fileTokens, loopTokens, rowT));
                        }
                    }
                    else
                    {
                        XmlNode groupStartNode = loopNode.SelectSingleNode("f:groupStart", nsmgr);
                        XmlNode groupEndNode = loopNode.SelectSingleNode("f:groupEnd", nsmgr);
                        XmlNode emptyGroupNode = loopNode.SelectSingleNode("f:emptyGroup", nsmgr);

                        int groupIndex = 0;
                        foreach (var group in loopData.GroupedData)
                        {
                            var groupTokens = newTokenManager();
                            groupTokens.AddToken("GroupSize", group.Count.ToString());
                            var groupValue = group[0][groupBy].ToString();
                            var isMissing = groupValue.EndsWith(" [MISSING!]");
                            if (isMissing)
                                groupValue = groupValue.Replace(" [MISSING!]","");
                            groupTokens.AddToken("GroupValue", groupValue);
                            groupTokens.AddToken("GroupIndex", groupIndex.ToString());
                            groupIndex += 1;

                            if(!isMissing)
                            {
                                string itemTemplate = GetContentTemplate(itemNode, 10, fileTokens, loopTokens, groupTokens);
                                string groupStartTemplate = GetContentTemplate(groupStartNode, 10, fileTokens, loopTokens, groupTokens);
                                string groupEndTemplate = GetContentTemplate(groupEndNode, 10, fileTokens, loopTokens, groupTokens);

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
                            else
                            {
                                string emptyGroupTemplate = GetContentTemplate(emptyGroupNode, 10, fileTokens, loopTokens, groupTokens);
                                if(emptyGroupNode != null)
                                {
                                    loopContent.Append(_rootTokens.DecodeString(emptyGroupTemplate, false, fileTokens, loopTokens, groupTokens));
                                }
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
                string templateNames = XmlHelper.XmlAttributeToString(contentNode.Attributes["template"]);
                if (!String.IsNullOrEmpty(templateNames))
                {
                    templateNames = _rootTokens.DecodeString(templateNames, false, additionalTokens);
                    var templateContent = new StringBuilder();
                    foreach (var templateName in templateNames.Split(','))
                    {
                        if (_templates.ContainsKey(templateName))
                            templateContent.Append(_templates[templateName]);
                        else
                            throw new Exception("Template \"" + templateName + "\" not found!");
                    }
                    return templateContent.ToString();
                }
                else
                {
                    return XmlHelper.CleanXMLFormatting(contentNode.InnerText, leadingSpacesToTrim, _skipXMLFormattingFix);
                }
            }
            return "";
        }
    }
}
