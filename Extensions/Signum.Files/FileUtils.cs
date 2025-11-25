using System.ComponentModel;

namespace Signum.Files;

public interface IFilePath : IFile
{
    string? FullPhysicalPath();
    string? FullWebPath();

    FileTypeSymbol FileType { get; }

    string Suffix { get; set; }
    void CleanBinaryFile();

    string? Hash { get; set; }
    long FileLength { get; set; }
}

public interface IFile
{
    byte[] BinaryFile { get; set; }
    string FileName { get; set; }
}

public enum FileMessage
{
    [Description("Download File")]
    DownloadFile,
    ErrorSavingFile,
    [Description("FileTypes")]
    FileTypes,
    Open,
    [Description("Opening has no default implementation for {0}")]
    OpeningHasNotDefaultImplementationFor0,
    [Description("Download")]
    WebDownload,
    [Description("Image")]
    WebImage,
    Remove,
    [Description("Saving has no default implementation for {0}")]
    SavingHasNotDefaultImplementationFor0,
    [Description("Select File")]
    SelectFile,
    ViewFile,
    [Description("Viewing has no default implementation for {0}")]
    ViewingHasNotDefaultImplementationFor0,
    OnlyOneFileIsSupported,
    [Description("or drag a file here")]
    OrDragAFileHere,
    [Description("The file {0} is not a {1}")]
    TheFile0IsNotA1,
    [Description("File '{0}' is too big, the maximum size is {1}")]
    File0IsTooBigTheMaximumSizeIs1,
    [Description("The name of the file must not contain '%'")]
    TheNameOfTheFileMustNotContainPercentSymbol,
    FileImage,

    [Description("File {0} is still uploading")]
    File0IsStillUploading,

    [Description("Uploading {0} ({1})")]
    Uploading01,

    [Description("Save the {0} when finished")]
    SaveThe0WhenFinished,

    [Description("Add more files")]
    AddMoreFiles,

    [Description("The file '{0}' contains a threat detected by {1}.")]
    File0ContainsAThreatBy1,
}
