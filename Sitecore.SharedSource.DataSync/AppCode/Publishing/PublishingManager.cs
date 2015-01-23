using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;
using Sitecore.Globalization;
using Sitecore.Publishing;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.Logger.Log.Builder;

namespace Sitecore.SharedSource.DataSync.Publishing
{
    public static class PublishingManager
    {
        private static PublishOptions Options(Data.Items.Item rootItem, Database source, Database target, Language lang, PublishMode publishMode)
        {
            return new PublishOptions(source, target, publishMode, lang, DateTime.Now) { RootItem = rootItem, Deep = true };
        }

        private static Publisher ItemPublisher(PublishOptions options)
        {
            return new Publisher(options);
        }

        public static void PublishFull(Data.Items.Item rootItem, Database source, Database target, Language lang, BaseDataMap map, ref LevelLogger logger)
        {
            var publishFullLogger = logger.CreateLevelLogger();
            if (rootItem == null)
            {
                publishFullLogger.AddError("RootItem cannot be [null]", "RootItem cannot be [null]");
                return;
            }
            try
            {
                ItemPublisher(Options(rootItem, source, target, lang, PublishMode.Full)).Publish();
            }
            catch (Exception ex)
            {
                publishFullLogger.AddError("PublishFull of item failed with exception.", String.Format("PublishFull of item {0} failed. Exception: {1}", rootItem.ID, map.GetExceptionDebugInfo(ex)));
            }
        }

        public static void PublishItemSmart(Data.Items.Item rootItem, Database source, Database target, Language lang, BaseDataMap map, ref LevelLogger logger)
        {
            var publishFullLogger = logger.CreateLevelLogger();
            if (rootItem == null)
            {
                publishFullLogger.AddError("RootItem cannot be [null]", "RootItem cannot be [null]");
                return;
            }
            try
            {
                ItemPublisher(Options(rootItem, source, target, lang, PublishMode.Smart)).Publish();
            }
            catch (Exception ex)
            {
                publishFullLogger.AddError("PublishItemSmart of item failed with exception.", String.Format("PublishItemSmart of item {0} failed. Exception: {1}", rootItem.ID, map.GetExceptionDebugInfo(ex)));
            }
        }
    }
}