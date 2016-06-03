using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.Files
{
    public interface IFilePath : IFile
    {
        string CalculatedDirectory { get; }

        string FullPhysicalPath();

        FileTypeSymbol FileType { get; }

        void SetPrefixPair(PrefixPair prefixPair);

        string Suffix { get; set; }
    }

    public interface IFile
    {
        byte[] BinaryFile { get; set; }
        string FileName { get; set; }
        string FullWebPath();
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
        [Description("drag a file here")]
        DragAndDropHere, 
    }


    [Serializable, DescriptionOptions(DescriptionOptions.Description | DescriptionOptions.Members)]
    public class WebImage
    {
        public string FullWebPath;
    }

    [Serializable, DescriptionOptions(DescriptionOptions.Description | DescriptionOptions.Members)]
    public class WebDownload
    {
        public string FullWebPath;
        public string FileName;
    }
}
