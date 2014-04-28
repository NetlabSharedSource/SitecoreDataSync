using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using Sitecore.Data.Items;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.SharedSource.DataSync.Log;
using Sitecore.SharedSource.DataSync.Mappings.Fields;
using Sitecore.SharedSource.DataSync.Extensions;
using Sitecore.SharedSource.DataSync.Mappings;
using Sitecore.SharedSource.DataSync.Utility;
using Sitecore.Collections;
using System.IO;
using Sitecore.Globalization;
using Sitecore.SecurityModel;

namespace Sitecore.SharedSource.DataSync.Providers
{
    /// <summary>
    /// The BaseDataMap is the base class for any data provider. It manages values stored in sitecore 
    /// and does the bulk of the work processing the fields
    /// </summary>
	public abstract class BaseDataMap
    {
        private const int DefaultMinimumNumberOfRowsRequiredToStartImport = 10;
        private const int DefaultNumberOfRowsToProcessBeforeLogStatus = 10;
        private const int DefaultSleepPeriodToRerunSqlExceptionInGetItemsByKey = 2000;
        private bool useFastQuery = true;

		#region Properties

        private string _Data;

        /// <summary>
        /// the query used to retrieve the data
        /// </summary>
        public string Data
        {
            get { return _Data; }
            set { _Data = value; }
        }

        public bool UseFastQuery
        {
            get { return useFastQuery; }
            set { useFastQuery = value; }
        }

        /// <summary>
        /// the log is returned with any messages indicating the status of the import
        /// </summary>
        protected Logging logBuilder;

        public Logging LogBuilder
        {
            get { return logBuilder; }
            set { logBuilder = value; }
        }

        private Item _Parent;
		/// <summary>
		/// the parent item where the new items will be imported into
		/// </summary>
        public Item Parent {
			get {
				return _Parent;
			}
			set {
				_Parent = value;
			}
		}

        private Dictionary<string, TemplateItem> _toWhatTemplates;

        public Dictionary<string, TemplateItem> ToWhatTemplates
        {
            get { return _toWhatTemplates; }
            set { _toWhatTemplates = value; }
        }

        private Database _SitecoreDB;
		/// <summary>
		/// the reference to the sitecore database that you'll import into and query from
		/// </summary>
        public Database SitecoreDB {
			get {
				return _SitecoreDB;
			}
			set {
				_SitecoreDB = value;
			}
		}
        
		private string _itemNameDataField;
		/// <summary>
		/// the sitecore field value of fields used to build the new item name
		/// </summary>
        public string ItemNameDataField {
			get {
				return _itemNameDataField;
			}
			set {
				_itemNameDataField = value;
			}
		}

        private string _itemTypeDataField;
        /// <summary>
        /// the import field name that defines the type of the item
        /// </summary>
        public string ItemTypeDataField
        {
            get
            {
                return _itemTypeDataField;
            }
            set
            {
                _itemTypeDataField = value;
            }
        }

        private Language _importToLanguageVersion;
        /// <summary>
        /// the import field name that defines the type of the item
        /// </summary>
        public Language ImportToLanguageVersion
        {
            get
            {
                return _importToLanguageVersion;
            }
            set
            {
                _importToLanguageVersion = value;
            }
        }

        private int _MinimumNumberOfRowsRequiredToRunTheImport;
        /// <summary>
        /// The minimum number of rows required to run the import
        /// </summary>
        public int MinimumNumberOfRowsRequiredToRunTheImport
        {
            get
            {
                return _MinimumNumberOfRowsRequiredToRunTheImport;
            }
            set
            {
                _MinimumNumberOfRowsRequiredToRunTheImport = value;
            }
        }

		private string[] _NameFields;
		/// <summary>
		/// the string array of fields used to build the new item name
		/// </summary>
        public string[] NameFields {
			get {
				if (_NameFields == null) {
					string[] comSplitr = { "," };
					_NameFields = ItemNameDataField.Split(comSplitr, StringSplitOptions.RemoveEmptyEntries);
				}
				return _NameFields;
			}
			set {
				_NameFields = value;
			}
		}

		private int _itemNameMaxLength;
		/// <summary>
		/// max length for item names
		/// </summary>
        public int ItemNameMaxLength {
			get {
				return _itemNameMaxLength;
			}
			set {
				_itemNameMaxLength = value;
			}
		}

		private List<IBaseField> _fieldDefinitions = new List<IBaseField>();
		/// <summary>
		/// the definitions of fields to import
		/// </summary>
        public List<IBaseField> FieldDefinitions {
			get {
				return _fieldDefinitions;
			}
			set {
				_fieldDefinitions = value;
			}
		}

		private bool _FolderByDate;
		/// <summary>
		/// tells whether or not to folder new items by a date
		/// </summary>
        public bool FolderByDate {
			get {
				return _FolderByDate;
			}
			set {
				_FolderByDate = value;
			}
		}

		private bool _FolderByName;
		/// <summary>
		/// tells whether or not to folder new items by first letter of their name
		/// </summary>
        public bool FolderByName {
			get {
				return _FolderByName;
			}
			set {
				_FolderByName = value;
			}
		}

		private string _DateField;
		/// <summary>
		/// the name of the field that stores a date to folder by
		/// </summary>
        public string DateField {
			get {
				return _DateField;
			}
			set {
				_DateField = value;
			}
		}

        private bool _FolderByParentHierarchy;
        /// <summary>
        /// Tells whether or not to folder new items by the provided parent hierarchy in the Import
        /// </summary>
        public bool FolderByParentHierarchy
        {
            get
            {
                return _FolderByParentHierarchy;
            }
            set
            {
                _FolderByParentHierarchy = value;
            }
        }

        private string _IdentifyParentByWhatFieldOnParent;
        /// <summary>
        /// Indicates which field on the parent Sitecore item to identify the parent by
        /// </summary>
        public string IdentifyParentByWhatFieldOnParent
        {
            get
            {
                return _IdentifyParentByWhatFieldOnParent;
            }
            set
            {
                _IdentifyParentByWhatFieldOnParent = value;
            }
        }

        private string _IdentifyParentByWhatFieldOnImportRow;
        /// <summary>
        /// Indicates which field on the Import Row to identify the parent by
        /// </summary>
        public string IdentifyParentByWhatFieldOnImportRow
        {
            get
            {
                return _IdentifyParentByWhatFieldOnImportRow;
            }
            set
            {
                _IdentifyParentByWhatFieldOnImportRow = value;
            }
        }

        private string _IdentifyRootLevelFoldersByWhatValue;
        /// <summary>
        /// Indicates which field on the Import Row to identify the parent by
        /// </summary>
        public string IdentifyRootLevelFoldersByWhatValue
        {
            get
            {
                return _IdentifyRootLevelFoldersByWhatValue;
            }
            set
            {
                _IdentifyRootLevelFoldersByWhatValue = value;
            }
        }

        private Sitecore.Data.Items.TemplateItem _FolderTemplate;
		/// <summary>
		/// the template used to create the folder items
		/// </summary>
        public Sitecore.Data.Items.TemplateItem FolderTemplate {
			get {
				return _FolderTemplate;
			}
			set {
				_FolderTemplate = value;
			}
		}

        private bool _SyncFolderByUpdatingAlreadyExistingItems;
        /// <summary>
        /// Tells if the items should be keeped in sync by updating already existing items with the same defined Key field
        /// </summary>
        public bool SyncFolderByUpdatingAlreadyExistingItems
        {
            get
            {
                return _SyncFolderByUpdatingAlreadyExistingItems;
            }
            set
            {
                _SyncFolderByUpdatingAlreadyExistingItems = value;
            }
        }
        
        private bool _UpdateItemName;
        /// <summary>
        /// Tells if the items should be keeped in sync by updating already existing items with the same defined Key field
        /// </summary>
        public bool UpdateItemName
        {
            get
            {
                return _UpdateItemName;
            }
            set
            {
                _UpdateItemName = value;
            }
        }

        private bool _OnDuplicateItemNamesGeneratePostfixNumber;
        /// <summary>
        /// Tells if a unique item name should be generated with postfix numbers
        /// </summary>
        public bool OnDuplicateItemNamesGeneratePostfixNumber
        {
            get
            {
                return _OnDuplicateItemNamesGeneratePostfixNumber;
            }
            set
            {
                _OnDuplicateItemNamesGeneratePostfixNumber = value;
            }
        }

        private bool _doNotMoveItemToParentFromImport;
        /// <summary>
        /// Indicates if the item should be moved to the new parent from import
        /// </summary>
        public bool DoNotMoveItemToParentFromImport
        {
            get
            {
                return _doNotMoveItemToParentFromImport;
            }
            set
            {
                _doNotMoveItemToParentFromImport = value;
            }
        }

        private bool _ValidateIfItemKeyIsUnique;
        /// <summary>
        /// Indicates if the item should be moved to the new parent from import
        /// </summary>
        public bool ValidateIfItemKeyIsUnique
        {
            get
            {
                return _ValidateIfItemKeyIsUnique;
            }
            set
            {
                _ValidateIfItemKeyIsUnique = value;
            }
        }

