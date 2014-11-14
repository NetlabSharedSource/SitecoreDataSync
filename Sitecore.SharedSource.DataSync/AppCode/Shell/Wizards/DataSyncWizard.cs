using System.Globalization;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.Logger.Log.Output;
using Sitecore.SharedSource.DataSync.Managers;
using Sitecore.SharedSource.DataSync.Providers;
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
    public class DataSyncWizard : WizardForm
    {
        // Fields
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
            if (!Context.ClientPage.IsEvent)
            {
            }
        }

        protected override void ActivePageChanged(string page, string oldPage)
        {
            Assert.ArgumentNotNull(page, "page");
            Assert.ArgumentNotNull(oldPage, "oldPage");

            NextButton.Header = "Next >";
            if (page == "StartImport")
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
            if (page == "Retry")
            {
                newpage = "StartImport";
            }

            if (newpage == "Importing")
            {

            }
            return base.ActivePageChanging(page, ref newpage);
        }

        protected void ShowResult()
        {
            this.ShowResultPane.Visible = false;
            this.ResultText.Visible = true;
            this.ResultLabel.Visible = true;
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
        
        #endregion
        
        protected void StartImport()
        {
            var dataSyncItem = GetDataSyncItem();

            var dataSyncManager = new DataSyncManager();
            LevelLogger logger = Manager.CreateLogger(dataSyncItem);
            OutputHandlerBase exporter = Manager.CreateOutputHandler(dataSyncItem, logger);
            logger.AddKey(Utility.Constants.DataSyncItemId, dataSyncItem.ID.ToString());
            logger.AddData(Utility.Constants.DataSyncItem, dataSyncItem);
            var map = dataSyncManager.InstantiateDataMap(dataSyncItem, ref logger);
            if (map != null)
            {
                var options = new JobOptions("DataSyncWizard", "Job category name", Context.Site.Name,
                                             new DataSyncWizard(), "Run", new object[] {map, map.Logger, exporter});
                var job = JobManager.Start(options);
                job.Options.CustomData = map.Logger;
                JobHandle = job.Handle.ToString();
                SheerResponse.Timer("CheckStatus", 5);
            }
            else
            {
                Active = "LastPage";
                BackButton.Disabled = true;
                if (logger.HasErrorsOrInfos())
                {
                    if (exporter != null)
                    {
                        ResultText.Value = exporter.Export();
                    }
                    else
                    {
                        ResultText.Value = "The Exporter class was null. Therefor the log was not written out.";
                    }
                }
            }
        }

        protected void Run(BaseDataMap map, LevelLogger logger, OutputHandlerBase exporter)
        {
            Context.Job.Status.State = JobState.Running;
            logger.AddKey(Utility.Constants.DataSyncItemId, map.ImportItem.ID.ToString());
            logger.AddData(Utility.Constants.DataSyncItem, map.ImportItem);
            logger.AddData(Logger.Log.Constants.Identifier, map.ImportItem.Name);
            var startedAt = DateTime.Now;
            logger = map.Process();
            var finishededAt = DateTime.Now;
            logger.AddData(Logger.Log.Constants.StartTime, startedAt);
            logger.AddData(Logger.Log.Constants.EndTime, finishededAt);
            try
            {
                MailManager.SendLogReport(ref logger, exporter);
            }
            catch (Exception exception)
            {
                Diagnostics.Log.Error(exporter.GetIdentifier() + " failed in sending out the mail. Please see the exception message for more details. Exception:" + exception.Message + ". Status:\r\n" +
                    exporter.Export(), typeof(DataSyncWizard));
            }
            if (logger.HasErrors())
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
                var logger = (LevelLogger)job.Options.CustomData;
                var dataSyncItem = logger.GetData(Utility.Constants.DataSyncItem) as Item;

                if (status == null)
                {
                    throw new Exception("The sync process was unexpectedly interrupted.");
                }
                if (state == JobState.Running)
                {
                    NextButton.Disabled = true;
                    BackButton.Disabled = false;
                    CancelButton.Disabled = false;
                    if (logger.GetCounter(IncrementConstants.TotalNumberOfItems) > 0)
                    {
                        ImportStatusText.Text = logger.GetCounter(IncrementConstants.ProcessedItems) + " of " + logger.GetCounter(IncrementConstants.TotalNumberOfItems) + " items processed.";
                    }
                    else
                    {
                        ImportStatusText.Text = "Retrieving import data...";
                    }
                    if (logger.GetCounter(IncrementConstants.ProcessedItems) == logger.GetCounter(IncrementConstants.TotalNumberOfItems) && logger.GetCounter(IncrementConstants.ProcessedItems) != 0)
                    {
                        NotPresentStatusText.Text = "Processing items not present in import...";
                    }
                    FailedText.Visible = logger.GetCounter(IncrementConstants.FailureItems) > 0;
                    FailedStatusText.Text = logger.GetCounter(IncrementConstants.FailureItems) + " items failed.";
                }
                else if (state == JobState.Finished)
                {
                    Status.Text = String.Format("Items processed: {0}.", logger.GetCounter(IncrementConstants.ProcessedItems).ToString(CultureInfo.CurrentCulture));
                    Active = "LastPage";
                    BackButton.Disabled = true;
                    var exporter = Manager.CreateOutputHandler(dataSyncItem, logger);
                    string str2 = exporter.Export();
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