using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sitecore.Data.Items;
using System.Data;
using System.Collections;
using Sitecore.SharedSource.Logger.Log;
using Sitecore.SharedSource.Logger.Log.Builder;
using Sitecore.SharedSource.DataSync.Providers;

namespace Sitecore.SharedSource.DataSync.Mappings.Fields {
	/// <summary>
	/// The IBaseField is the interface required for all Data Importer fields to function properly
	/// </summary>
	public interface IBaseField
	{
		#region Methods

	    string GetNewItemField();

	    /// <summary>
	    /// This uses the imported value to modify the newly created item. 
	    /// </summary>
	    /// <param name="map">provides settings related to the import</param>
	    /// <param name="newItem">the newly created item</param>
	    /// <param name="importValue">the imported value</param>
	    /// <param name="updatedField">indicates if the field has been updated</param>
	    void FillField(BaseDataMap map, object importRow, ref Item newItem, string importValue, out bool updatedField, ref LevelLogger fieldLogger);

        /// <summary>
        /// returns a list of the field names from the import row that you want to import into this field 
        /// </summary>
        /// <returns></returns>
        IEnumerable<string> GetExistingFieldNames();

        // This is a test comment.
	    /// <summary>
        /// returns a delimiter to use between the fields being imported
        /// </summary>
        /// <returns></returns>
        string GetFieldValueDelimiter();

		#endregion Methods
	}
}
