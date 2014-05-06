using System;
using System.IO;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Managers
{
    public class DataSyncManager
    {
        public Logging RunDataSyncJob(Item dataSyncItem, ref Logging logBuilder)
        {
            string errorMessage = String.Empty;

            var map = InstantiateDataMap(dataSyncItem, ref errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                logBuilder.Log("Error", errorMessage);
                return logBuilder;
            }
            if (map != null)
            {
                logBuilder = map.Process();
            }
            return logBuilder;
        }

        public BaseDataMap InstantiateDataMap(Item dataSyncItem, ref string errorMessage)
        {
            var currentDB = Configuration.Factory.GetDatabase("master");

            string handlerAssembly = dataSyncItem["Handler Assembly"];
            string handlerClass = dataSyncItem["Handler Class"];
            var logBuilder = new Logging();

            if (!String.IsNullOrEmpty(handlerAssembly))
            {
                if (!String.IsNullOrEmpty(handlerClass))
                {
                    BaseDataMap map = null;
                    try
                    {
                        map = (BaseDataMap)Reflection.ReflectionUtil.CreateObject(handlerAssembly, handlerClass, new object[] { currentDB, dataSyncItem, logBuilder });
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        errorMessage += "The binary specified could not be found" + fnfe.Message;
                    }
                    if (map != null)
                    {
                        return map;
                    }
                    errorMessage += String.Format("The data map provided could not be instantiated. Assembly:'{0}' Class:'{1}'", handlerAssembly, handlerClass);
                }
                else
                {
                    errorMessage += "Import handler class is not defined";
                }
            }
            else
            {
                errorMessage += "import handler assembly is not defined";
            }
            return null;
        }
    }
}