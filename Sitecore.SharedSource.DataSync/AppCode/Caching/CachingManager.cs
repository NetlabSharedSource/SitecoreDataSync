using System;
using System.Collections.Generic;
using System.Linq;
using Sitecore.Data.Items;
using Sitecore.Globalization;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.Logger.Log.Builder;

namespace Sitecore.SharedSource.DataSync.Caching
{
    public class CachingManager : ICachingManager
    {
        private BaseDataMap BaseDataMap { get; set; }
        private Caching Caching { get; set; }

        public CachingManager(BaseDataMap map)
        {
            BaseDataMap = map;
            Caching = new Caching();
        }

        public virtual void Initialize(ref LevelLogger logger)
        {
            if (BaseDataMap.IsDisableItemsNotPresentInImport && BaseDataMap.DisableItemsFolderItem != null)
            {
                var disabledLogger = logger.CreateLevelLogger();
                InitializeItemsCache(BaseDataMap.DisableItemsFolderItem, BaseDataMap.IdentifyTheSameItemsByFieldDefinition.GetNewItemField(), ref disabledLogger);
            }
            var itemsAndParentLogger = logger.CreateLevelLogger();
            if (BaseDataMap.FolderByParentHierarchy)
            {
                InitializeItemsCache(BaseDataMap.Parent, BaseDataMap.IdentifyTheSameItemsByFieldDefinition.GetNewItemField() + "|" + BaseDataMap.IdentifyParentByWhatFieldOnParent, ref itemsAndParentLogger);
            }
            else
            {
                InitializeItemsCache(BaseDataMap.Parent, BaseDataMap.IdentifyTheSameItemsByFieldDefinition.GetNewItemField(), ref itemsAndParentLogger);
            }
        }

        public virtual void InitializeItemsCache(Item parent, string fieldNameForKeys, ref LevelLogger logger)
        {
            if (parent == null)
            {
                logger.AddError("Parent was null in InitializeItemsCache", "The parent was null.");
                return;
            }
            if (String.IsNullOrEmpty(fieldNameForKeys))
            {
                logger.AddError("The fieldNameForKeys was null or empty.", String.Format("The fieldNameForKeys was null or empty. fieldNameForKeys: {0}", fieldNameForKeys));
                return;
            }
            var startKey = GetBaseCacheKey(parent, ref logger);
            var fieldNameKeys = fieldNameForKeys.Split('|');
            using (new LanguageSwitcher(BaseDataMap.ImportToLanguageVersion))
            {
                var getItemsByKeyLogger = logger.CreateLevelLogger();
                string pattern = "{0}//*";
                var query = String.Format(pattern, parent.Paths.FullPath);
                try
                {
                    var items = BaseDataMap.SitecoreDB.SelectItems(query);
                    if (items != null)
                    {
                        if (items.Any())
                        {
                            foreach (var item in items)
                            {
                                if (item != null)
                                {
                                    foreach (var fieldNameKey in fieldNameKeys)
                                    {
                                        if (!String.IsNullOrEmpty(fieldNameKey))
                                        {
                                            var keyValue = item[fieldNameKey];
                                            if (!String.IsNullOrEmpty(keyValue))
                                            {
                                                AddToCache(startKey, item, fieldNameKey, keyValue, ref getItemsByKeyLogger);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    getItemsByKeyLogger.AddError("Exception in InitializeItemsCache", ex.Message);
                }
            }
        }

        protected virtual string GetBaseCacheKey(Item parent, ref LevelLogger logger)
        {
            if (parent == null)
            {
                logger.AddError("The parent item was null in the GetBaseCacheKey method.", "The parent item was null in the GetBaseCacheKey method.");
                return null;
            }
            return parent.ID + "";
        }
        
        public virtual void AddItemToCache(Item parent, string fieldName, Item item, string key, ref LevelLogger logger)
        {
            var startKey = GetBaseCacheKey(parent, ref logger);
            if (startKey == null)
            {
                return;
            }
            AddToCache(startKey, item, fieldName, key, ref logger);
        }

        public List<Item> GetItemsFromCache(Item parent, string fieldName, string key, ref LevelLogger logger)
        {
            var startKey = GetBaseCacheKey(parent, ref logger);
            if (startKey == null)
            {
                return null;
            }
            var completeKey = startKey + "|" + fieldName + "|" + key;
            completeKey = completeKey.ToLower();
            return Caching.Get(completeKey);
        }

        private void AddToCache(string startKey, Item item, string fieldName, string key, ref LevelLogger logger)
        {
            if (Caching == null)
            {
                logger.AddError("The Caching object was null in AddToCache.", String.Format("StartKey: {0}. FieldName: {1}. Key: {2}.", startKey, fieldName, key));
                return;
            }
            if (item == null)
            {
                logger.AddError("The item was null in AddToCache.", String.Format("StartKey: {0}. FieldName: {1}. Key: {2}.", startKey, fieldName, key));
                return;
            }
            if (String.IsNullOrEmpty(fieldName))
            {
                logger.AddError("The fieldName was null or empty in AddToCache.", String.Format("StartKey: {0}. Item: {1}. FieldName: {2}. Key: {3}.", startKey, BaseDataMap.GetItemDebugInfo(item), fieldName, key));
                return;
            }
            if (String.IsNullOrEmpty(key))
            {
                logger.AddError("The key was null or empty in AddToCache.", String.Format("StartKey: {0}. Item: {1}. FieldName: {2}. Key: {3}.", startKey, BaseDataMap.GetItemDebugInfo(item), fieldName, key));
                return;
            }
            var completeKey = startKey + "|" + fieldName + "|" + key;
            completeKey = completeKey.ToLower();
            var keyItems = Caching.Get(completeKey) as List<Item>;
            if (keyItems == null)
            {
                keyItems = new List<Item> { item };
            }
            else
            {
                bool sameIdAlreadyAdded = false;
                foreach (var foundItem in keyItems)
                {
                    if (foundItem == item)
                    {
                        sameIdAlreadyAdded = true;
                    }
                }
                if (!sameIdAlreadyAdded)
                {
                    keyItems.Add(item);
                }
            }
            Caching.Set(completeKey, keyItems);
        }
    }
}