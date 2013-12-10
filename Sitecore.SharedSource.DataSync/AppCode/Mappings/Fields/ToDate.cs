using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Extensions;
using System.Data;
using System.Collections;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.Data.Fields;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields {
	
    /// <summary>
    /// This field converts a date value to a sitecore date field value
    /// </summary>
    public class ToDate : ToText {

		#region Properties 

		#endregion Properties
		
		#region Constructor

		//constructor
		public ToDate(Item i) : base(i) {
			
		}

		#endregion Constructor
		
		#region Methods

        public override string FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField)
        {
            updatedField = false;
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
                //try to parse date value
                DateTime date;
                if (!DateTime.TryParse(importValue, out date))
                {
                    return String.Format("The DateTime parsing failed. Therefor the field was not updated on item '{0}' of template type: '{1}' in field the date value could not be par2}'. The date parse string was: {3}.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue);
                }
                string value = date.ToDateFieldValue();
                if (value != importValue)
                {
                    newItem.Editing.BeginEdit();
                    f.Value = value;
                    updatedField = true;
                    newItem.Editing.EndEdit();
                }
            }
            if (IsRequired && f == null)
            {
                return String.Format("The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField);
            }
            return string.Empty;
		}

		#endregion Methods
	}
}
