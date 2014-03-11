using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Sitecore.IO;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.DataSync.Managers;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.DataSync.ScheduledTasks;
using Sitecore.Web.UI.Pages;
using Sitecore.Diagnostics;
using System;
using Sitecore.Web.UI.Sheer;
using Sitecore.Web.UI.HtmlControls;
using Sitecore.Globalization;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Jobs;
using System.Web;

namespace Sitecore.SharedSource.DataSync.Shell.Wizards
{
    public class FromFileDataSyncWizard : WizardForm
    {
        // Fields
        protected Edit Filename;
        private List<string> fieldList;
        private const string Failure = "Failure";
        private const string Success = "Success";
        private string TextJobHandle = "JobHandle";
        const string TextUserSyncDataValue = "DataSyncDataValue";
        protected Memo ErrorText;
        protected Literal ImportStatusText;
        protected Literal FailedStatusText;
        protected Border FailedText;
        protected Literal NotPresentStatusText;
        protected Border NotPresentText;
        protected Radiobutton IncrementalPublish;
        protected Border IncrementalPublishPane;
        protected Border Languages;
        protected Groupbox LanguagesPanel;
        protected Border NoPublishingTarget;
        protected Checkbox PublishChildren;
        protected Border PublishChildrenPane;
        protected Groupbox PublishingPanel;
        protected Border PublishingTargets;
        protected Groupbox PublishingTargetsPanel;
        protected Border PublishingText;
        protected Radiobutton Republish;
        protected Border ResultLabel;
        protected Memo ResultText;
        protected Scrollbox SettingsPane;
        protected Border ShowResultPane;
        protected Literal Status;
        protected Literal Welcome;
        protected Memo Data;

        protected List<string> FieldList
        {
            get
            {
                if (fieldList == null)
                    fieldList = (List<string>)ServerProperties["FieldsList"];
                return fieldList;
            }
            set
            {
                ServerProperties["FieldsList"] = value;
                fieldList = value;
            }
        }

        protected string DataValue
        {
            get
            {
                return (string)ServerProperties["DataValue"];
            }
            set
            {
                ServerProperties["DataValue"] = value;
            }
        }
        
        private string JobHandle
        {
            get
            {
                return Sitecore.StringUtil.GetString(ServerProperties[TextJobHandle]);
            }
            set
            {
                Assert.ArgumentNotNullOrEmpty(value, "value");
                ServerProperties[TextJobHandle] = value;
            }
        }
        
        // Events
        protected override void OnLoad(EventArgs e)
        {
            Assert.ArgumentNotNull(e, "e");
            base.OnLoad(e);
        }

        protected override void ActivePageChanged(string page, string oldPage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(oldPage, "oldPage");

            NextButton.Header = "Next >";
            if (page == "SelectFile")
            {
                NextButton.Header = "Import >";
            }
            base.ActivePageChanged(page, oldPage);
            if (page == "Importing")
            {
                NextButton.Disabled = true;
                BackButton.Disabled = true;
                CancelButton.Disabled = true;
                SheerResponse.SetInnerHtml("PublishingText", Translate.Text("Importing..."));
                SheerResponse.SetInnerHtml("PublishingTarget", "&nbsp;");
                SheerResponse.Timer("StartImport", 10);
            }
        }

        protected override bool ActivePageChanging(string page, ref string newpage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(newpage, "newpage");
            if (page == "SelectFile")
            {
                if (newpage.Equals("Importing") && !SelectFileCompleted())
                    return false;
            }
            if (page == "Retry")
            {
                newpage = "SelectFile";
            }

            if (newpage == "Importing")
            {

            }
            return base.ActivePageChanging(page, ref newpage);
        }

        protected void ShowResult()
        {
            ShowResultPane.Visible = false;
            ResultText.Visible = true;
            ResultLabel.Visible = true;
        }


        #region Helper methods

        private Item GetDataSyncItem()
        {
            string id = HttpContext.Current.Request.QueryString["id"];
            if (ID.IsID(id))
            {
                return Configuration.Factory.GetDatabase("master").GetItem(new ID(id));
            }
            return null;
        }

        private string GetStatusText(Item userSyncItem, Logging logBuilder, string startedAt, string finishedAt)
        {
            if (logBuilder != null)
            {
                if (logBuilder.LogBuilder != null)
                {
                    var logString = logBuilder.LogBuilder.ToString();
                    if (String.IsNullOrEmpty(logString))
                    {
                        logString += "The import completed successfully.\r\n\r\nStatus:\r\n" +
                                     logBuilder.GetStatusText();
                    }
                    else
                    {
                        logString = "The import failed.\r\n\r\nStatus:\r\n" + logBuilder.GetStatusText() + "\r\n\r\n" +
                                    logString;
                    }
                    return logString;
                }
                return GetIdentifierText(userSyncItem, startedAt, finishedAt) + " failed. The Logging.LogBuilder object was null. " + logBuilder + "\r\nStatus:\r\n" + logBuilder.GetStatusText();
            }
            return GetIdentifierText(userSyncItem, startedAt, finishedAt) + " - The Log object was null. This should not happen.";
        }

