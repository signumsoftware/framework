using Signum.UserAssets;
using Signum.Files;
using System.Xml.Linq;

namespace Signum.Mailing.Templates;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class ImageAttachmentEntity : Entity, IAttachmentGeneratorEntity
{


    [Ignore]
    internal object? FileNameNode;

    string? fileName;
    [StringLengthValidator(Min = 3, Max = 100), FileNameValidator]
    public string? FileName
    {
        get { return fileName; }
        set
        {
            if (Set(ref fileName, value))
                FileNameNode = null;
        }
    }

    [StringLengthValidator(Min = 1, Max = 300)]
    public string ContentId { get; set; }

    public EmailAttachmentType Type { get; set; }


    public FileEmbedded File { get; set; }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => FileName ?? (File == null ? "" : File.FileName));

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ImageAttachment",
            FileName == null ? null : new XAttribute(nameof(FileName), FileName),
            new XAttribute(nameof(ContentId), ContentId),
            new XAttribute(nameof(Type), Type),
            File.ToXml(nameof(File))
        );
    }

    public void ParseData(EmailTemplateEntity emailTemplateEntity, QueryDescription description)
    {
    }

    static ImageAttachmentEntity()
    {
        AttachmentFromXmlExtensions.TypeMapping.Add("ImageAttachment", typeof(ImageAttachmentEntity));
    }

    public void FromXml(XElement element, IFromXmlContext ctx, IUserAssetEntity userAsset)
    {
        FileName = element.Attribute(nameof(FileName))?.Value;
        ContentId = element.Attribute(nameof(ContentId))!.Value;
        Type = element.Attribute(nameof(Type))!.Value.ToEnum<EmailAttachmentType>();
        (File ??= new FileEmbedded()).FromXml(element.Element(nameof(File))!);
    }

    public IAttachmentGeneratorEntity Clone()
    {
        return new ImageAttachmentEntity()
        {
            FileName = this.FileName,
            Type = this.Type,
            ContentId = this.ContentId,
            File = new FileEmbedded() 
            { 
                FileName = this.File.FileName, 
                BinaryFile = this.File.BinaryFile 
            },
        }.Let(c => c.CopyMixinsFrom(this));
    }
}

