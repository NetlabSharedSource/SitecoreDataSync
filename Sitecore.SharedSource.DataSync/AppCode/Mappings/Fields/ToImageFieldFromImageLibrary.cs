using System;
using System.Diagnostics;
using System.IO;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.DataSync.Mappings.Fields;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.DataSync.Utility;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    public class ToImageFieldFromImageLibrary : ToText
    {
        private const string ReplaceIfExistFieldName = "ReplaceIfExist";
        private const string MediaLibraryPathFieldName = "MediaLibraryPath";
        private const string GetImageAltTextIfNotProvidedFromWhatFieldFieldName = "GetImageAltTextIfNotProvidedFromWhatField";
        private const string IdentifyMediaItemByWhatFieldFieldName = "IdentifyMediaItemByWhatFieldName";
        
        public bool IsReplaceIfExist{ get; set; }
        private ID MediaItemId { get; set; }
        private string GetImageAltTextIfNotProvidedFromWhatField { get; set; }
        private string IdentifyMediaItemByWhatFieldName { get; set; }


        public ToImageFieldFromImageLibrary(Item i)
            : base(i)
        {
            IsReplaceIfExist = i[ReplaceIfExistFieldName] == "1";
            
            MediaItemId = null;
            if (!string.IsNullOrEmpty(i[MediaLibraryPathFieldName]))
            {
                MediaItemId = new ID(i[MediaLibraryPathFieldName]);
            }
            GetImageAltTextIfNotProvidedFromWhatField = i[GetImageAltTextIfNotProvidedFromWhatFieldFieldName];
            IdentifyMediaItemByWhatFieldName = i[IdentifyMediaItemByWhatFieldFieldName];
        }

        public virtual string GetImageAltText(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger)
        {
            var getImageAltText = logger.CreateLevelLogger();
            var getImageAltTextIfNotProvidedFromWhatField = GetImageAltTextIfNotProvidedFromWhatField;
            if (!String.IsNullOrEmpty(getImageAltTextIfNotProvidedFromWhatField))
            {
                var altText = map.GetFieldValue(importRow, getImageAltTextIfNotProvidedFromWhatField, ref getImageAltText);
                if (!String.IsNullOrEmpty(altText))
                {
                    return Helper.TitleCaseString(altText);
                }
            }
            return String.Empty;
        }

        public virtual ID GetMediaFolderItem(BaseDataMap map, object importRow, ref Item newItem, ref LevelLogger logger)
        {
            return MediaItemId;
        }

        public virtual string GetImageHeight(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger)
        {
            return String.Empty;
        }

        public virtual string GetImageWidth(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger)
        {
            return String.Empty;
        }

        public virtual MediaItem GetMediaItemFromImageLibrary(BaseDataMap map, ID mediaItemId, string key, string value, ref LevelLogger logger)
        {
            var getMediaItemLogger = logger.CreateLevelLogger();
            var root = ImageHandler.Database.GetItem(mediaItemId);
            if (root != null)
            {
                var getItemsByKeyLogger = getMediaItemLogger.CreateLevelLogger();
                var items = map.GetItemsByKey(root, key, value, ref getItemsByKeyLogger);
                if (getItemsByKeyLogger.HasErrors() || items == null)
                {
                    getMediaItemLogger.AddError("Failed in retrieving mediaItem from key", String.Format(
                                       "An error occured trying to retrieve the mediaItem on fieldName: '{0}' and value: {1}. The error happend in GetItemsByKey method.",
                                       key, value));
                    return null;
                }
                if (items.Count > 1)
                {
                    getMediaItemLogger.AddError("More than one item with the same key", String.Format(
                                       "There were more than one items with the same key. The key must be unique. Therefor the image was not set. FieldName key: {0}. Value: {1}.",
                                       key, value));
                    return null;
                }
                if (items.Count == 1)
                {
                    return items[0];
                }
            }
            return null;
        }

        public override void FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField, ref LevelLogger logger)
        {
            var fillFieldLogger = logger.CreateLevelLogger();
            try
            {
                updatedField = false;
                
                if (ID.IsNullOrEmpty(MediaItemId))
                {
                    fillFieldLogger.AddError(CategoryConstants.TheImageMediaLibraryPathIsNotSet, String.Format("The image media library path is not set. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
                var getImageAlTextLogger = fillFieldLogger.CreateLevelLogger();
                var imageAltText = GetImageAltText(map, importRow, ref newItem, importValue, ref fillFieldLogger);
                if (getImageAlTextLogger.HasErrors())
                {
                    getImageAlTextLogger.AddError(CategoryConstants.GetimagealttextMethodFailed, String.Format("The method GetImageAltText failed. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
                if (!IsRequired && String.IsNullOrEmpty(importValue))
                {
                    return;
                }
                var getMediaFolderLogger = fillFieldLogger.CreateLevelLogger();
                var mediaItemId = GetMediaFolderItem(map, importRow, ref newItem, ref getMediaFolderLogger);
                if (mediaItemId == (ID)null)
                {
                    getMediaFolderLogger.AddError(CategoryConstants.GetMediaFolderItemReturnedANullItemForTheMediaFolderitem, String.Format("The method GetMediaFolderItem returned a null item for the media folderItem. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
                if (getMediaFolderLogger.HasErrors())
                {
                    getMediaFolderLogger.AddError(CategoryConstants.GetMediaFolderItemFailed, String.Format("The method GetMediaFolderItem failed with an error. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
                if(String.IsNullOrEmpty(IdentifyMediaItemByWhatFieldName))
                {
                    fillFieldLogger.AddError("No fieldName provided to identify the MediaItem with", String.Format("There wasn't provided a fieldname to locate the MediaItem with. Please provide a value in the field 'IdentifyMediaItemByWhatField'."));
                    return;
                }
                var getMediaItemLogger = fillFieldLogger.CreateLevelLogger();
                var imageItem = GetMediaItemFromImageLibrary(map, mediaItemId, IdentifyMediaItemByWhatFieldName, importValue, ref getMediaFolderLogger);
                if (getMediaFolderLogger.HasErrors())
                {
                    getMediaItemLogger.AddError("Could not find the mediaItem in Media Library", String.Format("Could not find the mediaItem in Media Libary in the GetMEdiaItemFromImageLibrary. ImportRow {0}", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
                if (imageItem != null)
                {
                    var height = GetImageHeight(map, importRow, ref newItem, importValue, ref fillFieldLogger);
                    var width = GetImageWidth(map, importRow, ref newItem, importValue, ref fillFieldLogger);
                    updatedField = ImageHandler.AttachImageToItem(map, importRow, newItem, NewItemField, imageItem, ref fillFieldLogger, height, width);
                }
                if(IsRequired && imageItem == null)
                {
                    fillFieldLogger.AddError("The field is required, but no MediaItem was found", String.Format("The field is required, but no MediaItem was found. ImportRow: {0}", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
            }
            catch (Exception ex)
            {
                updatedField = false;
                fillFieldLogger.AddError(CategoryConstants.ExceptionOccuredWhileProcessingImageImages, String.Format(
                        "An exception occured in FillField of the ToImageFieldFromImageLibrary. Exception: {0}. ImportRow: {1}.",
                        map.GetExceptionDebugInfo(ex), map.GetImportRowDebugInfo(importRow)));
            }
        }
    }
}
