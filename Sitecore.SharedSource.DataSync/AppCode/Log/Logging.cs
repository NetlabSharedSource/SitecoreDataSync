using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace Sitecore.SharedSource.DataSync.Log
{
    public class Logging
    {
        public int CreatedItems { get; set; }
        
        public int MovedItems { get; set; }

        public int ChangedTemplateItems { get; set; }

        public int RenamedItems { get; set; }

        public int ItemNameRegeneratedItems { get; set; }

        public int DisabledItems { get; set; }

        public int FailedDisabledItems { get; set; }

        public int FailureItems { get; set; }

        public int ProcessedItems { get; set; }

        public int MovedFromDisabledItems { get; set; }

        public int LanguageVersionAddedItems { get; set; }

        public int UpdatedFields { get; set; }

        public int SucceededItems { get; set; }

        public int ProcessedCustomDataItems { get; set; }

        public int TotalNumberOfItems { get; set; }

        public int TotalNumberOfNotPresentInImportItems { get; set; }

        public Logging()
        {
            LogBuilder = new StringBuilder();
            ProcessedItems = 0;
            CreatedItems = 0;
            MovedItems = 0;
            ChangedTemplateItems = 0;
            RenamedItems = 0;
            ItemNameRegeneratedItems = 0;
            DisabledItems = 0;
            FailedDisabledItems = 0;
            FailureItems = 0;
            MovedFromDisabledItems = 0;
            LanguageVersionAddedItems = 0;
            UpdatedFields = 0;
            SucceededItems = 0;
            ProcessedCustomDataItems = 0;
            TotalNumberOfItems = 0;
            TotalNumberOfNotPresentInImportItems = 0;
        }

        /// <summary>
        /// the log is returned with any messages indicating the status of the import
        /// </summary>
        protected StringBuilder logBuilder;

        public StringBuilder LogBuilder
        {
            get { return logBuilder; }
            set { logBuilder = value; }
        }

        public void Log(string errorType, string message)
        {
            string logText = String.Format("{0} : {1}", errorType, message);
            LogBuilder.AppendFormat("{0} : {1}", errorType, message).AppendLine().AppendLine();
            Diagnostics.Log.Error("DataSync --- " + logText, this);
        }

        //public void Log(string message)
        //{
        //    string logText = String.Format("{0}", message);
        //    LogBuilder.AppendFormat("{0}", message).AppendLine().AppendLine();
        //    Diagnostics.Log.Error(logText, this);
        //}

        public string GetStatusText()
        {
            var statusText = String.Empty;
            statusText += WriteLine("ProcessedItems", ProcessedItems);
            statusText += WriteLine("CreatedItems", CreatedItems);
            statusText += WriteLine("MovedItems", MovedItems);
            statusText += WriteLine("ChangedTemplateItems", ChangedTemplateItems);
            statusText += WriteLine("RenamedItems", RenamedItems);
            statusText += WriteLine("ItemNameRegeneratedItems", ItemNameRegeneratedItems);
            statusText += WriteLine("DisabledItems", DisabledItems);
            statusText += WriteLine("FailedDisabledItems", FailedDisabledItems);
            statusText += WriteLine("FailureItems", FailureItems);
            statusText += WriteLine("MovedFromDisabledItems", MovedFromDisabledItems);
            statusText += WriteLine("LanguageVersionAddedItems", LanguageVersionAddedItems);
            statusText += WriteLine("UpdatedFields", UpdatedFields);
            statusText += WriteLine("SucceededItems", SucceededItems);
            statusText += WriteLine("ProcessedCustomDataItems", ProcessedCustomDataItems);
            return statusText;
        }

        private string WriteLine(string type, int itemCount)
        {
            if (itemCount != 0)
            {
                return type + ": " + itemCount + "\r\n";
            }
            return String.Empty;
        }
    }
}
