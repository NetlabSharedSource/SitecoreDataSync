using System;
using System.Linq;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Managers;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.Tasks;

namespace Sitecore.SharedSource.DataSync.ScheduledTasks
{
    public class DataSyncTask
    {
        private const string Identifier = "DataSyncTask.RunJob";

        public void RunJob(Item[] itemArray, CommandItem commandItem, ScheduleItem scheduledItem)
        {
            try
            {
                if (scheduledItem != null)
                {
                    var itemIds = scheduledItem["Items"];
                    if (!String.IsNullOrEmpty(itemIds))
                    {
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
                                            var startedAt = DateTime.Now.ToLongDateString();
                                            Logging logBuilder = new Logging();
                                            var dataSyncManager = new DataSyncManager();
                                            dataSyncManager.RunDataSyncJob(dataSyncItem, ref logBuilder);
                                            var finishededAt = DateTime.Now.ToLongDateString();
                                            if (logBuilder != null)
                                            {
                                                try
                                                {
                                                    MailManager.SendLogReport(ref logBuilder,
                                                                              GetDataSyncIdentifier(dataSyncItem),
                                                                              dataSyncItem);
                                                }
                                                catch (Exception exception)
                                                {
                                                    Diagnostics.Log.Error(
                                                        GetIdentifierText(dataSyncItem, startedAt, finishededAt) +
                                                        " failed in sending out the mail. Please see the exception message for more details. Exception:" + exception.Message + ". Status:\r\n" +
                                                        logBuilder.GetStatusText(), typeof(DataSyncTask));
                                                }
                                                if (logBuilder.LogBuilder != null)
                                                {
                                                    if (!String.IsNullOrEmpty(logBuilder.LogBuilder.ToString()))
                                                    {
                                                        Diagnostics.Log.Error(
                                                            GetIdentifierText(dataSyncItem, startedAt, finishededAt) +
                                                            " failed. " +
                                                            logBuilder.LogBuilder + "\r\nStatus:\r\n" +
                                                            logBuilder.GetStatusText(),
                                                            typeof (DataSyncTask));
                                                    }
                                                    else
                                                    {
                                                        Diagnostics.Log.Debug(
                                                            GetIdentifierText(dataSyncItem, startedAt, finishededAt) +
                                                            " completed with success.\r\nStatus:\r\n" +
                                                            logBuilder.GetStatusText(),
                                                            typeof (DataSyncTask));
                                                    }
                                                }
                                                else
                                                {
                                                    Diagnostics.Log.Error(
                                                           GetIdentifierText(dataSyncItem, startedAt, finishededAt) +
                                                           " failed. The Logging.LogBuilder object was null. " +
                                                           logBuilder + "\r\nStatus:\r\n" +
                                                           logBuilder.GetStatusText(),
                                                           typeof(DataSyncTask));
                                                }
                                            }
                                            else
                                            {
                                                Diagnostics.Log.Error(
                                                    GetIdentifierText(dataSyncItem, startedAt, finishededAt) +
                                                    " - The Log object was null. This should not happen.",
                                                    typeof (DataSyncTask));
                                            }
                                        }
                                        else
                                        {
                                            Diagnostics.Log.Error(
                                                " - The Task item had Items defined in Items[] that was null. This should not happen.",
                                                typeof (DataSyncTask));
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

        private string GetIdentifierText(Item dataSyncItem, string startedAt, string finishedAt)
        {
            return GetDataSyncIdentifier(dataSyncItem) + " started " + startedAt + " and finished " + finishedAt;
        }

        private string GetDataSyncIdentifier(Item dataSyncItem)
        {
            if (dataSyncItem != null)
            {
                return dataSyncItem.Name;
            }
            return String.Empty;
        }
    }
}