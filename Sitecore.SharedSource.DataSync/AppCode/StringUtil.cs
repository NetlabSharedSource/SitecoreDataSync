using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.DataSync
{
    public class StringUtil
    {
        /// <summary>
        /// Funksjon for å lese ut innhold mellom to tagger
        /// </summary>
        /// <param name="stringData"></param>
        /// <param name="startText"></param>
        /// <param name="endText"></param>
        /// <returns></returns>
        public static string ReadBetweenTags(string stringData, string startText, string endText, bool includeTags = false)
        {
            int lngstart;
            int lngend;
            string returnValue = "";

            if (stringData.Contains(startText) && stringData.Contains(endText))
            {
                if (includeTags)
                {
                    lngstart = stringData.IndexOf(startText);
                    if (lngstart > 0 && stringData.Length > lngstart)
                    {
                        lngend = stringData.IndexOf(endText, lngstart);

                        try
                        {
                            if (lngend != 0)
                            {
                                returnValue = stringData.Substring(lngstart, lngend - lngstart + endText.Length);
                            }
                        }
                        catch (Exception ex)
                        {
                            //Debug.Error(ex.Message, "ReadBetweenTags()");
                        }
                    }
                }
                else
                {
                    lngstart = stringData.IndexOf(startText) + startText.Length;
                    if (lngstart != 0)
                    {
                        lngend = stringData.IndexOf(endText, lngstart);

                        if (lngend - lngstart >= 0)
                        {
                            returnValue = stringData.Substring(lngstart, lngend - lngstart);
                        }
                    }
                }
            }
            return returnValue;
        }
    }
}