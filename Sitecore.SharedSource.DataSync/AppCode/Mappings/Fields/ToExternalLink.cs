using System;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    public class ToExternalLink: ToText
    {
        private const string SitecoreExternalLinkXml = "<link text='{0}' linktype='external' url='{0}' anchor='' target='_blank' />";

        public ToExternalLink(Item i) : base(i)
        {
        }

        public override string FillField(Providers.BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField)
        {
            var xml = string.Empty;
            if (!String.IsNullOrEmpty(importValue))
            {
                xml = String.Format(SitecoreExternalLinkXml, importValue);
            }
            return base.FillField(map, importRow, ref newItem, xml, out updatedField);
        }
    }
}