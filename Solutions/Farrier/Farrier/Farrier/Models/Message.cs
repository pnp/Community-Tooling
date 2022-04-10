using System;
using System.Collections.Generic;
using System.Text;

namespace Farrier.Models
{
    class Message
    {
        public Message(MessageLevel level, string text, int prefix = 0)
        {
            Text = text;
            Level = level;
            Prefix = prefix;
        }
        
        public static Message Warning(string text, int prefix = 0)
        {
            return new Message(MessageLevel.warning, text, prefix);
        }
        
        public static Message Error(string text, int prefix = 0)
        {
            return new Message(MessageLevel.error, text, prefix);
        }

        public static Message Info(string text, int prefix = 0)
        {
            return new Message(MessageLevel.info, text, prefix);
        }

        public string Text { get; set; }
        public MessageLevel Level { get; set; }
        public int Prefix { get; set; }
    }
}
