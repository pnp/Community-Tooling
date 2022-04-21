using System;
using System.Collections.Generic;
using System.Text;
using Farrier.Helpers;
using System.Xml;
using System.IO;
using Farrier.Models;
using Farrier.Parser;
using System.Data;
using Json.Path;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using System.Linq;

namespace Farrier.RoundUp
{
    class Wrangler
    {
        private LogRouter _log;

        private FunctionResolver _functionResolver;
        private TokenManager _rootTokens;

        private string _map;
        private string _outputpath;
        private string _outputfilename;
        private string _startpath;
        private string _jsonfilepattern;
        private bool _listjsonfiles;
        private bool _listTokens;
        private bool _overwrite;
        private bool _skipHeaders;
        private bool _firstonly;
        private string _multivalueseparator;
        private int _skip;
        private int _limit;
        private int _pathdepth;

        public Wrangler(string map, string outputpath = "", string outputfilename = "roundup.csv", string startpath = "", string jsonfilepattern = "*.json", bool listjsonfiles = false, Dictionary<string, string> tokens = null, bool ListTokens = false, bool Overwrite = false, bool SkipHeaders = false, bool FirstOnly = false, string MultiValueSeparator = "|", int Skip = 0, int Limit = 10000, int PathDepth = 0, LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            _map = map;
            _outputpath = outputpath;
            _outputfilename = outputfilename;

            if (!String.IsNullOrEmpty(startpath))
                _startpath = startpath;
            else
                _startpath = Directory.GetCurrentDirectory();

            _jsonfilepattern = jsonfilepattern;
            _listjsonfiles = listjsonfiles;
            _listTokens = ListTokens;
            _overwrite = Overwrite;
            _skipHeaders = SkipHeaders;
            _firstonly = FirstOnly;
            _multivalueseparator = MultiValueSeparator;
            
            _skip = Skip;
            if (_skip < 0)
                _skip = 0;

            _limit = Limit;
            if (_limit < 1)
                _limit = 1;

            _pathdepth = PathDepth;
            if (_pathdepth < 0)
                _pathdepth = 0;

            _functionResolver = new FunctionResolver(log: _log);
            _rootTokens = new TokenManager(_functionResolver, log: _log);

            // Add any tokens passed in the options
            _rootTokens.AddTokens(tokens);
        }

        public void RoundUp()
        {
            try
            {
                _log.Info("Initializing...");

                var resultPath = Path.Combine(_outputpath, _outputfilename);
                if (String.IsNullOrEmpty(resultPath))
                    resultPath = Path.Combine(Directory.GetCurrentDirectory(), "roundup.csv");

                //Kill the file if it already exists
                if (!_overwrite && File.Exists(resultPath))
                {
                    throw new Exception($"File {resultPath} already exists! Please specify --overwrite or remove the file and try again.");
                }

                var startingDirectory = new DirectoryInfo(_startpath);
                var jsonFilePattern = Path.GetFileName(_jsonfilepattern);
                var jsonFiles = startingDirectory.GetFiles(_jsonfilepattern, new EnumerationOptions() { RecurseSubdirectories = true });
                
                _log.Info($"  Found {jsonFiles.Length} JSON file{(jsonFiles.Length > 1 ? "s" : "")} to round up");
                
                if(_skip > 0)
                {
                    jsonFiles = jsonFiles.Skip(_skip).ToArray();
                    _log.Info($"    Skipping the first {(_skip > 1 ? $"{_skip} files" : "file")}");
                }
                if(jsonFiles.Length > _limit)
                {
                    jsonFiles = jsonFiles.SkipLast(jsonFiles.Length - _limit).ToArray();
                    _log.Info($"    Limiting total files to process to {_limit}");
                }

                int totalFiles = jsonFiles.Length;
                int currentFile = 1;

                if(_listjsonfiles)
                {
                    foreach (var jsonFile in jsonFiles)
                    {
                        _log.Debug($"    Found {jsonFile.FullName}");
                    }
                }

                var doc = new XmlDocument();
                XmlNamespaceManager nsmgr;
                try
                {
                    doc.Load(_map);
                    doc.Schemas.Add("https://pnp.github.io/map", @"XML\Map.xsd");
                    nsmgr = new XmlNamespaceManager(doc.NameTable);
                    nsmgr.AddNamespace("f", "https://pnp.github.io/map");
                }
                catch (XmlException ex)
                {
                    _log.Error($"Unable to read {_map}, likely bad XML. Details: {ex.Message}");
                    return;
                }

                _rootTokens.AddTokens(doc.SelectSingleNode("//f:tokens", nsmgr));
                if (_listTokens)
                    _rootTokens.LogTokens();


                XmlNodeList columnNodes = doc.SelectNodes("//f:column", nsmgr);
                if(columnNodes != null && columnNodes.Count > 0)
                {
                    _log.Info($" Configuring {columnNodes.Count} columns...");

                    var mappedColumns = MappedColumn.FromXmlNodeList(columnNodes, _rootTokens);

                    var results = new DataTable();
                    foreach(var mappedColumn in mappedColumns)
                    {
                        results.Columns.Add(mappedColumn.Name);
                        if (!mappedColumn.ValidPath)
                            _log.Warn($"    The path for column {mappedColumn.Name} is invalid or blank. Untransformed results will contain an empty string for this column.");
                    }

                    _log.Info($"Extracting {columnNodes.Count} column{(columnNodes.Count > 1 ? "s" : "")} from {totalFiles} file{(totalFiles > 1 ? "s" : "")}");
                    
                    foreach (var jsonFile in jsonFiles)
                    {
                        string identifier = FileHelper.PathFromDepth(jsonFile.FullName, _pathdepth);
                        _log.Info($"  File ({currentFile}/{totalFiles}): {identifier}");
                        var result = ProcessFile(jsonFile.FullName, identifier, results, mappedColumns);
                        if(result != null)
                            results.Rows.Add(result);

                        currentFile += 1;
                    }

                    //Apply sorting (if specified)
                    XmlNodeList sortColumnNodes = doc.SelectNodes("//f:sortcolumn", nsmgr);
                    if(sortColumnNodes != null && sortColumnNodes.Count > 0)
                    {
                        var sort = new List<string>();
                        foreach(XmlNode sortColumnNode in sortColumnNodes)
                        {
                            var rawSortName = XmlHelper.XmlAttributeToString(sortColumnNode.Attributes["name"]);
                            var rawSortDirection = XmlHelper.XmlAttributeToString(sortColumnNode.Attributes["direction"]);
                            if(!String.IsNullOrEmpty(rawSortName))
                            {
                                var sortName = _rootTokens.DecodeString(rawSortName);
                                var sortDirection = _rootTokens.DecodeString(rawSortDirection);
                                sort.Add(sortName + (sortDirection == "descending" ? " desc" : ""));
                            }
                        }
                        if(sort.Count > 0)
                        {
                            results.DefaultView.Sort = String.Join(",", sort);
                            results = results.DefaultView.ToTable();
                        }
                    }

                    WriteResults(resultPath, results, !_skipHeaders);
                }
                else
                {
                    _log.Warn("  No column entries found in map!");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error Rounding Up Files using map: \"{_map}\"");
            }
        }

