using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.Logger.Log.Output;
using Sitecore.SharedSource.DataSync.Managers;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.Tasks;

namespace Sitecore.SharedSource.DataSync.ScheduledTasks
{
    public class DataSyncTask
    {
        private const string Identifier = "DataSyncTask.RunJob";
        private const string ItemsFieldName = "Items";

        public void RunJob(Item[] itemArray, CommandItem commandItem, ScheduleItem scheduledItem)
        {
            try
            {
                if (scheduledItem != null)
                {
                    var itemIds = scheduledItem[ItemsFieldName];
                    if (!String.IsNullOrEmpty(itemIds))
                    {
                        var dataSourceCache = new Dictionary<string, string>();
                        var idList = itemIds.Split('|');
                        if (idList.Any())
                        {
                            foreach (var id in idList)
                            {
                                if (ID.IsID(id))
                                {
                                    var dataSyncItem = scheduledItem.Database.GetItem(new ID(id));
                                    try
                                    {
                                        if (dataSyncItem != null)
                                        {
                                            var startedAt = DateTime.Now;
                                            LevelLogger logger = Manager.CreateLogger(dataSyncItem);
                                            logger.AddKey(Utility.Constants.DataSyncItemId, dataSyncItem.ID.ToString());
                                            logger.AddData(Utility.Constants.DataSyncItem, dataSyncItem);
                                            logger.AddData(Logger.Log.Constants.Identifier, dataSyncItem.Name);
                                            var dataSyncManager = new DataSyncManager();
                                            var dataSyncObject = dataSyncManager.InstantiateDataMap(dataSyncItem, ref logger) as XmlDataMap;
                                            if (dataSyncObject != null)
                                            {
                                                dataSyncObject.DataSourceCache = dataSourceCache;
                                                dataSyncObject.Process();
                                            }
                                            var finishededAt = DateTime.Now;
                                            logger.AddData(Logger.Log.Constants.StartTime, startedAt);
                                            logger.AddData(Logger.Log.Constants.EndTime, finishededAt);
                                            var exporter = Manager.CreateOutputHandler(dataSyncItem, logger);
                                            var logText = exporter.Export();
                                            if (exporter != null)
                                            {
                                                if (logger != null)
                                                {
                                                    try
                                                    {
                                                        MailManager.SendLogReport(ref logger, exporter);
                                                    }
                                                    catch (Exception exception)
                                                    {
                                                        Diagnostics.Log.Error(
                                                            "Failed in sending out the mail. Please see the exception message for more details. Exception:" +
                                                            exception.Message + "\r\n\r\n" + logText, typeof(DataSyncTask));
                                                    }
                                                    if (logger.HasErrors())
                                                    {
                                                        Diagnostics.Log.Error(logText, typeof (DataSyncTask));
                                                    }
                                                    else
                                                    {
                                                        Diagnostics.Log.Debug(logText, typeof(DataSyncTask));
                                                    }
                                                }
                                                else
                                                {
                                                    Diagnostics.Log.Error("The Log object was null. This should not happen." + "\r\n\r\n" + logText, typeof(DataSyncTask));
                                                }
                                            }
                                            else
                                            {
                                                Diagnostics.Log.Error("The Exporter class was null. Therefor the log was not written out.\r\n\r\n" + logText, typeof(DataSyncTask));
                                            }
                                        }
                                        else
                                        {
                                            Diagnostics.Log.Error("The Task item had Items defined in Items[] that was null. This should not happen.", typeof (DataSyncTask));
                                        }
                                    }
                                    catch (Exception exception)
                                    {
                                        var itemId = dataSyncItem != null ? dataSyncItem.ID.ToString() : string.Empty;
                                        Diagnostics.Log.Error(
                                            Identifier +
                                            String.Format(
                                                " - An exception occured in the execution of the task in the foreach (Item dataSyncItem in itemArray) of the DataSync item: {0}. This datasync job wasn't completed. Exception: {1}",
                                                itemId, exception.Message), typeof (DataSyncTask));
                                    }
                                }
                                else
                                {
                                    Diagnostics.Log.Error(
                                    Identifier +
                                    " - The provided value wasn't a correct Sitecore id. Please add at least one id to 'Items' field of the ScheduledItem. You can also use | to seperate ids. Therefor nothing was done.",
                                    typeof(DataSyncTask));
                                }
                            }
                        }
                        else
                        {
                            Diagnostics.Log.Error(
                                Identifier +
                                " - There wasn't defined any DataSync items to run. Please add at least one id to 'Items' field of the ScheduledItem. You can also use | to seperate ids. Therefor nothing was done.",
                                typeof (DataSyncTask));
                        }
                    }
                    else
                    {
                        Diagnostics.Log.Error(
                            Identifier + " - There wasn't defined any DataSync items to run. Therefor nothing was done.",
                            typeof (DataSyncTask));
                    }
                }
                else
                {
                    Diagnostics.Log.Error(
                            Identifier + " - The ScheduledItem was null. Therefor nothing was done.",
                            typeof(DataSyncTask));
                }
            }
            catch (Exception exception)
            {
                Diagnostics.Log.Error(Identifier + " - An exception occured in the execution of the task.", exception);
            }
        }
    }
}