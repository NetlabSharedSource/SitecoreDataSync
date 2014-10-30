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
        public override string FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField)
        {
            updatedField = false;
            if (IsRequired)
            {
                if (String.IsNullOrEmpty(importValue))
                {
                    return String.Format("The Item '{0}' of template type: '{1}', field '{2}', but the imported value '{3}' was empty. This field must be provided when the field is required. The field was not updated.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
                }
            }
            if (!ID.IsID(SourceList))
            {
                return String.Format(
                        "The 'Source List' provided was not an valid Sitecore ID. SourceList: {0}. The Fill Field method was aborted and the fieldvalue was not updated.",
                        SourceList);
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
                                IEnumerable<Item> t = GetMatchingChildItem(map, i, importValue);
                                string errorMessage = String.Empty;
                                var id = GetLookupId(map, importRow, ref newItem, keyValue, i, ref errorMessage);
                                if (!String.IsNullOrEmpty(errorMessage))
                                {
                                    return String.Format("An error occured in trying to locate the Lookup id in a MultiList attempt. The MultiListDelimiter: {0}. The importValue was '{1}'. Item: {2}. The errormessage was: {3}.",
                                    MultiListDelimiter, importValue, map.GetItemDebugInfo(newItem), errorMessage);
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
                            guidValue = GetLookupId(map, importRow, ref newItem, importValue, i, ref errorMessage);
                            if (!String.IsNullOrEmpty(errorMessage))
                            {
                                return String.Format("An error occured in trying to locate the Lookup id. The importValue was '{0}'. Item: {1}. The errormessage was: {2}.",
                                    importValue, map.GetItemDebugInfo(newItem), errorMessage);
                            }
                        }
                        if (String.IsNullOrEmpty(guidValue))
                        {
                            if (!DoNotRequireValueMatch)
                            {
                                return String.Format(
                                    "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' didn't result in a ID to store.",
                                    map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
                            }
                        }
                        if (f.Value != guidValue)
                        {
                            newItem.Editing.BeginEdit();
                            f.Value = guidValue;
                            updatedField = true;
                            newItem.Editing.EndEdit();
                        }
                    }
                    if (IsRequired && f == null)
                    {
                        return String.Format(
                                "The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.",
                                map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField);
                    }
                }
			    else
			    {
                    if (IsRequired)
                    {
                        return String.Format("The Item '{0}' of template type: '{1}' had a Guid field '{2}' where the imported value '{3}' didn't result any hit. Because the 'Is Required Field' is checked there must be found a value i Sitecore. The field was not updated.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
                    }
                    if (newItem[NewItemField] != importValue)
                    {
                        newItem.Editing.BeginEdit();
                        newItem[NewItemField] = importValue;
                        newItem.Editing.EndEdit();
                    }
			    }
			}
            if (IsRequired && i == null)
            {
                return String.Format("The Item '{0}' of template type: '{1}' had a Guid field '{2}' for which SourceList was null. This SourceList must be present because the 'Is Required Field' is checked.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField);
            }
            return string.Empty;
		}

        private string GetLookupId(BaseDataMap map, object importRow, ref Item newItem, string importValue, Item sourceListRootItem, ref string errorMessage)
        {
            IEnumerable<Item> t = GetMatchingChildItem(map, sourceListRootItem, importValue);
            if (t.Count() > 1)
            {
                errorMessage += String.Format(
                        "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' did result in more that one lookup item. The field was not updated.",
                        map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
                return null;
            }
            if (t.Count() == 1)
            {
                var guid = t.First().ID.ToString();
                if (String.IsNullOrEmpty(guid))
                {
                    errorMessage += String.Format(
                                "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' didn't result in a ID to store.",
                                map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
                    return null;
                }
                return guid;
            }
            else
            {
                if (!DoNotRequireValueMatch)
                {
                    errorMessage += String.Format("The Item '{0}' of template type: '{1}' didn't locate a lookup Item with the value '{2}'.",
                        newItem.ID.ToString(), newItem.TemplateName, importValue);
                    return null;
                }
            }
            return null;
        }
        #endregion Methods
	}
}
