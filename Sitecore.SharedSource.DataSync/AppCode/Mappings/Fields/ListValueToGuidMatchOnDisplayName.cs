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
			        //loop through children and look for anything that matches by name
			        IEnumerable<Item> t = GetMatchingChildItem(map, i, importValue);
			        //if you find one then store the id
			        if (t.Count() > 1)
			        {
			            return
			                String.Format(
			                    "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' did result in more that one lookup item. The field was not updated.",
			                    map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
			        }
			        if (t.Count() == 1)
			        {
			            Field f = newItem.Fields[NewItemField];
			            if (f != null)
			            {
			                var guid = t.First().ID.ToString();
			                if (String.IsNullOrEmpty(guid))
			                {
			                    return
			                            String.Format(
			                                "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' didn't result in a ID to store.",
			                                map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
			                }
                            if (f.Value != guid)
			                {
			                    newItem.Editing.BeginEdit();
			                    f.Value = guid;
			                    updatedField = true;
			                    newItem.Editing.EndEdit();
			                }
			            }
			            if (IsRequired && f == null)
			            {
			                return
			                    String.Format(
			                        "The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.",
                                    map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField);
			            }
			        }
			        else
			        {
			            if (!DoNotRequireValueMatch)
			            {
			                return String.Format("The Item '{0}' of template type: '{1}' didn't locate a lookup Item with the value '{2}'.",
			                    newItem.ID.ToString(), newItem.TemplateName, importValue);
			            }
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

		#endregion Methods
	}
}
