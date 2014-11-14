using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Sitecore.SharedSource.Logger.Log
{
    public class CategoryConstants
    {
        public const string ImportedValueToFieldWasEmpty = "Imported value to field was empty";
        public const string UpdatedField = "Updated field";
        public const string RequiredFieldNotFoundOnItem = "Required Field not found on item";
        public const string EmailWasInWrongFormatOnImportRow = "Email was in wrong format on ImportRow";
        public const string SourceListNotAnValidSitecoreId = "'Source List' not an valid Sitecore ID";
        public const string ErrorToLocateTheLookupIdInMultiList = "Error to locate the Lookup id in MultiList";
        public const string ErrorToLocateTheLookupId = "Error to locate the Lookup id";
        public const string TheGuidDidntResultInAHitForLookup = "The Guid didn't result in a hit for lookup";
        public const string TheGuidFieldHadASourcelistThatWasNull = "The Guid field had a SourceList that was null";
        public const string ImportedValueResultInMoreThanOneLookupItem = "Imported value result in more than one lookup item";
        public const string ImportedValueDidntResultInAIdToStore = "Imported value didn't result in a ID to store";
        public const string DidntLocateALookupItemWithTheValue = "Didn't locate a lookup Item with the value";
        public const string TheSrcForTheImageWasNullOrEmpty = "The src for the image was null or empty";
        public const string TheSrcForTheImageWasNullOrEmptyAfterReturnOfTheGetAbsoluteImageUrl = "The src for the image was null or empty after return of the GetAbsoluteImageUrl";
        public const string TheImageMediaLibraryPathIsNotSet = "The Image Media Library Path is Not Set";
        public const string GetimagealttextMethodFailed = "GetImageAltText method failed";
        public const string GetoriginalfilenamewithextensionFailed = "GetOriginalFileNameWithExtension failed";
        public const string GetfilenameFailed = "GetFileName failed";
        public const string GetImageAsMemoryStreamFailed = "GetImageAsMemoryStream failed.";
        public const string GetMediaFolderItemReturnedANullItemForTheMediaFolderitem = "GetMediaFolderItem returned a null item for the media folderItem";
        public const string GetMediaFolderItemFailed = "GetMediaFolderItem failed.";
        public const string ExceptionOccuredWhileProcessingImageImages = "Exception occured while processing image/images";
        public const string GetFilenameFromwWhatFieldWasNullOrEmpty = "'GetFileNameFromWhatField' was null or empty";
        public const string TheFilenameFromFieldWasNullOrEmpty = "The filename from field was null or empty";
        public const string TheImageRetrievedFromUrlWasNull = "The image retrieved from url was null";
        public const string TheImageBytesRetrievedWasNull = "The 'imageBytes' retrieved was null";
        public const string GetImageAsBytesFailedWithException = "GetImageAsBytes failed with exception";
        public const string TheMemoryStreamRetrievedWasNull = "The 'memoryStream' retrieved was null";
        public const string NumberOfFieldsInRowWasDifferentFromNumberOfFieldsInHeader = "Number of fields in row was different from number of fields in header";
        public const string TheRetrievedIndexForTheColumnWasOutOfRange = "The retrieved index for the column was out of range";
        public const string TheImportRowWasNull = "The Import Row was null";
        public const string TheFieldnameArgumentWasNullOrEmpty = "The fieldName argument was null or empty";
        public const string FailedToCastToStringBecauseValueWasNull = "Failed to cast to string because value was null";
        public const string TheFieldDoesNotExistInTheImportrow = "The field does not exist in the importRow";
        public const string GetFieldValueFailed = "GetFieldValue failed";
    }
}