        private IBaseField _identifyTheSameItemsByFieldDefinition;
        /// <summary>
        /// The field item used to identify equal items - the key field
        /// </summary>
        public IBaseField IdentifyTheSameItemsByFieldDefinition
        {
            get
            {
                return _identifyTheSameItemsByFieldDefinition;
            }
            set
            {
                _identifyTheSameItemsByFieldDefinition = value;
            }
        }


        private bool _isDisableItemsNotPresentInImport;
        /// <summary>
        /// Tells if the import should disable items not present in the import xml
        /// </summary>
        public bool IsDisableItemsNotPresentInImport
        {
            get
            {
                return _isDisableItemsNotPresentInImport;
            }
            set
            {
                _isDisableItemsNotPresentInImport = value;
            }
        }

        private Item _disableItemsFolderItem;
        /// <summary>
        /// Indicates which folder to move disabled items to
        /// </summary>
        public Item DisableItemsFolderItem
        {
            get
            {
                return _disableItemsFolderItem;
            }
            set
            {
                _disableItemsFolderItem = value;
            }
        }


        private string _dataSourceString;
        /// <summary>
        /// the data source string fx connection string, or other datasource string
        /// </summary>
        public string DataSourceString
        {
            get
            {
                return _dataSourceString;
            }
            set
            {
                _dataSourceString = value;
            }
        }

        private string _Query;
        /// <summary>
        /// the query used to retrieve the data
        /// </summary>
        public string Query
        {
            get
            {
                return _Query;
            }
            set
            {
                _Query = value;
            }
        }

        private bool _IsDoNotLogProgressStatusMessagesInSitecoreLog;

        public bool IsDoNotLogProgressStatusMessagesInSitecoreLog
        {
            get
            {
                return _IsDoNotLogProgressStatusMessagesInSitecoreLog;
            }
            set
            {
                _IsDoNotLogProgressStatusMessagesInSitecoreLog = value;
            }
        }

        public Item ImportItem { get; set; }

        #endregion Properties

		#region Constructor

        public BaseDataMap(Database db, Item importItem, Logging logging)
        {
            ImportItem = importItem;
            //instantiate LogBuilder
            LogBuilder = logging;

            //setup import details
			SitecoreDB = db;
            DataSourceString = importItem["Data Source"];
            //get query
            Query = importItem["Query"];
            
            InitializeImportToLanguageVersion(importItem);

            Int32.TryParse(importItem["Minimum Number Of Rows Required To Run The Import"],
                           out _MinimumNumberOfRowsRequiredToRunTheImport);
            if (MinimumNumberOfRowsRequiredToRunTheImport < 0)
            {
                MinimumNumberOfRowsRequiredToRunTheImport = 0;
            }

            // Get the root folder of the Import and store it
            string parentId = importItem.Fields["Import To Where"].Value;
            if (ImportToLanguageVersion != null)
            {
                using (new LanguageSwitcher(ImportToLanguageVersion))
                {
                    if (!string.IsNullOrEmpty(parentId))
                    {
                        Item parent = SitecoreDB.Items[parentId];
                        if (parent.IsNotNull())
                            Parent = parent;
                        else
                            LogBuilder.Log("Error", "the 'To Where' item is null");
                    }
                    else
                    {
                        LogBuilder.Log("Error", "the 'To Where' field is not set");
                    }
                }
            }
            else
            {
                LogBuilder.Log("Error", "The 'InitializeImportToLanguageVersion' method must be run before the 'Import To Where' field to enable for correct language version creation.");
            }

            // Initialize the type and template mapping
            ToWhatTemplates = new Dictionary<string, TemplateItem>();
            Item templatesFolderItem = importItem.Axes.SelectSingleItem(String.Format("./*[@@tid='{0}']", Utility.Constants.TemplateID));
            if (templatesFolderItem != null)
            {
                if (templatesFolderItem.Children.Any())
                {
                    foreach (Item templatesItem in templatesFolderItem.Children)
                    {
                        string type = templatesItem["Type"];
                        if (String.IsNullOrEmpty(type))
                        {
                            LogBuilder.Log("Error", String.Format("The Template items field 'Type' is not defined. Please add a value to the field. ID: {0}.", GetItemDebugInfo(templatesItem)));
                        }
                        else
                        {
                            AddTypeAndTemplate(templatesItem, type);
                        }
                    }
                }
            }
            if (ToWhatTemplates!=null && !ToWhatTemplates.Any())
            {
                AddTypeAndTemplate(importItem, "Default");
            }

            //more properties
            ItemNameDataField = importItem.Fields["Pull Item Name from What Fields"].Value;
			ItemNameMaxLength = int.Parse(importItem.Fields["Item Name Max Length"].Value);
            //foldering information
			FolderByDate = ((CheckboxField)importItem.Fields["Folder By Date"]).Checked;
			FolderByName = ((CheckboxField)importItem.Fields["Folder By Name"]).Checked;
			DateField = importItem.Fields["Date Field"].Value;
            if (FolderByDate && String.IsNullOrEmpty(DateField))
            {
                LogBuilder.Log("Error", "The 'Folder By Date' is checked and it requires the 'Date Field' to be provided, but this setting is empty. Provide a value for which field to get the Date from.");
            }

            if (FolderByName || FolderByDate) {
				//setup a default type to an ordinary folder
                Sitecore.Data.Items.TemplateItem FolderItem = SitecoreDB.Templates[Utility.Constants.CommonFolderID];
				//if they specify a type then use that
				string folderID = importItem.Fields["Folder Template"].Value;
				if (!string.IsNullOrEmpty(folderID))
					FolderItem = SitecoreDB.Templates[folderID];
				FolderTemplate = FolderItem;
			}
            FolderByParentHierarchy = ((CheckboxField)importItem.Fields["Folder By Parent Hierarchy"]).Checked;
            IdentifyParentByWhatFieldOnParent = importItem["Identify Parent By What Field On Parent"];
            IdentifyParentByWhatFieldOnImportRow = importItem["Identify Parent By What Field On Import Row"];
            IdentifyRootLevelFoldersByWhatValue = importItem["Identify Root Level Folders By What Value"];
            if (FolderByParentHierarchy)
            {
                if (String.IsNullOrEmpty(IdentifyParentByWhatFieldOnParent))
                {
                    LogBuilder.Log("Error", "The 'Folder By Parent Hierarchy' is checked and it requires the 'Identify Parent By What Field On Parent' to be provided, but this setting is empty. Provide a value for which field to locate the parent item from.");
                }
                if (String.IsNullOrEmpty(IdentifyParentByWhatFieldOnImportRow))
                {
                    LogBuilder.Log("Error", "The 'Folder By Parent Hierarchy' is checked and it requires the 'Identify Parent By What Field On Import Row' to be provided, but this setting is empty. Provide a value for which field on the Import Row to get the parent key from.");
                }
            }

            ItemTypeDataField = importItem["Pull Item Type from What Field"];

            //Item Syncronization
            SyncFolderByUpdatingAlreadyExistingItems = ((CheckboxField)importItem.Fields["Sync Folder By Updating Already Existing Items"]).Checked;
            var identifyTheSameItemsByFieldItemId = importItem["Identify The Same Items By Field"];
            if (ID.IsID(identifyTheSameItemsByFieldItemId))
            {
                var fieldItem = SitecoreDB.GetItem(new ID(identifyTheSameItemsByFieldItemId));
                if (fieldItem != null)
                {
                    var keyFieldDefinition = CreateFieldDefinition(fieldItem);
                    if (keyFieldDefinition != null)
                    {
                        IdentifyTheSameItemsByFieldDefinition = keyFieldDefinition;
                    }
                }
            }
            UpdateItemName = ((CheckboxField)importItem.Fields["Update Item Name"]).Checked;
            OnDuplicateItemNamesGeneratePostfixNumber = ((CheckboxField)importItem.Fields["On Duplicate Item Names Generate Postfix Number"]).Checked;
            DoNotMoveItemToParentFromImport = ((CheckboxField)importItem.Fields["Do Not Move Item To Parent From Import"]).Checked;
            ValidateIfItemKeyIsUnique = ((CheckboxField)importItem.Fields["Validate If Item Key Is Unique"]).Checked;
            
            IsDisableItemsNotPresentInImport = ((CheckboxField)importItem.Fields["Disable Items Not Present In Import"]).Checked;
            if (IsDisableItemsNotPresentInImport)
            {
                var disableItemsFolderId = importItem["Disable Items Folder"];
                if (!ID.IsID(disableItemsFolderId))
                {
                    LogBuilder.Log("Error",
                        "The 'Disable Items Folder' field is either not set or is not an correct Sitecore ID. This setting must point to a folder to move disabled items to. This field must be set when the 'Disable Items Not Present In Import' is checked.");
                }
                else
                {
                    DisableItemsFolderItem = SitecoreDB.GetItem(new ID(disableItemsFolderId));
                    if (DisableItemsFolderItem == null)
                    {
                        LogBuilder.Log("Error",
                            "The 'Disable Items Folder' contained and ID, but the item was null. This setting must point to a folder to move disabled items to. This field must be set when the 'Disable Items Not Present In Import' is checked.");
                    }
                }
            }
            IsDoNotLogProgressStatusMessagesInSitecoreLog = importItem["Do Not Log Progress Status Messages In Sitecore Log"] == "1";

            //start handling fields
            Item fields = GetItemByTemplate(importItem, Utility.Constants.FieldsFolderID);
            if (fields.IsNotNull())
            {
                ChildList c = fields.GetChildren();
                if (c.Any())
                {
                    foreach (Item child in c)
                    {
                        var fieldDefinition = CreateFieldDefinition(child);
                        if (fieldDefinition != null)
                        {
                            FieldDefinitions.Add(fieldDefinition);
                        }
                    }
                }
                else
                {
                    LogBuilder.Log("Warn", "there are no fields to import");
                }
            }
            else
            {
                LogBuilder.Log("Warn", "there is no 'Fields' folder");
            }
        }

