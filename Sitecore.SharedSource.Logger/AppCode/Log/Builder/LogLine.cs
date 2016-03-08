using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.Logger.Log.Builder
{
    public class LogLine
    {
        public LogLine(string category, string message)
        {
            Category = category;
            Message = message;
            TimeStamp = DateTime.Now;
        }

        public LogLine(string category, string message, LogType type)
        {
            Category = category;
            Message = message;
            TimeStamp = DateTime.Now;
            Type = type;
        }

        public string Category { get; set; }
        public string Message { get; set; }
        public DateTime TimeStamp { get; set; }
        
        public enum LogType
        {
            Info,
            Error,
            Fatal
        }
        public LogType Type { get; set; }
    }
}