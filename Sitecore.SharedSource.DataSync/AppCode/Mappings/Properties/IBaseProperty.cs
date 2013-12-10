using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using System.Data;
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Mappings.Properties {
	public interface IBaseProperty {

		#region Methods

        void FillField(BaseDataMap map, ref Item newItem, Item importRow);

		#endregion Methods
	}
}
