using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Managers
{
    public class DataSyncManager
    {
        public Logging RunDataSyncJob(Item dataSyncItem, ref Logging LogBuilder)
        {
            string errorMessage = String.Empty;

            var map = InstantiateDataMap(dataSyncItem, ref errorMessage);
            if (!String.IsNullOrEmpty(errorMessage))
            {
                LogBuilder.Log("Error", errorMessage);
            }
            if (map != null)
            {
                map.Process();
            }
            return LogBuilder;
        }

        public BaseDataMap InstantiateDataMap(Item dataSyncItem, ref string errorMessage)
        {
            Database currentDB = Configuration.Factory.GetDatabase("master");

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