using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;
using System.Xml.Linq;
using Sitecore.Data.Items;

namespace BackPack.Modules.AppCode.Import.Utility
{
    public static class LogUtil
    {
        public static string GetInnerXml(this XElement element)
        {
            var innerXml = new StringBuilder();

            foreach (XNode node in element.Nodes())
            {
                innerXml.Append(node);
            }
            return innerXml.ToString();
        }
    }
}