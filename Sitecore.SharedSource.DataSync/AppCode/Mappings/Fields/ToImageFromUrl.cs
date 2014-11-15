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
    public class ToImageFromUrl : ToText
    {
        private const string ReplaceIfExistFieldName = "ReplaceIfExist";
        private const string MediaLibraryPathFieldName = "MediaLibraryPath";
        private const string GetFileNameFromWhatFieldFieldName = "GetFileNameFromWhatField";
        private const string GetImageAltTextIfNotProvidedFromWhatFieldFieldName = "GetImageAltTextIfNotProvidedFromWhatField";
        private const string GetOriginalFileNameWithExtensionFromWhatFieldFieldName = "GetOriginalFileNameWithExtensionFromWhatField";

        private const string DoublePipe = "//";
        private const string HttpPrefix = "http://";
        private const string EndTagHtml = "\"";

        public bool IsReplaceIfExist{ get; set; }
        private ID MediaItemId { get; set; }
        private string GetFileNameFromWhatField { get; set; }
        private string GetImageAltTextIfNotProvidedFromWhatField { get; set; }
        private string GetOriginalFileNameWithExtensionFromWhatField { get; set; }


        public ToImageFromUrl(Item i)
            : base(i)
        {
            IsReplaceIfExist = i[ReplaceIfExistFieldName] == "1";
            
            MediaItemId = null;
            if (!string.IsNullOrEmpty(i[MediaLibraryPathFieldName]))
            {
                MediaItemId = new ID(i[MediaLibraryPathFieldName]);
            }
            GetFileNameFromWhatField = i[GetFileNameFromWhatFieldFieldName];
            GetImageAltTextIfNotProvidedFromWhatField = i[GetImageAltTextIfNotProvidedFromWhatFieldFieldName];
            GetOriginalFileNameWithExtensionFromWhatField = i[GetOriginalFileNameWithExtensionFromWhatFieldFieldName];
        }

        public virtual string GetAbsoluteImageUrl(BaseDataMap map, object importRow, ref Item newItem, string src, ref LevelLogger logger)
        {
            if (src.StartsWith(DoublePipe))
            {
                return src.Replace(DoublePipe, HttpPrefix);
            }
            return src;
        }

        public virtual string GetFileName(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger)
        {
            var getFileNameLogger = logger.CreateLevelLogger();
            var fileNameFieldName = GetFileNameFromWhatField;
            if (String.IsNullOrEmpty(fileNameFieldName))
            {
                getFileNameLogger.AddError(CategoryConstants.GetFilenameFromwWhatFieldWasNullOrEmpty, String.Format("The 'GetFileNameFromWhatField' was null or empty. Please indicate which field the filename should be retrieved from. ImportRow: {0}.",
                        map.GetImportRowDebugInfo(importRow)));
                return null;
            }
            var fileName = map.GetFieldValue(importRow, fileNameFieldName, ref getFileNameLogger);
            if (String.IsNullOrEmpty(fileName))
            {
                getFileNameLogger.AddError(CategoryConstants.TheFilenameFromFieldWasNullOrEmpty, String.Format("The filename was attempted retrieved from the field '{0}', but it was null or empty. The image field name must be provided. ImportRow: {1}.",
                        fileNameFieldName, map.GetImportRowDebugInfo(importRow)));
                return null;
            }
            return fileName;
        }

        public virtual string GetOriginalFileNameWithExtension(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger)
        {
            var getOriginalLogger = logger.CreateLevelLogger();
            var getOriginalFileNameWithExtensionFromWhatField = GetOriginalFileNameWithExtensionFromWhatField;
            if (!String.IsNullOrEmpty(getOriginalFileNameWithExtensionFromWhatField))
            {
                var fileName = map.GetFieldValue(importRow, getOriginalFileNameWithExtensionFromWhatField, ref getOriginalLogger);
                if (!String.IsNullOrEmpty(fileName))
                {
                    return fileName;
                }
            }
            return String.Empty;
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

        public virtual byte[] GetImageAsBytes(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger)
        {
            var getImageAsBytesLogger = logger.CreateLevelLogger();
            try
            {
                var stopWatch = new Stopwatch();
                stopWatch.Start();
                var image = ImageHandler.GetImageFromUrl(importValue);
                if (image == null)
                {
                    getImageAsBytesLogger.AddError(CategoryConstants.TheImageRetrievedFromUrlWasNull, String.Format("The image retrieved from the url was null. Src: '{0}'", importValue));
                    return null;
                }
                
                var imageBytes = ImageHandler.ImageToByteArray(image);
                if (imageBytes == null)
                {
                    getImageAsBytesLogger.AddError(CategoryConstants.TheImageBytesRetrievedWasNull, String.Format("The 'imageBytes' retrieved was null. Src: '{0}'", importValue));
                    return null;
                }
                return imageBytes;
            }
            catch (Exception ex)
            {
                getImageAsBytesLogger.AddError(CategoryConstants.GetImageAsBytesFailedWithException, String.Format("The GetImageAsBytes failed with an exception. Src: '{0}'. Exception: {1}.", importValue, map.GetExceptionDebugInfo(ex)));
                return null;
            }
        }

        public virtual MemoryStream GetImageAsMemoryStream(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref LevelLogger logger, out string imageType)
        {
            imageType = String.Empty;
            var getImageAsMemoryStreamLogger = logger.CreateLevelLogger();
            try
            {
                var image = ImageHandler.GetImageFromUrl(importValue);
                if (image == null)
                {   
                    getImageAsMemoryStreamLogger.AddError(CategoryConstants.TheImageRetrievedFromUrlWasNull, String.Format("The image retrieved from the url was null. Src: '{0}'", importValue));
                    return null;
                }
                imageType = ImageHandler.GetImageType(image);
                
                var memoryStream = ImageHandler.ImageToMemoryStream(image);
                if (memoryStream == null)
                {
                    getImageAsMemoryStreamLogger.AddError(CategoryConstants.TheMemoryStreamRetrievedWasNull, String.Format("The 'memoryStream' retrieved was null. Src: '{0}'", importValue));
                    return null;
                }
                return memoryStream;
            }
            catch (Exception ex)
            {
                getImageAsMemoryStreamLogger.AddError(CategoryConstants.GetImageAsMemoryStreamFailed, String.Format("The GetImageAsMemoryStream failed with an exception. Src: '{0}'. Exception: {1}.", importValue, map.GetExceptionDebugInfo(ex)));
                return null;
            }
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
                var getImageAlTextLogger = fillFieldLogger.CreateLevelLogger("GetImageAltText");
                var imageAltText = GetImageAltText(map, importRow, ref newItem, importValue, ref fillFieldLogger);
                if (getImageAlTextLogger.HasErrors())
                {
                    getImageAlTextLogger.AddError(CategoryConstants.GetimagealttextMethodFailed, String.Format("The method GetImageAltText failed. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
                var getOriginalFileNameLogger = fillFieldLogger.CreateLevelLogger("GetOriginalFileNameWithExtension");
                var originalFileNameWithExtension = GetOriginalFileNameWithExtension(map, importRow, ref newItem, importValue, ref getOriginalFileNameLogger);
                if (getOriginalFileNameLogger.HasErrors())
                {
                    getOriginalFileNameLogger.AddError(CategoryConstants.GetoriginalfilenamewithextensionFailed, String.Format("The method GetOriginalFileNameWithExtension failed. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
                var getFileNameLogger = fillFieldLogger.CreateLevelLogger("GetFileName");
                var fileName = GetFileName(map, importRow, ref newItem, importValue, ref getFileNameLogger);
                if (getFileNameLogger.HasErrors())
                {
                    getFileNameLogger.AddError(CategoryConstants.GetfilenameFailed, String.Format("The method GetFileName failed. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow)));
                    return;
                }
                fileName = StringUtility.GetNewItemName(fileName, map.ItemNameMaxLength);

                if (!IsRequired && String.IsNullOrEmpty(importValue))
                {
                    return;
                }
                var imageType = "";
                var getImageAsMemoryStreamLogger = fillFieldLogger.CreateLevelLogger("GetImageAsMemoryStream");
                var memoryStream = GetImageAsMemoryStream(map, importRow, ref newItem, importValue, ref getImageAsMemoryStreamLogger, out imageType);
                if (getImageAsMemoryStreamLogger.HasErrors())
                {
                    getImageAsMemoryStreamLogger.AddError(CategoryConstants.GetImageAsMemoryStreamFailed, String.Format("The method GetImageAsMemoryStream failed. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow)));
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
                        originalFileNameWithExtension = ".jpeg";
                    }                    
                }
                
                var getMediaFolderLogger = fillFieldLogger.CreateLevelLogger("GetMediaFolderItem");
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
                var imageItem = ImageHandler.ImageHelper(fileName, originalFileNameWithExtension, imageAltText, mediaItemId, IsReplaceIfExist, memoryStream, ref fillFieldLogger);
                if (imageItem != null)
                {
                    var height = GetImageHeight(map, importRow, ref newItem, importValue, ref fillFieldLogger);
                    var width = GetImageWidth(map, importRow, ref newItem, importValue, ref fillFieldLogger);
                    ImageHandler.AttachImageToItem(map, importRow, newItem, NewItemField, imageItem, ref fillFieldLogger, height, width);
                }
            }
            catch (Exception ex)
            {
                updatedField = false;
                fillFieldLogger.AddError(CategoryConstants.ExceptionOccuredWhileProcessingImageImages, String.Format(
                        "An exception occured in FillField in processing of the image/images. Exception: {0}. ImportRow: {1}.",
                        map.GetExceptionDebugInfo(ex), map.GetImportRowDebugInfo(importRow)));
            }
        }
    }
}
