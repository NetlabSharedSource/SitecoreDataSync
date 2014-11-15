using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Resources.Media;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Utility
{
    public class ImageHandler
    {
        public const string NewItemFieldIsNotDefinedForMediaitem = "NewItemField is not defined for mediaitem";
        public const string CreateMediaItemMissingDataToStream = "CreateMediaItem missing data to stream";
        public const string CreateMediaItemFailed = "CreateMediaItem failed";
        public const string ExceptionTryingToAttachNewImageToExistingMediaitem = "Exception trying to attach new image to existing mediaitem";
        public static readonly Database Database = Database.GetDatabase("master");

        public static MediaItem ImageHelper(string filename, string originalFileNameWithExtension, string imageAltText, ID mediaItemParentId, bool isToReplaceExistingImage, byte[] imageBytes, ref LevelLogger logger)
        {
            var imageHelperLogger = logger.CreateLevelLogger();

            var filepath = Helper.GetSitecoreMediaItemPath(mediaItemParentId);
            if (string.IsNullOrEmpty(filepath))
            {
                imageHelperLogger.AddError(CategoryConstants.TheImageMediaLibraryPathIsNotSet, String.Format("The image media library path is not set. mediaItemParentId: {0}.", mediaItemParentId));
                return null;
            }

            var mediaItem = Helper.IsImageExisting(mediaItemParentId, filename);
            if (mediaItem != null)
            {
                if (isToReplaceExistingImage)
                {
                    //AttachNewImageToMediaItem(mediaItem, filename, imageBytes, imageAltText, filepath, ref errorMessage);
                }
                else
                {
                    return mediaItem;
                }
            }
            var imageItem = CreateMediaItem(filename, originalFileNameWithExtension, imageAltText, filepath, imageBytes, ref imageHelperLogger);
            return imageItem;
        }

        public static MediaItem ImageHelper(string filename, string originalFileNameWithExtension, string imageAltText, ID mediaItemParentId, bool isToReplaceExistingImage, MemoryStream memoryStream, ref LevelLogger logger)
        {
            var filepath = Helper.GetSitecoreMediaItemPath(mediaItemParentId);
            if (string.IsNullOrEmpty(filepath))
            {
                logger.AddError(CategoryConstants.TheImageMediaLibraryPathIsNotSet, String.Format("The image media library path is not set. mediaItemParentId: {0}.", mediaItemParentId));
                return null;
            }

            var mediaItem = Helper.IsImageExisting(mediaItemParentId, filename);
            if (mediaItem != null)
            {
                if (isToReplaceExistingImage)
                {
                    //AttachNewImageToMediaItem(mediaItem, filename, imageBytes, imageAltText, filepath, ref errorMessage);
                }
                else
                {
                    return mediaItem;
                }
            }
            var imageItem = CreateMediaItem(filename, originalFileNameWithExtension, imageAltText, filepath, memoryStream, ref logger);
            return imageItem;
        }

        public static bool AttachImageToItem(BaseDataMap map, object importRow, Item newItem, string newItemField, MediaItem imageItem, ref LevelLogger logger, string height = null, string width = null)
        {
            var attachImageLogger = logger.CreateLevelLogger();
            if (imageItem != null)
            {
                if (string.IsNullOrEmpty(newItemField))
                {
                    {
                        attachImageLogger.AddError(NewItemFieldIsNotDefinedForMediaitem, String.Format("NewItemField is not defined for mediaitem: {0}.",
                                               map.GetImportRowDebugInfo(importRow)));
                        return false;
                    }
                }
                if (!newItem.Editing.IsEditing)
                {
                    newItem.Editing.BeginEdit();
                }
                ImageField imageField = newItem.Fields[newItemField];
                if (imageField.MediaID != imageItem.ID)
                {                    
                    imageField.Clear();
                    imageField.MediaID = imageItem.ID;
                    if (!String.IsNullOrEmpty(height))
                    {
                        imageField.Height = height;
                    }
                    if (!String.IsNullOrEmpty(width))
                    {
                        imageField.Width = width;
                    }

                    if (!String.IsNullOrEmpty(imageItem.Alt))
                    {
                        imageField.Alt = imageItem.Alt;
                    }
                    else
                    {
                        imageField.Alt = imageItem.DisplayName;
                    }
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Create new media item
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="altText"></param>
        /// <param name="filepath"></param>
        /// <param name="fileBytes"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private static Item CreateMediaItem(string filename, string originalFileNameWithExtension, string altText, string filepath, byte[] fileBytes, ref LevelLogger logger)
        {
            var createMediaItemLogger = logger.CreateLevelLogger();;
            try
            {
                if (fileBytes != null)
                {
                    Item imageItem = null;
                    var destPath = filepath + "/" + filename;
                    using (var ms = new MemoryStream(fileBytes))
                    {
                        var options = new MediaCreatorOptions
                        {
                            FileBased = false,
                            IncludeExtensionInItemName = false,
                            KeepExisting = false,
                            Versioned = false,
                            AlternateText = altText,
                            Destination = destPath,
                            Database = Database
                        };
                        imageItem = MediaManager.Creator.CreateFromStream(ms, originalFileNameWithExtension, options);
                    }
                    if (imageItem != null)
                    {
                        return imageItem;
                    }
                }
                else
                {
                    createMediaItemLogger.AddError(CreateMediaItemMissingDataToStream, String.Format("CreateMediaItem missing data to stream. Filename: {0}, AltText: {1}", filename, altText));
                }
            }
            catch (Exception ex)
            {
                createMediaItemLogger.AddError(CreateMediaItemFailed, String.Format("CreateMediaItem failed. Filename: {0}, AltText: {1}, Errormessage: {2}", filename, altText, ex.InnerException));
            }
            return null;
        }

        /// <summary>
        /// Create new media item
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="altText"></param>
        /// <param name="filepath"></param>
        /// <param name="fileBytes"></param>
        /// <param name="errorMessage"></param>
        /// <returns></returns>
        private static Item CreateMediaItem(string filename, string originalFileNameWithExtension, string altText, string filepath, MemoryStream memoryStream, ref LevelLogger logger)
        {
            var createMediaItemLogger = logger.CreateLevelLogger();
            try
            {
                if (memoryStream != null)
                {
                    Item imageItem = null;
                    var destPath = filepath + "/" + filename;
                    
                    var options = new MediaCreatorOptions
                    {
                        FileBased = false,
                        IncludeExtensionInItemName = false,
                        KeepExisting = false,
                        Versioned = false,
                        AlternateText = altText,
                        Destination = destPath,
                        Database = Database
                    };
                    imageItem = MediaManager.Creator.CreateFromStream(memoryStream, originalFileNameWithExtension, options);
                    if (imageItem != null)
                    {
                        return imageItem;
                    }
                }
                else
                {
                    createMediaItemLogger.AddError(CreateMediaItemMissingDataToStream, String.Format("CreateMediaItem missing data to stream. Filename: {0}, AltText: {1}", filename, altText));
                }
            }
            catch (Exception ex)
            {
                createMediaItemLogger.AddError(CreateMediaItemFailed, String.Format("CreateMediaItem failed. Filename: {0}, AltText: {1}, Errormessage: {2}", filename, altText, ex.Message));
            }
            return null;
        }

        /// <summary>
        /// Attach new image to media item
        /// </summary>
        /// <param name="imageItem"></param>
        /// <param name="filename"></param>
        /// <param name="imageBytes"></param>
        /// <param name="altText"></param>
        /// <param name="destPath"></param>
        /// <param name="errorMessage"></param>
        private static void AttachNewImageToMediaItem(MediaItem imageItem, string filename, byte[] imageBytes, string altText, string destPath, ref LevelLogger logger)
        {
            try
            {
                destPath = destPath + "/" + filename;
                if (imageBytes != null)
                {
                    using (var ms = new MemoryStream(imageBytes))
                    {
                        var options = new MediaCreatorOptions
                        {
                            FileBased = false,
                            IncludeExtensionInItemName = false,
                            KeepExisting = false,
                            Versioned = false,
                            AlternateText = altText,
                            Destination = destPath,
                            Database = Database
                        };
                        MediaManager.Creator.AttachStreamToMediaItem(ms, destPath, filename, options);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.AddError(ExceptionTryingToAttachNewImageToExistingMediaitem, String.Format("Error trying to attach new image to existing mediaitem: InnerException {0}. Message {1}", ex.InnerException, ex.Message));
            }
        }

        public static Image GetImageFromUrl(string url)
        {
            var httpWebRequest = (HttpWebRequest) WebRequest.Create(url);
            using (var httpWebReponse = (HttpWebResponse) httpWebRequest.GetResponse())
            {
                using (var stream = httpWebReponse.GetResponseStream())
                {
                    return Image.FromStream(stream);
                }
            }
        }

        public static byte[] ImageToByteArray(Image imageIn)
        {
            var ms = new MemoryStream();
            imageIn.Save(ms, imageIn.RawFormat);
            return ms.ToArray();
        }

        public static MemoryStream ImageToMemoryStream(Image imageIn)
        {
            var ms = new MemoryStream();
            imageIn.Save(ms, imageIn.RawFormat);
            return ms;
        }

        public static string GetImageType(Image image)
        {
            if (System.Drawing.Imaging.ImageFormat.Jpeg.Equals(image.RawFormat))
            {
                return ".jpeg";
            }
            else if (System.Drawing.Imaging.ImageFormat.Png.Equals(image.RawFormat))
            {
                return ".png";
            }
            else if (System.Drawing.Imaging.ImageFormat.Gif.Equals(image.RawFormat))
            {
                return ".gif";
            }
            else
            {
                return ".jpeg";
            }
        }
    }
}