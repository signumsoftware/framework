using System.IO;

namespace Signum.Files;

public class FilePathEmbedded : EmbeddedEntity, IFile, IFilePath
{
    public static string? ForceExtensionIfEmpty = ".dat";

    public FilePathEmbedded() { }

    public FilePathEmbedded(FileTypeSymbol fileType)
    {
        this.FileType = fileType;
    }

    public FilePathEmbedded(FileTypeSymbol fileType, FilePathEmbedded cloneFrom) //Usefull for Email Attachments when combined with WeekFileReference
    {
        this.FileType = fileType;
        this.Suffix = cloneFrom.Suffix;
        this.Hash = cloneFrom.Hash;
        this.FileLength = cloneFrom.FileLength;
        this.fileName = cloneFrom.FileName;
    }

    public FilePathEmbedded(FileTypeSymbol fileType, FilePathEntity cloneFrom) //Usefull for Email Attachments when combined with WeekFileReference
    {
        this.FileType = fileType;
        this.Suffix = cloneFrom.Suffix;
        this.Hash = cloneFrom.Hash;
        this.FileLength = cloneFrom.FileLength;
        this.fileName = cloneFrom.FileName;
    }

    public FilePathEmbedded(FileTypeSymbol fileType, string readFileFrom)
        : this(fileType)
    {
        this.FileName = Path.GetFileName(readFileFrom)!;
        this.BinaryFile = File.ReadAllBytes(readFileFrom);
    }

    public FilePathEmbedded(FileTypeSymbol fileType, string fileName, byte[] fileData)
        : this(fileType)
    {
        this.FileName = fileName;
        this.BinaryFile = fileData;
    }

    string fileName;
    [StringLengthValidator(Min = 1, Max = 260), FileNameValidator]
    public string FileName
    {
        get { return fileName; }
        set
        {
            var newValue = fileName;
            if (ForceExtensionIfEmpty.HasText() && !Path.GetExtension(value).HasText())
                value += ForceExtensionIfEmpty;

            Set(ref fileName, value);
        }
    }

    [Ignore]
    public PrimaryKey EntityId;
    [Ignore]
    public PrimaryKey? MListRowId;
    [Ignore]
    public string PropertyRoute;
    [Ignore]
    public string RootType;

    [Ignore]
    byte[] binaryFile;
    [NotNullValidator(Disabled = true)]
    public byte[] BinaryFile
    {
        get { return binaryFile; }
        set
        {
            if (Set(ref binaryFile, value) && binaryFile != null)
            {
                FileLength = binaryFile.Length;
                Hash = CryptorEngine.CalculateMD5Hash(binaryFile);
            }
        }
    }


    public void CleanBinaryFile()
    {
        binaryFile = null!;
    }

    public string? Hash { get; set; }

    [Format("N0")]
    public long FileLength { get; set; }

    [AutoExpressionField]
    public string FileLengthString => As.Expression(() => ((long)FileLength).ToComputerSize());

    [StringLengthValidator(Min = 1, Max = 1024), NotNullValidator(DisabledInModelBinder = true)]
    public string Suffix { get; set; }

    [ForceNotNullable]
    public FileTypeSymbol FileType { get; internal set; }

    public string? FullPhysicalPath() => FileTypeLogic.GetAlgorithm(FileType).GetFullPhysicalPath(this);
    public string? FullWebPath() => FileTypeLogic.GetAlgorithm(FileType).GetFullWebPath(this);


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{FileName} - {((long)FileLength).ToComputerSize()}");

    public static Action<FilePathEmbedded> OnPreSaving;
    protected override void PreSaving(PreSavingContext ctx)
    {
        if (OnPreSaving == null)
            throw new InvalidOperationException("OnPreSaving not set");

        OnPreSaving(this);
    }

    public FilePathEmbedded Clone() => new FilePathEmbedded(FileType, FileName, this.GetByteArray());


}
