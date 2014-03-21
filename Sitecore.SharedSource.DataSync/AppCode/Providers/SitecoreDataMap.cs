using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Globalization;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.DataSync.Extensions;
using Sitecore.Data.Items;
using Sitecore.Data;
using Sitecore.SharedSource.DataSync.Mappings.Properties;
using Sitecore.SharedSource.DataSync.Mappings;
using Sitecore.SharedSource.DataSync.Utility;
using Sitecore.Collections;
using System.IO;
using Sitecore.Data.Fields;

namespace Sitecore.SharedSource.DataSync.Providers
{
	public class SitecoreDataMap : BaseDataMap {
		
		#region Properties
		
        /// <summary>
        /// List of properties
        /// </summary>
		public List<IBaseProperty> PropertyDefinitions {
			get {
				return _propDefinitions;
			}
			set {
				_propDefinitions = value;
			}
		}
		private List<IBaseProperty> _propDefinitions = new List<IBaseProperty>();
		
		#endregion Properties

		#region Constructor

        public SitecoreDataMap(Database db, Item importItem, Logging logging)
            : base(db, importItem, logging)
        {

            //deal with sitecore properties if any
            Item Props = GetItemByTemplate(importItem, Utility.Constants.PropertiesFolderID);
            if (Props.IsNotNull()) {
                ChildList c = Props.GetChildren();
                if (c.Any()) {
                    foreach (Item child in c) {
                        //create an item to get the class / assembly name from
                        BaseMapping bm = new BaseMapping(child);
                        if (!string.IsNullOrEmpty(bm.HandlerAssembly)) {
                            if (!string.IsNullOrEmpty(bm.HandlerClass)) {
                                //create the object from the class and cast as base field to add it to field definitions
                                IBaseProperty bp = null;
                                try {
                                    bp = (IBaseProperty)Sitecore.Reflection.ReflectionUtil.CreateObject(bm.HandlerAssembly, bm.HandlerClass, new object[] { child });
                                } catch (FileNotFoundException fnfe) {
                                    LogBuilder.Log("Error", string.Format("the property:{0} binary {1} specified could not be found", child.Name, bm.HandlerAssembly));
                                }
                                if (bp != null)
                                    PropertyDefinitions.Add(bp);
                                else
                                    LogBuilder.Log("Error", string.Format("the property: '{0}' class type {1} could not be instantiated", child.Name, bm.HandlerClass));
                            } else {
                                LogBuilder.Log("Error", string.Format("the property: '{0}' Handler Class {1} is not defined", child.Name, bm.HandlerClass));
                            }
                        } else {
                            LogBuilder.Log("Error", string.Format("the property: '{0}' Handler Assembly {1} is not defined", child.Name, bm.HandlerAssembly));
                        }
                    }
                } else {
                    LogBuilder.Log("Warn", "there are no properties to import");
                }
            } 
		}

		#endregion Constructor

        #region Override Methods

        /// <summary>
        /// uses the sitecore database and xpath query to retrieve data
        /// </summary>
        /// <returns></returns>
        public override IList<object> GetImportData()
        {
            using (new LanguageSwitcher(ImportToLanguageVersion))
            {
                if (String.IsNullOrEmpty(DataSourceString))
                {
                    LogBuilder.Log("Error", "The DataSourceString was null or empty. Please provide a connectionstring for the Sitecore database.");
                    return null;
                }
                var database = Configuration.Factory.GetDatabase(DataSourceString);
                if (database == null)
                {
                    LogBuilder.Log("Error", string.Format("The retrieved database from connectionstring: '{0}' was not found. Please verify the database connection", DataSourceString));
                    return null;
                }
                return database.SelectItems(StringUtility.CleanXPath(Query));
            }
        }

        /// <summary>
        /// deals with the sitecore properties
        /// </summary>
        /// <param name="newItem"></param>
        /// <param name="importRow"></param>
        public override bool ProcessCustomData(ref Item newItem, object importRow, out bool processedCustomData)
        {
            processedCustomData = false;
            Item row = importRow as Item;
            //add in the property mappings
            foreach (IBaseProperty d in this.PropertyDefinitions)
                d.FillField(this, ref newItem, row);
            return true;
        }
        
        /// <summary>
        /// gets a field value from an item
        /// </summary>
        /// <param name="importRow"></param>
        /// <param name="fieldName"></param>
        /// <returns></returns>
        public override string GetFieldValue(object importRow, string fieldName, ref string errorMessage)
        {
            Item item = importRow as Item;
            if (item != null)
            {
                if (!String.IsNullOrEmpty(fieldName))
                {
                    Field f = item.Fields[fieldName];
                    if (f != null)
                    {
                        return item[fieldName];
                    }
                    errorMessage += String.Format("The GetFieldValue method failed because the the 'fieldName' didn't result in any field on the item. ImportRow: {0}.", GetImportRowDebugInfo(importRow));
                }
                else
                {
                    errorMessage += String.Format("The GetFieldValue method failed because the the 'fieldName' was null or empty. ImportRow: {0}.", GetImportRowDebugInfo(importRow));
                }
            }
            else
            {
                errorMessage += String.Format(
                                   "The GetFieldValue method failed because the Import Row was null. FieldName: {0}.",
                                   fieldName);
            }
            return String.Empty;
        }

	    public override string GetImportRowDebugInfo(object importRow)
	    {
            Item item = importRow as Item;
	        if (item != null)
	        {
	            return item.ID.ToString();
	        }
	        return importRow.ToString();
	    }

	    #endregion Override Methods

        #region Methods

        #endregion Methods
	}
}
