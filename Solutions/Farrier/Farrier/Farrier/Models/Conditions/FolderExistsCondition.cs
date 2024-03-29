﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using Farrier.Helpers;
using Farrier.Parser;
using System.IO;

namespace Farrier.Models.Conditions
{
    class FolderExistsCondition : BaseCondition
    {
        public new static FolderExistsCondition FromNode(XmlNode conditionNode)
        {
            return new FolderExistsCondition(conditionNode);
        }

        public FolderExistsCondition(XmlNode conditionNode) : base(conditionNode)
        {
            rawPath = XmlHelper.XmlAttributeToString(conditionNode.Attributes["path"]);
            rawMatchCase = XmlHelper.XmlAttributeToString(conditionNode.Attributes["matchcase"]);
        }

        public override bool IsValid(TokenManager tokens, DelRunRule runRule = null, InspectionRule parentRule = null, int prefix = 0, string startingpath = "")
        {
            messages.Clear();
            var path = System.IO.Path.Combine(startingpath, tokens.DecodeString(rawPath));
            var matchcase = tokens.DecodeString(rawMatchCase) == "true";
            if (Directory.Exists(path))
            {
                if(matchcase)
                {
                    var origFoldername = new DirectoryInfo(path).Name;
                    var folders = Directory.GetParent(path).GetDirectories();
                    foreach (var folder in folders)
                    {
                        var foldername = folder.Name;
                        if(origFoldername.Equals(foldername,StringComparison.CurrentCultureIgnoreCase) && !origFoldername.Equals(foldername))
                        {
                            //Invalid casing
                            this.setFailureMessage(tokens, $"Folder exists but casing does not match (found {foldername})");
                            return false;
                        }
                    }
                    return true;
                }
                else
                {
                    return true;
                }
            }
            else
            {
                this.setFailureMessage(tokens, $"Folder not found at {path}");
                return false;
            }
        }

        private string rawPath;
        private string rawMatchCase;
    }
}
