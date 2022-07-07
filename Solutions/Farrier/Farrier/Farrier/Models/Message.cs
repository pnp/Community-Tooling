using System;
using System.Collections.Generic;
using System.Text;

namespace Farrier.Models
{
    class Message
    {
        public Message(MessageLevel level, string source, string text, int prefix = 0)
        {
            Source = source;
            Text = text;
            Level = level;
            Prefix = prefix;
        }
        
        public static Message Warning(string source, string text, int prefix = 0)
        {
            return new Message(MessageLevel.warning, source, text, prefix);
        }
        
        public static Message Error(string source, string text, int prefix = 0)
        {
            return new Message(MessageLevel.error, source, text, prefix);
        }

        public static Message Info(string source, string text, int prefix = 0)
        {
            return new Message(MessageLevel.info, source, text, prefix);
        }

        public string Source { get; set; }
        public string Text { get; set; }
        public MessageLevel Level { get; set; }
        public int Prefix { get; set; }
        public Exception Ex { get; set; }
    }
}
