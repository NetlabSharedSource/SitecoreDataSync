using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using System.Data;
using System.Collections;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.Data.Fields;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields {
	
    /// <summary>
    /// this is used to set a field to a specific predetermined value when importing data.
    /// </summary>
    public class ToStaticValue : BaseMapping, IBaseField {

		#region Properties

		private string _Value;
		/// <summary>
		/// value to import
		/// </summary>
        public string Value {
			get {
				return _Value;
			}
			set {
				_Value = value;
			}
		}

		#endregion Properties

		#region Constructor

		public ToStaticValue(Item i) : base(i) {
			Value = i.Fields["Value"].Value;
		}

		#endregion Constructor

		#region Methods

        public string GetNewItemField()
        {
            return NewItemField;
        }

        public string FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField)
        {
            updatedField = false;
            //ignore import value and store value provided
            Field f = newItem.Fields[NewItemField];
            if (f != null)
            {
                if (f.Value != Value)
                {
                    newItem.Editing.BeginEdit();
                    f.Value = Value;
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
        /// doesn't provide any existing fields
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetExistingFieldNames()
        {
            return Enumerable.Empty<string>();
        }

        /// <summary>
        /// doesn't need any delimiter
        /// </summary>
        /// <returns></returns>
        public string GetFieldValueDelimiter()
        {
            return string.Empty;
        }

        #endregion IBaseField Methods
	}
}
