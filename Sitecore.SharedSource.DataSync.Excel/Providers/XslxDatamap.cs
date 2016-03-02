using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using OfficeOpenXml;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.Logger.Log.Builder;

namespace Sitecore.SharedSource.DataSync.Excel.Providers
{
    public class XslxDatamap : BaseDataMap
    {

        protected IList<object> ImportData { get; set; }

        public XslxDatamap(Database db, Item importItem, LevelLogger logger)
            : base(db, importItem, logger)
        {
            Data = importItem["Data"];
            ImportData = new List<object>();
        }

        public override IList<object> GetImportData()
        {
            if (ImportData.Count > 0)
            {
                return ImportData;
            }
            try
            {
                IList<object> result = new List<object>();
                if (File.Exists(DataSourceString))
                {
                    using (var xlPackage = new ExcelPackage(new FileInfo(DataSourceString)))
                    {
                        var worksheet = xlPackage.Workbook.Worksheets[1];
                        if (worksheet != null)
                        {
                            var startRow = worksheet.Dimension.Start.Row + 1;
                            var endRow = worksheet.Dimension.End.Row;
                            var startCol = worksheet.Dimension.Start.Column;
                            var endCol = worksheet.Dimension.End.Column;
                            var headers = new Dictionary<int, String>();
                            try
                            {
                                for (var c = startCol; c < endCol; c++)
                                {
                                    var cell = worksheet.Cells[startRow - 1, c];
                                    if (cell != null && cell.Value != null)
                                    {
                                        var headerValue = SafeString(cell.Value);
                                        Logger.AddInfo("Info", "Adding header '" + headerValue + "'");
                                        headers.Add(c, headerValue.ToLower());
                                    }
                                    else
                                    {
                                        break;
                                    }
                                }
                                for (var r = startRow; r < endRow; r++)
                                {
                                    try
                                    {
                                        var row = new Dictionary<string, string>();
                                        for (var c = startCol; c < headers.Count; c++)
                                        {
                                            var value = SafeString(worksheet.Cells[r, c].Value);
                                            row.Add(headers[c], value);
                                        }
                                        result.Add(row);
                                    }
                                    catch (Exception ex)
                                    {
                                        Logger.AddError("Error",
                                            string.Format(
                                                "An exception occured when reading row #{0}, the import will continue, but the current row will not be processed. {1}",
                                                r, ex));
                                    }
                                }
                                ImportData = result;
                            }
                            catch (Exception ex)
                            {
                                Logger.AddError("Error",
                                    string.Format(
                                        "An exception occured when reading header names '{0}', the import will not continue",
                                        ex));
                            }
                        }
                    }
                }
                else
                {
                    Logger.AddError("Error",
                        string.Format("The DataSource filepath points to a file that doesnt exist. DataSource: '{0}'",
                            DataSourceString));
                }
            }
            catch (Exception ex)
            {
                Logger.AddError("Error",
                    string.Format("The GetFieldValue method failed with an exception. Exception: {0}.", ex));
            }
            return ImportData;
        }

        public override string GetFieldValue(object importRow, string fieldName, ref LevelLogger logger)
        {
            var levelLogger = logger.CreateLevelLogger();
            try
            {
                var row = importRow as Dictionary<string, string>;
                if (row != null)
                {
                    if (row.ContainsKey(fieldName))
                    {
                        return row[fieldName];
                    }
                    Logger.AddError("Error", string.Format("The fieldName {0} didn't exist in the import row.", fieldName));
                }
            }
            catch (Exception ex)
            {
                levelLogger.AddError("GetFieldValue failed",
                    string.Format(
                        "The GetFieldValue method failed with an exception. ImportRow: {0}. FieldName: {1}. Exception: {2}.",
                        GetImportRowDebugInfo(importRow), fieldName, ex));
            }
            return string.Empty;
        }

        private string SafeString(object value)
        {
            var result = (value != null)
                ? value.ToString()
                : String.Empty;
            return Regex.Replace(result, @"\p{C}+", String.Empty);
        }

        public override string GetImportRowDebugInfo(object importRow)
        {
            var result = new StringBuilder();
            var cells = importRow as Dictionary<string, string>;
            if (cells != null)
            {
                var first = true;
                foreach (var pair in cells)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        result.Append(";");
                    }
                    result.AppendFormat("{0}={1}", pair.Key, pair.Value);
                }
            }

            return result.ToString();
        }
    }
}
