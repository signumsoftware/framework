using System.IO;

namespace Signum.Files;

[EntityKind(EntityKind.SharedPart, EntityData.Transactional)]
public class FilePathEntity : Entity, IFile, IFilePath
{
    public static string? ForceExtensionIfEmpty = ".dat";

    public FilePathEntity() { }

    public FilePathEntity(FileTypeSymbol fileType)
    {
        this.FileType = fileType;
    }

    public FilePathEntity(FileTypeSymbol fileType, string readFromFileName)
        : this(fileType)
    {
        this.FileName = Path.GetFileName(readFromFileName)!;
        this.BinaryFile = File.ReadAllBytes(readFromFileName);
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

    public void CleanBinaryFile()
    {
        binaryFile = null!;
    }

    public string? Hash { get; set; }

    [Format("N0")]
    public long FileLength { get; set; }

    [AutoExpressionField]
    public string FileLengthString => As.Expression(() => ((long)FileLength).ToComputerSize());

    [StringLengthValidator(Min = 3, Max = 1024), NotNullValidator(DisabledInModelBinder = true)]
    public string Suffix { get; set; }

    public FileTypeSymbol FileType { get; internal set; }

    public string? FullPhysicalPath() => FileTypeLogic.GetAlgorithm(FileType).GetFullPhysicalPath(this);
    public string? FullWebPath() => FileTypeLogic.GetAlgorithm(FileType).GetFullWebPath(this);

    public override string ToString()
    {
        return "{0} - {1}".FormatWith(FileName, ((long)FileLength).ToComputerSize());
    }
}

[AutoInit]
public static class FilePathOperation
{
    public static ExecuteSymbol<FilePathEntity> Save;
}
