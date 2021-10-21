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

        public Wrangler(string map, string outputpath = "", string outputfilename = "roundup.csv", string startpath = "", string jsonfilepattern = "*.json", bool listjsonfiles = false, Dictionary<string, string> tokens = null, bool ListTokens = false, bool Overwrite = false, bool SkipHeaders = false, bool FirstOnly = false, string MultiValueSeparator = "|", LogRouter log = null)
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

            _functionResolver = new FunctionResolver(log: _log);
            _rootTokens = new TokenManager(_functionResolver, log: _log);

            // Add any tokens passed in the options
            _rootTokens.AddTokens(tokens);
        }

        public void RoundUp()
        {
            try
            {
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
                
                _log.Info($"Found {jsonFiles.Length} JSON file{(jsonFiles.Length > 1 ? "s" : "")} to round up");
                if(_listjsonfiles)
                {
                    foreach (var jsonFile in jsonFiles)
                    {
                        _log.Debug($"  Found {jsonFile.FullName}");
                    }
                }

                var doc = new XmlDocument();
                doc.Load(_map);

                _rootTokens.AddTokens(doc.SelectSingleNode("//tokens"));
                if (_listTokens)
                    _rootTokens.LogTokens();

                XmlNodeList columnNodes = doc.SelectNodes("//column");
                if(columnNodes != null && columnNodes.Count > 0)
                {
                    _log.Info($"Mapping {columnNodes.Count} columns");

                    var mappedColumns = MappedColumn.FromXmlNodeList(columnNodes);

                    var results = new DataTable();
                    foreach(var mappedColumn in mappedColumns)
                    {
                        results.Columns.Add(mappedColumn.Name);
                        if (!mappedColumn.ValidPath)
                            _log.Warn($"The path for column {mappedColumn.Name} is invalid or blank. Untransformed results will contain an empty string for this column.");
                    }
                    
                    foreach (var jsonFile in jsonFiles)
                    {
                        results.Rows.Add(ProcessFile(jsonFile.FullName, results, mappedColumns));
                    }

                    //Apply sorting (if specified)
                    XmlNodeList sortColumnNodes = doc.SelectNodes("//sortcolumn");
                    if(sortColumnNodes != null && sortColumnNodes.Count > 0)
                    {
                        var xmlHelper = new XmlHelper();
                        var sort = new List<string>();
                        foreach(XmlNode sortColumnNode in sortColumnNodes)
                        {
                            var sortName = xmlHelper.XmlAttributeToString(sortColumnNode.Attributes["name"]);
                            var sortDirection = xmlHelper.XmlAttributeToString(sortColumnNode.Attributes["direction"]);
                            if(!String.IsNullOrEmpty(sortName))
                            {
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
                    _log.Warn("No column entries found in map!");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error Rounding Up Files using map: \"{_map}\"");
            }
        }

        private DataRow ProcessFile(string filePath, DataTable dt, List<MappedColumn> mappedColumns)
        {
            var row = dt.NewRow();

            var content = File.ReadAllText(filePath);
            var doc = JsonDocument.Parse(content);

            //Pull out raw values (just using paths, no transformation)
            foreach(var mappedColumn in mappedColumns)
            {
                string rawValue = String.Empty;
                if (mappedColumn.ValidPath)
                {
                    var pathValue = mappedColumn.ParsedPath.Evaluate(doc.RootElement);
                    if (!String.IsNullOrEmpty(pathValue.Error))
                    {
                        _log.Warn($"Error while processing column {mappedColumn.Name} in {filePath}: {pathValue.Error}");
                    }
                    else if (pathValue.Matches == null || pathValue.Matches.Count < 1)
                    {
                        _log.Warn($"No result found for column {mappedColumn.Name} in {filePath}");
                    }
                    else
                    {
                        rawValue = pathValue.Matches[0].Value.GetString();

                        //Handle multivalue results
                        if (pathValue.Matches.Count > 1)
                        {
                            if(_firstonly)
                                _log.Warn($"Found {pathValue.Matches.Count} results for the specified path for column {mappedColumn.Name} in {filePath}, only the first value will be used");
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
