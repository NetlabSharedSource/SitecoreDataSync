using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Sitecore.SharedSource.DataSync.Utility
{
    public class Helper
    {
        public static String TitleCaseString(String s)
        {
            if (s == null) return null;

            String[] words = s.Split(' ');
            Helper.ProcessWords(ref words);
            return String.Join(" ", words);
        }

        public String TitleCaseString909451480(String s)
        {
            if (s == null) return null;

            String[] words = s.Split(' ');
            ProcessWords(ref words);
            return String.Join(" ", words);
        }

        private static void ProcessWords(ref string[] words)
        {
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length == 0) continue;

                Char firstChar = Char.ToUpper(words[i][0]);
                String rest = "";
                if (words[i].Length > 1)
                {
                    rest = words[i].Substring(1).ToLower();
                }
                var wordString = firstChar + rest;
                if (wordString.Contains('-'))
                {
                    var subwords = wordString.Split('-');
                    ProcessWords(ref subwords);
                    wordString = String.Join("-", subwords);
                }
                words[i] = wordString;
            }
        }

        /// <summary>
        /// Get filepath
        /// </summary>
        /// <param name="mediaItemParentId"></param>
        /// <returns></returns>
        public static string GetSitecoreMediaItemPath(ID mediaItemParentId)
        {
            var root = ImageHandler.Database.GetItem(mediaItemParentId);
            if (root != null)
            {
                return root.Paths.Path;
            }
            return string.Empty;
        }

        /// <summary>
        /// Find existing image if exist. On level structure
        /// </summary>
        /// <param name="mediaItemParentId"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public static MediaItem IsImageExisting(ID mediaItemParentId, string filename)
        {
            var root = ImageHandler.Database.GetItem(mediaItemParentId);
            if (root != null)
            {
                var child = root.Axes.GetItem(filename);
                if (child != null)
                {
                    return child;
                }
            }
            return null;
        }


    }
}