        private void InitializeImportToLanguageVersion(Item importItem)
        {
            string languageItemId = importItem["Import to Language Version"];
            if (ID.IsID(languageItemId))
            {
                var languageItem = SitecoreDB.GetItem(new ID(languageItemId));
                if (languageItem != null)
                {
                    Language language;
                    if (Language.TryParse(languageItem.Name, out language))
                    {
                        ImportToLanguageVersion = language;
                    }
                    else
                    {
                        LogBuilder.Log("Error",
                            "The 'Import to Language Version' field contained a language item, but a Language object couldn't be created. This setting is used to define which language to import to.");
                    }
                }
                else
                {
                    LogBuilder.Log("Error",
                        "The 'Import to Language Version' field did contain a Sitecore ID but the item wasn't instantiated. This setting is used to define which language to import to.");
                }
            }
            else
            {
                LogBuilder.Log("Error",
                    "The 'Import to Language Version' field must be defined. This setting is used to define which language to import to.");
            }
        }

        private void AddTypeAndTemplate(Item item, string type)
        {
            if (String.IsNullOrEmpty(type))
            {
                LogBuilder.Log("Error",
                        String.Format("The parameter 'type' was null or empty. ID: {0}.",
                            item.ID));
                return;
            }
            string toWhatTemplateId = item["To What Template"];
            if (ID.IsID(toWhatTemplateId))
            {
                TemplateItem templateItem = SitecoreDB.GetTemplate(new ID(toWhatTemplateId));
                if (templateItem != null)
                {
                    ToWhatTemplates.Add(type, templateItem);
                }
                else
                {
                    LogBuilder.Log("Error",
                        String.Format(
                            "The field 'To What Template' had an correct ID, but the templateItem was null. ID: {0}.",
                            item.ID));
                }
            }
            else
            {
                LogBuilder.Log("Error",
                    String.Format(
                        "The field 'To What Template' is not defined or not a correct Sitecore ID for a template. Please add a value to the field. ID: {0}.",
                        item.ID));
            }
        }

        #endregion Constructor

        private IBaseField CreateFieldDefinition(Item fieldItem)
        {
            BaseMapping bm = new BaseMapping(fieldItem);
            if (!string.IsNullOrEmpty(bm.HandlerAssembly))
            {
                if (!string.IsNullOrEmpty(bm.HandlerClass))
                {
                    //create the object from the class and cast as base field to add it to field definitions
                    try
                    {
                        var bf = (IBaseField)Sitecore.Reflection.ReflectionUtil.CreateObject(bm.HandlerAssembly, bm.HandlerClass, new object[] { fieldItem });
                        if (bf != null)
                        {
                            return bf;
                        }
                        LogBuilder.Log("Error", string.Format("the field: '{0}' class type {1} could not be instantiated",
                                            fieldItem.Name, bm.HandlerClass));
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        LogBuilder.Log("Error", string.Format("the field:{0} binary {1} specified could not be found. Exception: {2}", fieldItem.Name, bm.HandlerAssembly, GetExceptionDebugInfo(fnfe)));
                    }
                }
                else
                {
                    LogBuilder.Log("Error",
                        string.Format("the field: '{0}' Handler Class {1} is not defined", fieldItem.Name,
                                      bm.HandlerClass));
                }
            }
            else
            {
                LogBuilder.Log("Error",
                    string.Format("the field: '{0}' Handler Assembly {1} is not defined", fieldItem.Name,
                                  bm.HandlerAssembly));
            }
            return null;
        }

        #region Abstract Methods

        /// <summary>
        /// gets the data to be imported
        /// </summary>
        /// <returns></returns>
        public abstract IList<object> GetImportData();

        public virtual void ValidateImportData(IList<object> importData, ref string errorMessage)
        {
            var keyList = new Dictionary<string, object>();
            var keyErrors = 0;
            foreach (var importRow in importData)
            {
                string importRowErrorMessage = "";
                string keyValue = GetValueFromFieldToIdentifyTheSameItemsBy(importRow, ref importRowErrorMessage);
                if (!String.IsNullOrEmpty(importRowErrorMessage))
                {
                    errorMessage +=
                        String.Format(
                            "--- An error occured trying to validate if the key was unique in the importData. ErrorMessage: {0}. ImportRow: {1}.\r\n",
                            importRowErrorMessage, GetImportRowDebugInfo(importRow));
                    keyErrors++;
                    continue;
                }
                if (String.IsNullOrEmpty(keyValue))
                {
                    errorMessage +=
                        String.Format("--- The keyValue was null or empty in the importData. ImportRow: {0}.\r\n",
                                      GetImportRowDebugInfo(importRow));
                    keyErrors++;
                    continue;
                }
                if (!keyList.ContainsKey(keyValue))
                {
                    keyList.Add(keyValue, importRow);
                }
                else
                {
                    errorMessage +=
                        String.Format("--- There were duplicate keyValue's in the importData. ImportRow: {0}.\r\n",
                                      GetImportRowDebugInfo(importRow));
                    keyErrors++;
                }
            }
            if (keyErrors > 0)
            {
                errorMessage = String.Format("Validation found {0} duplicate keys errors.\r\n", keyErrors) +
                               errorMessage;
            }
        }

        /// <summary>
        /// this is used to process custom fields or properties
        /// </summary>
        public virtual bool ProcessCustomData(ref Item newItem, object importRow, out bool processedCustomData)
        {
            processedCustomData = false;
            return true;
        }

        /// <summary>
        /// Defines how the subclass will retrieve a field value
        /// </summary>
        public abstract string GetFieldValue(object importRow, string fieldName, ref string errorMessage);

        public abstract string GetImportRowDebugInfo(object importRow);

        public virtual string GetExceptionDebugInfo(Exception exception)
        {
            if (exception != null)
            {
                string log = "Message: " + exception.Message + ".\r\nSource: " + exception.Source
                       + "\r\nStacktrace: " + exception.StackTrace;
                if (exception.InnerException != null)
                {
                    log += "\r\nInnerException:\r\n" + GetExceptionDebugInfo(exception.InnerException);
                }
                return log;
            }
            return "Exception was null";
        }

        public virtual string GetItemListDebugInfo(Item[] itemList)
        {
            string debugInfo = "";
            for(int i=0; i<itemList.Count(); i++)
            {
                Item item = itemList[i];
                if (item != null)
                {
                    debugInfo += GetItemDebugInfo(item);
                }
                if (i != itemList.Count() - 1)
                {
                    debugInfo += ", ";
                }
            }
            return debugInfo;
        }

        public virtual string GetItemListDebugInfo(List<Item> itemList)
        {
            string debugInfo = "";
            for (int i = 0; i < itemList.Count(); i++)
            {
                Item item = itemList[i];
                if (item != null)
                {
                    debugInfo += GetItemDebugInfo(item);
                }
                if (i != itemList.Count() - 1)
                {
                    debugInfo += ", ";
                }
            }
            return debugInfo;
        }

        public virtual string GetItemDebugInfo(Item item)
        {
            if (item != null)
            {
                return item.Paths.ContentPath.Replace("sitecore/content", "") + " (" + item.ID + ")";
            }
            return string.Empty;
        }

        #endregion Abstract Methods

        #region Static Methods

        /// <summary>
        /// will begin looking for or creating date folders to get a parent node to create the new items in
        /// </summary>
        /// <param name="parentNode">current parent node to create or search folder under</param>
        /// <param name="dt">date time value to folder by</param>
        /// <param name="folderType">folder template type</param>
        /// <returns></returns>
        public static Item GetDateParentNode(Item parentNode, DateTime dt, TemplateItem folderType) {
            //get year folder
            Item year = parentNode.Children[dt.Year.ToString()];
            if (year == null) {
                //build year folder if you have to
                year = parentNode.Add(dt.Year.ToString(), folderType);
            }
            //set the parent to year
            parentNode = year;

            //get month folder
            Item month = parentNode.Children[dt.ToString("MM")];
            if (month == null) {
                //build month folder if you have to
                month = parentNode.Add(dt.ToString("MM"), folderType);
            }
            //set the parent to year
            parentNode = month;

            //get day folder
            Item day = parentNode.Children[dt.ToString("dd")];
            if (day == null) {
                //build day folder if you have to
                day = parentNode.Add(dt.ToString("dd"), folderType);
            }
            //set the parent to year
            parentNode = day;

            return parentNode;
        }

