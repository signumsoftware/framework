using System.IO;
using Signum.Services;

namespace Signum.Entities.Files;

[EntityKind(EntityKind.SharedPart, EntityData.Transactional)]
public class FilePathEntity : Entity, IFile, IFilePath
{
    public static string? ForceExtensionIfEmpty = ".dat";

    public FilePathEntity() { }

    public FilePathEntity(FileTypeSymbol fileType)
    {
        this.FileType = fileType;
    }

    public FilePathEntity(FileTypeSymbol fileType, string path)
        : this(fileType)
    {
        this.FileName = Path.GetFileName(path)!;
        this.BinaryFile = File.ReadAllBytes(path);
    }

    public FilePathEntity(FileTypeSymbol fileType, string fileName, byte[] fileData)
        : this(fileType)
    {
        this.FileName = fileName;
        this.BinaryFile = fileData;
    }

    public DateTime CreationDate { get; private set; } = Clock.Now;

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

    [StringLengthValidator(Min = 3, Max = 260), NotNullValidator(DisabledInModelBinder = true)]
    public string Suffix { get; set; }

    public FileTypeSymbol FileType { get; internal set; }

    [Ignore]
    internal PrefixPair _prefixPair;
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

    public static Func<FilePathEntity, PrefixPair> CalculatePrefixPair;

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

    public override string ToString()
    {
        return "{0} - {1}".FormatWith(FileName, ((long)FileLength).ToComputerSize());
    }

    protected override void PostRetrieving(PostRetrievingContext ctx)
    {
        if (CalculatePrefixPair == null)
            throw new InvalidOperationException("OnCalculatePrefixPair not set");

        this.GetPrefixPair();
    }
}

public class PrefixPair
{
    string? physicalPrefix;
    public string PhysicalPrefix => physicalPrefix ?? throw new InvalidOperationException("No PhysicalPrefix defined");

    public string? WebPrefix { get; set; }

    private PrefixPair()
    {
        this.physicalPrefix = null;
    }

    public PrefixPair(string physicalPrefix)
    {
        this.physicalPrefix = physicalPrefix;
    }

    public static PrefixPair None()
    {
        return new PrefixPair();
    }

    public static PrefixPair WebOnly(string webPrefix)
    {
        return new PrefixPair { WebPrefix = webPrefix };
    }
}

[AutoInit]
public static class FilePathOperation
{
    public static ExecuteSymbol<FilePathEntity> Save;
}
