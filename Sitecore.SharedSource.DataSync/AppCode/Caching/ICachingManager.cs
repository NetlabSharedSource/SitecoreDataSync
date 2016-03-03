using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.DataSync.Caching
{
    public interface ICachingManager
    {
        void Initialize(ref LevelLogger logger);
        void AddItemToCache(Item parent, string fieldName, Item item, string key, ref LevelLogger logger);
        List<string> GetItemsFromCache(Item parent, string fieldName, string key, ref LevelLogger logger);
    }
}
