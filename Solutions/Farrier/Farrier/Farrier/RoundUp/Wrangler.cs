using System;
using System.Collections.Generic;
using System.Text;
using Farrier.Helpers;
using System.Xml;
using System.IO;
using Farrier.Models;

namespace Farrier.RoundUp
{
    class Wrangler
    {
        private LogRouter _log;

        private string _map;
        private string _outputpath;
        private string _startpath;
        private string _jsonfilepattern;

        public Wrangler(string map, string outputpath = "", string startpath = "", string jsonfilepattern = "*.json", LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            _map = map;
            _outputpath = outputpath;
            if (!String.IsNullOrEmpty(startpath))
                _startpath = startpath;
            else
                _startpath = Directory.GetCurrentDirectory();
            _jsonfilepattern = jsonfilepattern;
        }

        public void RoundUp()
        {
            try
            {
                var startingDirectory = new DirectoryInfo(_startpath);
                var jsonFilePattern = Path.GetFileName(_jsonfilepattern);
                var jsonFiles = startingDirectory.GetFiles(_jsonfilepattern, new EnumerationOptions() { RecurseSubdirectories = true });
                
                _log.Info($"Found {jsonFiles.Length} JSON file{(jsonFiles.Length > 1 ? "s" : "")} to round up");
                foreach(var jsonFile in jsonFiles)
                {
                    _log.Debug($"  Found {jsonFile.FullName}");
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error Rounding Up Files using map: \"{_map}\"");
            }
        }
    }
}
