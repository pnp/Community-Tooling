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

        public void Error(Exception ex, string message)
        {
            _logError(ex, message);
        }

        public void Info(string message)
        {
            _logInfo(message);
        }

        public void Debug(string message)
        {
            _logDebug(message);
        }

        public void Warn(string message)
        {
            _logWarn(message);
        }
    }
}
