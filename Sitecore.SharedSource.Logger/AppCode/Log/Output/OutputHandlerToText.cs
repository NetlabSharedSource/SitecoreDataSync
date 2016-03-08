using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Logger.Log.Builder;

namespace Sitecore.SharedSource.Logger.Log.Output
{
    public class OutputHandlerToText: OutputHandlerBase
    {
        public OutputHandlerToText(LevelLogger logger) : base(logger)
        {
        }

        public override string Export(LogLine.LogType[] logTypes)
        {
            var sb = new StringBuilder();
            var mainStatus = GetMainStatus();
            if (!String.IsNullOrEmpty(mainStatus))
            {
                sb.AppendLine(mainStatus + "\r\n");
            }
            var countersStatus = GetCountersStatus();
            if (!String.IsNullOrEmpty(countersStatus))
            {
                sb.AppendLine(countersStatus + "\r\n");
            }
            var logLines = GetLogLines(logTypes);

            if (!String.IsNullOrEmpty(logLines))
            {
                sb.AppendLine(logLines + "\r\n");
            }
            return sb.ToString();
        }

        public override string GetCountersStatus()
        {
            var statusText = String.Empty;
            var logger = Logger as Builder.Logger;
            if (logger != null)
            {
                foreach (var counter in logger.Counters)
                {
                    var key = counter.Key;
                    var count = counter.Value;
                    if (!String.IsNullOrEmpty(key))
                    {
                        statusText += WriteLine(key, count);
                    }
                }
            }
            statusText = statusText.TrimEnd(new[] {'\n', '\r'});
            return statusText;
        }

        private string WriteLine(string type, int itemCount)
        {
            if (itemCount != 0)
            {
                return type + ": " + itemCount + "\r\n";
            }
            return String.Empty;
        }

        public override string GetMainStatus()
        {
            var startTime = Logger.GetData(Constants.StartTime) as DateTime?;
            var endTime = Logger.GetData(Constants.EndTime) as DateTime?;
            var identifier = Logger.GetData(Constants.Identifier) as string;
            
            var startText = GetIdentifierText(identifier, startTime, endTime);
            if (!Logger.HasFatalsOrErrors())
            {
                return startText + " completed with success.";
            }
            return startText + " failed.";
        }

        public override string GetIdentifierText(string identifier, DateTime? startedAt, DateTime? finishedAt)
        {
            var identiferText = identifier;
            if (String.IsNullOrEmpty(identifier))
            {
                identiferText = "Job";
            }
            if (startedAt!=null)
            {
                var startedAtDateTime = (DateTime) startedAt;
                identiferText += " started " + startedAtDateTime.ToString("dd/MM/yyyy HH:mm:ss");
                if (finishedAt!=null)
                {
                    identiferText += " and";
                }
            }
            if (finishedAt != null)
            {
                var finishedAtDateTime = (DateTime)finishedAt;
                string finishedText;
                if (startedAt != null)
                {
                    var startedAtDateTime = (DateTime)startedAt;
                    if (startedAtDateTime.Day != finishedAtDateTime.Day)
                    {
                        finishedText = finishedAtDateTime.ToString("dd/MM/yyyy HH:mm:ss");
                    }
                    else
                    {
                        finishedText = finishedAtDateTime.ToString("HH:mm:ss");        
                    }
                    var timeSpan = finishedAtDateTime.Subtract(startedAtDateTime);
                    finishedText += " in " + Math.Round(timeSpan.TotalSeconds, 3) + " s.";
                }
                else
                {
                    finishedText = finishedAtDateTime.ToString("HH:mm:ss");    
                }
                identiferText += " finished " + finishedText;
            }
            return identiferText;
        }

        public override string GetIdentifier()
        {
            var identifier = Logger.GetData(Constants.Identifier) as string;
            return identifier;
        }

        public override string GetLogLines(LogLine.LogType[] logTypes)
        {
            var sb = new StringBuilder();
            if (Logger != null)
            {
                var startText = Logger.HasKeys() ? "Import job:\r\n" + GetKeys(Logger) + "\r\n" : "Import job:\r\n";
                sb.AppendLine(startText);
                //sb.AppendLine(String.Format("{0}", GetTimeAndTimeSpanInfo()));
                if (Logger.HasFatalsErrorsOrInfos())
                {
                    sb.AppendLine(String.Format("Log:"));
                    var logger = Logger as Builder.Logger;
                    if (logger != null)
                    {
                        OutputGroupedCategories(logger, logTypes, ref sb);
                    }
                    ProcessLevelLoggerAndChildren(Logger, logTypes, ref sb, 0);
                }
                else
                {
                    sb.AppendLine(
                        String.Format(
                            "The job was processed, with no errors or infos.", startText));
                }
            }
            return sb.ToString();
        }

