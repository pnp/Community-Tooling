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
        private string _jsonpath;

        public Wrangler(string map, string outputpath = "", string jsonpath = "*.json", LogRouter log = null)
        {
            if (log == null)
                _log = new LogRouter();
            else
                _log = log;

            _map = map;
            _outputpath = outputpath;
            _jsonpath = jsonpath;
        }

        public void RoundUp()
        {
            try
            {

            }
            catch (Exception ex)
            {
                _log.Error(ex, $"Error Rounding Up Files using map: \"{_map}\"");
            }
        }
    }
}
