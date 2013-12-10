using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Items;

namespace BackPack.Modules.AppCode.Import.Utility
{
    public class SitecoreUtil
    {
        public static bool DoesItemImplementTemplate(Item item, string templateId)
        {
            if (item == null || item.Template == null)
            {
                return false;
            }

            var items = new List<TemplateItem> { item.Template };
            int index = 0;

            // flatten the template tree during search
            while (index < items.Count)
            {
                // check for match
                TemplateItem template = items[index];
                if (template.ID.ToString() == templateId)
                {
                    return true;
                }

                // add base templates to the list
                items.AddRange(template.BaseTemplates);

                // inspect next template
                index++;
            }

            // nothing found
            return false;
        }
    }
}