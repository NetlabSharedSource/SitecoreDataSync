using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.Logger.Log.Output;

namespace Sitecore.SharedSource.DataSync.Log
{
    public static class Manager
    {
        private const string FieldNameLogAssembly = "Log Assembly";
        private const string FieldNameLogClass = "Log Class";
        private const string FieldNameOutputHandlerAssembly = "OutputHandler Assembly";
        private const string FieldNameOutputHandlerClass = "OutputHandler Class";

        public static LevelLogger CreateLogger(Item dataSyncItem)
        {
            string logAssembly = dataSyncItem[FieldNameLogAssembly];
            string logClass = dataSyncItem[FieldNameLogClass];

            if (!String.IsNullOrEmpty(logAssembly))
            {
                if (!String.IsNullOrEmpty(logClass))
                {
                    LevelLogger logger = null;
                    try
                    {
                        logger = (LevelLogger)Reflection.ReflectionUtil.CreateObject(logAssembly, logClass, new object[] { });
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        throw new Exception("The binary specified could not be found" + fnfe.Message, fnfe);
                    }
                    if (logger != null)
                    {
                        return logger;
                    }
                    throw new Exception(String.Format("The Log provided could not be instantiated. Assembly:'{0}' Class:'{1}'", logAssembly, logClass));
                }
                else
                {
                    throw new Exception(String.Format("Log class is not defined"));
                }
            }
            else
            {
                throw new Exception("Log assembly is not defined");
            }
            return null;
        }

        public static OutputHandlerBase CreateOutputHandler(Item dataSyncItem, LevelLogger logger)
        {
            string outHandlerAssembly = dataSyncItem[FieldNameOutputHandlerAssembly];
            string outputHandlerClass = dataSyncItem[FieldNameOutputHandlerClass];

            if (!String.IsNullOrEmpty(outHandlerAssembly))
            {
                if (!String.IsNullOrEmpty(outputHandlerClass))
                {
                    OutputHandlerBase outputHandler = null;
                    try
                    {
                        outputHandler = (OutputHandlerBase)Reflection.ReflectionUtil.CreateObject(outHandlerAssembly, outputHandlerClass, new object[] { logger });
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        throw new Exception("The binary specified could not be found" + fnfe.Message, fnfe);
                    }
                    if (outputHandler != null)
                    {
                        return outputHandler;
                    }
                    throw new Exception(String.Format("The OutputHandler provided could not be instantiated. Assembly:'{0}' Class:'{1}'", outHandlerAssembly, outputHandlerClass));
                }
                else
                {
                    throw new Exception(String.Format("OutputHandler class is not defined"));
                }
            }
            else
            {
                throw new Exception("OutputHandler assembly is not defined");
            }
            return null;
        }
    }
}