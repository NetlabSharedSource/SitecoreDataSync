using System;
using System.Linq;
using System.Threading;
using System.Timers;
using System.Web.UI;
using System.Web.UI.WebControls;
using BackPack.Modules.AppCode.Import.Utility;
using Sitecore.Data.Items;
using System.Collections.Generic;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.Logger.Log.Output;
using Sitecore.SharedSource.DataSync.Managers;
using Sitecore.SharedSource.DataSync.Utility;
using Sitecore.SharedSource.DataSync.Providers;
using System.Text;
using System.IO;
using Constants = Sitecore.SharedSource.DataSync.Utility.Constants;

namespace sitecore_modules.Shell.BackPack_Modules.Import
{
    public partial class DataSyncEditor : Page
    {
        protected Database currentDB;
        private DataSyncJob dataSyncJob;
        private Thread worker;
        
        protected void Page_Load(object sender, EventArgs e)
        {
            currentDB = Sitecore.Configuration.Factory.GetDatabase("master");

            if (!IsPostBack)
            {
                PopulateImportDDL();
            }
        }

        protected void PopulateImportDDL()
        {
            List<string> importTypes = new List<string>();
            Item importNode = currentDB.GetItem(new ID(Constants.ImportRootID));
            IEnumerable<Item> imports = (from i in importNode.Axes.GetDescendants()
                                         where SitecoreUtil.DoesItemImplementTemplate(i, Constants.BaseImportMapID)
                                         select i).ToList();
            ddlImport.Items.Clear();

            if (imports.Any())
            {
                foreach (Item i in imports)
                {
                    ddlImport.Items.Add(new ListItem(i.DisplayName, i.ID.ToString()));
                }
            }
        }

        protected void btnRefresh_Click(object sender, EventArgs e)
        {
            txtMessage.Text = String.Empty;
            PopulateImportDDL();
        }

        //protected void Timer1_Tick(object sender, EventArgs e)
        //{
        //    litStatus.Text = "Panel refreshed at: " + DateTime.Now.ToLongTimeString();
        //    if (dataSyncJob != null)
        //    {
        //        if (dataSyncJob.logBuilder != null)
        //        {
        //            var processedItems = dataSyncJob.logBuilder.ProcessedItems;
        //            litStatus.Text += processedItems + "";
        //        }
        //    }
        //}

        //public delegate void LongTimeTask_Delegate(string str);

        //public void LongTimeTask(string str)
        //{
        //    Item dataSyncItem = currentDB.Items[ddlImport.SelectedValue];
        //    if (dataSyncItem != null)
        //    {
        //        Logging logBuilder = new Logging();
        //        DataSyncManager.RunDataSyncJob(dataSyncItem, ref logBuilder);

        //        if (logBuilder != null)
        //        {
        //            txtMessage.Text = logBuilder.LogBuilder.Length < 1 ? "The import completed successfully. \r\nStatus: \r\n" + logBuilder.GetItemStatusText() : logBuilder.LogBuilder + "\r\nStatus: \r\n" + logBuilder.GetItemStatusText();
        //        }
        //        else
        //        {
        //            txtMessage.Text = "The log object was null.";
        //        }
        //    }
        //}

        //public void TaskCompleted(IAsyncResult r)
        //{
        //    litStatus.Text = "Job done: " + DateTime.Now.ToLongTimeString();
        //    this.updatePanelStatus.Update();
        //}

        //protected void btnImport_Click(object sender, EventArgs e)
        //{
        //    //LongTimeTask_Delegate d = new LongTimeTask_Delegate(LongTimeTask);
        //    //IAsyncResult r = d.BeginInvoke("String", new AsyncCallback(TaskCompleted), null);
        //    //d.EndInvoke(r);

        //}

        protected void btnImport_Click(object sender, EventArgs e)
        {
            Item dataSyncItem = currentDB.Items[ddlImport.SelectedValue];
            if (dataSyncItem != null)
            {
                LevelLogger logger = Manager.CreateLogger(dataSyncItem);
                var dataSyncManager = new DataSyncManager();
                dataSyncManager.RunDataSyncJob(dataSyncItem, ref logger);

                if (logger != null)
                {
                    var exporter = Manager.CreateOutputHandler(dataSyncItem, logger);
                    if (exporter != null)
                    {
                        txtMessage.Text = exporter.Export();
                    }
                    else
                    {
                        txtMessage.Text = "The Exporter class was null. Therefor the log was not written out.";
                    }
                }
                else
                {
                    txtMessage.Text = "The log object was null.";
                }
            }
        }
        
        protected void ddlImport_SelectedIndexChanged(object sender, EventArgs e)
        {
            txtMessage.Text = String.Empty;
        }
    }

    public class DataSyncJob
    {
        protected Database currentDB;
        protected string dataSyncItemId = "";
        public LevelLogger logger;

        public DataSyncJob(string dataSyncItemId)
        {
            currentDB = Sitecore.Configuration.Factory.GetDatabase("master");
            this.dataSyncItemId = dataSyncItemId;
        }

        public void Work()
        {
            Item dataSyncItem = currentDB.Items[dataSyncItemId];
            logger = Manager.CreateLogger(dataSyncItem);

            if (dataSyncItem != null)
            {
                var dataSyncManager = new DataSyncManager();
                dataSyncManager.RunDataSyncJob(dataSyncItem, ref logger);

                //if (logBuilder != null)
                //{
                //    txtMessage.Text = logBuilder.LogBuilder.Length < 1 ? "The import completed successfully. \r\nStatus: \r\n" + logBuilder.GetItemStatusText() : logBuilder.LogBuilder + "\r\nStatus: \r\n" + logBuilder.GetItemStatusText();
                //}
                //else
                //{
                //    txtMessage.Text = "The log object was null.";
                //}
            }
        }
    }
}