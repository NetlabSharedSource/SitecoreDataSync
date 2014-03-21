using System;
using System.Text.RegularExpressions;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    public class ToEmail: ToText
    {
        private const string SitecoreEmailLinkXml = "<link text='{0}' linktype='mailto' style='' url='mailto:{0}'  title='' />";

        public ToEmail(Item i) : base(i)
        {
        }

        public override string FillField(Providers.BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField)
        {
            var xml = string.Empty;
            if (!String.IsNullOrEmpty(importValue))
            {
                const string pattern = @".+\@.+\..+";
                if (Regex.Match(importValue, pattern).Success)
                {
                    xml = String.Format(SitecoreEmailLinkXml, importValue);
                }
                else
                {
                    updatedField = false;
                    return string.Format("The import value '{0}' was not a correct email. The value was not stored on the field. Importrow: {1}. ", importValue, map.GetImportRowDebugInfo(importRow));
                }
            }
            return base.FillField(map, importRow, ref newItem, xml, out updatedField);
        }
    }
}