using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using Sitecore.Data.Fields;
using System.Data;
using System.Collections;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields {
	
    /// <summary>
    /// this field uses the url stored in the field and converts it to a LinkField value
    /// </summary>
    public class UrlToLink : ToText {
		#region Properties 

		#endregion Properties
		
		#region Constructor

		//constructor
		public UrlToLink(Item i)
			: base(i) {
			
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
                    fillFieldLogger.AddError(CategoryConstants.ImportedValueToFieldWasEmpty, String.Format("The Item '{0}' of template type: '{1}' has a field '{2}', but the imported value '{3}' was empty. This field must be provided when the field is required. The field was not updated.", map.GetItemDebugInfo(newItem), newItem.TemplateName, NewItemField, importValue));
                    return;
                }
            }
            //get the field as a link field and store the url
            LinkField lf = newItem.Fields[NewItemField];
            if (lf != null)
            {
                if (lf.Url != importValue)
                {
                    lf.Url = importValue;
                    updatedField = true;
                }
            }
            if(IsRequired && lf==null)
            {
                fillFieldLogger.AddError(CategoryConstants.RequiredFieldNotFoundOnItem, String.Format("The Item '{0}' of template type: '{1}' didn't contain a field with name '{2}'. This field must be present because the 'Is Required Field' is checked.", newItem.ID.ToString(), newItem.TemplateName, NewItemField));
                return;
            }
		}

		#endregion Methods
	}
}