        /// <summary>
        /// will begin looking for or creating letter folders to get a parent node to create the new items in
        /// </summary>
        /// <param name="parentNode">current parent node to create or search folder under</param>
        /// <param name="letter">the letter to folder by</param>
        /// <param name="folderType">folder template type</param>
        /// <returns></returns>
        public static Item GetNameParentNode(Item parentNode, string letter, TemplateItem folderType) {
            //get letter folder
            Item letterItem = parentNode.Children[letter];
            if (letterItem == null) {
                //build year folder if you have to
                letterItem = parentNode.Add(letter, folderType);
            }
            //set the parent to year
            return letterItem;
        }

        #endregion Static Methods
        
        #region Methods

        /// <summary>
        /// searches under the parent for an item whose template matches the id provided
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="TemplateID"></param>
        /// <returns></returns>
        public Item GetItemByTemplate(Item parent, string TemplateID) {
            IEnumerable<Item> x = from Item i in parent.GetChildren()
                                  where i.Template.IsID(TemplateID)
                                  select i;
            return (x.Any()) ? x.First() : null;
        }
        
        public virtual List<Item> GetItemsByKey(Item parent, string keyFieldName, string key, ref string errorMessage)
        {
            using (new LanguageSwitcher(ImportToLanguageVersion))
            {
                var replacedKey = key.Replace("'", "_");
                string pattern = "{0}//*[@{1}='{2}']";
                pattern = (UseFastQuery ? "fast:" : String.Empty) + pattern; 
                var query = string.Format(pattern, parent.Paths.FullPath, keyFieldName, replacedKey);
                try
                {
                    List<Item> list;
                    if (QueryItemsAndVerifyUniqueness(parent, keyFieldName, key, query, out list)) return list;
                }
                catch (SqlException sqlException)
                {
                    try
                    {
                        // TO FIX SQL Exceptions, like "Transaction (Process ID X) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction." exception.
                        // Sleep for a period and try again one more time. Then fail.
                        LogBuilder.Log("Info", String.Format("The GetItemsByKey method met a SqlException. A rerun is initiated. Parent: {0}, Query: {1}. Exception: {2}. KeyFieldName: {3}. Key: {4}", parent.ID, query, GetExceptionDebugInfo(sqlException), keyFieldName, key));
                        Thread.Sleep(DefaultSleepPeriodToRerunSqlExceptionInGetItemsByKey);
                        List<Item> list;
                        if (QueryItemsAndVerifyUniqueness(parent, keyFieldName, key, query, out list)) return list;
                    }
                    catch (Exception ex)
                    {
                        errorMessage +=
                        String.Format(
                            "The GetItemsByKey method met an Exception twice after being rerun. The processing of the item is aborted. Query: {0}. Exception: {1}",
                            query, GetExceptionDebugInfo(ex));
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    errorMessage +=
                        String.Format(
                            "The GetItemsByKey thrown an exception in trying to query the item. Query: {0}. Exception: {1}",
                            query, GetExceptionDebugInfo(ex));
                    return null;
                }
                return new List<Item>();
            }
        }

        private bool QueryItemsAndVerifyUniqueness(Item parent, string keyFieldName, string key, string query, out List<Item> list)
        {
            list = new List<Item>();
            var items = SitecoreDB.SelectItems(query);
            if (items != null)
            {
                if (items.Any())
                {
                    var equalItems = new List<Item>();
                    // Check that the items found has the same key as the key we searched for. This is done because Sitecore Fast query uses wilcards like "_". 
                    AddItemsThatHaveSameKeyAndAreDescendantsOfParent(keyFieldName, key, items, equalItems, parent);
                    {
                        list = equalItems;
                        return true;
                    }
                }
            }
            return false;
        }

        private static void AddItemsThatHaveSameKeyAndAreDescendantsOfParent(string keyFieldName, string key, Item[] items, List<Item> equalItems, Item parent)
        {
            foreach (Item item in items)
            {
                string fieldValue = item[keyFieldName];
                // Check if key is indead identical in lower case.
                if (key.ToLower().Equals(fieldValue.ToLower()))
                {
                    // Only add if the item is a descendant of the parent provided. This is a workaround around the fact that fast query isn't always returning result only from the path specified.
                    if (item.Paths.IsDescendantOf(parent))
                    {
                        equalItems.Add(item);
                    }
                }
            }
        }

        protected virtual List<Item> GetItemsByTemplate(Item parent, List<TemplateItem> templates, ref string errorMessage)
        {
           string pattern = "{0}//*[{1}]";
           pattern = (UseFastQuery ? "fast:" : String.Empty) + pattern; 
           const string tidpattern = "@@templateid='{0}'";
            string tempPattern = string.Empty;
            for (int i = 0; i < templates.Count; i++)
            {
                var template = templates[i];
                tempPattern += String.Format(tidpattern, template.ID);
                if (i != templates.Count - 1)
                    tempPattern += " or ";
            }
            var query = string.Format(pattern, parent.Paths.FullPath, tempPattern);
            try
            {
                using (new LanguageSwitcher(ImportToLanguageVersion))
                {
                    var items = parent.Database.SelectItems(query);
                    return (items != null && items.Any())
                               ? new List<Item>(items)
                               : new List<Item>();
                }
            }
            catch (Exception ex)
            {
                errorMessage += String.Format("The GetItemsByTemplate thrown an exception in trying to query the item. Query: {0}. Exception: {1}", query, GetExceptionDebugInfo(ex));
            }
            return new List<Item>();
        }

        public virtual List<Item> GetItemsByKeyAndTemplate(Item parent, string itemKey, List<TemplateItem> templates, ref string errorMessage)
        {
            using (new LanguageSwitcher(ImportToLanguageVersion))
            {
                const string tidpattern = "@@templateid='{0}'";
                string tempPattern = string.Empty;
                for (int i = 0; i < templates.Count; i++)
                {
                    var template = templates[i];
                    tempPattern += String.Format(tidpattern, template.ID);
                    if (i != templates.Count - 1)
                        tempPattern += " or ";
                }

                string pattern = "{0}//*[@@key='{1}' and ({2})]";
                pattern = (UseFastQuery ? "fast:" : String.Empty) + pattern; 
                var query = string.Format(pattern, parent.Paths.FullPath, itemKey, tempPattern);
                try
                {
                    return SitecoreDB.SelectItems(query).ToList();
                }
                catch (SqlException sqlException)
                {
                    try
                    {
                        // TO FIX SQL Exceptions like the "Transaction (Process ID X) was deadlocked on lock resources with another process and has been chosen as the deadlock victim. Rerun the transaction." exception.
                        // Sleep for a period and try again one more time. Then fail.
                        LogBuilder.Log("Info", String.Format("The GetItemsByKeyAndTemplate method met a SqlException. A rerun is initiated. Parent: {0}, Query: {1}. Exception: {2}. KeyFieldName: {3}. Key: {4}", parent.ID, query, GetExceptionDebugInfo(sqlException)));
                        Thread.Sleep(DefaultSleepPeriodToRerunSqlExceptionInGetItemsByKey);
                        return SitecoreDB.SelectItems(query).ToList();
                    }
                    catch (Exception ex)
                    {
                        errorMessage +=
                        String.Format(
                            "The GetItemsByKeyAndTemplate method met an Exception twice after being rerun. The processing of the item is aborted. Query: {0}. Exception: {1}",
                            query, GetExceptionDebugInfo(ex));
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    errorMessage +=
                        String.Format(
                            "The GetItemsByKeyAndTemplate thrown an exception in trying to query the item. Query: {0}. Exception: {1}",
                            query, GetExceptionDebugInfo(ex));
                    return null;
                }
                return new List<Item>();
            }
        }

        /// <summary>
        /// processes each field against the data provided by subclasses
        /// </summary>
        public virtual Logging Process()
        {
            string importIdentifier = String.Format("{0} - {1}", String.Format(DateTime.Now.ToLongDateString(), "dd-MMM-yyyy") + " " + String.Format(DateTime.Now.ToLongTimeString(), "hh:mm:ss"), ImportItem.Name);
            Diagnostics.Log.Info(String.Format("DataSync job started - {0}.", importIdentifier), typeof(BaseDataMap));
            //if log messages then don't start Process method.
            if (LogBuilder.LogBuilder.Length >= 1)
            {
                LogBuilder.Log("Error", "The import did not run.");
                return LogBuilder;
            }
            IEnumerable<object> importedRows;
            try 
            {
                importedRows = GetImportData();
            } 
            catch (Exception ex) 
            {
                LogBuilder.Log("Error", String.Format("Connection Error in Process method. Exception: {0}", GetExceptionDebugInfo(ex)));
                return LogBuilder;
            }

            if (importedRows == null)
            {
                LogBuilder.Log("Error", "The GetImportData method returned a null object. Therefor the import was not performed.");
                return LogBuilder;
            }
            int numberOfRows = importedRows.Count();
            LogBuilder.TotalNumberOfItems = numberOfRows;
            if (numberOfRows < GetMinimumNumberOfRowsRequiredToStartImport())
            {
                LogBuilder.Log("Error", String.Format("The GetImportData method encounted that the number of rows in import was lower than the minimum number of rows required. Therefor the import wasn't started. This value is defined in the GetMinimumNumberOfRowsRequiredToStartImport method. This value can be changed by the field 'Minimum Number Of Rows Required To Run The Import' on the import item or by overwriting the method in a custom DataMap object. Therefor the import was not performed. MinimumNumberOfRowsRequiredToStartImport: {0}. NumberOfRows: {1}.", GetMinimumNumberOfRowsRequiredToStartImport(), numberOfRows));
                return LogBuilder;
            }
            int minimumNumberOfRowsRequiredToStartImport = GetNumberOfRowsToProcessBeforeLogStatus(numberOfRows);
            if (minimumNumberOfRowsRequiredToStartImport < 1)
            {
                minimumNumberOfRowsRequiredToStartImport = DefaultNumberOfRowsToProcessBeforeLogStatus;
            }

            //Loop through the data source
            foreach (object importRow in importedRows)
            {
                if (!IsDoNotLogProgressStatusMessagesInSitecoreLog)
                {
                    if (LogBuilder.ProcessedItems%minimumNumberOfRowsRequiredToStartImport == 0)
                    {
                        Diagnostics.Log.Info(
                            String.Format("DataSync job - {0} - Total {1} rows. \r\n{2}", importIdentifier,
                                          numberOfRows, LogBuilder.GetStatusText()), typeof(BaseDataMap));
                    }
                }
                LogBuilder.ProcessedItems += 1;
                if (!ProcessImportRow(importRow))
                {
                    continue;
                }
                LogBuilder.SucceededItems += 1;
            }

            // Disables Items Not Present In Import
            if (IsDisableItemsNotPresentInImport)
            {
                DisableItemsNotPresentInImport();
            }
            Diagnostics.Log.Info(String.Format("DataSync job - {0} ended. {1}.", importIdentifier, LogBuilder.GetStatusText()), typeof(BaseDataMap));
            return LogBuilder;
        }

        protected virtual bool ProcessImportRow(object importRow)
        {
            try
            {
                string errorMessage = String.Empty;
                string itemName = GetItemName(importRow, ref errorMessage);
                if (string.IsNullOrEmpty(itemName) || !String.IsNullOrEmpty(errorMessage))
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "An error occured during generation of a new item name. Therefor the item could not be created. This happened in the Process method in foreach (object importRow in importedRows). Errors: {0}. ImportRow: {1}.",
                                       errorMessage, GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureItems += 1;
                    return false;
                }
                Item parent = GetParentNode(importRow, itemName);
                if (parent.IsNull())
                {
                    LogBuilder.FailureItems += 1;
                    return false;
                }

                if (!NameFields.Any())
                {
                    LogBuilder.Log("Error",
                                   String.Format("There are no 'Name' fields specified. ImportRow: {0}.",
                                                 GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureItems += 1;
                    return false;
                }

                // The item to process
                Item item = null;

                // Find existing item by key defined if the folder should be syncronized
                if (SyncFolderByUpdatingAlreadyExistingItems)
                {
                    errorMessage = String.Empty;
                    var keyValue = GetValueFromFieldToIdentifyTheSameItemsBy(importRow, ref errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        LogBuilder.Log("Error", String.Format("An error occured in Process method. {0}. {1}",
                                                              GetImportRowDebugInfo(importRow), errorMessage));
                        LogBuilder.FailureItems += 1;
                        return false;
                    }

                    if (String.IsNullOrEmpty(keyValue))
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "The fieldDefinition defined in 'Identify The Same Items By Field' didn't result in any value on the import row. This field is used to identify the item unique. Therefor the following item wasn't imported: {0}.",
                                           GetImportRowDebugInfo(importRow)));
                        LogBuilder.FailureItems += 1;
                        return false;
                    }

                    // Check if key exists before
                    var toWhatField = IdentifyTheSameItemsByFieldDefinition.GetNewItemField();
                    if (String.IsNullOrEmpty(toWhatField))
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "The 'To What Field' field on fieldDefinition defined in 'Identify The Same Items By Field' didn't have any value. This field is used to identify which field on the imported item contains the key and identifies it unique. The import was aborted: {0}.",
                                           GetImportRowDebugInfo(importRow)));
                        LogBuilder.FailureItems += 1;
                        return false;
                    }
                    var items = GetExistingItemsToSyncByKey(Parent, parent, toWhatField, keyValue, ref errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage) || items == null)
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "An error occured trying to determine if the item with the key: '{0}' exists before. The processing of that item was aborted. The error happend in GetItemsByKey method. ImportRow: {1}. THe errorMessage was: {2}",
                                           keyValue, GetImportRowDebugInfo(importRow), errorMessage));
                        LogBuilder.FailureItems += 1;
                        return false;
                    }
                    if (items.Count() > 1)
                    {
                        LogBuilder.Log("Error",
                                       String.Format(
                                           "There were more than one items with the same key. The key must be unique. Therefor the following item wasn't imported. Key: {0}. Items: {1}. ImportRow: {2}.",
                                           keyValue, GetItemListDebugInfo(items), GetImportRowDebugInfo(importRow)));
                        LogBuilder.FailureItems += 1;
                        return false;
                    }
                    // The item exists before
                    if (items.Count == 1)
                    {
                        item = items.First();
                    }

                    // Try look for the item in the Disabled Items Folder
                    if (item == null)
                    {
                        if (IsDisableItemsNotPresentInImport)
                        {
                            items = GetItemsByKey(DisableItemsFolderItem, toWhatField, keyValue, ref errorMessage);
                            if (!String.IsNullOrEmpty(errorMessage) || items == null)
                            {
                                LogBuilder.Log("Error",
                                               String.Format(
                                                   "An error occured trying to determine if the item with the key: '{0}' exists before in the DisabledItemsFolder. The processing of that item was aborted. An error occured in GetItemsByKey method used in the IsDisableItemsNotPresentInImport clause. ImportRow: {1}. The errorMessage was: {2}",
                                                   keyValue, GetImportRowDebugInfo(importRow), errorMessage));
                                LogBuilder.FailureItems += 1;
                                return false;
                            }
                            // If an item exist we use the first one
                            if (items.Any())
                            {
                                item = items.First();
                                LogBuilder.MovedFromDisabledItems += 1;
                            }
                        }
                    }
                }

                // Create a new item
                if (item == null)
                {
                    if (!CreateItem(importRow, parent, ref item, itemName))
                    {
                        return false;
                    }
                }
                // Update the item
                if (!UpdateItem(item, importRow, itemName, parent))
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error",
                               String.Format(
                                   "An exception occured in Process method in foreach (object importRow in importItems). The processing of the importRow was aborted. ImportRow: '{0}'. Exception: {1}",
                                   GetImportRowDebugInfo(importRow), GetExceptionDebugInfo(ex)));
                LogBuilder.FailureItems += 1;
                return false;
            }
            return true;
        }

        public virtual List<Item> GetExistingItemsToSyncByKey(Item rootItem, Item currentParent, string toWhatField, string keyValue, ref string errorMessage)
        {
            var items = GetItemsByKey(rootItem, toWhatField, keyValue, ref errorMessage);
            return items;
        }

        protected virtual int GetMinimumNumberOfRowsRequiredToStartImport()
        {
            return MinimumNumberOfRowsRequiredToRunTheImport;
        }

        protected virtual int GetNumberOfRowsToProcessBeforeLogStatus(int numberOfRowsTotal)
        {
            if (numberOfRowsTotal > 0)
            {
                return numberOfRowsTotal / 10;
            }
            return DefaultNumberOfRowsToProcessBeforeLogStatus;
        }

        protected virtual void DisableItemsNotPresentInImport()
        {
            try
            {
                if (DisableItemsFolderItem == null)
                {
                    LogBuilder.Log("Error",
                        "The 'Disable Items Folder' contained an ID, but the item was null. This setting must point to a folder to move disabled items to. This field must be set when the 'Disable Items Not Present In Import' is checked.");
                    return;
                }
                using (new SecurityDisabler())
                {
                    string errorMessage = String.Empty;
                    var importRows = GetImportData();
                    var itemsInSitecore = GetItemsByTemplate(Parent, ToWhatTemplates.Values.ToList(), ref errorMessage);
                    var itemsKeyList = GetItemsKeyList(importRows, ref errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        LogBuilder.Log("Error", String.Format("In the DisableItemsNotPresentInImport method the GetItemsKeyList failed. The disabling process was terminated. {0}.", errorMessage));
                        return;
                    }

                    if (itemsInSitecore.Any())
                    {
                        foreach (var item in itemsInSitecore)
                        {
                            try
                            {
                                var field = item.Fields[IdentifyTheSameItemsByFieldDefinition.GetNewItemField()];
                                if (field != null)
                                {
                                    string keyValue = field.Value;
                                    if (!String.IsNullOrEmpty(keyValue))
                                    {
                                        if (!itemsKeyList.Contains(keyValue))
                                        {
                                            MoveItemToDisabledFolder(item);
                                        }
                                    }
                                    else
                                    {
                                        LogBuilder.Log("Error",
                                                        String.Format(
                                                            "In the method DisableItemsNotPresentInImport the field: '{0}' was found but was empty. This module assumes that all key field must be shared and contain a value. So please make the field shared or alter the module code :-). Item: '{1}'. itemsInSitecore: '{2}'.",
                                                            IdentifyTheSameItemsByFieldDefinition.GetNewItemField
                                                                (), GetItemDebugInfo(item), itemsInSitecore.Count));
                                        LogBuilder.FailedDisabledItems += 1;
                                    }
                                }
                                else
                                {
                                    LogBuilder.Log("Error",
                                                            String.Format(
                                                                "In the method DisableItemsNotPresentInImport the field: '{0}' was not found. This field should be provided since it markes the identifyer of the item. Item: '{1}'. ItemsInSitecore: '{2}'.",
                                                                IdentifyTheSameItemsByFieldDefinition.GetNewItemField
                                                                    (), GetItemDebugInfo(item), itemsInSitecore.Count));
                                    LogBuilder.FailedDisabledItems += 1;
                                }
                            }
                            catch (Exception ex)
                            {
                                LogBuilder.Log("Error",
                                    String.Format(
                                        "An exception occured in Process method When Disabling Items Not Present In Import in foreach (var item in itemsInSitecore). Item: {0}. Exception: {1}",
                                        GetItemDebugInfo(item), GetExceptionDebugInfo(ex)));
                                LogBuilder.FailedDisabledItems += 1;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error",
                    String.Format(
                        "An exception occured in Process method When Disabling Items Not Present In Import. Exception: {0}",
                        ex.Message));
            }
        }

        protected virtual void MoveItemToDisabledFolder(Item item)
        {
            item.Editing.BeginEdit();
            item.MoveTo(DisableItemsFolderItem);
            LogBuilder.DisabledItems += 1;
            item.Editing.EndEdit();
        }

        protected virtual List<string> GetItemsKeyList(IList<object> importRows, ref string errorMessage)
        {
            List<string> keyList = new List<string>();
            var existingFieldNames = IdentifyTheSameItemsByFieldDefinition.GetExistingFieldNames();
            var fieldNames = existingFieldNames.ToList();
            var fieldValueDelimiter = IdentifyTheSameItemsByFieldDefinition.GetFieldValueDelimiter();
            foreach (object importRow in importRows)
            {
                var value = GetValueFromFieldToIdentifyTheSameItemsBy(importRow, fieldNames, fieldValueDelimiter, ref errorMessage);
                keyList.Add(value);
            }
            return keyList;
        }

        protected string GetValueFromFieldToIdentifyTheSameItemsBy(object importRow, IEnumerable<string> existingFieldNames, string fieldValueDelimiter, ref string errorMessage)
        {
            if (IdentifyTheSameItemsByFieldDefinition != null)
            {
                IEnumerable<string> keyValues = GetFieldValues(existingFieldNames, importRow, ref errorMessage);
                var keyValue = String.Join(fieldValueDelimiter, keyValues.ToArray());
                return keyValue;
            }
            return null;
        }

        protected string GetValueFromFieldToIdentifyTheSameItemsBy(object importRow, ref string errorMessage)
        {
            if (IdentifyTheSameItemsByFieldDefinition != null)
            {
                var existingFieldNames = IdentifyTheSameItemsByFieldDefinition.GetExistingFieldNames();
                var fieldValueDelimiter = IdentifyTheSameItemsByFieldDefinition.GetFieldValueDelimiter();
                IEnumerable<string> keyValues = GetFieldValues(existingFieldNames, importRow, ref errorMessage);
                var keyValue = String.Join(fieldValueDelimiter, keyValues.ToArray());
                return keyValue;
            }
            return null;
        }

        protected virtual bool CreateItem(object importRow, Item parent, ref Item item, string itemName)
        {
            try
            {
                if (string.IsNullOrEmpty(itemName))
                {
                    LogBuilder.Log("Error",
                        String.Format(
                            "The item name could not be parsed for importRow: {0}. Therefor the item could not be created.",
                            GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureItems += 1;
                    return false;
                }
                if (parent == null)
                {
                    LogBuilder.Log("Error",
                        String.Format(
                            "The 'parent' parameter is null. Therefor the item could not be created. ImportRow: {0}.",
                            GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureItems += 1;
                    return false;
                }
                // Create mot provider
                if (!ToWhatTemplates.Any())
                {
                    LogBuilder.Log("Error",
                        String.Format(
                            "The 'Import To What Template' item is null. ImportRow: {0}. Therefor the item could not be created.",
                            GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureItems += 1;
                    return false;
                }
                string errorMessage = String.Empty;
                TemplateItem toWhatTemplate = GetToWhatTemplateItem(importRow, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    LogBuilder.Log("Error", String.Format(
                            "The 'GetToWhatTemplateItem' method failed with an error. ImportRow: {0}. Therefor the item could not be created. {1}",
                            GetImportRowDebugInfo(importRow), errorMessage));
                    LogBuilder.FailureItems += 1;
                    return false;
                }
                // Create mot provider

                if (toWhatTemplate == null)
                {
                    LogBuilder.FailureItems += 1;
                    return false;
                }

                using (new LanguageSwitcher(ImportToLanguageVersion))
                {
                    item = parent.Add(itemName, toWhatTemplate);
                }
                if (item == null)
                {
                    LogBuilder.Log("Error",
                        string.Format("The new item created was null. ImportRow: {0}.", GetImportRowDebugInfo(importRow)));
                    LogBuilder.FailureItems += 1;
                    return false;
                }
                LogBuilder.CreatedItems += 1;
                return true;
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error",
                    String.Format(
                        "An exception occured in CreateItem. Exception: {0}",
                        GetExceptionDebugInfo(ex))); 
                LogBuilder.FailureItems += 1;
                return false;
            }
        }

        /// <summary>
        /// Gets the item type to be used to get the right template definition item by matching the field 'Type' in the Templates folder.
        /// </summary>
        /// <param name="importRow"></param>
        /// <param name="itemTypeDateField"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        protected virtual string GetItemType(object importRow, string itemTypeDateField, ref string errorMessage)
        {
            if (!String.IsNullOrEmpty(itemTypeDateField))
            {
                return GetFieldValue(importRow, itemTypeDateField, ref errorMessage);
            }
            return String.Empty;
        }

        protected virtual TemplateItem GetToWhatTemplateItem(object importRow, ref string errorMessage)
        {
            TemplateItem toWhatTemplate = null;
            var type = GetItemType(importRow, ItemTypeDataField, ref errorMessage);
            if (!String.IsNullOrEmpty(type))
            {
                if (ToWhatTemplates.ContainsKey(type))
                {
                    toWhatTemplate = ToWhatTemplates[type];
                }
                else
                {
                    errorMessage += String.Format(
                            "The 'type' field on the importrow was of a type that is not defined in the import. The type was '{0}'. ImportRow: {1}.",
                            type, GetImportRowDebugInfo(importRow));
                }
            }
            else
            {
                if (ToWhatTemplates.ContainsKey("Default"))
                {
                    toWhatTemplate = ToWhatTemplates["Default"];
                }
                else
                {
                    errorMessage += String.Format(
                            "A default import template could not be found in the import. Please check that the field 'To What Template' is provided. ImportRow: {0}. Therefor the item could not be created.",
                            GetImportRowDebugInfo(importRow));
                }
            }
            return toWhatTemplate;
        }

        protected virtual bool UpdateItem(Item item, object importRow, string itemName, Item parentItem)
        {
            var updatedItem = false;
            try
            {
                using (new LanguageSwitcher(ImportToLanguageVersion))
                {
                    using (new SecurityDisabler())
                    {
                        if (AddLanguageVersionIfNoneExists(item))
                        {
                            LogBuilder.LanguageVersionAddedItems += 1;
                        }

                        // Move the item to it's correct position
                        if (!DoNotMoveItemToParentFromImport)
                        {
                            if (item.ParentID != parentItem.ID)
                            {
                                MoveItem(item, parentItem);
                            }
                        }
                        else
                        {
                            // When we do not move the item to it's importet parent, then we must set the parentItem to the currenct parent, to enable the validation of duplicate items names.
                            parentItem = item.Parent;
                        }

                        if (OnDuplicateItemNamesGeneratePostfixNumber)
                        {
                            string errorMessage = "";
                            var newItemName = CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot(importRow, item, itemName, itemName, parentItem, 1, ref errorMessage);
                            if (!String.IsNullOrEmpty(errorMessage))
                            {
                                LogBuilder.Log("Error", String.Format(
                                         "The 'CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot' method failed with an error. The itemname was not changed to a postfixnumber since the check failed. But the Update process was continued. ImportRow: {0}. ErrorMessage: {1}",
                                         GetImportRowDebugInfo(importRow), errorMessage)); 
                                LogBuilder.FailureItems += 1;
                            }
                            if (!itemName.Equals(newItemName))
                            {
                                if (!itemName.Equals(newItemName))
                                {
                                    itemName = newItemName;
                                    LogBuilder.ItemNameRegeneratedItems += 1;
                                }
                            }
                        }
                        if(OnDuplicateItemNamesGeneratePostfixNumber || UpdateItemName)
                        {
                            if (item.Name != itemName)
                            {
                                item.Editing.BeginEdit();
                                item.Name = itemName;
                                item.Editing.EndEdit();
                                LogBuilder.RenamedItems += 1;
                            }
                        }

                        if (ValidateIfItemKeyIsUnique)
                        {
                            if (!IsItemNameUnique(parentItem, item))
                            {
                                // The validation of the item found that there exists more than one item under the parent with the same item key.
                                // We logged the error, but continue processing item. The duplicated item names must be corrected manually.
                                LogBuilder.FailureItems += 1;
                                return false;
                            }
                        }

                        // Change template if type has changed
                        if (ToWhatTemplates.Count > 0)
                        {
                            string errorMessage = String.Empty;
                            var toWhatTemplate = GetToWhatTemplateItem(importRow, ref errorMessage);
                            if (!String.IsNullOrEmpty(errorMessage))
                            {
                                LogBuilder.Log("Error", String.Format(
                                        "The 'GetToWhatTemplateItem' method failed with an error. ImportRow: {0}. Therefor it was not possible to determine wheether the item template should change. The Change template process was aborted as well as the rest of the update item process. {1}",
                                        GetImportRowDebugInfo(importRow), errorMessage));
                                LogBuilder.FailureItems += 1;
                                return false;
                            }
                            if (toWhatTemplate != null)
                            {
                                if (item.TemplateID != toWhatTemplate.ID)
                                {
                                    item.Editing.BeginEdit();
                                    item.ChangeTemplate(toWhatTemplate);
                                    item.Editing.EndEdit();
                                    LogBuilder.ChangedTemplateItems += 1;
                                }
                            }
                            else
                            {
                                LogBuilder.FailureItems += 1;
                                return false;
                            }
                        } 

                        // Add in the field mappings
                        var updatedFields = false;
                        var failedItem = false;
                        foreach (IBaseField fieldDefinition in FieldDefinitions)
                        {
                            string errorMessage = String.Empty;
                            var existingFieldNames = fieldDefinition.GetExistingFieldNames();
                            var fieldValueDelimiter = fieldDefinition.GetFieldValueDelimiter();
                            IEnumerable<string> values = GetFieldValues(existingFieldNames, importRow, ref errorMessage);
                            if (!String.IsNullOrEmpty(errorMessage))
                            {
                                LogBuilder.Log("Error", String.Format("An error occured in extracting the values from a specific field: '{0}' on the item: '{1}'. The processing of the item is aborted. ErrorMessage: {2}", 
                                    fieldDefinition, GetItemDebugInfo(item), errorMessage));
                                LogBuilder.FailureItems += 1;
                                return false;
                            }

                            bool updateField;
                            var status = fieldDefinition.FillField(this, importRow, ref item, String.Join(fieldValueDelimiter, values.ToArray()), out updateField);
                            if (!String.IsNullOrEmpty(status))
                            {
                                LogBuilder.Log("FieldError", String.Format("An error occured in processing a field on the item: '{0}'. The processing of the item in itself is not aborted and the rest of the fields has been processed. The error was: {1}", GetItemDebugInfo(item), status));
                                failedItem = true;
                            }
                            if (updateField)
                            {
                                updatedFields = true;
                            }
                        }
                        if (updatedFields)
                        {
                            LogBuilder.UpdatedFields += 1;
                        }
                        if (failedItem)
                        {
                            LogBuilder.FailureItems += 1;
                            return false;
                        }

                        // Calls the subclass method to handle custom fields and properties
                        bool processedCustomData;
                        if (!ProcessCustomData(ref item, importRow, out processedCustomData))
                        {
                            LogBuilder.FailureItems += 1;
                            return false;
                        }
                        if (processedCustomData)
                        {
                            LogBuilder.ProcessedCustomDataItems += 1;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                LogBuilder.Log("Error", String.Format("An exception occured in UpdateItem. ImportRow: {0}. Item: {1}, ItemName: {2}, Exception: {3}", GetImportRowDebugInfo(importRow), GetItemDebugInfo(item), itemName, GetExceptionDebugInfo(ex))); 
                LogBuilder.FailureItems += 1;
                return false;
            }
            return true;
        }

        protected virtual bool AddLanguageVersionIfNoneExists(Item item)
        {
            using (new SecurityDisabler())
            {
                Item languageVersion = item.Versions.GetLatestVersion(ImportToLanguageVersion);
                if (languageVersion == null || languageVersion.Versions.Count == 0)
                {
                    item.Editing.BeginEdit();
                    item.Versions.AddVersion();
                    item.Editing.EndEdit();
                    return true;
                }
            }
            return false;
        }

        protected virtual void MoveItem(Item item, Item targetParentItem)
        {
            using (new SecurityDisabler())
            {
                item.Editing.BeginEdit();
                item.MoveTo(targetParentItem);
                item.Editing.EndEdit();
                LogBuilder.MovedItems += 1;
            }
        }

        private bool IsItemNameUnique(Item parentItem, Item item)
        {
            if (parentItem != null)
            {
                var existingItemsWithSameItemName = parentItem.Axes.SelectItems(String.Format("./*[@@key='{0}']", item.Key));
                if (existingItemsWithSameItemName != null)
                {
                    if (existingItemsWithSameItemName.Count() > 1)
                    {
                        LogBuilder.Log("Error", string.Format("The method IsItemNameIsUnique method found {0} items with the same key under the parentitem: '{1}'. Please correct the item names. Items: '{2}'. ", existingItemsWithSameItemName.Count(), GetItemDebugInfo(parentItem), GetItemListDebugInfo(existingItemsWithSameItemName)));
                        return false;
                    }
                    return true;
                }
                // This cannot happen, but if it does. Then throw error.
                LogBuilder.Log("Error", string.Format("In the IsItemNameIsUnique method the item processed was not found in the parent spesified. This should not happen. Item: '{0}'. ", GetItemDebugInfo(item)));
                return false;
            }
            LogBuilder.Log("Error", string.Format("In the IsItemNameIsUnique method the parent item was null. Item: '{0}'. ", GetItemDebugInfo(item)));
            return false;
        }

        public virtual string GetConcatenatedItemNameFromImport(object importRow, ref string errorMessage)
        {
            StringBuilder strItemName = new StringBuilder();
            foreach (string nameField in NameFields) 
            {
                try
                {
                    var fieldValue = GetFieldValue(importRow, nameField, ref errorMessage);
                    strItemName.Append(fieldValue);
                } 
                catch (ArgumentException ex) 
                {
                    if (string.IsNullOrEmpty(ItemNameDataField))
                    {
                        errorMessage += String.Format("In method GetConcatenatedItemNameFromImport the 'Name' field is empty. Exception: {0}", GetExceptionDebugInfo(ex));
                    }
                    else
                    {
                        errorMessage += String.Format("In method GetConcatenatedItemNameFromImport the field name: '{0}' does not exist in the import row. Exception: {1}.", nameField, GetExceptionDebugInfo(ex));
                    }
                } 
            }
            var trimmedItemName = strItemName.ToString().Trim(' ');
            if (String.IsNullOrEmpty(trimmedItemName))
            {
                string emptyFieldNames = "";
                foreach (string nameField in NameFields)
                {
                    emptyFieldNames += nameField + "|";
                }
                errorMessage += String.Format("In method GetConcatenatedItemNameFromImport the 'strItemName' was empty. The fields that didn't contain any value was {0}.", emptyFieldNames);
            }
            return trimmedItemName;
        }

        /// <summary>
        /// creates an item name based on the name field values in the importRow
        /// </summary>
        public virtual string GetItemName(object importRow, ref string errorMessage)
        {
            try
            {
                string strItemName = GetConcatenatedItemNameFromImport(importRow, ref errorMessage);
                return StringUtility.GetNewItemName(strItemName, ItemNameMaxLength);
            }
            catch (Exception ex)
            {
                errorMessage +=
                    String.Format("In method GetItemName an exception occured in GetItemName. Exception: {0}",
                                  GetExceptionDebugInfo(ex));
            }
            return String.Empty;
        }

        protected virtual Item[] QueryItemsByKey(Item rootItem, string key, ref string errorMessage)
        {
            try
            {
                return rootItem.Axes.SelectItems(String.Format("./*[@@key='{0}']", key.ToLower()));
            }
            catch (Exception ex)
            {
                errorMessage += String.Format("An error occured trying to query the item by key in the QueryItemsByKey method. RootItem: {0}. Key: {1}. Exception: {2}", rootItem.ID, key, GetExceptionDebugInfo(ex));
                return null;
            }
        }

        /// <summary>
        /// creates an item name based on the name field values in the importRow
        /// </summary>
        public string CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot(object importRow, Item item, string itemName, string newItemName, Item parentItem, int? postFixCount, ref string errorMessage)
        {
            if (parentItem != null)
            {
                var existingItemsWithSameItemName = QueryItemsByKey(parentItem, newItemName.ToLower(), ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    errorMessage += String.Format("In the CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot method an error occured querying items. ErrorMessage: {0}. ItemName: {1}. NewItemName: {2}. Item: '{3}'. ", errorMessage, itemName, newItemName, GetItemDebugInfo(item));
                    return null;
                }
                if (existingItemsWithSameItemName != null)
                {
                    if (existingItemsWithSameItemName.Count() > 2)
                    {
                        errorMessage += String.Format("In the CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot method there were found {0} items with the same key '{1}'. This cannot be fixed in the import. Please correct the item names. NewItemName: {2}. Item: '{3}'. ", existingItemsWithSameItemName.Count(), itemName, newItemName, GetItemDebugInfo(item));
                        return null;
                    }
                    else if (existingItemsWithSameItemName.Count() == 2)
                    {
                        if (existingItemsWithSameItemName.Select(i => i.ID.ToString() == item.ID.ToString()).Any())
                        {
                            if (postFixCount == null)
                            {
                                postFixCount = 1;
                            }
                            newItemName = itemName + "_" + postFixCount;
                            return CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot(importRow, item, itemName, newItemName, parentItem,
                                                                         postFixCount+1, ref errorMessage);
                        }
                        else
                        {
                            // This cannot happen, but if it does. Then throw error.
                            errorMessage += String.Format("In the CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot method there were found 2 items with the same key and the processed item was not part of them. Item: '{0}'. ItemName: {1}. NewItemName: {2}", GetItemDebugInfo(item), itemName, newItemName);
                            return null;
                        }
                    }
                    else if (existingItemsWithSameItemName.Count() == 1)
                    {
                        if(existingItemsWithSameItemName.First().ID.ToString() == item.ID.ToString())
                        {
                            return newItemName;
                        }
                        else
                        {
                            if (postFixCount == null)
                            {
                                postFixCount = 1;
                            }
                            newItemName = itemName + "_" + postFixCount;
                            return CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot(importRow, item, itemName, newItemName, parentItem,
                                                                         postFixCount + 1, ref errorMessage);
                        }
                    }
                    else
                    {
                        // Everything is good, the item does not exist before.
                        return newItemName;
                    }
                }
                else
                {
                    // Everything is good, the item does not exist before.
                    return newItemName;
                }
            }
            else
            {
                errorMessage += String.Format("In the CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot method the parent was null. Cannot check if item name is unique if not spesified. This should not happen. Item: '{0}'. ItemName: {1}. NewItemName: {2}.", GetItemDebugInfo(item), itemName, newItemName);
                return null;    
            }
        }

        /// <summary>
        /// retrieves all the import field values specified
        /// </summary>
        public IEnumerable<string> GetFieldValues(IEnumerable<string> fieldNames, object importRow, ref string errorMessage) {
            var list = new List<string>();
            foreach (string f in fieldNames) 
            {
                try
                {
                    var value = GetFieldValue(importRow, f, ref errorMessage);
                    if (value == null)
                    {
                        errorMessage += String.Format("In GetFieldValues method the field value was null. This should not happen. An empty string was added. FieldName: '{0}'", f);
                        list.Add(String.Empty);
                    }
                    else
                    {
                        list.Add(value.Trim());
                    }
                } 
                catch (ArgumentException ex) 
                {
                    if (string.IsNullOrEmpty(f))
                    {
                        errorMessage += String.Format("In GetFieldValues method the 'From' field name is empty. Exception: {0}", GetExceptionDebugInfo(ex));
                    }
                    else
                    {
                        errorMessage += String.Format("In GetFieldValues method the field name: '{0}' does not exist in the import row. Exception: {1}", f, GetExceptionDebugInfo(ex));
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Gets the parent to use to create the new item below. Will create folders based on name or date if configured to. Default is to use the root as parent.
        /// </summary>
        protected virtual Item GetParentNode(object importRow, string itemName)
        {
            if (FolderByParentHierarchy)
            {
                if (String.IsNullOrEmpty(IdentifyParentByWhatFieldOnImportRow))
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "The 'IdentifyParentByWhatFieldOnImportRow' setting was not set. This setting must be set to identify which field on the import row to get the parent from. Therefor the following item wasn't imported. ImportRow: {0}.",
                                       GetImportRowDebugInfo(importRow)));
                    return null;
                }
                if (String.IsNullOrEmpty(IdentifyParentByWhatFieldOnParent))
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "The 'IdentifyParentByWhatFieldOnParent' setting was not set. This setting must be set to identify which field to identify the parent item with. Therefor the following item wasn't imported. ImportRow: {0}.",
                                       GetImportRowDebugInfo(importRow)));
                    return null;
                }

                string errorMessage = String.Empty;
                string identifyParentFieldNameOnImportRow = GetFieldValue(importRow, IdentifyParentByWhatFieldOnImportRow, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "The value drawn from field '{0}' resultet in an error. Therefor the parent was not found. The processing of the item was aborted. ImportRow: {1}.",
                                       IdentifyParentByWhatFieldOnImportRow, GetImportRowDebugInfo(importRow)));
                    return null;
                }

                if (String.IsNullOrEmpty(identifyParentFieldNameOnImportRow))
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "The value drawn form field '{0}' was empty. Therefor the following item wasn't imported. ImportRow: {1}.",
                                       IdentifyParentByWhatFieldOnImportRow, GetImportRowDebugInfo(importRow)));
                    return null;
                }
                // If its root level folders, then return the root folder
                if (identifyParentFieldNameOnImportRow == IdentifyRootLevelFoldersByWhatValue)
                {
                    return Parent;
                }
                var items = GetItemsByKey(Parent, IdentifyParentByWhatFieldOnParent, identifyParentFieldNameOnImportRow, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage) || items == null)
                {
                    LogBuilder.Log("Error", String.Format("An error occured in locating the parent item of item: '{0}'. The processing of the item was aborted. The error occured in GetItemsByKey method used in the GetParentNode method. ImportRow: {1}. ErrorMessage: {2}",
                        itemName, GetImportRowDebugInfo(importRow), errorMessage));
                    return null;
                }
                if (items.Count() > 1)
                {
                    LogBuilder.Log("Error",
                                   String.Format(
                                       "There were more than one parent with the same key. The key must be unique. Therefor the following item wasn't imported. identifyParentFieldNameOnImportRow: {0}. Items: {1}. ImportRow: {2}.",
                                       identifyParentFieldNameOnImportRow, GetItemListDebugInfo(items),
                                       GetImportRowDebugInfo(importRow)));
                    return null;
                }
                // There is only one parent
                if (items.Count == 1)
                {
                    return items.First();
                }
                LogBuilder.Log("Error",
                               string.Format(
                                   "Could not found the parent node in GetParentNode method. The item was not imported. ItemName: {0}. ImportRow: {1}.",
                                   itemName, GetImportRowDebugInfo(importRow)));
                return null;
            }
            if (FolderByDate)
            {
                DateTime date = DateTime.Now;
                string dateValue = string.Empty;
                try
                {
                    string errorMessage = String.Empty;
                    dateValue = GetFieldValue(importRow, DateField, ref errorMessage);
                    if (!String.IsNullOrEmpty(errorMessage))
                    {
                        LogBuilder.Log("Error", String.Format("The date field used in FolderByDate setting resultet in an error. DateField: {0}. dateValue: {1}. {2}.", DateField, dateValue, errorMessage));
                        return null;
                    }
                }
                catch (ArgumentException ex)
                {
                    if (string.IsNullOrEmpty(DateField))
                    {
                        LogBuilder.Log("Error", String.Format("The date name field is empty. Exception: {0}", GetExceptionDebugInfo(ex)));
                        return null;
                    }
                    else
                    {
                        LogBuilder.Log("Error",
                                       string.Format("The field name: '{0}' does not exist in the import row. Exception: {1}.",
                                                     DateField, GetExceptionDebugInfo(ex)));
                        return null;
                    }
                }
                if (!string.IsNullOrEmpty(dateValue))
                {
                    if (DateTime.TryParse(dateValue, out date))
                    {
                        return GetDateParentNode(Parent, date, this.FolderTemplate);
                    }
                    else
                    {
                        LogBuilder.Log("Error", "the date value could not be parsed");
                        return null;
                    }
                }
                else
                {
                    LogBuilder.Log("Error", "the date value was empty");
                    return null;
                }
            }
            else if (FolderByName)
            {
                return GetNameParentNode(Parent, itemName.Substring(0, 1), this.FolderTemplate);
            }
            // Return Root folder if none of the other options is selected
            return Parent;
        }

        #endregion Methods
	}
}
