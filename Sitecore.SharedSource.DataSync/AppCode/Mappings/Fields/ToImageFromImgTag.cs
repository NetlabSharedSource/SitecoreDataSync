using System;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    public class ToImageFromImgTag: ToImageFromUrl
    {
        protected const string SrcTagHtml = "src=\"";
        protected const string HeightTagHtml = "height=\"";
        protected const string WidthTagHtml = "width=\"";
        protected const string EndTagHtml = "\"";

        public ToImageFromImgTag(Item i)
            : base(i)
        {
        }

        public override string GetAbsoluteImageUrl(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            var src = StringUtil.ReadBetweenTags(importValue, SrcTagHtml, EndTagHtml);
            if (!IsRequired && String.IsNullOrEmpty(src))
            {
                return String.Empty;
            }
            if (IsRequired && String.IsNullOrEmpty(src))
            {
                return string.Format("The src for the image was null or empty. This must be provided since the isRequired value is set for this field. ImportRow: {0}.",
                                     map.GetImportRowDebugInfo(importRow));
            }
            return base.GetAbsoluteImageUrl(map, importRow, ref newItem, src, ref errorMessage);
        }

        public override string GetImageHeight(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            return StringUtil.ReadBetweenTags(importValue, HeightTagHtml, EndTagHtml);
        }

        public override string GetImageWidth(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            return StringUtil.ReadBetweenTags(importValue, WidthTagHtml, EndTagHtml);
        }

        public override byte[] GetImageAsBytes(BaseDataMap map, object importRow, ref Item newItem, string importValue, ref string errorMessage)
        {
            var src = GetAbsoluteImageUrl(map, importRow, ref newItem, importValue, ref errorMessage);
            if (String.IsNullOrEmpty(src) && !IsRequired)
            {
                return null;
            }
            if (String.IsNullOrEmpty(src))
            {
                errorMessage += String.Format("The src for the image was null or empty after return of the GetAbsoluteImageUrl method in the ToImageFromIamgeHtmlTag field class. An src must be provided. ImportRow: {0}.",
                                     map.GetImportRowDebugInfo(importRow));
                return null;
            }
            return base.GetImageAsBytes(map, importRow, ref newItem, src, ref errorMessage);
        }
    }
}