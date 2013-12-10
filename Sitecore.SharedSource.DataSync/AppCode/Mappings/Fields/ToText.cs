﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.SharedSource.DataSync.Extensions;
using Sitecore.Data.Items;
using System.Web;
using Sitecore.Data.Fields;
using System.Data;
using System.Collections;
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

        public virtual string FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField)
        {
            updatedField = false;
            //store the imported value as is
            if (IsRequired)
            {
                if (String.IsNullOrEmpty(importValue))
                {
                    return String.Format("The Item '{0}' of template type: '{1}', field '{2}', but the imported value '{3}' was empty. This field must be provided when the field is required. The field was not updated.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
                }
            }
            Field f = newItem.Fields[NewItemField];
            if (f != null)
            {
// ReSharper disable RedundantCheckBeforeAssignment
                if (f.Value != importValue)
// ReSharper restore RedundantCheckBeforeAssignment
                {
                    newItem.Editing.BeginEdit();
                    f.Value = importValue;
                    updatedField = true;
                    newItem.Editing.EndEdit();
                }
            }
            if (IsRequired && f == null)
            {
                return String.Format("The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField);
            }
            return String.Empty;
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
        public string GetFieldValueDelimiter()
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