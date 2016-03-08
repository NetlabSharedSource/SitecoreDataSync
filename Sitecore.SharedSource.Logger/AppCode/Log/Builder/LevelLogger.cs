using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.Logger.Log.Builder
{
    public class LevelLogger
    {
        private Logger Logger { get; set; }

        // Both LevelLogger and LogLine
        private ArrayList Children = new ArrayList();

        private bool IsErrorsAdded = false;
        private bool IsInfosAdded = false;
        private bool IsFatalsAdded = false;
        
        public Dictionary<string, object> Keys = new Dictionary<string, object>();
        private Dictionary<string, object> Data = new Dictionary<string, object>();

        public string Name { get; set; }

        public delegate void AddedLogLineHandler(object sender, LogLine logLine);

        public event AddedLogLineHandler AddedLogLine;

        public LevelLogger(Logger logger)
        {
            Logger = logger;
        }

        public virtual void AddKey(string keyName, object keyValue)
        {
            if (!Keys.ContainsKey(keyName))
            {
                Keys.Add(keyName, keyValue);
            }
            else
            {
                Keys[keyName] = keyValue;
            }
        }

        public virtual object GetData(string keyName)
        {
            if (Data.ContainsKey(keyName))
            {
                return Data[keyName];
            }
            return null;
        }

        public virtual void AddData(string keyName, object dataValue)
        {
            if (!Data.ContainsKey(keyName))
            {
                Data.Add(keyName, dataValue);
            }
            else
            {
                Data[keyName] = dataValue;
            }
        }

        public virtual bool HasKeys()
        {
            if (Keys != null)
            {
                return Keys.Any();
            }
            return false;
        }

        public virtual ArrayList GetChildren()
        {
            return Children;
        }

        public virtual bool HasFatalsErrorsOrInfos()
        {
            return HasFatals() || HasErrors() || HasInfos();
        }

        public virtual bool HasErrorsOrInfos()
        {
            return HasErrors() || HasInfos();
        }

        public virtual bool HasFatals()
        {
            return IsFatalsAdded;
        }

        public virtual bool HasErrors()
        {
            return IsErrorsAdded;
        }

        public virtual bool HasInfos()
        {
            return IsInfosAdded;
        }

        private void UpdateInfosAndErrorAdded(LogLine logLine)
        {
            if (!IsInfosAdded && logLine.Type == LogLine.LogType.Info)
            {
                IsInfosAdded = true;
            }
            if (!IsErrorsAdded && logLine.Type == LogLine.LogType.Error)
            {
                IsErrorsAdded = true;
            }
            if (!IsFatalsAdded && logLine.Type == LogLine.LogType.Fatal)
            {
                IsFatalsAdded = true;
            }
        }

        private void AddToLogLines(LogLine logLine)
        {
            UpdateInfosAndErrorAdded(logLine);
            if (AddedLogLine != null)
            {
                AddedLogLine(this, logLine);
            }
            if (Children == null)
            {
                Children = new ArrayList();
            }
            Children.Add(logLine);
        }

        public virtual void AddInfo(string key, string message)
        {
            var logLine = new LogLine(key, message, LogLine.LogType.Info);
            AddToLogLines(logLine);
        }

        public virtual void AddError(string key, string message)
        {
            var logLine = new LogLine(key, message, LogLine.LogType.Error);
            AddToLogLines(logLine);
        }
        
        public virtual void AddFatal(string key, string message)
        {
            var logLine = new LogLine(key, message, LogLine.LogType.Fatal);
            AddToLogLines(logLine);
        }

        protected void AddChild(LevelLogger child)
        {
            if (Children != null && child!=null)
            {
                Children.Add(child);
                child.AddedLogLine += ChildAddedLogLine;
            }
        }

        protected virtual void ChildAddedLogLine(object sender, LogLine logLine)
        {
            UpdateInfosAndErrorAdded(logLine);
            if (AddedLogLine != null)
            {
                AddedLogLine(sender, logLine);
            }
        }

        public virtual LevelLogger CreateLevelLogger(string name)
        {
            var levelLogger = new LevelLogger(Logger) { Name = name };
            AddChild(levelLogger);
            return levelLogger;
        }

        public virtual LevelLogger CreateLevelLogger()
        {
            var levelLogger = new LevelLogger(Logger);
            AddChild(levelLogger);
            return levelLogger;
        }

        public virtual void IncrementCounter(string key)
        {
            if (Logger != null)
            {
                Logger.IncrementCounter(key);
            }
        }

        public virtual void SetCounter(string key, int count)
        {
            if (Logger != null)
            {
                Logger.SetCounter(key, count);
            }
        }

        public virtual int GetCounter(string key)
        {
            if (Logger != null)
            {
                return Logger.GetCounter(key);
            }
            return 0;
        }
    }
}