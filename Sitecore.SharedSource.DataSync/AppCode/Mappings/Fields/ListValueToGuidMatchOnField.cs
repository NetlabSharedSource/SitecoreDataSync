using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Exceptions;
using Sitecore.SharedSource.DataSync;
using Sitecore.Data.Items;
using Sitecore.Data;
using System.Data;
using Sitecore.SharedSource.DataSync.Extensions;
using System.Collections;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.DataSync.Providers;
using Sitecore.SharedSource.DataSync.Utility;
using Sitecore.Data.Fields;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields {
	
    /// <summary>
    /// This uses imported values to match by fieldname an existing content item in the list provided
    /// then stores the GUID of the existing item
    /// </summary>
    public class ListValueToGuidMatchOnField : ListValueToGuidMatchOnDisplayName {

		#region Properties

        private string _MatchOnFieldName;
        /// <summary>
        /// This is the list that you will compare the imported values against
        /// </summary>
        public string MatchOnFieldName
        {
            get
            {
                return _MatchOnFieldName;
            }
            set
            {
                _MatchOnFieldName = value;
            }
        }

		#endregion Properties

		#region Constructor

        public ListValueToGuidMatchOnField(Item i) 
            : base(i)
        {
			// Stores the Match On FieldName
            MatchOnFieldName = i.Fields["Match On FieldName"].Value;
            if (String.IsNullOrEmpty(MatchOnFieldName))
            {
                throw new InvalidValueException(String.Format("The 'MatchOnFieldName' was not provided. Therefor it wasn't possible to match the importValue with a sourcelist. ItemId: {0}.", i.ID));
            }
		}

		#endregion Constructor

		#region Methods

        public override IEnumerable<Item> GetMatchingChildItem(BaseDataMap map, Item listParent, string importValue)
        {
            IEnumerable<Item> t = (from Item c in listParent.GetChildren()
                                   where c[MatchOnFieldName].ToLower().Equals(importValue.ToLower())
                                   select c).ToList();
            return t;
        }

		#endregion Methods
	}
}
