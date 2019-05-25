﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Vocup.Properties {
    using System;
    
    
    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "16.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class Messages {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal Messages() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Vocup.Properties.Messages", typeof(Messages).Assembly);
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to You have correctly translated all words 	successively for multiple times.
        ///
        ///Hint: In the settings you can configure how often a word has to be correctly translated in turn before it is marked as learnt..
        /// </summary>
        internal static string BookPracticeFinished {
            get {
                return ResourceManager.GetString("BookPracticeFinished", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Congratulations!.
        /// </summary>
        internal static string BookPracticeFinishedT {
            get {
                return ResourceManager.GetString("BookPracticeFinishedT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected error occured when exporting a CSV file.
        ///Details:
        ///{0}.
        /// </summary>
        internal static string CsvExportError {
            get {
                return ResourceManager.GetString("CsvExportError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Do you want to save your changes before exporting a CSV file? You can save your changes later on. Vocup always exports the current version including any unsafed changes..
        /// </summary>
        internal static string CsvExportSave {
            get {
                return ResourceManager.GetString("CsvExportSave", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save now?.
        /// </summary>
        internal static string CsvExportSaveT {
            get {
                return ResourceManager.GetString("CsvExportSaveT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An unexpected error occured when importing a CSV file.
        ///Details:
        ///{0}.
        /// </summary>
        internal static string CsvImportError {
            get {
                return ResourceManager.GetString("CsvImportError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The CSV header could not be read.
        ///Please ensure that the file is not empty..
        /// </summary>
        internal static string CsvInvalidHeader {
            get {
                return ResourceManager.GetString("CsvInvalidHeader", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The CSV header consists of {0} column but has to be exactly 2 columns.
        ///Please ensure the CSV file to be exported with Excel using UTF-8 format. Files exported by older versions of Vocup are not compatible anymore..
        /// </summary>
        internal static string CsvInvalidHeaderColumns {
            get {
                return ResourceManager.GetString("CsvInvalidHeaderColumns", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid CSV header.
        /// </summary>
        internal static string CsvInvalidHeaderT {
            get {
                return ResourceManager.GetString("CsvInvalidHeaderT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Do you want to import an {0} - {1} vocabulary book although the languages do not match your vocabulary book?
        ///If {0} and {1} are vocabulary words and no languages, please remember to fill the first row of the CSV file with your mother tongue in left column and your foreign language in the right column..
        /// </summary>
        internal static string CsvInvalidLanguages {
            get {
                return ResourceManager.GetString("CsvInvalidLanguages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This vocabulary word already exists in your vocabulary book. Do you want to discard your input?.
        /// </summary>
        internal static string EditAddDuplicate {
            get {
                return ResourceManager.GetString("EditAddDuplicate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Word already exists.
        /// </summary>
        internal static string EditDuplicateT {
            get {
                return ResourceManager.GetString("EditDuplicateT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The changed vocabulary word already exists in your vocabulary book. Do you want to delete this word?.
        /// </summary>
        internal static string EditToDuplicate {
            get {
                return ResourceManager.GetString("EditToDuplicate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Do you want to save your changes and/or practice results?.
        /// </summary>
        internal static string GeneralSaveChanges {
            get {
                return ResourceManager.GetString("GeneralSaveChanges", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save?.
        /// </summary>
        internal static string GeneralSaveChangesT {
            get {
                return ResourceManager.GetString("GeneralSaveChangesT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An outdated version of Vocup has been found on your computer. This could prevent automatic file type association configuration and could cause vocabulary books failing to open. Do you want to uninstall this outdated version?.
        /// </summary>
        internal static string LegacyVersionUninstall {
            get {
                return ResourceManager.GetString("LegacyVersionUninstall", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Uninstall incompatible version?.
        /// </summary>
        internal static string LegacyVersionUninstallT {
            get {
                return ResourceManager.GetString("LegacyVersionUninstallT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A vocabulary book file from the same path has already been loaded. Do you want to load the current vocabulary book and override existing data?.
        /// </summary>
        internal static string MergeOverride {
            get {
                return ResourceManager.GetString("MergeOverride", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Override data?.
        /// </summary>
        internal static string MergeOverrideT {
            get {
                return ResourceManager.GetString("MergeOverrideT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The current vocabulary book could not be opened in Explorer. Please be aware that this feature is not avalaible on some operatening systems like Linux or Windows 10 S.
        ///Details: {0}.
        /// </summary>
        internal static string OpenInExplorerError {
            get {
                return ResourceManager.GetString("OpenInExplorerError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error when opening in Explorer.
        /// </summary>
        internal static string OpenInExplorerErrorT {
            get {
                return ResourceManager.GetString("OpenInExplorerErrorT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The vocabulary book file at {0} does not exist anymore..
        /// </summary>
        internal static string OpenInExplorerNotFound {
            get {
                return ResourceManager.GetString("OpenInExplorerNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to File not found.
        /// </summary>
        internal static string OpenInExplorerNotFoundT {
            get {
                return ResourceManager.GetString("OpenInExplorerNotFoundT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Do you want to save your changes to the vocabulary book before opening it in the Explorer? You can save your changes later in every case..
        /// </summary>
        internal static string OpenInExplorerSave {
            get {
                return ResourceManager.GetString("OpenInExplorerSave", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Save now?.
        /// </summary>
        internal static string OpenInExplorerSaveT {
            get {
                return ResourceManager.GetString("OpenInExplorerSaveT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Vocup cannot open the file at {0} because it is neither a vocabulary book file nor a Vocup backup file..
        /// </summary>
        internal static string OpenUnknownFile {
            get {
                return ResourceManager.GetString("OpenUnknownFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unknown file format.
        /// </summary>
        internal static string OpenUnknownFileT {
            get {
                return ResourceManager.GetString("OpenUnknownFileT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to There are already special chars for this language.
        /// </summary>
        internal static string SpecialCharAlreadyExists {
            get {
                return ResourceManager.GetString("SpecialCharAlreadyExists", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The special char file for {0} could not be loaded. Please check the file at {1} and delete it if necessary..
        /// </summary>
        internal static string SpecialCharCorruptedFile {
            get {
                return ResourceManager.GetString("SpecialCharCorruptedFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Corrupted file.
        /// </summary>
        internal static string SpecialCharCorruptedFileT {
            get {
                return ResourceManager.GetString("SpecialCharCorruptedFileT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occured when deleting the special char file for {0}:
        ///{1}.
        /// </summary>
        internal static string SpecialCharDeleteError {
            get {
                return ResourceManager.GetString("SpecialCharDeleteError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error when deleting.
        /// </summary>
        internal static string SpecialCharDeleteErrorT {
            get {
                return ResourceManager.GetString("SpecialCharDeleteErrorT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An error occured when saving the special char file for {0}:
        ///{1}.
        /// </summary>
        internal static string SpecialCharSaveError {
            get {
                return ResourceManager.GetString("SpecialCharSaveError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error when saving.
        /// </summary>
        internal static string SpecialCharSaveErrorT {
            get {
                return ResourceManager.GetString("SpecialCharSaveErrorT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected error.
        /// </summary>
        internal static string UnexpectedErrorT {
            get {
                return ResourceManager.GetString("UnexpectedErrorT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The Vocup backup file is corrupt and could not be opened:
        ///{0}.
        /// </summary>
        internal static string VdpCorruptFile {
            get {
                return ResourceManager.GetString("VdpCorruptFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Corrupt file.
        /// </summary>
        internal static string VdpCorruptFileT {
            get {
                return ResourceManager.GetString("VdpCorruptFileT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Summary
        /// -  {0} File(s) restored
        /// -  {1} File(s) skipped
        /// -  {2} File(s) failed.
        /// </summary>
        internal static string VdpRestored {
            get {
                return ResourceManager.GetString("VdpRestored", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Recovery finished.
        /// </summary>
        internal static string VdpRestoredT {
            get {
                return ResourceManager.GetString("VdpRestoredT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The vocabulary book file is corrupt and could not be opened..
        /// </summary>
        internal static string VhfCorruptFile {
            get {
                return ResourceManager.GetString("VhfCorruptFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Corrupt vocabulary book file.
        /// </summary>
        internal static string VhfCorruptFileT {
            get {
                return ResourceManager.GetString("VhfCorruptFileT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Mother tongue and foreign language could not be read from the vocabulary book file or are identical.
        ///Please ensure that the file is not empty..
        /// </summary>
        internal static string VhfInvalidLanguages {
            get {
                return ResourceManager.GetString("VhfInvalidLanguages", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to An entry of this vocabulary book file is invalid. Therefore the file cannot be opened..
        /// </summary>
        internal static string VhfInvalidRow {
            get {
                return ResourceManager.GetString("VhfInvalidRow", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The version field of the vocabulary book file could not be read.
        ///Please ensure that the file is not empty..
        /// </summary>
        internal static string VhfInvalidVersion {
            get {
                return ResourceManager.GetString("VhfInvalidVersion", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The result file reference of the vocabulary book file could not be read.
        ///Please ensure that the file is not empty..
        /// </summary>
        internal static string VhfInvalidVhrCode {
            get {
                return ResourceManager.GetString("VhfInvalidVhrCode", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The vocabulary book file requires a later version of Vocup. Please search for updates inside the application or on the Internet..
        /// </summary>
        internal static string VhfMustUpdate {
            get {
                return ResourceManager.GetString("VhfMustUpdate", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Update required.
        /// </summary>
        internal static string VhfMustUpdateT {
            get {
                return ResourceManager.GetString("VhfMustUpdateT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The result file is corrupt and going to be deleted. Therefore you are going to loose your practice results..
        /// </summary>
        internal static string VhrCorruptFile {
            get {
                return ResourceManager.GetString("VhrCorruptFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Corrupt result file.
        /// </summary>
        internal static string VhrCorruptFileT {
            get {
                return ResourceManager.GetString("VhrCorruptFileT", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The referenced result file does not match the current vocabulary book. All results are going to be deleted..
        /// </summary>
        internal static string VhrInvalidRowCount {
            get {
                return ResourceManager.GetString("VhrInvalidRowCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The referenced result file is in use by another vocabulary book. Results are not going to be loaded..
        /// </summary>
        internal static string VhrInvalidRowCountAndOtherFile {
            get {
                return ResourceManager.GetString("VhrInvalidRowCountAndOtherFile", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A vocabulary book or result file could not be saved..
        /// </summary>
        internal static string VocupFileWriteError {
            get {
                return ResourceManager.GetString("VocupFileWriteError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to A vocabulary book or result file could not be saved:
        ///{0}.
        /// </summary>
        internal static string VocupFileWriteErrorEx {
            get {
                return ResourceManager.GetString("VocupFileWriteErrorEx", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Error when saving.
        /// </summary>
        internal static string VocupFileWriteErrorT {
            get {
                return ResourceManager.GetString("VocupFileWriteErrorT", resourceCulture);
            }
        }
    }
}
