using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Exceptions;
using Sitecore.SharedSource.DataSync;
using Sitecore.Data.Items;
using Sitecore.Data;
using System.Data;
using Sitecore.SharedSource.DataSync.Extensions;
using System.Collections;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.DataSync.Utility;
using Sitecore.Data.Fields;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields {
	
    /// <summary>
    /// This uses imported values to match by name an existing content item in the list provided
    /// then stores the GUID of the existing item
    /// </summary>
    public class ListValueToGuidMatchOnDisplayName : ToText {
        #region Properties

        private bool _DoNotRequireValueMatch;

        public bool DoNotRequireValueMatch {
			get {
                return _DoNotRequireValueMatch;
			}
			set {
                _DoNotRequireValueMatch = value;
			}
		}

        private string _SourceList;
        /// <summary>
        /// This is the list that you will compare the imported values against
        /// </summary>
        public string SourceList
        {
            get
            {
                return _SourceList;
            }
            set
            {
                _SourceList = value;
            }
        }

        private string _MultiListDelimiter;

        /// <summary>
        /// This is the list that you will compare the imported values against
        /// </summary>
        public string MultiListDelimiter
        {
            get
            {
                return _MultiListDelimiter;
            }
            set
            {
                _MultiListDelimiter = value;
            }
        }

		#endregion Properties

		#region Constructor
        
        public ListValueToGuidMatchOnDisplayName(Item i)
            : base(i)
        {
			//stores the source list value
            SourceList = i.Fields["Source List"].Value;
            if (String.IsNullOrEmpty(SourceList))
            {
                throw new InvalidValueException(String.Format("The 'Source List' was not provided. Therefor it wasn't possible to match the importValue with a sourcelist. ItemId: {0}.", i.ID));
            }

            DoNotRequireValueMatch = i.Fields["Do Not Require Value Match"].Value == "1";

            MultiListDelimiter = i["MultiList Delimiter"];
        }

		#endregion Constructor

		#region Methods

        public virtual IEnumerable<Item> GetMatchingChildItem(BaseDataMap map, Item listParent,string importValue)
        {
            IEnumerable<Item> t = (from Item c in listParent.GetChildren()
                                   where c.DisplayName.ToLower().Equals(StringUtility.GetNewItemName(importValue, map.ItemNameMaxLength))
                                   select c).ToList();
            return t;
        }

        /// <summary>
        /// uses the import value to search for a matching item in the SourceList and then stores the GUID
        /// </summary>
        /// <param name="map">provides settings for the import</param>
        /// <param name="importRow"></param>
        /// <param name="newItem">newly created item</param>
        /// <param name="importValue">imported value to match</param>
        public override void FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField, ref LevelLogger logger)
        {
            var fillFieldLogger = logger.CreateLevelLogger();
            updatedField = false;
            if (IsRequired)
            {
                if (String.IsNullOrEmpty(importValue))
                {
                    fillFieldLogger.AddError(CategoryConstants.ImportedValueToFieldWasEmpty, String.Format("The Item '{0}' of template type: '{1}', field '{2}', but the imported value '{3}' was empty. This field must be provided when the field is required. The field was not updated.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue));
                    return;
                }
            }
            if (!ID.IsID(SourceList))
            {
                fillFieldLogger.AddError(CategoryConstants.SourceListNotAnValidSitecoreId, String.Format(
                        "The 'Source List' provided was not an valid Sitecore ID. SourceList: {0}. The Fill Field method was aborted and the fieldvalue was not updated.", SourceList));
                return;
            }
            //get parent item of list to search
            Item i = newItem.Database.GetItem(SourceList);
			if (i != null) 
            {
			    if (!String.IsNullOrEmpty(importValue))
			    {
                    Field f = newItem.Fields[NewItemField];
                    if (f != null)
                    {
                        var guidValue = String.Empty;
                        if (!String.IsNullOrEmpty(MultiListDelimiter))
                        {
                            var keys = importValue.Split(new[] {MultiListDelimiter}, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var keyValue in keys)
                            {
                                var trimmedKeyValue = keyValue.Trim();
                                string errorMessage = String.Empty;
                                var id = GetLookupId(map, importRow, ref newItem, trimmedKeyValue, i, ref fillFieldLogger);
                                if (!String.IsNullOrEmpty(errorMessage))
                                {
                                    fillFieldLogger.AddError(CategoryConstants.ErrorToLocateTheLookupIdInMultiList, String.Format("An error occured in trying to locate the Lookup id in a MultiList attempt. The MultiListDelimiter: {0}. The importValue was '{1}'. Item: {2}. The errormessage was: {3}.",
                                    MultiListDelimiter, importValue, map.GetItemDebugInfo(newItem), errorMessage));
                                    return;
                                }
                                guidValue += id + "|";
                            }
                            if (guidValue.EndsWith("|"))
                            {
                                guidValue = guidValue.Substring(0, guidValue.Length - 1);
                            }
                        }
                        else
                        {
                            string errorMessage = String.Empty;
                            guidValue = GetLookupId(map, importRow, ref newItem, importValue, i, ref fillFieldLogger);
                            if (!String.IsNullOrEmpty(errorMessage))
                            {
                                fillFieldLogger.AddError(CategoryConstants.ErrorToLocateTheLookupId, String.Format("An error occured in trying to locate the Lookup id. The importValue was '{0}'. Item: {1}. The errormessage was: {2}.",
                                    importValue, map.GetItemDebugInfo(newItem), errorMessage));
                                return;
                            }
                        }
                        if (String.IsNullOrEmpty(guidValue))
                        {
                            if (!DoNotRequireValueMatch)
                            {
                                fillFieldLogger.AddError(CategoryConstants.ImportedValueDidntResultInAIdToStore, String.Format(
                                    "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' didn't result in a ID to store.",
                                    map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue));
                                return;
                            }
                        }
                        if (f.Value != guidValue)
                        {
                            f.Value = guidValue;
                            updatedField = true;
                        }
                    }
                    if (IsRequired && f == null)
                    {
                        fillFieldLogger.AddError(CategoryConstants.RequiredFieldNotFoundOnItem, String.Format(
                                "The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.",
                                map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField));
                        return;
                    }
                }
			    else
			    {
                    if (IsRequired)
                    {
                        fillFieldLogger.AddError(CategoryConstants.TheGuidDidntResultInAHitForLookup, String.Format("The Item '{0}' of template type: '{1}' had a Guid field '{2}' where the imported value '{3}' didn't result any hit. Because the 'Is Required Field' is checked there must be found a value i Sitecore. The field was not updated.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue));
                        return;
                    }
                    if (newItem[NewItemField] != importValue)
                    {
                        newItem[NewItemField] = importValue;
                        updatedField = true;
                    }
			    }
			}
            if (IsRequired && i == null)
            {
                fillFieldLogger.AddError(CategoryConstants.TheGuidFieldHadASourcelistThatWasNull, String.Format("The Item '{0}' of template type: '{1}' had a Guid field '{2}' for which SourceList was null. This SourceList must be present because the 'Is Required Field' is checked.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField));
                return;
            }
		}

        private string GetLookupId(BaseDataMap map, object importRow, ref Item newItem, string importValue, Item sourceListRootItem, ref LevelLogger logger)
        {
            var getLookupIdLogger = logger.CreateLevelLogger();
            IEnumerable<Item> t = GetMatchingChildItem(map, sourceListRootItem, importValue);
            if (t.Count() > 1)
            {
                getLookupIdLogger.AddError(CategoryConstants.ImportedValueResultInMoreThanOneLookupItem, String.Format(
                        "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' did result in more that one lookup item. The field was not updated.",
                        map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue));
                return String.Empty;
            }
            if (t.Count() == 1)
            {
                var guid = t.First().ID.ToString();
                if (String.IsNullOrEmpty(guid))
                {
                    getLookupIdLogger.AddError(CategoryConstants.ImportedValueDidntResultInAIdToStore, String.Format(
                                "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' didn't result in a ID to store.",
                                map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue));
                    return String.Empty;
                }
                return guid;
            }
            else
            {
                if (!DoNotRequireValueMatch)
                {
                    getLookupIdLogger.AddError(CategoryConstants.DidntLocateALookupItemWithTheValue, String.Format("The Item '{0}' of template type: '{1}' didn't locate a lookup Item with the value '{2}'.",
                        newItem.ID.ToString(), newItem.TemplateName, importValue));
                    return String.Empty;
                }
            }
            return String.Empty;
        }
        #endregion Methods
	}
}
