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
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Utility
{
    public class ImageHandler
    {
        public static readonly Database Database = Database.GetDatabase("master");

        public static MediaItem ImageHelper(string filename, string imageAltText, ID mediaItemParentId, bool isToReplaceExistingImage, byte[] imageBytes, ref string errorMessage)
        {
            var filepath = Helper.GetSitecoreMediaItemPath(mediaItemParentId);
            if (string.IsNullOrEmpty(filepath))
            {
                errorMessage = string.Format("The image media library path is not set. mediaItemParentId: {0}.", mediaItemParentId);
                return null;
            }

            var mediaItem = Helper.IsImageExisting(mediaItemParentId, filename);
            if (mediaItem != null)
            {
                if (isToReplaceExistingImage)
                {
                    AttachNewImageToMediaItem(mediaItem, filename, imageBytes, imageAltText, filepath, ref errorMessage);
                }
                else
                {
                    return mediaItem;
                }
            }
            var imageItem = CreateMediaItem(filename, imageAltText, filepath, imageBytes, ref errorMessage);
            return imageItem;
        }

        public static bool AttachImageToItem(BaseDataMap map, object importRow, Item newItem, string newItemField, MediaItem imageItem, ref string errorMessage, string height = null, string width = null)
        {
            if (imageItem != null)
            {
                if (string.IsNullOrEmpty(newItemField))
                {
                    {
                        errorMessage = string.Format("NewItemField is not defined for mediaitem: {0}.",
                                               map.GetImportRowDebugInfo(importRow));
                        return true;
                    }
                }
                ImageField imageField = newItem.Fields[newItemField];
                if (imageField.MediaID != imageItem.ID)
                {
                    newItem.Editing.BeginEdit();
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
                    newItem.Editing.EndEdit();
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
        private static Item CreateMediaItem(string filename, string altText, string filepath, byte[] fileBytes, ref string errorMessage)
        {
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
                        imageItem = MediaManager.Creator.CreateFromStream(ms, "/images/image.png", options);
                    }
                    if (imageItem != null)
                    {
                        return imageItem;
                    }
                }
                else
                {
                    errorMessage = string.Format("CreateMediaItem missing data to stream. Filename: {0}, AltText: {1}", filename, altText);
                }
            }
            catch (Exception ex)
            {
                errorMessage = string.Format("CreateMediaItem failed. Filename: {0}, AltText: {1}, Errormessage: {2}", filename, altText, ex.InnerException);
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
        private static void AttachNewImageToMediaItem(MediaItem imageItem, string filename, byte[] imageBytes, string altText, string destPath, ref string errorMessage)
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
                errorMessage = string.Format("Error trying to attach new image to existing mediaitem: InnerException {0}. Message {1}", ex.InnerException, ex.Message);
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
            imageIn.Save(ms, System.Drawing.Imaging.ImageFormat.Gif);
            return ms.ToArray();
        }
    }
}