using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.Logger.Log.Builder
{
    public static class LoggerManager
    {
        public static LevelLogger CreateLogger()
        {
            return new Logger();
        }
    }
}