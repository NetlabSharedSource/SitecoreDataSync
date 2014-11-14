using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.Logger.Log.Builder
{
    public class Logger: LevelLogger
    {
        public Dictionary<string, int> Counters = new Dictionary<string, int>();

        public Dictionary<string, List<LogLine>> LogLineByCategory = new Dictionary<string, List<LogLine>>();
 
        public Logger(): base(null)
        {

        }

        public Logger(Logger logger) : base(logger)
        {
        }

        public override void IncrementCounter(string key)
        {
            if (Counters != null)
            {
                if (Counters.ContainsKey(key))
                {
                    var counter = Counters[key];
                    Counters[key] = counter + 1;
                }
                else
                {
                    Counters.Add(key, 1);
                }
            }
        }

        public override void SetCounter(string key, int count)
        {
            if (Counters != null)
            {
                if (Counters.ContainsKey(key))
                {
                    Counters[key] = count;
                }
                else
                {
                    Counters.Add(key, count);
                }
            }
        }

        public override int GetCounter(string key)
        {
            if (Counters != null)
            {
                if (Counters.ContainsKey(key))
                {
                    return Counters[key];
                }
            }
            return 0;
        }

        public override LevelLogger CreateLevelLogger(string name)
        {
            var levelLogger = new LevelLogger(this) { Name = name };
            AddChild(levelLogger);
            return levelLogger;
        }

        public override LevelLogger CreateLevelLogger()
        {
            var levelLogger = new LevelLogger(this);
            AddChild(levelLogger);
            return levelLogger;
        }

        protected override void ChildAddedLogLine(object sender, LogLine logLine)
        {
            base.ChildAddedLogLine(sender, logLine);
            if (logLine != null)
            {
                var category = logLine.Category;
                if (LogLineByCategory != null)
                {
                    if (LogLineByCategory.ContainsKey(category))
                    {
                        var logLineList = LogLineByCategory[category];
                        if (logLineList != null)
                        {
                            logLineList.Add(logLine);
                        }
                        else
                        {
                            logLineList = new List<LogLine> {logLine};
                        }
                        LogLineByCategory[category] = logLineList;
                    }
                    else
                    {
                        LogLineByCategory.Add(category, new List<LogLine> {logLine});
                    }
                }
            }
        }
    }
}