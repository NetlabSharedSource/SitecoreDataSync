using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.DataSync.Providers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    public class ToGuidListMatchOnField : ListValueToGuidMatchOnField
    {
        public ToGuidListMatchOnField(Item i) : base(i) {}

        public override void FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField, ref LevelLogger logger)
        {
            var fillFieldLogger = logger.CreateLevelLogger();
            updatedField = false;
            if (IsRequired && string.IsNullOrEmpty(importValue))
            {
                fillFieldLogger.AddError(CategoryConstants.ImportedValueToFieldWasEmpty, String.Format(
                        "The Item '{0}' of template type: '{1}', field '{2}', but the imported value '{3}' was empty. This field must be provided when the field is required. The field was not updated.",
                        (object) map.GetItemDebugInfo(newItem), (object) newItem.TemplateName,
                        (object) NewItemField, (object) importValue));
                return;
            }
            if (!ID.IsID(SourceList))
            {
                fillFieldLogger.AddError(CategoryConstants.SourceListNotAnValidSitecoreId, String.Format(
                        "The 'Source List' provided was not an valid Sitecore ID. SourceList: {0}. The Fill Field method was aborted and the fieldvalue was not updated.",
                        SourceList));
                return;
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
                                fillFieldLogger.AddError(CategoryConstants.ImportedValueDidntResultInAIdToStore, String.Format(
                                        "The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' didn't result in a ID to store.",
                                        (object) map.GetItemDebugInfo(newItem), (object) newItem.TemplateName,
                                        (object) NewItemField, (object) importValue));
                                return;
                            }
                            if (field.Value != str)
                            {
                                field.Value = str;
                                updatedField = true;
                            }
                        }
                        if (IsRequired && field == null)
                        {
                            fillFieldLogger.AddError(CategoryConstants.RequiredFieldNotFoundOnItem, String.Format(
                                    "The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.",
                                    map.GetItemDebugInfo(newItem), newItem.TemplateName,
                                    NewItemField));
                            return;
                        }
                    }
                    else if (!this.DoNotRequireValueMatch)
                    {
                        fillFieldLogger.AddError(CategoryConstants.DidntLocateALookupItemWithTheValue, String.Format(
                                "The Item '{0}' of template type: '{1}' didn't locate a lookup Item with the value '{2}'.",
                                newItem.ID.ToString(), newItem.TemplateName, importValue));
                        return;
                    }
                }
                else if (this.IsRequired)
                {
                    fillFieldLogger.AddError(CategoryConstants.TheGuidDidntResultInAHitForLookup, String.Format(
                        "The Item '{0}' of template type: '{1}' had a Guid field '{2}' where the imported value '{3}' didn't result any hit. Because the 'Is Required Field' is checked there must be found a value i Sitecore. The field was not updated.",
                        (object) map.GetItemDebugInfo(newItem), (object) newItem.TemplateName,
                        (object) NewItemField, (object) importValue));
                    return;
                }
                else if (newItem[NewItemField] != importValue)
                {
                    newItem[NewItemField] = importValue; 
                    updatedField = true;
                }
            }
            if (IsRequired && listParent == null)
            {
                fillFieldLogger.AddError(CategoryConstants.TheGuidFieldHadASourcelistThatWasNull, String.Format(
                    "The Item '{0}' of template type: '{1}' had a Guid field '{2}' for which SourceList was null. This SourceList must be present because the 'Is Required Field' is checked.",
                    (object) map.GetItemDebugInfo(newItem), (object) newItem.TemplateName,
                    (object) this.NewItemField));
            }
        }

        private List<String> ParseImportValues(string importValue)
        {
            var delimiter = !string.IsNullOrEmpty(MultiListDelimiter)
                                ? MultiListDelimiter[0]: ';';
            var values = importValue.Split(delimiter).ToList();
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