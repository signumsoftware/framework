using System.IO;
using Signum.Services;

namespace Signum.Entities.Files;

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

    public string? Hash { get; private set; }

    public int FileLength { get; internal set; }

    [AutoExpressionField]
    public string FileLengthString => As.Expression(() => ((long)FileLength).ToComputerSize());

    [StringLengthValidator(Min = 1, Max = 260), NotNullValidator(DisabledInModelBinder = true)]
    public string Suffix { get; set; }

    [ForceNotNullable]
    public FileTypeSymbol FileType { get; internal set; }

    [Ignore]
    internal PrefixPair? _prefixPair;
    public void SetPrefixPair(PrefixPair prefixPair)
    {
        this._prefixPair = prefixPair;
    }

    public PrefixPair GetPrefixPair()
    {
        if (this._prefixPair != null)
            return this._prefixPair;

        if (CalculatePrefixPair == null)
            throw new InvalidOperationException("OnCalculatePrefixPair not set");

        this._prefixPair = CalculatePrefixPair(this);

        return this._prefixPair;
    }

    public static Func<FilePathEmbedded, PrefixPair> CalculatePrefixPair;

    public string FullPhysicalPath()
    {
        var pp = this.GetPrefixPair();

        return FilePathUtils.SafeCombine(pp.PhysicalPrefix, Suffix);
    }

    public static Func<string, string> ToAbsolute = str => str;


    public string? FullWebPath()
    {
        var pp = this.GetPrefixPair();

        if (string.IsNullOrEmpty(pp.WebPrefix))
            return null;

        var result = ToAbsolute(pp.WebPrefix + "/" + FilePathUtils.UrlPathEncode(Suffix.Replace("\\", "/")));

        return result;
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{FileName} - {((long)FileLength).ToComputerSize()}");

    public static Action<FilePathEmbedded> OnPreSaving;
    protected override void PreSaving(PreSavingContext ctx)
    {
        if (OnPreSaving == null)
            throw new InvalidOperationException("OnPreSaving not set");

        OnPreSaving(this);
    }


    protected override void PostRetrieving(PostRetrievingContext ctx)
    {
        if (CalculatePrefixPair == null)
            throw new InvalidOperationException("OnCalculatePrefixPair not set");

        this.GetPrefixPair();
    }

    public static Func<FilePathEmbedded, FilePathEmbedded> CloneFunc;
    internal FilePathEmbedded Clone()
    {
        return CloneFunc(this);
    }
}
