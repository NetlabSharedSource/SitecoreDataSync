using System;
using System.Diagnostics;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Mappings.Fields;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.DataSync.Utility;
using Sitecore.SharedSource.Logger.Log.Builder;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    public class ToMediaItemBlob : ToImageFromUrl
    {
        public const string GetTimeStampIdentifierFromWhatFieldFieldName = "GetTimeStampIdentifierFromWhatField";
        public const string UpdateMediaItemStrategyFieldName = "UpdateMediaItemStrategy";
        private const string AlwaysUpdateItemId = "{88AA037C-D236-4FBE-9443-9D6FB9F5ED27}";
        private const string OnlyWhenUpdatedTimestampItemId = "{AA659734-8B46-4E56-AD0F-7487A2173B87}";
        private const string OnlyOnceItemId = "{13FA8213-8D81-46EF-9676-1E76BD01D4EE}";
        private const string FieldNameBlog = "Blob";
        private const string DefaultMediaType = ".jpeg";

        public static readonly Database MasterDB = Database.GetDatabase("master");
        public ID MediaItemId { get; set; }

        public UpdateStrategy UpdateMediaItemStrategy { get; set; }
        public string GetTimeStampIdentifierFromWhatField { get; set; }

        public enum UpdateStrategy
        {
            AlwaysUpdate,
            OnlyOnce,
            OnlyWhenUpdatedTimestamp
        }

        public ToMediaItemBlob(Item i)
            : base(i)
        {
            UpdateMediaItemStrategy = GetUpdateMediaItemStrategy(i[UpdateMediaItemStrategyFieldName]);
            GetTimeStampIdentifierFromWhatField = i[GetTimeStampIdentifierFromWhatFieldFieldName];
        }

        private UpdateStrategy GetUpdateMediaItemStrategy(string updateMediaItemStrategyId)
        {
            if (String.IsNullOrEmpty(updateMediaItemStrategyId))
            {
                return UpdateStrategy.OnlyOnce;
            }
            switch (updateMediaItemStrategyId)
            {
                case AlwaysUpdateItemId:
                    return UpdateStrategy.AlwaysUpdate;
                case OnlyWhenUpdatedTimestampItemId:
                    return UpdateStrategy.OnlyWhenUpdatedTimestamp;
                case OnlyOnceItemId:
                default:
                    return UpdateStrategy.OnlyOnce;
            }
        }

        public virtual bool IsUpdateMediaItem(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger, out string newTimeStamp)
        {
            var isUpdateMediaItemLogger = logger.CreateLevelLogger();
            newTimeStamp = null;
            if (UpdateMediaItemStrategy == UpdateStrategy.AlwaysUpdate)
            {
                return true;
            }
            if (UpdateMediaItemStrategy == UpdateStrategy.OnlyWhenUpdatedTimestamp)
            {
                var getTimeLogger = isUpdateMediaItemLogger.CreateLevelLogger();
                var importRowTimeStamp = GetTimeStampIdentifier(map, importRow, ref newItem, importValue, ref getTimeLogger);
                if (getTimeLogger.HasErrors())
                {
                    return false;
                }
                if (importRowTimeStamp == null)
                {
                    isUpdateMediaItemLogger.AddError("IsUpdateMediaItem failed when 'importRowTimeStamp' was null", String.Format(
                            "The method IsUpdateMediaItem failed because the 'importRowTimeStamp' was null. This field is used to retrieve the timestamp of last updated value on importRow. ImportRow: {0}.",
                            map.GetImportRowDebugInfo(importRow)));
                    return false;
                }
                newTimeStamp = importRowTimeStamp;
                var lastUpdatedTimeStamp = newItem[Sitecore.SharedSource.DataSync.Utility.Constants.FieldNameDataSyncTimeStamp];
                if (!String.IsNullOrEmpty(lastUpdatedTimeStamp))
                {
                    return !lastUpdatedTimeStamp.Trim().ToLower().Equals(importRowTimeStamp.Trim().ToLower());
                }
                // If the timestamp field is empty, then we must update
                return true;
            }
            var mediaItem = (MediaItem)newItem;
            if (mediaItem != null)
            {
                return !mediaItem.HasMediaStream(FieldNameBlog);
            }
            isUpdateMediaItemLogger.AddError("Item could not be case to mediaItem in IsUpdateMediaItem", String.Format("The method IsUpdateMediaItem failed because the item could not be casted to a mediaItem and therefor we couldn't be determined if the item had a MediaStream. ImportRow: {0}.",
                    map.GetImportRowDebugInfo(importRow)));
            return false;
        }

        public virtual string GetTimeStampIdentifier(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger)
        {
            var getTimeStampLogger = logger.CreateLevelLogger();
            if (String.IsNullOrEmpty(GetTimeStampIdentifierFromWhatField))
            {
                getTimeStampLogger.AddError("No value in 'GetTimeStampIdentifierFromWhatField'", String.Format(
                        "The method GetTimeStampIdentifier failed because there wasn't any value in the field 'GetTimeStampIdentifierFromWhatField'. This field is used to retrieve the timestamp of last updated value on importRow. ImportRow: {0}.",
                        map.GetImportRowDebugInfo(importRow)));
                return null;
            }
            var getFieldValueLogger = getTimeStampLogger.CreateLevelLogger();
            var timeStamp = map.GetFieldValue(importRow, GetTimeStampIdentifierFromWhatField, ref getFieldValueLogger);
            if (String.IsNullOrEmpty(timeStamp))
            {
                getFieldValueLogger.AddError("GetTimeStampIdentifier failed", String.Format("The method GetTimeStampIdentifier failed because the 'timestamp' from the importRow was null or empty. Please make sure each importRow has a timestamp. ImportRow: {0}.",
                        map.GetImportRowDebugInfo(importRow)));
                return null;
            }
            return timeStamp;
        }

        public override void FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField, ref LevelLogger logger)
        {
            var fillFieldLogger = logger.CreateLevelLogger();
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                
                updatedField = false;
                string errorMessage = String.Empty;

                string newTimeStamp;
                var isUpdateMediaItemLogger = fillFieldLogger.CreateLevelLogger();
                var isUpdateMediaItem = IsUpdateMediaItem(map, importRow, ref newItem, importValue, ref isUpdateMediaItemLogger, out newTimeStamp);
                if (isUpdateMediaItemLogger.HasErrors())
                {
                    fillFieldLogger.AddError("IsUpdateMediaItem failed with error", String.Format("The method IsUpdateMediaItem failed with the following error: {0}. ImportRow: {1}.",
                            errorMessage, map.GetImportRowDebugInfo(importRow)));
                    DeleteMediaItemIfEmpty(map, ref newItem, ref fillFieldLogger);
                    return;
                }
                if (isUpdateMediaItem)
                {
                    var getImageAltLogger = fillFieldLogger.CreateLevelLogger();
                    var imageAltText = GetImageAltText(map, importRow, ref newItem, importValue, ref getImageAltLogger);
                    if (getImageAltLogger.HasErrors())
                    {
                        getImageAltLogger.AddError("GetImageAltText failed with error", String.Format(
                                "The method GetImageAltText failed with the following error: {0}. ImportRow: {1}.",
                                errorMessage, map.GetImportRowDebugInfo(importRow)));
                        DeleteMediaItemIfEmpty(map, ref newItem, ref getImageAltLogger);
                        return;
                    }
                    var getOriginalLogger = fillFieldLogger.CreateLevelLogger();
                    var originalFileNameWithExtension = GetOriginalFileNameWithExtension(map, importRow, ref newItem,
                                                                                         importValue, ref getOriginalLogger);
                    if (getOriginalLogger.HasErrors())
                    {
                        getOriginalLogger.AddError("GetOriginalFileNameWithExtensio failed", String.Format("The method GetOriginalFileNameWithExtension failed with the following error: {0}. ImportRow: {1}.",
                                errorMessage, map.GetImportRowDebugInfo(importRow)));
                        DeleteMediaItemIfEmpty(map, ref newItem, ref getOriginalLogger);
                        return;
                    }
                    var getFileNameLogger = fillFieldLogger.CreateLevelLogger();
                    var fileName = GetFileName(map, importRow, ref newItem, importValue, ref getFileNameLogger);
                    if (getFileNameLogger.HasErrors())
                    {
                        getFileNameLogger.AddError("GetFileName failed", String.Format("The method GetFileName failed with the following error: {0}. ImportRow: {1}.",
                                errorMessage, map.GetImportRowDebugInfo(importRow)));
                        DeleteMediaItemIfEmpty(map, ref newItem, ref getFileNameLogger);
                        return;
                    }
                    fileName = StringUtility.GetNewItemName(fileName, map.ItemNameMaxLength);
                    if (!IsRequired && String.IsNullOrEmpty(importValue))
                    {
                        return;
                    }
                    var imageType = "";
                    var getImageAsMemoryLogger = fillFieldLogger.CreateLevelLogger();
                    var memoryStream = GetImageAsMemoryStream(map, importRow, ref newItem, importValue, ref getImageAsMemoryLogger, out imageType);
                    if (getImageAsMemoryLogger.HasErrors())
                    {
                        getImageAsMemoryLogger.AddError("GetImageAsStream failed", String.Format("The method GetImageAsStream failed with the following error: {0}. ImportRow: {1}.",
                                errorMessage, map.GetImportRowDebugInfo(importRow)));
                        DeleteMediaItemIfEmpty(map, ref newItem, ref getImageAsMemoryLogger);
                        return;
                    }
                    if (memoryStream == null && !IsRequired)
                    {
                        return;
                    }
                    if (String.IsNullOrEmpty(originalFileNameWithExtension))
                    {
                        if (!String.IsNullOrEmpty(imageType))
                        {
                            originalFileNameWithExtension = imageType;
                        }
                        else
                        {
                            originalFileNameWithExtension = DefaultMediaType;
                        }
                    }

                    var imageHelperLogger = fillFieldLogger.CreateLevelLogger();
                    var imageItem = ImageHandler.ImageHelper(fileName, originalFileNameWithExtension, imageAltText,
                                                             newItem.Parent.ID, true, memoryStream, ref imageHelperLogger);
                    if (imageItem != null)
                    {
                        newItem = imageItem;
                        newItem.Editing.BeginEdit();
                        newItem[Utility.Constants.FieldNameDataSyncTimeStamp] = newTimeStamp;
                        updatedField = true;
                    }
                }
            }
            catch (Exception ex)
            {
                updatedField = false;
                fillFieldLogger.AddError("Exception occured in FillField processing image/images", String.Format(
                        "An exception occured in FillField in processing of the image/images. Exception: {0}. ImportRow: {1}.",
                        map.GetExceptionDebugInfo(ex), map.GetImportRowDebugInfo(importRow)));
            }
        }

        private static void DeleteMediaItemIfEmpty(BaseDataMap map, ref Item newItem, ref LevelLogger logger)
        {
            var mediaItem = (MediaItem)newItem;
            if (mediaItem != null)
            {
                if (!mediaItem.HasMediaStream(FieldNameBlog))
                {
                    logger.AddInfo("Deleted Empty MediaItem Without Blob", String.Format("The media doesn't contain any Blob value (no image). To prevent an empty Media Item without a blob to remain in the Media Library, the MediaItem was deleted. Item: {0}", map.GetItemDebugInfo(newItem)));
                    newItem.Delete();
                    newItem = null;
                    logger.IncrementCounter("DeletedEmptyMediaItem");
                }
            }
        }
    }
}