        private DataRow ProcessFile(string filePath, string identifier, DataTable dt, List<MappedColumn> mappedColumns)
        {
            var row = dt.NewRow();
            string indent = "    ";

            var content = File.ReadAllText(filePath);
            if(String.IsNullOrEmpty(content))
            {
                _log.Error($"{indent}Empty JSON File! Unable to pull details from {identifier}");
                return null;
            }
            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(content);
            }
            catch(JsonException ex)
            {
                _log.Error($"{indent}Unable to parse JSON File {identifier}! Details: {ex.Message}");
                _log.Warn($"{indent}Values from {identifier} will NOT be included in results!");
                return null;
            }

            //Pull out raw values (just using paths, no transformation)
            foreach(var mappedColumn in mappedColumns)
            {
                string rawValue = String.Empty;
                if (mappedColumn.ValidPath)
                {
                    var pathValue = mappedColumn.ParsedPath.Evaluate(doc.RootElement);
                    if (!String.IsNullOrEmpty(pathValue.Error))
                    {
                        _log.Warn($"{indent}Error while processing column {mappedColumn.Name}: {pathValue.Error}");
                    }
                    else if (pathValue.Matches == null || pathValue.Matches.Count < 1)
                    {
                        _log.Warn($"{indent}No result found for column {mappedColumn.Name}");
                    }
                    else
                    {
                        rawValue = pathValue.Matches[0].Value.GetString();

                        //Handle multivalue results
                        if (pathValue.Matches.Count > 1)
                        {
                            if(_firstonly)
                                _log.Warn($"{indent}Found {pathValue.Matches.Count} results for the specified path for column {mappedColumn.Name}, only the first value will be used (set FirstOnly to false to capture all values)");
                            else
                            {
                                var values = new List<string>();
                                foreach(var match in pathValue.Matches)
                                {
                                    values.Add(match.Value.GetString());
                                }
                                rawValue = String.Join(_multivalueseparator, values);
                            }    
                        }
                    }
                }

                row[mappedColumn.Name] = rawValue;
            }

            //Apply transformations (using raw values)
            var rowTokens = TokenManager.TokenizeRow(row, dt.Columns, _functionResolver, log: _log);
            foreach(var mappedColumn in mappedColumns)
            {
                var transform = mappedColumn.Transform.Replace("@@currentvalue@@", row[mappedColumn.Name].ToString());
                row[mappedColumn.Name] = _rootTokens.DecodeString(transform, false, rowTokens);
            }

            return row;
        }

        private void WriteResults(string csvPath, DataTable results, bool includeHeaders = true)
        {
            using (var stringWriter = new StringWriter())
            {
                var config = new CsvConfiguration(System.Globalization.CultureInfo.CurrentCulture);
                using (var csvWriter = new CsvWriter(stringWriter, config))
                {
                    if(includeHeaders)
                    {
                        //Write the CSV Headers
                        foreach (DataColumn col in results.Columns)
                        {
                            csvWriter.WriteField(col.ColumnName);
                        }
                        csvWriter.NextRecord();
                    }
                    
                    //Write the data
                    foreach(DataRow row in results.Rows)
                    {
                        foreach(DataColumn col in results.Columns)
                        {
                            csvWriter.WriteField(row[col]);
                        }
                        csvWriter.NextRecord();
                    }
                }
                File.WriteAllText(csvPath, stringWriter.ToString());
            }
        }
    }
}
