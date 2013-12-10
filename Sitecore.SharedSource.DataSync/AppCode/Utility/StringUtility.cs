using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace Sitecore.SharedSource.DataSync.Utility
{
    public class StringUtility
    {
        /// <summary>
        /// This method checks to see if there are any sections in the xpath that contain dashes and wraps them in pound signs if it does unless it's a guid
        /// </summary>
        /// <param name="s">
        /// The XPath string
        /// </param>
        /// <returns>
        /// Returns a string that will run without validation errors
        /// </returns>
        public static string CleanXPath(string s)
        {

            string scQuery = s;

            //loop through each match and replace it in the query with the escaped pattern
            char[] splitArr = { '/' };
            string[] strArr = scQuery.Split(splitArr);

            //search for {6E729CE5-558A-4851-AA30-4BB019E5F9DB}
            string re1 = ".*?";	// Non-greedy match on filler
            string re2 = "([A-Z0-9]{8}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{12})";	// SQL GUID 1

            Regex r = new Regex(re1 + re2, RegexOptions.IgnoreCase | RegexOptions.Singleline);

            for (int z = 0; z <= strArr.Length - 1; z++)
            {
                Match m = r.Match(strArr[z]);

                //if it contains a dash and it's not a guid
                if ((strArr[z].Contains("-") || (strArr[z].StartsWith("0"))) && !m.Success)
                {
                    strArr[z] = "#" + strArr[z] + "#";
                }
            }
            scQuery = string.Join("/", strArr);

            return scQuery;
        }

        /// <summary>
        /// This is used to get a trimmed down name suitable for Sitecore
        /// </summary>
        public static string GetNewItemName(string nameValue, int maxLength)
        {
            var name = TrimText(StripInvalidChars(nameValue), maxLength, string.Empty);
            if (!String.IsNullOrEmpty(name))
            {
                return name.Trim(' ');
            }
            return name;
        }

        public static string TrimText(string val, int maxLength, string endingString)
        {
            string strRetVal = val;
            return (val.Length > maxLength) ? val.Substring(0, maxLength) + endingString : strRetVal;
        }

        public static string StripInvalidChars(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return string.Empty;
            }
            var regexu = new Regex(@"[üúûù]");
            var regexU = new Regex(@"[ÜÚÛÙ]");
            var regexa = new Regex(@"[æåäáâàã]");
            var regexA = new Regex(@"[ÆÅÄÁÂÀÃ]");
            var regexo = new Regex(@"[øöóôòõ]");
            var regexO = new Regex(@"[ØÖÓÔÒÕ]");
            var regexe = new Regex(@"[ëéêè]");
            var regexE = new Regex(@"[ËÉÊÈ]");
            var regexAnd = new Regex(@"[&+]");
            var regexSpace = new Regex(@"[^a-zA-Z0-9]");

            var decoded = HttpUtility.UrlDecode(name);
            if (decoded != null)
            {
                name = decoded;
            }
            name = regexu.Replace(name, "u");
            name = regexU.Replace(name, "U");
            name = regexa.Replace(name, "a");
            name = regexA.Replace(name, "A");
            name = regexo.Replace(name, "o");
            name = regexO.Replace(name, "O");
            name = regexe.Replace(name, "e");
            name = regexE.Replace(name, "E");
            var andCharacter = GetAndCharacter();
            if (!String.IsNullOrEmpty(andCharacter))
            {
                name = regexAnd.Replace(name, andCharacter);
            }
            name = regexSpace.Replace(name, " ");
            var counter = 0;
            while (name.Contains("  ") &&
                   ++counter < 20)
            {
                name = name.Replace("  ", " ");
            }
            var cleanedItemName = name;
            return cleanedItemName.Trim();
        }

        private static string GetAndCharacter()
        {
            var andCharacter = Globalization.Translate.Text("{Sitecore.SharedSource.DataSync.AndCharacter}");
            if (!String.IsNullOrEmpty(andCharacter) && !andCharacter.Contains("Sitecore"))
            {
                return andCharacter;
            }
            return "and";
        }
    }
}
