using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Logger.Log.Builder;

namespace Sitecore.SharedSource.Logger.Log.Output
{
    public abstract class OutputHandlerBase
    {
        protected LevelLogger Logger;

        protected OutputHandlerBase(LevelLogger logger)
        {
            Logger = logger;
        }

        public virtual string Export()
        {
            return Export(null);
        }

        public abstract string Export(LogLine.LogType[] logTypes);

        public abstract string GetCountersStatus();

        public abstract string GetMainStatus();

        public abstract string GetLogLines(LogLine.LogType[] logTypes);

        public abstract string GetIdentifierText(string identifier, DateTime? startedAt, DateTime? finishedAt);
        
        public abstract string GetIdentifier();
    }
}