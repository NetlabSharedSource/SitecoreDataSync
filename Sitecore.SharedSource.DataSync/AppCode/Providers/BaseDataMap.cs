using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading;
using Sitecore.Data.Items;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.Logger.Log.Output;
using Sitecore.SharedSource.DataSync.Mappings.Fields;
using Sitecore.SharedSource.DataSync.Extensions;
using Sitecore.SharedSource.DataSync.Mappings;
using Sitecore.SharedSource.DataSync.Utility;
using Sitecore.Collections;
using System.IO;
using Sitecore.Configuration;
using Sitecore.Globalization;
using Sitecore.SecurityModel;
using Sitecore.SharedSource.DataSync.Caching;
using Sitecore.SharedSource.DataSync.Publishing;

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
        protected const string FieldNameData = "Data";
        public const string AlwaysItemId = "{C10CF05B-7987-435C-A173-841BEA7E57A6}";
        public const string OnInfosAndErrorsItemId = "{A0DA4521-DB88-4857-93B9-AA18F6648C14}";
        public const string OnErrorsItemId = "{1F8AF105-FE1A-4A29-9644-11BC3E22676B}";
        public const string NeverItemId = "{6D970175-2A58-4A48-AA2F-091DC458A899}";
        private const string DoPublishFieldName = "Do Publish";
        private const string PublishRootItemsFieldName = "Publish Root Items";
        private const string PublishingSourceDatabaseFieldName = "Publishing Source Database";
        private const string PublishingTargetDatabaseFieldName = "Publishing Target Database";
        private const string PublishingModeFieldName = "Publishing Mode";
        private const string PublishingModeSmart = "Smart";
        private const string SitecoreBooleanTrue = "1";
        private const char PipeSeparatorChar = '|';

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

        public LevelLogger Logger { get; set; }

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

        private Dictionary<string, CustomItemBase> _toWhatTemplates;

        public Dictionary<string, CustomItemBase> ToWhatTemplates
        {
            get { return _toWhatTemplates; }
            set { _toWhatTemplates = value; }
        }

        private bool _doNotChangeTemplate;

        public bool DoNotChangeTemplate
        {
            get { return _doNotChangeTemplate; }
            set { _doNotChangeTemplate = value; }
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

        public string ItemLoggingStrategy
        {
            get;
            set;
        }

        public Item ImportItem { get; set; }
        
        public bool IsDoPublish { get; set; }
        public List<Item> PublishRootItems = new List<Item>();
        public string PublishingMode { get; set; }
        public string PublishingSourceDatabase { get; set; }
        public string PublishingTargetDatabase { get; set; }

        public ICachingManager CachingManager { get; set; }

        #endregion Properties

		#region Constructor

        public BaseDataMap(Database db, Item importItem, LevelLogger logger)
        {
            ImportItem = importItem;
            //instantiate LogBuilder
            Logger = logger;

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
                            logger.AddError("The 'To Where' item is null", "The 'To Where' item is null");
                    }
                    else
                    {
                        logger.AddError("The 'To Where' field is not set", "The 'To Where' field is not set");
                    }
                }
            }
            else
            {
                logger.AddError("ImportToLanguageVersion was null", "The 'InitializeImportToLanguageVersion' method must be run before the 'Import To Where' field to enable for correct language version creation.");
            }

            // Initialize the type and template mapping
            ToWhatTemplates = new Dictionary<string, CustomItemBase>();
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
                            logger.AddError("No 'Type' defined for Template items", String.Format("The Template items field 'Type' is not defined. Please add a value to the field. ID: {0}.", GetItemDebugInfo(templatesItem)));
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

            DoNotChangeTemplate = importItem.Fields["Do Not Change Template"].Value == SitecoreBooleanTrue;

            //more properties
            ItemNameDataField = importItem.Fields["Pull Item Name from What Fields"].Value;
			ItemNameMaxLength = int.Parse(importItem.Fields["Item Name Max Length"].Value);
            //foldering information
			FolderByDate = ((CheckboxField)importItem.Fields["Folder By Date"]).Checked;
			FolderByName = ((CheckboxField)importItem.Fields["Folder By Name"]).Checked;
			DateField = importItem.Fields["Date Field"].Value;
            if (FolderByDate && String.IsNullOrEmpty(DateField))
            {
                logger.AddError("No 'Date Field' defined", "The 'Folder By Date' is checked and it requires the 'Date Field' to be provided, but this setting is empty. Provide a value for which field to get the Date from.");
            }

            if (FolderByName || FolderByDate) {
				//setup a default type to an ordinary folder
                TemplateItem FolderItem = SitecoreDB.Templates[Utility.Constants.CommonFolderID];
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
                    logger.AddError("Error", "The 'Folder By Parent Hierarchy' is checked and it requires the 'Identify Parent By What Field On Parent' to be provided, but this setting is empty. Provide a value for which field to locate the parent item from.");
                }
                if (String.IsNullOrEmpty(IdentifyParentByWhatFieldOnImportRow))
                {
                    logger.AddError("Error", "The 'Folder By Parent Hierarchy' is checked and it requires the 'Identify Parent By What Field On Import Row' to be provided, but this setting is empty. Provide a value for which field on the Import Row to get the parent key from.");
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
                    logger.AddError("Error",
                        "The 'Disable Items Folder' field is either not set or is not an correct Sitecore ID. This setting must point to a folder to move disabled items to. This field must be set when the 'Disable Items Not Present In Import' is checked.");
                }
                else
                {
                    DisableItemsFolderItem = SitecoreDB.GetItem(new ID(disableItemsFolderId));
                    if (DisableItemsFolderItem == null)
                    {
                        logger.AddError("Error",
                            "The 'Disable Items Folder' contained and ID, but the item was null. This setting must point to a folder to move disabled items to. This field must be set when the 'Disable Items Not Present In Import' is checked.");
                    }
                }
            }
            IsDoNotLogProgressStatusMessagesInSitecoreLog = importItem["Do Not Log Progress Status Messages In Sitecore Log"] == SitecoreBooleanTrue;

            ItemLoggingStrategy = importItem["Item Logging"];

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
                    logger.AddInfo("Warning", "There are no fields to import");
                }
            }
            else
            {
                logger.AddInfo("Warning", "There is no 'Fields' folder");
            }

            IsDoPublish = importItem[DoPublishFieldName] == SitecoreBooleanTrue;
            if (IsDoPublish && !String.IsNullOrEmpty(importItem[PublishRootItemsFieldName]))
            {
                PublishingMode = importItem[PublishingModeFieldName];
                PublishingSourceDatabase = importItem[PublishingSourceDatabaseFieldName];
                PublishingTargetDatabase = importItem[PublishingTargetDatabaseFieldName];

                var publishRootItemIds = importItem[PublishRootItemsFieldName].Split(PipeSeparatorChar);
                if (publishRootItemIds.Any())
                {
                    foreach (var id in publishRootItemIds)
                    {
                        PublishRootItems.Add(SitecoreDB.GetItem(new ID(id)));
                    }
                }
            }
            CachingManager = new CachingManager(this);
            CachingManager.Initialize(ref logger);
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
                        Logger.AddError("Error", "The 'Import to Language Version' field contained a language item, but a Language object couldn't be created. This setting is used to define which language to import to.");
                    }
                }
                else
                {
                    Logger.AddError("Error",
                        "The 'Import to Language Version' field did contain a Sitecore ID but the item wasn't instantiated. This setting is used to define which language to import to.");
                }
            }
            else
            {
                Logger.AddError("Error",
                    "The 'Import to Language Version' field must be defined. This setting is used to define which language to import to.");
            }
        }

        private void AddTypeAndTemplate(Item item, string type)
        {
            if (String.IsNullOrEmpty(type))
            {
                Logger.AddError("Error",
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
                    BranchItem branchItem = SitecoreDB.Branches.GetMaster(new ID(toWhatTemplateId));
                    if (branchItem != null)
                    {
                        ToWhatTemplates.Add(type, branchItem);
                    }
                    else
                    {
                        Logger.AddError("Error",
                            String.Format(
                                "The field 'To What Template' had an correct ID, but the templateItem or branchItem was null. ID: {0}.",
                                item.ID));
                    }
                }
            }
            else
            {
                Logger.AddError("Error",
                    String.Format(
                        "The field 'To What Template' is not defined or not a correct Sitecore ID for a template. Please add a value to the field. ID: {0}.",
                        item.ID));
            }
        }

        #endregion Constructor
        
        public virtual string GetIdentifier()
        {
            if (ImportItem != null)
            {
                return ImportItem.Name;
            }
            return String.Empty;
        }

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
                        Logger.AddError("Error", String.Format("the field: '{0}' class type {1} could not be instantiated",
                                            fieldItem.Name, bm.HandlerClass));
                    }
                    catch (FileNotFoundException fnfe)
                    {
                        Logger.AddError("Error", String.Format("the field:{0} binary {1} specified could not be found. Exception: {2}", fieldItem.Name, bm.HandlerAssembly, GetExceptionDebugInfo(fnfe)));
                    }
                }
                else
                {
                    Logger.AddError("Error", String.Format("the field: '{0}' Handler Class {1} is not defined", fieldItem.Name,
                                      bm.HandlerClass));
                }
            }
            else
            {
                Logger.AddError("Error", String.Format("the field: '{0}' Handler Assembly {1} is not defined", fieldItem.Name,
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

        public virtual void ValidateImportData(IList<object> importData, ref LevelLogger logger)
        {
            var validateImportLogger = logger.CreateLevelLogger("ValidateImportData");
            var keyList = new Dictionary<string, object>();
            var keyErrors = 0;
            foreach (var importRow in importData)
            {
                var getValueFromFieldLogger = validateImportLogger.CreateLevelLogger();
                string keyValue = GetValueFromFieldToIdentifyTheSameItemsBy(importRow, ref getValueFromFieldLogger);
                if (getValueFromFieldLogger.HasErrors())
                {
                    getValueFromFieldLogger.AddError("Error when validating if the key was unique", String.Format(
                            "--- An error occured trying to validate if the key was unique in the importData. ImportRow: {0}.\r\n",
                            GetImportRowDebugInfo(importRow)));
                    keyErrors++;
                    continue;
                }
                if (String.IsNullOrEmpty(keyValue))
                {
                    getValueFromFieldLogger.AddError("The KeyValue was null or empty in the ImportData", String.Format("--- The keyValue was null or empty in the importData. ImportRow: {0}.\r\n",
                                      GetImportRowDebugInfo(importRow)));
                    keyErrors++;
                    continue;
                }
                if (!keyList.ContainsKey(keyValue))
                {
                    keyList.Add(keyValue, importRow);
                }
                else
                {
                    validateImportLogger.AddError("The were duplicate keyValues in ImportData", String.Format("--- There were duplicate keyValue's in the importData. ImportRow: {0}.\r\n",
                                      GetImportRowDebugInfo(importRow)));
                    keyErrors++;
                }
            }
            if (keyErrors > 0)
            {
                validateImportLogger.AddError("Validation found duplicate keys errors", String.Format("Validation found {0} duplicate keys errors.\r\n", keyErrors));
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
        public abstract string GetFieldValue(object importRow, string fieldName, ref LevelLogger logger);

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

        public virtual string GetItemPathInfo(Item item)
        {
            if (item != null)
            {
                return item.Paths.FullPath + item.ID;
            }
            return string.Empty;
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

        public virtual string GetTemplateDebugInfo(TemplateItem templateItem)
        {
            if (templateItem != null)
            {
                return templateItem.FullName.Replace("sitecore/templates", "") + " (" + templateItem.ID + ")";
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

        public virtual List<Item> GetItemsByKey(Item parent, string keyFieldName, string key, ref LevelLogger logger, bool useCache = false)
        {
            using (new LanguageSwitcher(ImportToLanguageVersion))
            {
                var getItemsByKeyLogger = logger.CreateLevelLogger();

                if (useCache)
                {
                    var itemsFromCacheLogger = getItemsByKeyLogger.CreateLevelLogger();
                    var itemList = CachingManager.GetItemsFromCache(parent, keyFieldName, key, ref itemsFromCacheLogger);
                    if (itemList != null)
                    {
                        var list = new List<Item>();
                        foreach (var id in itemList)
                        {
                            var item = SitecoreDB.GetItem(new ID(id), ImportToLanguageVersion);
                            if (item != null)
                            {
                                list.Add(item);
                            }
                        }
                        return list;
                    }
                    return new List<Item>();
                }
                var replacedKey = key.Replace("'", "_");
                string pattern = "{0}//*[@{1}='{2}']";
                //pattern = (UseFastQuery ? "fast:" : String.Empty) + pattern;
                //var query = String.Format(pattern, DoFastQuerySafe(parent.Paths.FullPath), keyFieldName, replacedKey);
                var query = String.Format(pattern, parent.Paths.FullPath, keyFieldName, replacedKey);
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
                        getItemsByKeyLogger.AddInfo("Trying rerun after SQLException in GetItemsByKey.",
                            String.Format(
                                "The GetItemsByKey method met a SqlException. A rerun is initiated. Parent: {0}, Query: {1}. Exception: {2}. KeyFieldName: {3}. Key: {4}",
                                parent.ID, query, GetExceptionDebugInfo(sqlException), keyFieldName, key));
                        Thread.Sleep(DefaultSleepPeriodToRerunSqlExceptionInGetItemsByKey);
                        List<Item> list;
                        if (QueryItemsAndVerifyUniqueness(parent, keyFieldName, key, query, out list)) return list;
                    }
                    catch (Exception ex)
                    {
                        getItemsByKeyLogger.AddError("Exception after rerun in GetItemsByKey", String.Format(
                            "The GetItemsByKey method met an Exception twice after being rerun. The processing of the item is aborted. Query: {0}. Exception: {1}",
                            query, GetExceptionDebugInfo(ex)));
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    getItemsByKeyLogger.AddError("Exception while querying the item in GetItemsByKey",
                        String.Format(
                            "The GetItemsByKey thrown an exception in trying to query the item. Query: {0}. Exception: {1}",
                            query, GetExceptionDebugInfo(ex)));
                    return null;
                }
                return new List<Item>();
            }
        }

        //public static string DoFastQuerySafe(string fastQuery)
        //{
        //    if (!String.IsNullOrEmpty(fastQuery))
        //    {
        //        var terms = fastQuery.Split('/');
        //        if (terms.Any())
        //        {
        //            string safeFastQuery = String.Empty;
        //            foreach (var term in terms)
        //            {
        //                var tempTerm = term;
        //                if (term != null && (term.Contains(" and ") || term.Contains(" or ") || term.Contains("-")))
        //                {
        //                    tempTerm = "#" + term + "#";
        //                }
        //                safeFastQuery += "/" + tempTerm;
        //            }
        //            return safeFastQuery;
        //        }
        //    }
        //    return fastQuery;
        //}

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

        protected virtual List<Item> GetItemsByTemplate(Item parent, List<CustomItemBase> templates, ref LevelLogger logger)
        {
            var getItemsByLogger = logger.CreateLevelLogger();
            string pattern = "{0}//*[{1}]";
            //pattern = (UseFastQuery ? "fast:" : String.Empty) + pattern; 
            const string tidpattern = "@@templateid='{0}'";
            string tempPattern = string.Empty;
            for (int i = 0; i < templates.Count; i++)
            {
                var template = templates[i];
                string templateId = String.Empty;
                var templateItem = template as TemplateItem;
                if (templateItem != null)
                {
                    templateId = template.ID.ToString();
                }
                else
                {
                    var branchItem = template as BranchItem;
                    if (branchItem != null)
                    {
                        var branchMainItem = branchItem.InnerItem.Children[0];
                        if (branchMainItem != null)
                        {
                            templateId = branchMainItem.TemplateID.ToString();
                        }
                    }
                }
                tempPattern += String.Format(tidpattern, templateId);
                if (i != templates.Count - 1)
                    tempPattern += " or ";
            }
            //var query = String.Format(pattern, DoFastQuerySafe(parent.Paths.FullPath), tempPattern);
            var query = String.Format(pattern, parent.Paths.FullPath, tempPattern);
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
                getItemsByLogger.AddError("An exception occured trying to query the item in GetItemsByTemplate", String.Format("The GetItemsByTemplate thrown an exception in trying to query the item. Query: {0}. Exception: {1}", query, GetExceptionDebugInfo(ex)));
            }
            return new List<Item>();
        }

        public virtual List<Item> GetItemsByKeyAndTemplate(Item parent, string itemKey, List<TemplateItem> templates, ref LevelLogger logger)
        {
            var getItemsByKeyLogger = logger.CreateLevelLogger();
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
                //pattern = (UseFastQuery ? "fast:" : String.Empty) + pattern;
                //var query = String.Format(pattern, DoFastQuerySafe(parent.Paths.FullPath), itemKey, tempPattern);
                var query = String.Format(pattern, parent.Paths.FullPath, itemKey, tempPattern);
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
                        getItemsByKeyLogger.AddInfo("Rerun after SQLException in GetItemsByKeyAndTemplate", String.Format("The GetItemsByKeyAndTemplate method met a SqlException. A rerun is initiated. Parent: {0}, Query: {1}. Exception: {2}. KeyFieldName: {3}. Key: {4}", parent.ID, query, GetExceptionDebugInfo(sqlException)));
                        Thread.Sleep(DefaultSleepPeriodToRerunSqlExceptionInGetItemsByKey);
                        return SitecoreDB.SelectItems(query).ToList();
                    }
                    catch (Exception ex)
                    {
                        getItemsByKeyLogger.AddError("Exception happened twice after being rerun in GetItemsByKeyAndTemplate", String.Format(
                            "The GetItemsByKeyAndTemplate method met an Exception twice after being rerun. The processing of the item is aborted. Query: {0}. Exception: {1}",
                            query, GetExceptionDebugInfo(ex)));
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    getItemsByKeyLogger.AddError("Exception happened in GetItemsByKeyAndTemplate", String.Format(
                            "The GetItemsByKeyAndTemplate thrown an exception in trying to query the item. Query: {0}. Exception: {1}",
                            query, GetExceptionDebugInfo(ex)));
                    return null;
                }
                return new List<Item>();
            }
        }

        /// <summary>
        /// processes each field against the data provided by subclasses
        /// </summary>
        public virtual LevelLogger Process()
        {
            var processLogger = Logger.CreateLevelLogger("Process Import Data");
            string importIdentifier = String.Format("{0} - {1}", String.Format(DateTime.Now.ToLongDateString(), "dd-MMM-yyyy") + " " + String.Format(DateTime.Now.ToLongTimeString(), "hh:mm:ss"), ImportItem.Name);
            Diagnostics.Log.Info(String.Format("DataSync job started - {0}.", importIdentifier), typeof(BaseDataMap));
            //if log messages then don't start Process method.
            if (Logger.HasErrors())
            {
                processLogger.AddError("Import did not run due to error", "The import did not run due to error");
                return Logger;
            }
            IEnumerable<object> importedRows;
            try 
            {
                importedRows = GetImportData();
            } 
            catch (Exception ex) 
            {
                processLogger.AddError("Connection Error", String.Format("Connection Error in Process method. Exception: {0}", GetExceptionDebugInfo(ex)));
                return Logger;
            }

            if (importedRows == null)
            {
                processLogger.AddError("No import data", "The GetImportData method returned a null object. Therefor the import was not performed.");
                return Logger;
            }
            int numberOfRows = importedRows.Count();
            processLogger.SetCounter(IncrementConstants.TotalNumberOfItems, numberOfRows);
            if (numberOfRows < GetMinimumNumberOfRowsRequiredToStartImport())
            {
                processLogger.AddError("Number of rows less than minimum required", String.Format("The GetImportData method encounted that the number of rows in import was lower than the minimum number of rows required. Therefor the import wasn't started. This value is defined in the GetMinimumNumberOfRowsRequiredToStartImport method. This value can be changed by the field 'Minimum Number Of Rows Required To Run The Import' on the import item or by overwriting the method in a custom DataMap object. Therefor the import was not performed. MinimumNumberOfRowsRequiredToStartImport: {0}. NumberOfRows: {1}.", GetMinimumNumberOfRowsRequiredToStartImport(), numberOfRows));
                return Logger;
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
                    if (processLogger.GetCounter(IncrementConstants.ProcessedItems) % minimumNumberOfRowsRequiredToStartImport == 0)
                    {
                        Diagnostics.Log.Info(
                            String.Format("DataSync job - {0} ({1}) - Processed {2}. Total {3} rows.", importIdentifier, ImportToLanguageVersion.Name,
                                          processLogger.GetCounter(IncrementConstants.ProcessedItems), numberOfRows), typeof(BaseDataMap));
                    }
                }
                processLogger.IncrementCounter(IncrementConstants.ProcessedItems);

                if (!ProcessImportRow(importRow, ref processLogger))
                {
                    continue;
                }
                processLogger.IncrementCounter(IncrementConstants.SucceededItems);
            }

            // Disables Items Not Present In Import
            if (IsDisableItemsNotPresentInImport)
            {
                DisableItemsNotPresentInImport(ref processLogger);
            }

            // Starts Publish of Changed Items
            if (IsDoPublish)
            {
                PublishChanges(ref processLogger);
            }
            Diagnostics.Log.Info(String.Format("DataSync job - {0} ended.", importIdentifier), typeof(BaseDataMap));
            return Logger;
        }

        protected virtual void PublishChanges(ref LevelLogger logger)
        {
            var publishAndReIndexChangesLogger = logger.CreateLevelLogger();
            try
            {
                if (String.IsNullOrEmpty(PublishingSourceDatabase))
                {
                    publishAndReIndexChangesLogger.AddError("Publishing Source Database missing",
                        String.Format(
                            "The Publishing Source Database was not provided. This is needed to publish changes."));
                }
                var sourceDb = Factory.GetDatabase(PublishingSourceDatabase);
                if (sourceDb == null)
                {
                    publishAndReIndexChangesLogger.AddError("Publishing Source Database was null",
                        String.Format(
                            "The Publishing Source Database was null. The string provided was: '{0}'. This is needed to publish changes.",
                            PublishingSourceDatabase));
                }
                if (String.IsNullOrEmpty(PublishingTargetDatabase))
                {
                    publishAndReIndexChangesLogger.AddError("Publishing Target Database missing",
                        String.Format(
                            "The Publishing Target Database was not provided. This is needed to publish changes."));
                }
                var targetDb = Factory.GetDatabase(PublishingTargetDatabase);
                if (targetDb == null)
                {
                    publishAndReIndexChangesLogger.AddError("Publishing Target Database was null",
                        String.Format(
                            "The Publishing Target Database was null. The string provided was: '{0}'. This is needed to publish changes.",
                            PublishingTargetDatabase));
                }
                if (publishAndReIndexChangesLogger.HasErrors())
                {
                    return;
                }
                if (PublishRootItems != null && PublishRootItems.Any())
                {
                    foreach (var rootItem in PublishRootItems)
                    {
                        try
                        {
                            switch (PublishingMode)
                            {
                                case PublishingModeSmart:
                                    PublishingManager.PublishItemSmart(rootItem, sourceDb, targetDb, ImportToLanguageVersion, this, ref publishAndReIndexChangesLogger);
                                    break;
                                default:
                                    PublishingManager.PublishFull(rootItem, sourceDb, targetDb, ImportToLanguageVersion, this, ref publishAndReIndexChangesLogger);
                                    break;
                            }
                        }
                        catch (Exception exception)
                        {
                            publishAndReIndexChangesLogger.AddError("Publish exception for RootItem",
                                String.Format(
                                    "An exception occured during a publishing task of a rootitem. RootItem: {0}. Exception: {1}.",
                                    GetItemDebugInfo(rootItem), GetExceptionDebugInfo(exception)));
                        }
                    }
                }
            }
            catch (Exception exception)
            {
                publishAndReIndexChangesLogger.AddError("An exception occured in PublishChanges",
                                String.Format("An exception occured during the PublishChanges method. Exception: {0}.", GetExceptionDebugInfo(exception)));
            }
        }

        protected virtual bool ProcessImportRow(object importRow, ref LevelLogger logger)
        {
            var importRowLogger = logger.CreateLevelLogger("ImportRow");
            importRowLogger.AddData(SharedSource.Logger.Log.Constants.StartTime, DateTime.Now);
            try
            {
                var getItemNameLogger = importRowLogger.CreateLevelLogger("GetItemName");
                string itemName = GetItemName(importRow, ref getItemNameLogger);
                if (String.IsNullOrEmpty(itemName) || getItemNameLogger.HasErrors())
                {
                    getItemNameLogger.AddError("Error generating new item name", String.Format(
                                       "An error occured during generation of a new item name. Therefor the item could not be created. This happened in the Process method in foreach (object importRow in importedRows). ImportRow: {0}.",
                                       GetImportRowDebugInfo(importRow)));
                    importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                    importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                    return false;
                }
                var getParentNodeLogger = importRowLogger.CreateLevelLogger();
                Item parent = GetParentNode(importRow, itemName, ref getParentNodeLogger);
                if (parent.IsNull())
                {
                    importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                    importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                    return false;
                }

                if (!NameFields.Any())
                {
                    importRowLogger.AddError("No 'Name' fields specified", String.Format("There are no 'Name' fields specified. ImportRow: {0}.", GetImportRowDebugInfo(importRow)));
                    importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                    importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                    return false;
                }

                // The item to process
                Item item = null;

                // Find existing item by key defined if the folder should be syncronized
                if (SyncFolderByUpdatingAlreadyExistingItems)
                {
                    var getValueLogger = importRowLogger.CreateLevelLogger("GetValueFromFieldToIdentifyTheSameItemsBy");
                    var keyValue = GetValueFromFieldToIdentifyTheSameItemsBy(importRow, ref getValueLogger);
                    if (getValueLogger.HasErrors())
                    {
                        getValueLogger.AddError("Error while locating key to synchronize", String.Format("An error occured in ProcessImportRow method. {0}",
                                                              GetImportRowDebugInfo(importRow)));
                        importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                        importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                        return false;
                    }

                    if (String.IsNullOrEmpty(keyValue))
                    {
                        importRowLogger.AddError("No value in 'Identify The Same Items By Field'", String.Format(
                                           "The fieldDefinition defined in 'Identify The Same Items By Field' didn't result in any value on the import row. This field is used to identify the item unique. Therefor the following item wasn't imported: {0}.",
                                           GetImportRowDebugInfo(importRow)));
                        importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                        importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                        return false;
                    }
                    importRowLogger.AddKey("ImportRowKey", keyValue);

                    // Check if key exists before
                    var toWhatField = IdentifyTheSameItemsByFieldDefinition.GetNewItemField();
                    if (String.IsNullOrEmpty(toWhatField))
                    {
                        importRowLogger.AddError("The 'To What Field' didn't have a value", String.Format(
                                           "The 'To What Field' field on fieldDefinition defined in 'Identify The Same Items By Field' didn't have any value. This field is used to identify which field on the imported item contains the key and identifies it unique. The import was aborted: {0}.",
                                           GetImportRowDebugInfo(importRow)));
                        importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                        importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                        return false;
                    }
                    var getExistingLogger = importRowLogger.CreateLevelLogger("GetExistingItemsToSyncByKey");
                    var items = GetExistingItemsToSyncByKey(Parent, parent, toWhatField, keyValue, ref getExistingLogger);
                    if (getExistingLogger.HasErrors() || items == null)
                    {
                        getExistingLogger.AddError("Failed to determine if key is used before", String.Format(
                                           "An error occured trying to determine if the item with the key: '{0}' exists before. The processing of that item was aborted. The error happend in GetItemsByKey method. ImportRow: {1}. THe errorMessage was: {2}",
                                           keyValue, GetImportRowDebugInfo(importRow)));
                        importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                        importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                        return false;
                    }
                    if (items.Count() > 1)
                    {
                        importRowLogger.AddError("More than one item with the same key",  String.Format(
                                           "There were more than one items with the same key. The key must be unique. Therefor the following item wasn't imported. Key: {0}. Items: {1}. ImportRow: {2}.",
                                           keyValue, GetItemListDebugInfo(items), GetImportRowDebugInfo(importRow)));
                        importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                        importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                        return false;
                    }
                    // The item exists before
                    if (items.Count == 1)
                    {
                        item = items.First();
                        importRowLogger.AddKey("Item", item);
                        importRowLogger.AddKey("ItemId", item.ID.ToString());
                    }

                    // Try look for the item in the Disabled Items Folder
                    if (item == null)
                    {
                        if (IsDisableItemsNotPresentInImport)
                        {
                            var getItemsByKeyLogger = importRowLogger.CreateLevelLogger();
                            items = GetItemsByKey(DisableItemsFolderItem, toWhatField, keyValue, ref getItemsByKeyLogger, true);
                            if (getItemsByKeyLogger.HasErrors() || items == null)
                            {
                                getItemsByKeyLogger.AddError("Failed to determine if item exists before in DisabledItemsFolder", String.Format(
                                                   "An error occured trying to determine if the item with the key: '{0}' exists before in the DisabledItemsFolder. The processing of that item was aborted. An error occured in GetItemsByKey method used in the IsDisableItemsNotPresentInImport clause. ImportRow: {1}. The errorMessage was: {2}",
                                                   keyValue, GetImportRowDebugInfo(importRow)));
                                importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                                importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                                return false;
                            }
                            // If an item exist we use the first one
                            if (items.Any())
                            {
                                item = items.First();
                                importRowLogger.AddKey("Item", item);
                                importRowLogger.AddKey("ItemId", item.ID.ToString());

                                importRowLogger.AddInfo("Moved from Disabled Items", String.Format("The item was found among the disabled items and moved from '{0}'.", GetItemDebugInfo(item)));
                                importRowLogger.IncrementCounter(IncrementConstants.MovedFromDisabledItems);
                            }
                        }
                    }
                }

                // Create a new item
                if (item == null)
                {
                    if (!CreateItem(importRow, parent, ref item, itemName, ref importRowLogger))
                    {
                        importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                        return false;
                    }
                    importRowLogger.AddKey("Item", item);
                    importRowLogger.AddKey("ItemId", item.ID.ToString());
                    importRowLogger.AddInfo("Item Created", String.Format("The item was newly created under parent '{0}'.", GetItemDebugInfo(parent)));
                }
                // Update the item
                var isUpdatedItem = UpdateItem(item, importRow, itemName, parent, ref importRowLogger);
                importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                UpdateLoggingField(item, importRowLogger);
                if (!isUpdatedItem)
                {
                    importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                    return false;
                }
            }
            catch (Exception ex)
            {
                importRowLogger.AddError("Exception occured in Process method", String.Format(
                                   "An exception occured in Process method in foreach (object importRow in importItems). The processing of the importRow was aborted. ImportRow: '{0}'. Exception: {1}",
                                   GetImportRowDebugInfo(importRow), GetExceptionDebugInfo(ex)));
                importRowLogger.IncrementCounter(IncrementConstants.FailureItems);
                importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
                return false;
            }
            importRowLogger.AddData(SharedSource.Logger.Log.Constants.EndTime, DateTime.Now);
            return true;
        }

        private void UpdateLoggingField(Item item, LevelLogger importRowLogger)
        {
            switch (ItemLoggingStrategy)
            {
                case AlwaysItemId:
                    item.Editing.BeginEdit();
                    var outputHandler = new OutputHandlerToText(importRowLogger);
                    var log = outputHandler.Export();
                    item[Utility.Constants.FieldNameDataSyncLogging] = log;
                    item.Editing.EndEdit();
                    break;
                case OnInfosAndErrorsItemId:
                    if (importRowLogger.HasErrorsOrInfos())
                    {
                        item.Editing.BeginEdit();
                        outputHandler = new OutputHandlerToText(importRowLogger);
                        log = outputHandler.Export();
                        item[Utility.Constants.FieldNameDataSyncLogging] = log;
                        item.Editing.EndEdit();
                    }
                    break;
                case NeverItemId:
                    break;
                case OnErrorsItemId:
                default:
                    if (importRowLogger.HasErrors())
                    {
                        item.Editing.BeginEdit();
                        outputHandler = new OutputHandlerToText(importRowLogger);
                        log = outputHandler.Export(new[]{LogLine.LogType.Error});
                        item[Utility.Constants.FieldNameDataSyncLogging] = log;
                        item.Editing.EndEdit();
                    }
                    break;
            }
        }

        public virtual List<Item> GetExistingItemsToSyncByKey(Item rootItem, Item currentParent, string toWhatField, string keyValue, ref LevelLogger logger)
        {
            var getItemsbyKeyLogger = logger.CreateLevelLogger();
            var items = GetItemsByKey(rootItem, toWhatField, keyValue, ref getItemsbyKeyLogger, true);
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

        protected virtual void DisableItemsNotPresentInImport(ref LevelLogger logger)
        {
            var disableItemsLogger = logger.CreateLevelLogger("DisableItemsNotPresentInImport");
            try
            {
                if (DisableItemsFolderItem == null)
                {
                    disableItemsLogger.AddError("Error", "The 'Disable Items Folder' contained an ID, but the item was null. This setting must point to a folder to move disabled items to. This field must be set when the 'Disable Items Not Present In Import' is checked.");
                    return;
                }
                using (new SecurityDisabler())
                {
                    string errorMessage = String.Empty;
                    var importRows = GetImportData();
                    var itemsInSitecore = GetItemsByTemplate(Parent, ToWhatTemplates.Values.ToList(), ref disableItemsLogger);
                    var itemsKeyList = GetItemsKeyList(importRows, ref disableItemsLogger);
                    if (disableItemsLogger.HasErrors())
                    {
                        disableItemsLogger.AddError("Error", String.Format("In the DisableItemsNotPresentInImport method the GetItemsKeyList failed. The disabling process was terminated. {0}.", errorMessage));
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
                                            MoveItemToDisabledFolder(item, ref disableItemsLogger);
                                        }
                                    }
                                    else
                                    {
                                        disableItemsLogger.AddError("Error",
                                                        String.Format(
                                                            "In the method DisableItemsNotPresentInImport the field: '{0}' was found but was empty. This module assumes that all key field must be shared and contain a value. So please make the field shared or alter the module code :-). Item: '{1}'. itemsInSitecore: '{2}'.",
                                                            IdentifyTheSameItemsByFieldDefinition.GetNewItemField
                                                                (), GetItemDebugInfo(item), itemsInSitecore.Count));
                                        //Logger.FailedDisabledItems += 1;
                                        logger.IncrementCounter(IncrementConstants.FailedDisabledItems);
                                    }
                                }
                                else
                                {
                                    disableItemsLogger.AddError("Error", String.Format(
                                                                "In the method DisableItemsNotPresentInImport the field: '{0}' was not found. This field should be provided since it markes the identifyer of the item. Item: '{1}'. ItemsInSitecore: '{2}'.",
                                                                IdentifyTheSameItemsByFieldDefinition.GetNewItemField
                                                                    (), GetItemDebugInfo(item), itemsInSitecore.Count));
                                    //Logger.FailedDisabledItems += 1;
                                    logger.IncrementCounter(IncrementConstants.FailedDisabledItems);
                                }
                            }
                            catch (Exception ex)
                            {
                                disableItemsLogger.AddError("Error",
                                    String.Format(
                                        "An exception occured in Process method When Disabling Items Not Present In Import in foreach (var item in itemsInSitecore). Item: {0}. Exception: {1}",
                                        GetItemDebugInfo(item), GetExceptionDebugInfo(ex)));
                                //Logger.FailedDisabledItems += 1;
                                logger.IncrementCounter(IncrementConstants.FailedDisabledItems);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                disableItemsLogger.AddError("Error",
                    String.Format(
                        "An exception occured in Process method When Disabling Items Not Present In Import. Exception: {0}",
                        ex.Message));
            }
        }

        protected virtual void MoveItemToDisabledFolder(Item item, ref LevelLogger logger)
        {
            item.Editing.BeginEdit();
            item.MoveTo(DisableItemsFolderItem);
            logger.IncrementCounter(IncrementConstants.DisabledItems);
            item.Editing.EndEdit();
        }

        protected virtual List<string> GetItemsKeyList(IList<object> importRows, ref LevelLogger logger)
        {
            List<string> keyList = new List<string>();
            var existingFieldNames = IdentifyTheSameItemsByFieldDefinition.GetExistingFieldNames();
            var fieldNames = existingFieldNames.ToList();
            var fieldValueDelimiter = IdentifyTheSameItemsByFieldDefinition.GetFieldValueDelimiter();
            foreach (object importRow in importRows)
            {
                var value = GetValueFromFieldToIdentifyTheSameItemsBy(importRow, fieldNames, fieldValueDelimiter, ref logger);
                keyList.Add(value);
            }
            return keyList;
        }

        protected string GetValueFromFieldToIdentifyTheSameItemsBy(object importRow, IEnumerable<string> existingFieldNames, string fieldValueDelimiter, ref LevelLogger logger)
        {
            if (IdentifyTheSameItemsByFieldDefinition != null)
            {
                IEnumerable<string> keyValues = GetFieldValues(existingFieldNames, importRow, ref logger);
                var keyValue = String.Join(fieldValueDelimiter, keyValues.ToArray());
                return keyValue;
            }
            return null;
        }

        protected string GetValueFromFieldToIdentifyTheSameItemsBy(object importRow, ref LevelLogger logger)
        {
            if (IdentifyTheSameItemsByFieldDefinition != null)
            {
                var existingFieldNames = IdentifyTheSameItemsByFieldDefinition.GetExistingFieldNames();
                var fieldValueDelimiter = IdentifyTheSameItemsByFieldDefinition.GetFieldValueDelimiter();
                var getFieldLogger = logger.CreateLevelLogger();
                IEnumerable<string> keyValues = GetFieldValues(existingFieldNames, importRow, ref getFieldLogger);
                var keyValue = String.Join(fieldValueDelimiter, keyValues.ToArray());
                return keyValue;
            }
            return null;
        }

        protected virtual bool CreateItem(object importRow, Item parent, ref Item item, string itemName, ref LevelLogger logger)
        {
            var createItemLogger = logger.CreateLevelLogger();
            try
            {
                if (string.IsNullOrEmpty(itemName))
                {
                    createItemLogger.AddError("Item name could not be parsed", String.Format(
                            "The item name could not be parsed for importRow: {0}. Therefor the item could not be created.",
                            GetImportRowDebugInfo(importRow)));
                    logger.IncrementCounter(IncrementConstants.FailureItems);
                    return false;
                }
                if (parent == null)
                {
                    createItemLogger.AddError("Parent is null", String.Format(
                            "The 'parent' parameter is null. Therefor the item could not be created. ImportRow: {0}.",
                            GetImportRowDebugInfo(importRow)));
                    logger.IncrementCounter(IncrementConstants.FailureItems);
                    return false;
                }
                if (!ToWhatTemplates.Any())
                {
                    createItemLogger.AddError("The 'Import To What Template' item is null", String.Format(
                            "The 'Import To What Template' item is null. ImportRow: {0}. Therefor the item could not be created.",
                            GetImportRowDebugInfo(importRow)));
                    logger.IncrementCounter(IncrementConstants.FailureItems);
                    return false;
                }
                string errorMessage = String.Empty;
                var getToWhatLogger = createItemLogger.CreateLevelLogger();
                CustomItemBase toWhatTemplate = GetToWhatTemplateItem(importRow, ref getToWhatLogger);
                if (getToWhatLogger.HasErrors())
                {
                    getToWhatLogger.AddError("The 'GetToWhatTemplateItem' failed", String.Format(
                            "The 'GetToWhatTemplateItem' method failed with an error. ImportRow: {0}. Therefor the item could not be created. {1}",
                            GetImportRowDebugInfo(importRow), errorMessage));
                    logger.IncrementCounter(IncrementConstants.FailureItems);
                    return false;
                }

                if (toWhatTemplate == null)
                {
                    logger.IncrementCounter(IncrementConstants.FailureItems);
                    return false;
                }

                using (new LanguageSwitcher(ImportToLanguageVersion))
                {
                    var template = toWhatTemplate as TemplateItem;
                    if (template != null)
                    {
                        var templateItem = template;
                        item = parent.Add(itemName, templateItem);
                    }
                    var whatTemplate = toWhatTemplate as BranchItem; 
                    if (whatTemplate != null)
                    {
                        var branchItem = whatTemplate;
                        item = parent.Add(itemName, branchItem);
                    }
                }
                if (item == null)
                {
                    createItemLogger.AddError("Created item was null", String.Format("The new item created was null. ImportRow: {0}.", GetImportRowDebugInfo(importRow)));
                    logger.IncrementCounter(IncrementConstants.FailureItems);
                    return false;
                }
                logger.IncrementCounter("Created Items");
                var addItemToCacheLogger = createItemLogger.CreateLevelLogger();
                var keyValue = GetValueFromFieldToIdentifyTheSameItemsBy(importRow, ref addItemToCacheLogger);
                CachingManager.AddItemToCache(Parent, IdentifyTheSameItemsByFieldDefinition.GetNewItemField(), item, keyValue, ref addItemToCacheLogger);
                if (addItemToCacheLogger.HasErrors())
                {
                    addItemToCacheLogger.AddError("The 'AddItemToCache' failed in the CreateItem", String.Format(
                            "The 'AddItemToCache' method failed with an error. ImportRow: {0}. The item was created, but not added to the cache. {1}",
                            GetImportRowDebugInfo(importRow), errorMessage));
                    logger.IncrementCounter(IncrementConstants.FailureItems);
                    return false;
                }
                return true;
            }
            catch (Exception ex)
            {
                createItemLogger.AddError("Exception in CreateItem", String.Format(
                        "An exception occured in CreateItem. Exception: {0}",
                        GetExceptionDebugInfo(ex)));
                logger.IncrementCounter(IncrementConstants.FailureItems);
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
        protected virtual string GetItemType(object importRow, string itemTypeDateField, ref LevelLogger logger)
        {
            if (!String.IsNullOrEmpty(itemTypeDateField))
            {
                var getFieldLogger = logger.CreateLevelLogger();
                var fieldValues = GetFieldValue(importRow, itemTypeDateField, ref getFieldLogger);
                return fieldValues;
            }
            return String.Empty;
        }

        protected virtual CustomItemBase GetToWhatTemplateItem(object importRow, ref LevelLogger logger)
        {
            var getToWhatLogger = logger.CreateLevelLogger();
            CustomItemBase toWhatTemplate = null;
            var getItemTypeLogger = getToWhatLogger.CreateLevelLogger();
            var type = GetItemType(importRow, ItemTypeDataField, ref getItemTypeLogger);
            if (!String.IsNullOrEmpty(type))
            {
                if (ToWhatTemplates.ContainsKey(type))
                {
                    toWhatTemplate = ToWhatTemplates[type];
                }
                else
                {
                    getToWhatLogger.AddError("The 'type' field on the importRow was not defined.", String.Format(
                            "The 'type' field on the importrow was of a type that is not defined in the import. The type was '{0}'. ImportRow: {1}.",
                            type, GetImportRowDebugInfo(importRow)));
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
                    getToWhatLogger.AddError("Default import template could not be found", String.Format(
                            "A default import template could not be found in the import. Please check that the field 'To What Template' is provided. ImportRow: {0}. Therefor the item could not be created.",
                            GetImportRowDebugInfo(importRow)));
                }
            }
            return toWhatTemplate;
        }

        protected virtual bool UpdateItem(Item item, object importRow, string itemName, Item parentItem, ref LevelLogger logger)
        {
            var updateItemLogger = logger.CreateLevelLogger("Update Item");
            var updatedItem = false;
            try
            {
                using (new LanguageSwitcher(ImportToLanguageVersion))
                {
                    using (new SecurityDisabler())
                    {
                        if (AddLanguageVersionIfNoneExists(item, ref logger))
                        {
                            updateItemLogger.IncrementCounter(IncrementConstants.LanguageVersionAdded);
                        }

                        // Move the item to it's correct position
                        if (!DoNotMoveItemToParentFromImport)
                        {
                            if (item.ParentID != parentItem.ID)
                            {
                                MoveItem(item, parentItem, ref updateItemLogger);
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
                            var checkIfItemNameLogger = updateItemLogger.CreateLevelLogger();
                            var newItemName = CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot(importRow, item, itemName, itemName, parentItem, 1, ref checkIfItemNameLogger);
                            if (checkIfItemNameLogger.HasErrors())
                            {
                                checkIfItemNameLogger.AddError("Error in 'CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot'", String.Format(
                                         "The 'CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot' method failed with an error. The itemname was not changed to a postfixnumber since the check failed. But the Update process was continued. ImportRow: {0}. ErrorMessage: {1}",
                                         GetImportRowDebugInfo(importRow), errorMessage));
                                logger.IncrementCounter(IncrementConstants.FailureItems);
                            }
                            if (!itemName.Equals(newItemName))
                            {
                                updateItemLogger.AddInfo("Item name regenerated", String.Format("The item name '{0}' was regenerated to '{1}' to avoid duplicate item name.", itemName, newItemName));
                                itemName = newItemName;
                                logger.IncrementCounter(IncrementConstants.ItemNameRegenerated);
                            }
                        }
                        if(OnDuplicateItemNamesGeneratePostfixNumber || UpdateItemName)
                        {
                            if (item.Name != itemName)
                            {
                                item.Editing.BeginEdit();
                                updateItemLogger.AddInfo("Item name updated", String.Format("The item name '{0}' was updated to '{1}'.", item.Name, itemName));
                                item.Name = itemName;
                                item.Editing.EndEdit();
                                logger.IncrementCounter(IncrementConstants.RenamedItems);
                            }
                        }

                        if (ValidateIfItemKeyIsUnique)
                        {
                            if (!IsItemNameUnique(parentItem, item, ref updateItemLogger))
                            {
                                // The validation of the item found that there exists more than one item under the parent with the same item key.
                                // We logged the error, but continue processing item. The duplicated item names must be corrected manually.
                                updateItemLogger.IncrementCounter(IncrementConstants.FailureItems);
                                return false;
                            }
                        }

                        // Change template if type has changed and we are not explicit told not to
                        if (!DoNotChangeTemplate && ToWhatTemplates.Count > 0)
                        {
                            string errorMessage = String.Empty;
                            var getToWhatLogger = updateItemLogger.CreateLevelLogger();
                            var toWhatTemplate = GetToWhatTemplateItem(importRow, ref getToWhatLogger);
                            if (getToWhatLogger.HasErrors())
                            {
                                getToWhatLogger.AddError("Failure in 'GetToWhatTemplateItem' method", String.Format(
                                        "The 'GetToWhatTemplateItem' method failed with an error. ImportRow: {0}. Therefor it was not possible to determine wheether the item template should change. The Change template process was aborted as well as the rest of the update item process. {1}",
                                        GetImportRowDebugInfo(importRow), errorMessage));
                                updateItemLogger.IncrementCounter(IncrementConstants.FailureItems);
                                return false;
                            }
                            if (toWhatTemplate != null)
                            {
                                var templateItem = toWhatTemplate as TemplateItem;
                                if (templateItem != null)
                                {
                                    if (item.TemplateID != toWhatTemplate.ID)
                                    {
                                        string fromTemplateDebugInfo = GetTemplateDebugInfo(item.Template);
                                        item.Editing.BeginEdit();
                                        item.ChangeTemplate(templateItem);
                                        updateItemLogger.AddInfo("Change Template",
                                            String.Format("Changed template from '{0}' to '{1}'.", fromTemplateDebugInfo,
                                                GetTemplateDebugInfo(item.Template)));
                                        item.Editing.EndEdit();
                                        updateItemLogger.IncrementCounter(IncrementConstants.ChangedTemplate);
                                    }
                                }
                            }
                            else
                            {
                                updateItemLogger.IncrementCounter(IncrementConstants.FailureItems);
                                return false;
                            }
                        } 

                        // Add in the field mappings
                        var updatedFields = false;
                        var failedItem = false;
                        item.Editing.BeginEdit();
                        
                        foreach (IBaseField fieldDefinition in FieldDefinitions)
                        {
                            var fieldLogger = updateItemLogger.CreateLevelLogger("Field");
                            string errorMessage = String.Empty;
                            var existingFieldNames = fieldDefinition.GetExistingFieldNames();
                            var fieldValueDelimiter = fieldDefinition.GetFieldValueDelimiter();
                            fieldLogger.AddKey("FieldName", fieldDefinition.GetNewItemField());
                            fieldLogger.AddKey("ImportRowFieldName", String.Join(fieldValueDelimiter, fieldDefinition.GetExistingFieldNames().ToArray()));

                            var getFieldLogger = fieldLogger.CreateLevelLogger();
                            IEnumerable<string> values = GetFieldValues(existingFieldNames, importRow, ref getFieldLogger);

                            if (getFieldLogger.HasErrors())
                            {
                                getFieldLogger.AddError("Error in extracting a value from a field", String.Format("An error occured in extracting the values from a specific field: '{0}' on the item: '{1}'. The processing of the item is aborted and no fields has been updated. ErrorMessage: {2}", 
                                    fieldDefinition, GetItemDebugInfo(item), errorMessage));
                                updateItemLogger.IncrementCounter(IncrementConstants.FailureItems);
                                return false;
                            }
                            bool updateField;
                            var importValue = String.Join(fieldValueDelimiter, values.ToArray());
                            var fieldDefinitionLogger = fieldLogger.CreateLevelLogger();
                            fieldDefinition.FillField(this, importRow, ref item, importValue, out updateField, ref fieldDefinitionLogger);
                            
                            if (fieldDefinitionLogger.HasErrors())
                            {
                                fieldDefinitionLogger.AddError("Error processing a field", String.Format("An error occured in processing a field on the item: '{0}'. The processing of the item in itself is not aborted and the rest of the fields has been processed.", GetItemDebugInfo(item)));
                                failedItem = true;
                            }
                            if (updateField)
                            {
                                fieldLogger.AddInfo("Update Field", String.Format("The field '{0}' was updated.", fieldDefinition.GetNewItemField()));
                                updatedFields = true;
                            }
                        }
                        if (updatedFields)
                        {
                            item.Editing.EndEdit();
                            updateItemLogger.AddInfo("Updated Field on Item", String.Format("The item had fields that were updated."));
                            updateItemLogger.IncrementCounter(IncrementConstants.UpdatedFields);
                        }
                        else
                        {
                            item.Editing.CancelEdit();
                        }
                        if (failedItem)
                        {
                            updateItemLogger.IncrementCounter(IncrementConstants.FailureItems);
                            return false;
                        }

                        // Calls the subclass method to handle custom fields and properties
                        bool processedCustomData;
                        if (!ProcessCustomData(ref item, importRow, out processedCustomData))
                        {
                            updateItemLogger.IncrementCounter(IncrementConstants.FailureItems);
                            return false;
                        }
                        if (processedCustomData)
                        {
                            updateItemLogger.AddInfo("Custom Data processed", String.Format("Custom data was processed on item."));
                            updateItemLogger.IncrementCounter(IncrementConstants.ProcessedCustomData);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                updateItemLogger.AddError("Exception occured in UpdateItem method", String.Format("An exception occured in UpdateItem. ImportRow: {0}. Item: {1}, ItemName: {2}, Exception: {3}", GetImportRowDebugInfo(importRow), GetItemDebugInfo(item), itemName, GetExceptionDebugInfo(ex)));
                updateItemLogger.IncrementCounter(IncrementConstants.FailureItems);
                return false;
            }
            return true;
        }

        protected virtual bool AddLanguageVersionIfNoneExists(Item item, ref LevelLogger logger)
        {
            using (new SecurityDisabler())
            {
                Item languageVersion = item.Versions.GetLatestVersion(ImportToLanguageVersion);
                if (languageVersion == null || languageVersion.Versions.Count == 0)
                {
                    item.Editing.BeginEdit();
                    var addedVersionItem = item.Versions.AddVersion();
                    item.Editing.EndEdit();
                    logger.AddInfo("Add Language Version", String.Format("Added a language version '{0}' to the item.", addedVersionItem.Language.Name));
                    return true;
                }
            }
            return false;
        }

        protected virtual void MoveItem(Item item, Item targetParentItem, ref LevelLogger logger)
        {
            using (new SecurityDisabler())
            {
                item.Editing.BeginEdit();
                logger.AddInfo("Moved Item", String.Format("Moved item from '{0}' to '{1}'.", GetItemDebugInfo(item), GetItemDebugInfo(targetParentItem)));
                item.MoveTo(targetParentItem);
                item.Editing.EndEdit();
                //Logger.MovedItems += 1;
                logger.IncrementCounter(IncrementConstants.MovedItems);
            }
        }

        public virtual bool IsItemNameUnique(Item parentItem, Item item, ref LevelLogger logger)
        {
            var isItemNameLogger = logger.CreateLevelLogger();
            if (parentItem != null)
            {
                var existingItemsWithSameItemName = parentItem.Axes.SelectItems(String.Format("./*[@@key='{0}']", item.Key));
                if (existingItemsWithSameItemName != null)
                {
                    if (existingItemsWithSameItemName.Count() > 1)
                    {
                        //LogBuilder.Log("Error", String.Format("The method IsItemNameIsUnique method found {0} items with the same key under the parentitem: '{1}'. Please correct the item names. Items: '{2}'. ", existingItemsWithSameItemName.Count(), GetItemDebugInfo(parentItem), GetItemListDebugInfo(existingItemsWithSameItemName)));
                        isItemNameLogger.AddError("Duplicate items found under Parent in IsItemNameUnique", String.Format("The method IsItemNameIsUnique method found {0} items with the same key under the parentitem: '{1}'. Please correct the item names. Items: '{2}'. ", existingItemsWithSameItemName.Count(), GetItemDebugInfo(parentItem), GetItemListDebugInfo(existingItemsWithSameItemName)));
                        return false;
                    }
                    return true;
                }
                // This cannot happen, but if it does. Then throw error.
                //LogBuilder.Log("Error", String.Format("In the IsItemNameIsUnique method the item processed was not found in the parent spesified. This should not happen. Item: '{0}'. ", GetItemDebugInfo(item)));
                isItemNameLogger.AddError("The item wasn't found in the parent spesified in IsItemNameIsUnique", String.Format("In the IsItemNameIsUnique method the item processed was not found in the parent spesified. This should not happen. Item: '{0}'. ", GetItemDebugInfo(item)));
                return false;
            }
            //LogBuilder.Log("Error", String.Format("In the IsItemNameIsUnique method the parent item was null. Item: '{0}'. ", GetItemDebugInfo(item)));
            isItemNameLogger.AddError("The parent item was null in IsItemNameIsUnique", String.Format("In the IsItemNameIsUnique method the parent item was null. Item: '{0}'. ", GetItemDebugInfo(item)));
            return false;
        }

        public virtual string GetConcatenatedItemNameFromImport(object importRow, ref LevelLogger logger)
        {
            var strItemName = new StringBuilder();
            foreach (string nameField in NameFields) 
            {
                try
                {
                    var getFieldValueLogger = logger.CreateLevelLogger();
                    var fieldValue = GetFieldValue(importRow, nameField, ref getFieldValueLogger);
                    strItemName.Append(fieldValue);
                    strItemName.Append(' ');
                } 
                catch (ArgumentException ex) 
                {
                    if (string.IsNullOrEmpty(ItemNameDataField))
                    {
                        //errorMessage += String.Format("In method GetConcatenatedItemNameFromImport the 'Name' field is empty. Exception: {0}", GetExceptionDebugInfo(ex));
                        logger.AddError("Name field empty in GetConcatenatedItemNameFromImport", String.Format("In method GetConcatenatedItemNameFromImport the 'Name' field is empty. Exception: {0}", GetExceptionDebugInfo(ex)));
                    }
                    else
                    {
                        //errorMessage += String.Format("In method GetConcatenatedItemNameFromImport the field name: '{0}' does not exist in the import row. Exception: {1}.", nameField, GetExceptionDebugInfo(ex));
                        logger.AddError("Field does not exist in GetConcatenatedItemNameFromImport", String.Format("In method GetConcatenatedItemNameFromImport the field name: '{0}' does not exist in the import row. Exception: {1}.", nameField, GetExceptionDebugInfo(ex)));
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
                //errorMessage += String.Format("In method GetConcatenatedItemNameFromImport the 'strItemName' was empty. The fields that didn't contain any value was {0}.", emptyFieldNames);
                logger.AddError("The 'strItemName' was empty in GetConcatenatedItemNameFromImport", String.Format("In method GetConcatenatedItemNameFromImport the 'strItemName' was empty. The fields that didn't contain any value was {0}.", emptyFieldNames));
            }
            return trimmedItemName;
        }

        /// <summary>
        /// creates an item name based on the name field values in the importRow
        /// </summary>
        public virtual string GetItemName(object importRow, ref LevelLogger logger)
        {
            var getConcatenatedLogger = logger.CreateLevelLogger();
            try
            {
                string strItemName = GetConcatenatedItemNameFromImport(importRow, ref getConcatenatedLogger);
                return StringUtility.GetNewItemName(strItemName, ItemNameMaxLength);
            }
            catch (Exception ex)
            {
                getConcatenatedLogger.AddError("GetItemName Exception", String.Format("In method GetItemName an exception occured in GetItemName. Exception: {0}", GetExceptionDebugInfo(ex)));
                //errorMessage +=
                //    String.Format("In method GetItemName an exception occured in GetItemName. Exception: {0}",
                //                  GetExceptionDebugInfo(ex));
            }
            return String.Empty;
        }

        protected virtual Item[] QueryItemsByKey(Item rootItem, string key, ref LevelLogger logger)
        {
            var queryItemsByKeyLogger = logger.CreateLevelLogger();
            try
            {
                return rootItem.Axes.SelectItems(String.Format("./*[@@key='{0}']", key.ToLower()));
            }
            catch (Exception ex)
            {
                queryItemsByKeyLogger.AddError("An error occured trying query the item in QueryItemsByKey", String.Format("An error occured trying to query the item by key in the QueryItemsByKey method. RootItem: {0}. Key: {1}. Exception: {2}", rootItem.ID, key, GetExceptionDebugInfo(ex)));
                return null;
            }
        }

        /// <summary>
        /// creates an item name based on the name field values in the importRow
        /// </summary>
        public string CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot(object importRow, Item item, string itemName, string newItemName, Item parentItem, int? postFixCount, ref LevelLogger logger)
        {
            var checkIfItemNameLogger = logger.CreateLevelLogger("CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot");
            if (parentItem != null)
            {
                var queryItemsLogger = checkIfItemNameLogger.CreateLevelLogger();
                var existingItemsWithSameItemName = QueryItemsByKey(parentItem, newItemName.ToLower(), ref queryItemsLogger);
                if (queryItemsLogger.HasErrors())
                {
                    queryItemsLogger.AddError("Error while querying items in QueryItemsByKey", String.Format("In the CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot method an error occured querying items. ItemName: {0}. NewItemName: {1}. Item: '{2}'. ", itemName, newItemName, GetItemDebugInfo(item)));
                    return null;
                }
                if (existingItemsWithSameItemName != null)
                {
                    if (existingItemsWithSameItemName.Count() > 2)
                    {
                        checkIfItemNameLogger.AddError("Found duplicate items with same key", String.Format("In the CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot method there were found {0} items with the same key '{1}'. This cannot be fixed in the import. Please correct the item names. NewItemName: {2}. Item: '{3}'. ", existingItemsWithSameItemName.Count(), itemName, newItemName, GetItemDebugInfo(item)));
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
                                                                         postFixCount+1, ref checkIfItemNameLogger);
                        }
                        else
                        {
                            // This cannot happen, but if it does. Then throw error.
                            checkIfItemNameLogger.AddError("Found 2 items with the same key", String.Format("In the CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot method there were found 2 items with the same key and the processed item was not part of them. Item: '{0}'. ItemName: {1}. NewItemName: {2}", GetItemDebugInfo(item), itemName, newItemName));
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
                                                                         postFixCount + 1, ref checkIfItemNameLogger);
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
                checkIfItemNameLogger.AddError("Parent was null in CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot", String.Format("In the CheckIfItemNameIsUniqueAndGeneratePostFixNumberIfNot method the parent was null. Cannot check if item name is unique if not spesified. This should not happen. Item: '{0}'. ItemName: {1}. NewItemName: {2}.", GetItemDebugInfo(item), itemName, newItemName));
                return null;    
            }
        }

        /// <summary>
        /// retrieves all the import field values specified
        /// </summary>
        public IEnumerable<string> GetFieldValues(IEnumerable<string> fieldNames, object importRow, ref LevelLogger logger) {
            var list = new List<string>();
            foreach (string f in fieldNames)
            {
                var getFieldValuesLogger = logger.CreateLevelLogger(); 
                try
                {
                    var value = GetFieldValue(importRow, f, ref getFieldValuesLogger);
                    if (value == null)
                    {
                        //errorMessage += String.Format("In GetFieldValues method the field value was null. This should not happen. An empty string was added. FieldName: '{0}'", f);
                        getFieldValuesLogger.AddError("Field value was null in GetFieldValues", String.Format("In GetFieldValues method the field value was null. This should not happen. An empty string was added. FieldName: '{0}'", f));
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
                        //errorMessage += String.Format("In GetFieldValues method the 'From' field name is empty. Exception: {0}", GetExceptionDebugInfo(ex));
                        getFieldValuesLogger.AddError("The 'From' field name empty in GetFieldValues", String.Format("In GetFieldValues method the 'From' field name is empty. Exception: {0}", GetExceptionDebugInfo(ex)));
                    }
                    else
                    {
                        //errorMessage += String.Format("In GetFieldValues method the field name: '{0}' does not exist in the import row. Exception: {1}", f, GetExceptionDebugInfo(ex));
                        getFieldValuesLogger.AddError("FieldName do not exist on import row in GetFieldValues", String.Format("In GetFieldValues method the field name: '{0}' does not exist in the import row. Exception: {1}", f, GetExceptionDebugInfo(ex)));
                    }
                } 
            }
            return list;
        }

        /// <summary>
        /// Gets the parent to use to create the new item below. Will create folders based on name or date if configured to. Default is to use the root as parent.
        /// </summary>
        protected virtual Item GetParentNode(object importRow, string itemName, ref LevelLogger logger)
        {
            var getParentNode = logger.CreateLevelLogger();
            if (FolderByParentHierarchy)
            {
                if (String.IsNullOrEmpty(IdentifyParentByWhatFieldOnImportRow))
                {
                    getParentNode.AddError("Error",
                                   String.Format(
                                       "The 'IdentifyParentByWhatFieldOnImportRow' setting was not set. This setting must be set to identify which field on the import row to get the parent from. Therefor the following item wasn't imported. ImportRow: {0}.",
                                       GetImportRowDebugInfo(importRow)));
                    return null;
                }
                if (String.IsNullOrEmpty(IdentifyParentByWhatFieldOnParent))
                {
                    getParentNode.AddError("Error",
                                   String.Format(
                                       "The 'IdentifyParentByWhatFieldOnParent' setting was not set. This setting must be set to identify which field to identify the parent item with. Therefor the following item wasn't imported. ImportRow: {0}.",
                                       GetImportRowDebugInfo(importRow)));
                    return null;
                }

                string errorMessage = String.Empty;
                var getFieldValueLogger = getParentNode.CreateLevelLogger();
                var identifyParentByWhatFields = IdentifyParentByWhatFieldOnImportRow.Split(',');
                var values = GetFieldValues(identifyParentByWhatFields, importRow, ref getFieldValueLogger);
                var identifyParentFieldNameOnImportRow = String.Join(String.Empty, values.ToArray());
                if (getFieldValueLogger.HasErrors())
                {
                    getFieldValueLogger.AddError("Error",
                                   String.Format(
                                       "The value drawn from field '{0}' resultet in an error. Therefor the parent was not found. The processing of the item was aborted. ImportRow: {1}.",
                                       IdentifyParentByWhatFieldOnImportRow, GetImportRowDebugInfo(importRow)));
                    return null;
                }

                if (String.IsNullOrEmpty(identifyParentFieldNameOnImportRow))
                {
                    getFieldValueLogger.AddError("Error",
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
                var getItemsByKeyLogger = getParentNode.CreateLevelLogger();
                var items = GetItemsByKey(Parent, IdentifyParentByWhatFieldOnParent, identifyParentFieldNameOnImportRow, ref getItemsByKeyLogger, true);
                if (getItemsByKeyLogger.HasErrors() || items == null)
                {
                    getItemsByKeyLogger.AddError("Error", String.Format("An error occured in locating the parent item of item: '{0}'. The processing of the item was aborted. The error occured in GetItemsByKey method used in the GetParentNode method. ImportRow: {1}. ErrorMessage: {2}",
                        itemName, GetImportRowDebugInfo(importRow), errorMessage));
                    return null;
                }
                if (items.Count() > 1)
                {
                    getItemsByKeyLogger.AddError("Error",
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
                getParentNode.AddError("Error",
                               string.Format(
                                   "Could not find the parent node in GetParentNode method. The item was not imported. ItemName: {0}. ImportRow: {1}.",
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
                    var getFieldValueLogger = getParentNode.CreateLevelLogger();
                    dateValue = GetFieldValue(importRow, DateField, ref getFieldValueLogger);
                    if (getFieldValueLogger.HasErrors())
                    {
                        getFieldValueLogger.AddError("Error", String.Format("The date field used in FolderByDate setting resultet in an error. DateField: {0}. dateValue: {1}. {2}.", DateField, dateValue, errorMessage));
                        return null;
                    }
                }
                catch (ArgumentException ex)
                {
                    if (string.IsNullOrEmpty(DateField))
                    {
                        getParentNode.AddError("Error", String.Format("The date name field is empty. Exception: {0}", GetExceptionDebugInfo(ex)));
                        return null;
                    }
                    else
                    {
                        getParentNode.AddError("Error", String.Format("The field name: '{0}' does not exist in the import row. Exception: {1}.",
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
                        getParentNode.AddError("Error", "the date value could not be parsed");
                        return null;
                    }
                }
                else
                {
                    getParentNode.AddError("Error", "the date value was empty");
                    return null;
                }
            }
            else if (FolderByName)
            {
                return GetNameParentNode(Parent, itemName.Substring(0, 1), FolderTemplate);
            }
            // Return Root folder if none of the other options is selected
            return Parent;
        }
        #endregion Methods
	}
}
