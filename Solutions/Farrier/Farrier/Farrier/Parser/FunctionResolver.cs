﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using Farrier.Helpers;

namespace Farrier.Parser
{
    class FunctionResolver
    {
        public string FUNCPATTERN { get; }
        public string PARAMSTART { get; }

        private LogRouter _log;

        public FunctionResolver(string pattern = "\\$.+?\\(", string paramstart = ",#", LogRouter log = null)
        {
            FUNCPATTERN = pattern;
            PARAMSTART = paramstart;

            if (log == null)
                _log = new LogRouter();
            else
                _log = log;
        }

        public string ResolveFunctions(string expression)
        {
            try
            {
                Match function = Regex.Match(expression, FUNCPATTERN);
                if (function.Success)
                {
                    string prefix = String.Empty;
                    if (function.Index > 0)
                    {
                        prefix = expression.Substring(0, function.Index);
                    }
                    string keyword = expression.Substring(function.Index + 1, function.Length - 2);
                    string remaining = ResolveFunctions(expression.Substring(function.Index + function.Length));
                    string innards = remaining.Substring(0, remaining.IndexOf(')'));
                    string result = executeFunction(keyword, innards);
                    string postfix = remaining.Substring(remaining.IndexOf(')') + 1);
                    return prefix + result + postfix;
                }
                else
                {
                    return expression;
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, String.Format("Unable to resolve expression: {0}", expression));
                return "";
            }
        }

        private string executeFunction(string keyword, string innards)
        {
            try
            {
                switch (keyword)
                {
                    case "UPPER":
                        return innards.ToUpper();
                    case "LOWER":
                        return innards.ToLower();
                    case "TRIM":
                        return innards.Trim();
                    case "INDEXOF":
                        var ioP = functionParameters(keyword, innards, 2);
                        return ioP[0].IndexOf(ioP[1]).ToString();
                    case "LASTINDEXOF":
                        var lioP = functionParameters(keyword, innards, 2);
                        return lioP[0].LastIndexOf(lioP[1]).ToString();
                    case "LENGTH":
                        return innards.Length.ToString();
                    case "SUBSTRING":
                        var ssP = functionParameters(keyword, innards, 3);
                        try
                        {
                            var ssp2 = int.Parse(ssP[1]);
                            var ssp3 = int.Parse(ssP[2]);
                            return ssP[0].Substring(ssp2, ssp3);
                        }
                        catch
                        {
                            throw new Exception(keyword + " requires the 2nd and 3rd parameters to be integers!");
                        }
                    case "STARTSWITH":
                        var swP = functionParameters(keyword, innards, 2);
                        return swP[0].StartsWith(swP[1]) ? "true" : "false";
                    case "ENDSWITH":
                        var ewP = functionParameters(keyword, innards, 2);
                        return ewP[0].EndsWith(ewP[1]) ? "true" : "false";
                    case "CONTAINS":
                        var cP = functionParameters(keyword, innards, 2);
                        return cP[0].Contains(cP[1]) ? "true" : "false";
                    case "REPLACE":
                        var rP = functionParameters(keyword, innards, 3);
                        return rP[0].Replace(rP[1], rP[2]);
                    case "FORMATDATE":
                        var fdP = functionParameters(keyword, innards, 2);
                        return DateTime.Parse(fdP[0]).ToString(fdP[1]);
                    case "FORMATNUMBER":
                        var fnP = functionParameters(keyword, innards, 2);
                        try
                        {
                            var fnP1 = int.Parse(fnP[0]);
                            return fnP1.ToString(fnP[1]);
                        }
                        catch
                        {
                            throw new Exception(keyword + " requires the first parameter to be an integer!");
                        }
                    case "ADD":
                        var addP = FunctionNumberParameters(keyword, innards, 2);
                        return (addP[0] + addP[1]).ToString();
                    case "SUBTRACT":
                        var subP = FunctionNumberParameters(keyword, innards, 2);
                        return (subP[0] - subP[1]).ToString();
                    case "MULTIPLY":
                        var mulP = FunctionNumberParameters(keyword, innards, 2);
                        return (mulP[0] * mulP[1]).ToString();
                    case "DIVIDE":
                        var divP = FunctionNumberParameters(keyword, innards, 2);
                        return (divP[0] / divP[1]).ToString();
                    case "MOD":
                        var modP = FunctionNumberParameters(keyword, innards, 2);
                        return (modP[0] % modP[1]).ToString();
                    case "EQUALS":
                        var eqP = functionParameters(keyword, innards, 2);
                        return eqP[0] == eqP[1] ? "true" : "false";
                    case "GT":
                        var gtP = FunctionNumberParameters(keyword, innards, 2);
                        return gtP[0] > gtP[1] ? "true" : "false";
                    case "GTE":
                        var gteP = FunctionNumberParameters(keyword, innards, 2);
                        return gteP[0] >= gteP[1] ? "true" : "false";
                    case "LT":
                        var ltP = FunctionNumberParameters(keyword, innards, 2);
                        return ltP[0] < ltP[1] ? "true" : "false";
                    case "LTE":
                        var lteP = FunctionNumberParameters(keyword, innards, 2);
                        return lteP[0] <= lteP[1] ? "true" : "false";
                    case "AND":
                        var andP = functionParameters(keyword, innards, 2);
                        return andP[0] == "true" && andP[1] == "true" ? "true" : "false";
                    case "OR":
                        var orP = functionParameters(keyword, innards, 2);
                        return orP[0] == "true" || orP[1] == "true" ? "true" : "false";
                    case "NOT":
                        return innards == "true" ? "false" : "true";
                    case "ISEMPTY":
                        return String.IsNullOrEmpty(innards) ? "true" : "false";
                    case "WHEN":
                        var whenP = functionParameters(keyword, innards, 2);
                        return whenP[0] == "true" ? whenP[1] : String.Empty;
                    case "IF":
                        var ifP = functionParameters(keyword, innards, 3);
                        return ifP[0] == "true" ? ifP[1] : ifP[2];
                    default:
                        throw new Exception("Unknown function: " + keyword);
                }

            }
            catch (Exception ex)
            {
                _log.Warn(ex.Message);
                return innards;
            }
        }

        private string[] functionParameters(string keyword, string innards, int expected)
        {
            var p = innards.Split(PARAMSTART);
            if (p.Length != expected)
            {
                throw new Exception(keyword + " requires " + expected + " parameters (found " + p.Length + ": " + innards + ")");
            }
            return p;
        }

        private int[] FunctionNumberParameters(string keyword, string innards, int expected)
        {
            try
            {
                var p = functionParameters(keyword, innards, expected);
                var i = new int[p.Length];
                for (int j = 0; j < p.Length; j++)
                {
                    i[j] = int.Parse(p[j]);
                }
                return i;
            }
            catch
            {
                throw new Exception(keyword + " requires all parameters to be integers!");
            }
        }
    }
}
