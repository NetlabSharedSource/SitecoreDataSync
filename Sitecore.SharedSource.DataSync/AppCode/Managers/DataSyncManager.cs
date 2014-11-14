using System;
using System.IO;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.Logger.Log.Builder;

namespace Sitecore.SharedSource.DataSync.Managers
{
    public class DataSyncManager
    {
        private const string FieldNameHandlerAssembly = "Handler Assembly";
        private const string FieldNameHandlerClass = "Handler Class";

        public LevelLogger RunDataSyncJob(Item dataSyncItem, ref LevelLogger logger)
        {
            string errorMessage = String.Empty;

            var map = InstantiateDataMap(dataSyncItem, ref logger);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                logger.AddError("Error", errorMessage);
                return logger;
            }
            if (map != null)
            {
                logger = map.Process();
            }
            return logger;
        }

        public BaseDataMap InstantiateDataMap(Item dataSyncItem, ref LevelLogger logger)
        {
            var currentDB = Configuration.Factory.GetDatabase("master");

            string handlerAssembly = dataSyncItem[FieldNameHandlerAssembly];
            string handlerClass = dataSyncItem[FieldNameHandlerClass];

            if (!String.IsNullOrEmpty(handlerAssembly))
            {
                if (!String.IsNullOrEmpty(handlerClass))
                {
                    BaseDataMap map = null;
                    try
                    {
                        map = (BaseDataMap)Reflection.ReflectionUtil.CreateObject(handlerAssembly, handlerClass, new object[] { currentDB, dataSyncItem, logger });
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        logger.AddError("Error", "The binary specified could not be found" + fnfe.Message);
                    }
                    if (map != null)
                    {
                        return map;
                    }
                    logger.AddError("Error", String.Format("The data map provided could not be instantiated. Assembly:'{0}' Class:'{1}'", handlerAssembly, handlerClass));
                }
                else
                {
                    logger.AddError("Error", "Import handler class is not defined");
                }
            }
            else
            {
                logger.AddError("Error", "import handler assembly is not defined");
            }
            return null;
        }
    }
}