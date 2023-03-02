using Signum.Entities.Basics;
using System.IO;
using System.Xml.Linq;

namespace Signum.Entities.Files;

public class FileEmbedded : EmbeddedEntity, IFile
{
    public FileEmbedded()
    {
    }

    public FileEmbedded(string readFileFrom)
    {
        this.FileName = Path.GetFileName(readFileFrom)!;
        this.BinaryFile = File.ReadAllBytes(readFileFrom);
    }

    [StringLengthValidator(Min = 3, Max = 200)]
    public string FileName { get; set; }


    public byte[] BinaryFile { get; set; }

    public override string ToString()
    {
        return "{0} - {1}".FormatWith(FileName, BinaryFile?.Let(bf => StringExtensions.ToComputerSize(bf.Length)) ?? "??");
    }

    public string? FullWebPath()
    {
        throw new NotImplementedException("Full web path not implemented for File Embedded");
    }

    public XElement ToXml(string elementName)
    {
        return new XElement(elementName,
            new XAttribute(nameof(FileEmbedded.FileName), FileName),
            new XCData(Convert.ToBase64String(BinaryFile))
        );
    }


    public void FromXml(XElement element)
    {
        this.FileName = element.Attribute(nameof(FileName))!.Value;
        var bytes = Convert.FromBase64String(element.Value);
        if (!MemoryExtensions.SequenceEqual<byte>(bytes, this.BinaryFile))
            this.BinaryFile = bytes;
    }

    public FileContent ToFileContent() => new FileContent(FileName, BinaryFile);

}
