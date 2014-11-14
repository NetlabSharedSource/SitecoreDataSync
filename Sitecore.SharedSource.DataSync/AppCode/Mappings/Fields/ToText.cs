using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.SharedSource.DataSync.Extensions;
using Sitecore.Data.Items;
using System.Web;
using Sitecore.Data.Fields;
using System.Data;
using System.Collections;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields
{
    /// <summary>
    /// this stores the plain text import value as is into the new field
    /// </summary>
    public class ToText : BaseMapping, IBaseField
    {
		
		#region Properties 

        /// <summary>
        /// name field delimiter
        /// </summary>
		public char[] comSplitr = { ',' };

		private IEnumerable<string> _existingDataNames;
		/// <summary>
		/// the existing data fields you want to import
		/// </summary>
        public IEnumerable<string> ExistingDataNames {
			get {
				return _existingDataNames;
			}
			set {
				_existingDataNames = value;
			}
		}

		private string _delimiter;
		/// <summary>
		/// the delimiter you want to separate imported data with
		/// </summary>
        public string Delimiter {
			get {
				return _delimiter;
			}
			set {
				_delimiter = value;
			}
		}
		
		#endregion Properties
		
		#region Constructor

		public ToText(Item i) : base(i) {
            //store fields
            ExistingDataNames = i.Fields["From What Fields"].Value.Split(comSplitr, StringSplitOptions.RemoveEmptyEntries);
			Delimiter = i.Fields["Delimiter"].Value;
		}

		#endregion Constructor
		
		#region Methods

        public string GetNewItemField()
        {
            return NewItemField;
        }

        public virtual void FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField, ref LevelLogger logger)
        {
            var fillFieldLogger = logger.CreateLevelLogger();
            updatedField = false;
            //store the imported value as is
            if (IsRequired)
            {
                if (String.IsNullOrEmpty(importValue))
                {
                    fillFieldLogger.AddError(CategoryConstants.ImportedValueToFieldWasEmpty, String.Format("The Item '{0}' of template type: '{1}', field '{2}', but the imported value '{3}' was empty. This field must be provided when the field is required. The field was not updated.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue));
                    return;
                }
            }
            Field f = newItem.Fields[NewItemField];
            if (f != null)
            {
                if (f.Value != importValue)
                {
                    fillFieldLogger.AddInfo(CategoryConstants.UpdatedField, String.Format("Updated value in field from '{0}' to '{1}'", LimitText(f.Value), LimitText(importValue)));
                    f.Value = importValue;
                    updatedField = true;
                }
            }
            if (IsRequired && f == null)
            {
                fillFieldLogger.AddError(CategoryConstants.RequiredFieldNotFoundOnItem, String.Format("The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField));
            }
        }

        private string LimitText(string text)
        {
            if(!String.IsNullOrEmpty(text) && text.Length > 200)
            {
                return text.Substring(0, 199) + " {...}";
            }
            return text;
        }

        #endregion Methods

        #region IBaseField Methods

        /// <summary>
        /// returns a string list of fields to import
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetExistingFieldNames()
        {
            return ExistingDataNames;
        }

        /// <summary>
        /// return the delimiter to separate imported values with
        /// </summary>
        /// <returns></returns>
        public virtual string GetFieldValueDelimiter()
        {
            return Delimiter;
        }

        #endregion IBaseField Methods

        public override string ToString()
        {
            string info = "";
            info += "NewItemField: " + GetNewItemField();
            info += ". ExistingFieldNames: ";
            foreach (var existingFieldName in GetExistingFieldNames())
            {
                info += existingFieldName + "|";
            }
            info += ". FieldValueDelimiter: " + GetFieldValueDelimiter();
            return info;
        }
    }
}
