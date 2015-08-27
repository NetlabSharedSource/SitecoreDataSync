using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using Sitecore.SharedSource.DataSync.Extensions;
using System.Data;
using System.Collections;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.Data.Fields;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields {
	
    /// <summary>
    /// This field converts a date value to a sitecore date field value
    /// </summary>
    public class ToDate : ToText {
        private const string TheDatetimeParsingFailed = "The DateTime parsing failed";

        #region Properties 

		#endregion Properties
		
		#region Constructor

		//constructor
		public ToDate(Item i) : base(i) {
			
		}

		#endregion Constructor
		
		#region Methods

        public override void FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField, ref LevelLogger logger)
        {
            var fillFieldLogger = logger.CreateLevelLogger();
            updatedField = false;
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
                //try to parse date value
                DateTime date;
                if (!DateTime.TryParse(importValue, out date))
                {
                    fillFieldLogger.AddError(TheDatetimeParsingFailed, String.Format("The DateTime parsing failed. Therefor the field was not updated on item '{0}' of template type: '{1}' in field the date value could not be parsed '{2}'. The date parse string was: {3}.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue));
                    return;
                }
                string value = date.ToDateFieldValue();
                if (value != importValue)
                {
                    f.Value = value;
                    updatedField = true;
                }
            }
            if (IsRequired && f == null)
            {
                fillFieldLogger.AddError(CategoryConstants.RequiredFieldNotFoundOnItem, String.Format("The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField));
                return;
            }
		}

		#endregion Methods
	}
}
