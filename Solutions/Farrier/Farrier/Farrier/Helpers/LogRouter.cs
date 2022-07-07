using System;
using System.Collections.Generic;
using System.Text;

namespace Farrier.Helpers
{
    class LogRouter
    {
        private Action<Exception, string> _logError;
        private Action<string> _logWarn;
        private Action<string> _logInfo;
        private Action<string> _logDebug;

        public LogRouter(Action<Exception, string> logError = null, Action<string> logWarning = null, Action<string> logInfo = null, Action<string> logDebug = null)
        {
            if (logError == null)
                _logError = (e, s) => { };
            else
                _logError = logError;

            if (logWarning == null)
                _logWarn = s => { };
            else
                _logWarn = logWarning;

            if (logInfo == null)
                _logInfo = s => { };
            else
                _logInfo = logInfo;

            if (logDebug == null)
                _logDebug = s => { };
            else
                _logDebug = logDebug;
        }

        public void Error(Exception ex, string message, int prefix = 0)
        {
            _logError(ex, _getPrefix(prefix) + message);
        }

        public void Error(string message, int prefix = 0)
        {
            _logError(null, _getPrefix(prefix) + message);
        }

        public void Info(string message, int prefix = 0)
        {
            _logInfo(_getPrefix(prefix) + message);
        }

        public void Debug(string message, int prefix = 0)
        {
            _logDebug(_getPrefix(prefix) + message);
        }

        public void Warn(string message, int prefix = 0)
        {
            _logWarn(_getPrefix(prefix) + message);
        }

        private string _getPrefix(int count)
        {
            return new string('|', count);
        }
    }
}