        private string GetIdentifierText(Item userSyncItem, string startedAt, string finishedAt)
        {
            return GetDataSyncIdentifier(userSyncItem) + " started " + startedAt + " and finished " + finishedAt;
        }

        private string GetDataSyncIdentifier(Item dataSyncItem)
        {
            if (dataSyncItem != null)
            {
                return dataSyncItem.Name;
            }
            return String.Empty;
        }

        #endregion

        protected bool SelectFileCompleted()
        {
            if (string.IsNullOrEmpty(Filename.Value))
            {
                SheerResponse.Alert("You must select a file to import of type .xml", new string[0]);
                return false;
            }
            else
            {
                string filename = FileUtil.MapPath("/temp/" + FileUtil.GetFileName(Filename.Value));
                if (!FileUtil.FileExists(filename))
                {
                    SheerResponse.Alert(String.Format("The file with filename '{0}' does not exist. Please select another file.", filename), new string[0]);
                    return false;
                }
                else
                {
                    using (var streamreader = new StreamReader(filename))
                    {
                        DataValue = streamreader.ReadToEnd();
                    }
                    return true;
                }
            }
        }

        protected void StartImport()
        {
            var dataSyncItem = GetDataSyncItem();

            var dataSyncManager = new DataSyncManager();
            string errorMessage = String.Empty;
            var map = dataSyncManager.InstantiateDataMap(dataSyncItem, ref errorMessage);
            if (map != null)
            {
                if (!String.IsNullOrEmpty(DataValue))
                {
                    map.Data = DataValue;
                }
                var options = new JobOptions("FromFileDataSyncWizard", "Job category name", Context.Site.Name, new FromFileDataSyncWizard(), "Run", new object[] { map, map.LogBuilder });
                var job = JobManager.Start(options);
                job.Options.CustomData = map.LogBuilder;
                JobHandle = job.Handle.ToString();
                SheerResponse.Timer("CheckStatus", 5);
            }
            else
            {
                Active = "LastPage";
                BackButton.Disabled = true;
                if (!string.IsNullOrEmpty(errorMessage))
                {
                    ResultText.Value = errorMessage;
                }
            }
        }
        
        protected void Run(BaseDataMap map, Logging logBuilder)
        {
            Context.Job.Status.State = JobState.Running;

            var startedAt = DateTime.Now.ToLongDateString();
            map.Process();
            var finishededAt = DateTime.Now.ToLongDateString();
            try
            {
                MailManager.SendLogReport(ref logBuilder, 
                                          GetDataSyncIdentifier(map.ImportItem),
                                          map.ImportItem);
            }
            catch (Exception exception)
            {
                Sitecore.Diagnostics.Log.Error(
                    GetIdentifierText(map.ImportItem, startedAt, finishededAt) +
                    " failed in sending out the mail. Please see the exception message for more details. Exception:" + exception.Message + ". Status:\r\n" +
                    logBuilder.GetStatusText(), typeof(DataSyncTask));
            }
            var logString = logBuilder.LogBuilder.ToString();
            if (!String.IsNullOrEmpty(logString))
            {
                Context.Job.Status.Failed = true;
            }
            else
            {
                Context.Job.Status.State = JobState.Finished;    
            }   
        }

        public void CheckStatus()
        {
            var job = JobManager.GetJob(Handle.Parse(JobHandle));
            if (job != null)
            {
                var status = job.Status;
                var state = status.State;
                var logBuilder = (Logging)job.Options.CustomData;

                if (status == null)
                {
                    throw new Exception("The sync process was unexpectedly interrupted.");
                }
                if (state == JobState.Running)
                {
                    NextButton.Disabled = true;
                    BackButton.Disabled = false;
                    CancelButton.Disabled = false;
                    if (logBuilder.TotalNumberOfItems > 0)
                    {
                        ImportStatusText.Text = logBuilder.ProcessedItems + " of " + logBuilder.TotalNumberOfItems + " items processed.";
                    }
                    else
                    {
                        ImportStatusText.Text = "Retrieving import data...";
                    }
                    if (logBuilder.ProcessedItems == logBuilder.TotalNumberOfItems && logBuilder.ProcessedItems != 0)
                    {
                        NotPresentStatusText.Text = "Processing items not present in import...";

                        if (logBuilder.TotalNumberOfNotPresentInImportItems > 0)
                        {
                            NotPresentStatusText.Text = logBuilder.DisabledItems + " of " +
                                                        logBuilder.TotalNumberOfNotPresentInImportItems +
                                                        " items not present in import processed.";
                        }
                    }
                    FailedText.Visible = logBuilder.FailureItems > 0;
                    FailedStatusText.Text = logBuilder.FailureItems + " items failed.";
                }
                else if (state == JobState.Finished)
                {
                    Status.Text = String.Format("Items processed: {0}.", logBuilder.ProcessedItems.ToString(CultureInfo.CurrentCulture));
                    Active = "LastPage";
                    BackButton.Disabled = true;
                    string str2 = GetStatusText(GetDataSyncItem(), logBuilder, String.Empty, string.Empty);
                    if (!string.IsNullOrEmpty(str2))
                    {
                        ResultText.Value = str2;
                    }
                    return;
                }
                SheerResponse.Timer("CheckStatus", 5);
            }
        }
    }
}