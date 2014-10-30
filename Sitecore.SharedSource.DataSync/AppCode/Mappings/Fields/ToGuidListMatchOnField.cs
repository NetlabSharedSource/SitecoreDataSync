using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Mappings.Fields;
using Sitecore.SharedSource.DataSync.Providers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sitecore.SharedSource.DataSync.Utility;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    public class ToGuidListMatchOnField : ListValueToGuidMatchOnField
    {
        public ToGuidListMatchOnField(Item i) : base(i) {}

        public override string FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField)
        {
            updatedField = false;
            if (IsRequired && string.IsNullOrEmpty(importValue))
            {
                return
                    string.Format(
                        "The Item '{0}' of template type: '{1}', field '{2}', but the imported value '{3}' was empty. This field must be provided when the field is required. The field was not updated.",
                        (object) map.GetItemDebugInfo(newItem), (object) newItem.TemplateName,
                        (object) NewItemField, (object) importValue);
            }
            if (!ID.IsID(SourceList))
            {
                return
                    string.Format(
                        "The 'Source List' provided was not an valid Sitecore ID. SourceList: {0}. The Fill Field method was aborted and the fieldvalue was not updated.",
                        SourceList);
            }
            var listParent = newItem.Database.GetItem(SourceList);
            if (listParent != null)
            {
                if (!string.IsNullOrEmpty(importValue))
                {
                    var matchingChildItem = GetMatchingChildItem(map, listParent, importValue);
                    var childItems = matchingChildItem as Item[] ?? matchingChildItem.ToArray();
                    if (childItems.Any())
                    {
                        var str = string.Empty;
                        foreach (var childItem in childItems)
                        {
                            if (childItem != null)
                            {
                                str += (str.Length > 0)
                                           ? "|" + childItem.ID
                                           : childItem.ID.ToString();
                            }
                        }
                        var field = newItem.Fields[NewItemField];
                        if (field != null)
                        {
                            if (string.IsNullOrEmpty(str))
                            {
                                return
                                    string.Format(
                                        "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' didn't result in a ID to store.",
                                        (object) map.GetItemDebugInfo(newItem), (object) newItem.TemplateName,
                                        (object) this.NewItemField, (object) importValue);
                            }
                            if (field.Value != str)
                            {
                                newItem.Editing.BeginEdit();
                                field.Value = str;
                                updatedField = true;
                                newItem.Editing.EndEdit();
                            }
                        }
                        if (IsRequired && field == null)
                        {
                            return
                                string.Format(
                                    "The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.",
                                    map.GetItemDebugInfo(newItem), newItem.TemplateName,
                                    NewItemField);
                        }
                    }
                    else if (!this.DoNotRequireValueMatch)
                    {
                        return
                            string.Format(
                                "The Item '{0}' of template type: '{1}' didn't locate a lookup Item with the value '{2}'.",
                                newItem.ID.ToString(), newItem.TemplateName, importValue);
                    }
                }
                else if (this.IsRequired)
                    return
                        string.Format(
                            "The Item '{0}' of template type: '{1}' had a Guid field '{2}' where the imported value '{3}' didn't result any hit. Because the 'Is Required Field' is checked there must be found a value i Sitecore. The field was not updated.",
                            (object) map.GetItemDebugInfo(newItem), (object) newItem.TemplateName,
                            (object) this.NewItemField, (object) importValue);
                else if (newItem[this.NewItemField] != importValue)
                {
                    newItem.Editing.BeginEdit();
                    newItem[this.NewItemField] = importValue;
                    newItem.Editing.EndEdit();
                }
            }
            if (this.IsRequired && listParent == null)
                return
                    string.Format(
                        "The Item '{0}' of template type: '{1}' had a Guid field '{2}' for which SourceList was null. This SourceList must be present because the 'Is Required Field' is checked.",
                        (object) map.GetItemDebugInfo(newItem), (object) newItem.TemplateName,
                        (object) this.NewItemField);
            else
                return string.Empty;
        }

        private List<String> ParseImportValues(string importValue)
        {
            var delimiter = !string.IsNullOrEmpty(GetFieldValueDelimiter())
                                ? GetFieldValueDelimiter()[0]
                                : ';';
            var values = importValue.Split(delimiter).ToList();
            foreach (var value in values)
            {

            }
            return values;
        }

        public override IEnumerable<Item> GetMatchingChildItem(BaseDataMap map, Item listParent, string importValue)
        {
            var values = ParseImportValues(importValue);
            var result = new List<Item>();
            foreach (var value in values)
            {
                if (!string.IsNullOrEmpty(value))
                {
                    var list =  (IEnumerable<Item>)Enumerable.ToList<Item>(Enumerable.Where<Item>(Enumerable.Cast<Item>((IEnumerable)listParent.GetChildren()), (Func<Item, bool>)(c => c[this.MatchOnFieldName].ToLower().Equals(value.ToLower()))));
                    result.AddRange(list);
                }    
            }
            return result;
        }   
    }
}