using System;
using Sitecore.Data.Items;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    public class ToExternalLink: ToText
    {
        private const string SitecoreExternalLinkXml = "<link text='{0}' linktype='external' url='{0}' anchor='' target='_blank' />";

        public ToExternalLink(Item i) : base(i)
        {
        }

        public override void FillField(Providers.BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField, ref LevelLogger logger)
        {
            var xml = string.Empty;
            if (!String.IsNullOrEmpty(importValue))
            {
                xml = String.Format(SitecoreExternalLinkXml, importValue);
            }
            base.FillField(map, importRow, ref newItem, xml, out updatedField, ref logger);
        }
    }
}