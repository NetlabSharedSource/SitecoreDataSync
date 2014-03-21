using System;
using Sitecore.Data;
using Sitecore.Data.Items;
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

        private const string DoublePipe = "//";
        private const string HttpPrefix = "http://";
        private const string EndTagHtml = "\"";

        private bool IsReplaceIfExist{ get; set; }
        private ID MediaItemId { get; set; }
        private string GetFileNameFromWhatField { get; set; }
        private string GetImageAltTextIfNotProvidedFromWhatField { get; set; }


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
        }

        public virtual string GetAbsoluteImageUrl(BaseDataMap map, object importRow, ref Item newItem, string src, ref string errorMessage)
        {
            if (src.StartsWith(DoublePipe))
            {
                return src.Replace(DoublePipe, HttpPrefix);
            }
            return src;
        }

        public virtual string GetFileName(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            var fileNameFieldName = GetFileNameFromWhatField;
            if (String.IsNullOrEmpty(fileNameFieldName))
            {
                errorMessage += string.Format("The 'GetFileNameFromWhatField' was null or empty. Please indicate which field the filename should be retrieved from. ImportRow: {0}.",
                        map.GetImportRowDebugInfo(importRow));
                return null;
            }
            var fileName = map.GetFieldValue(importRow, fileNameFieldName, ref errorMessage);
            if (String.IsNullOrEmpty(fileName))
            {
                errorMessage += string.Format("The filename was attempted retrieved from the field '{0}', but it was null or empty. The image field name must be provided. ImportRow: {1}.",
                        fileNameFieldName, map.GetImportRowDebugInfo(importRow));
                return null;
            }
            return fileName;
        }

        public virtual string GetImageAltText(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            var getImageAltTextIfNotProvidedFromWhatField = GetImageAltTextIfNotProvidedFromWhatField;
            if (!String.IsNullOrEmpty(getImageAltTextIfNotProvidedFromWhatField))
            {
                var altText = map.GetFieldValue(importRow, getImageAltTextIfNotProvidedFromWhatField, ref errorMessage);
                if (!String.IsNullOrEmpty(altText))
                {
                    return Helper.TitleCaseString(altText);
                }
            }
            return String.Empty;
        }

        public virtual byte[] GetImageAsBytes(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            try
            {
                var image = ImageHandler.GetImageFromUrl(importValue);
                if (image == null)
                {
                    errorMessage += String.Format("The image retrieved from the url was null. Src: '{0}'", importValue);
                    return null;
                }
                var imageBytes = ImageHandler.ImageToByteArray(image);
                if (imageBytes == null)
                {
                    errorMessage += String.Format("The 'imageBytes' retrieved was null. Src: '{0}'", importValue);
                    return null;
                }
                return imageBytes;
            }
            catch (Exception ex)
            {
                errorMessage += String.Format("The GetImageAsBytes failed with an exception. Src: '{0}'. Exception: {1}.", importValue, map.GetExceptionDebugInfo(ex));
                return null;
            }
        }

        public virtual ID GetMediaFolderItem(BaseDataMap map, object importRow, ref Item newItem, ref string errorMessage)
        {
            return MediaItemId;
        }

        public virtual string GetImageHeight(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            return String.Empty;
        }

        public virtual string GetImageWidth(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            return String.Empty;
        }

        public override string FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField)
        {
            try
            {
                updatedField = false;
                var errorMessage = String.Empty;

                if (ID.IsNullOrEmpty(MediaItemId))
                {
                    return string.Format("The image media library path is not set. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow));
                }
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    return string.Format("An error occured in the GetAbsoluteImageUrl method in the ToImageFromIamgeHtmlTag field class. Error: {0}. ImportRow: {1}.",
                                         errorMessage, map.GetImportRowDebugInfo(importRow));
                }
                var imageAltText = GetImageAltText(map, importRow, ref newItem, importValue, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    return String.Format("The method GetImageAltText failed with the following error: {0}. ImportRow: {1}.", errorMessage, map.GetImportRowDebugInfo(importRow));
                }
                var fileName = GetFileName(map, importRow, ref newItem, importValue, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    return String.Format("The method GetFileName failed with the following error: {0}. ImportRow: {1}.", errorMessage, map.GetImportRowDebugInfo(importRow));
                }
                fileName = StringUtility.GetNewItemName(fileName, map.ItemNameMaxLength);

                var imageBytes = GetImageAsBytes(map, importRow, ref newItem, importValue, ref errorMessage);
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    return String.Format("The method GetImageAsBytes failed with the following error: {0}. ImportRow: {1}.", errorMessage, map.GetImportRowDebugInfo(importRow));
                }
                if (imageBytes == null && !IsRequired)
                {
                    return String.Empty;
                }
                var mediaItemId = GetMediaFolderItem(map, importRow, ref newItem, ref errorMessage);
                if (mediaItemId == (ID)null)
                {
                    return String.Format("The method GetMediaFolderItem returned a null item for the media folderItem. ImportRow: {0}.", map.GetImportRowDebugInfo(importRow));
                }
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    return String.Format("The method GetMediaFolderItem failed with an error. Error: {0}. ImportRow: {1}.", errorMessage, map.GetImportRowDebugInfo(importRow));
                }

                var imageItem = ImageHandler.ImageHelper(fileName, imageAltText, mediaItemId, IsReplaceIfExist, imageBytes, ref errorMessage);

                if (imageItem != null)
                {
                    var height = GetImageHeight(map, importRow, ref newItem, importValue, ref errorMessage);
                    var width = GetImageWidth(map, importRow, ref newItem, importValue, ref errorMessage);
                    ImageHandler.AttachImageToItem(map, importRow, newItem, NewItemField, imageItem, ref errorMessage, height, width);
                }
                if (!String.IsNullOrEmpty(errorMessage))
                {
                    return errorMessage;
                }
                return string.Empty;
            }
            catch (Exception ex)
            {
                updatedField = false;
                return
                    String.Format(
                        "An exception occured in FillField in processing of the image/images. Exception: {0}. ImportRow: {1}.",
                        map.GetExceptionDebugInfo(ex), map.GetImportRowDebugInfo(importRow));
            }
        }
    }
}
