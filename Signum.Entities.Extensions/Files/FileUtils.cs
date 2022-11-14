using System.ComponentModel;

namespace Signum.Entities.Files;

public interface IFilePath : IFile
{
    string FullPhysicalPath();

    FileTypeSymbol FileType { get; }

    void SetPrefixPair(PrefixPair prefixPair);

    string Suffix { get; set; }
}

public interface IFile
{
    byte[] BinaryFile { get; set; }
    string FileName { get; set; }
    string? FullWebPath();
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
}