        protected virtual void OutputGroupedCategories(Builder.Logger logger, LogLine.LogType[] logTypes, ref StringBuilder sb)
        {
            var fatalSb = new StringBuilder();
            var errorSb = new StringBuilder();
            var infoSb = new StringBuilder();
            foreach (var categoryLogLines in logger.LogLineByCategory)
            {
                var currentSb = infoSb;
                var category = categoryLogLines.Key;
                var loglineList = categoryLogLines.Value;
                if (loglineList != null && loglineList.Any())
                {
                    int totalLines = loglineList.Count;
                    var firstLogLine = loglineList.FirstOrDefault();
                    if (firstLogLine != null)
                    {
                        if (firstLogLine.Type == LogLine.LogType.Fatal && (logTypes == null || logTypes.Contains(firstLogLine.Type)))
                        {
                            currentSb = fatalSb;
                        }
                        else if (firstLogLine.Type == LogLine.LogType.Error && (logTypes == null || logTypes.Contains(firstLogLine.Type)))
                        {
                            currentSb = errorSb;
                        }
                        else if (firstLogLine.Type == LogLine.LogType.Info && (logTypes == null || logTypes.Contains(firstLogLine.Type)))
                        {
                            currentSb = infoSb;
                        }
                        else
                        {
                            continue;
                        }
                    }
                    currentSb.AppendLine(String.Format("* {0} ({1}) ", category, totalLines));
                    foreach (var logLine in loglineList)
                    {
                        if (logTypes == null || logTypes.Contains(logLine.Type))
                        {
                            currentSb.AppendLine(String.Format("** {0}", OutputLogLine(logLine)));
                        }
                    }
                }
            }
            var fatal = fatalSb.ToString();
            var error = errorSb.ToString();
            var info = infoSb.ToString();
            if (!String.IsNullOrEmpty(fatal))
            {
                sb.AppendLine("Fatals by type:");
                sb.AppendLine(fatal + "\r\n\r\n");
            }
            if (!String.IsNullOrEmpty(error))
            {
                sb.AppendLine("Errors by type:");
                sb.AppendLine(error + "\r\n\r\n");
            }
            if (!String.IsNullOrEmpty(info))
            {
                sb.AppendLine("Info by type:");
                sb.AppendLine(info + "\r\n\r\n");
            }
        }

        protected virtual string OutputLogLine(LogLine logLine)
        {
            if (logLine != null)
            {
                return String.Format("{0} - {1} - {2}", logLine.Category, logLine.Message, logLine.TimeStamp);
            }
            return String.Empty;
        }

        protected virtual void ProcessLevelLoggerAndChildren(LevelLogger levelLogger, LogLine.LogType[] logTypes, ref StringBuilder sb, int level)
        {
            var children = levelLogger.GetChildren();
            if (children != null)
            {
                var prefix = GetPrefixIndent(level);
                foreach (var child in children)
                {
                    var logLine = child as LogLine;
                    if (logLine != null)
                    {
                        if (logTypes == null || logTypes.Contains(logLine.Type))
                        {
                            sb.AppendLine(String.Format("{0} {1}", prefix, OutputLogLine(logLine)));
                            continue;
                        }
                    }
                    var childLevelLogger = child as LevelLogger;
                    if (childLevelLogger != null)
                    {
                        if (childLevelLogger.HasErrorsOrInfos())
                        {
                            var keys = childLevelLogger.HasKeys() ? GetKeys(childLevelLogger) : "";
                            var name = GetCategoryName(childLevelLogger);
                            if (!String.IsNullOrEmpty(name))
                            {
                                sb.AppendLine(String.Format("{0} {1} {2}", prefix, name, keys));
                            }
                            ProcessLevelLoggerAndChildren(childLevelLogger, logTypes, ref sb, level + 1);
                        }
                    }
                }
            }
        }

        protected virtual string GetCategoryName(LevelLogger logger)
        {
            var name = logger.Name;
            return name;
        }

        protected virtual string GetPrefixIndent(int level)
        {
            var prefix = String.Empty;
            for (int i = 0; i <= level; i++ )
            {
                prefix += "*";
            }
            return prefix;
        }

        protected virtual string GetKeys(LevelLogger logger)
        {
            var sb = new StringBuilder();
            if (logger != null && logger.Keys!=null)
            {
                foreach (var keyValue in logger.Keys)
                {
                    if (keyValue.Key == "Item")
                    {
                        continue;
                    }
                    sb.AppendFormat("{0}:{1} - ", keyValue.Key, keyValue.Value);
                }
            }
            return sb.ToString().TrimEnd(new[]{'-',' '});
        }

        //private string GetTimeAndTimeSpanInfo()
        //{
        //    var timeSpan = EndTime.Subtract(StartTime);
        //    return String.Format("Start: {0}. End: {1}. Time used: {2} ", StartTime, EndTime, FormatTimeSpan(timeSpan));
        //}

        protected virtual string FormatTimeSpan(TimeSpan timeSpan)
        {
            string timeUsed = String.Empty;
            if (timeSpan.TotalSeconds > 0)
            {
                timeUsed += Math.Round(timeSpan.TotalSeconds, 2) + " s.";
            }
            return timeUsed;
        }
    }
}