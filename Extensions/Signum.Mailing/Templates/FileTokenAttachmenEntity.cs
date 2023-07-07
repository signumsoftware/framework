using Signum.UserAssets;
using Signum.Files;
using System.Xml.Linq;
using Signum.UserAssets.Queries;
using Microsoft.Azure.Amqp.Framing;

namespace Signum.Mailing.Templates;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class FileTokenAttachmentEntity : Entity, IAttachmentGeneratorEntity
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

    public QueryTokenEmbedded FileToken { get; set; }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => FileName ?? (FileToken == null ? "" : FileToken.ToString()));

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("FileTokenAttachment",
            FileName == null ? null : new XAttribute(nameof(FileName), FileName),
            new XAttribute(nameof(ContentId), ContentId),
            new XAttribute(nameof(Type), Type),
            new XAttribute(nameof(FileToken), FileToken.Token.FullKey())
        );
    }

    static FileTokenAttachmentEntity()
    {
        AttachmentFromXmlExtensions.TypeMapping.Add("FileTokenAttachment", typeof(FileTokenAttachmentEntity));
    }

    public void FromXml(XElement element, IFromXmlContext ctx, IUserAssetEntity userAsset)
    {
        FileName = element.Attribute(nameof(FileName))?.Value;
        ContentId = element.Attribute(nameof(ContentId))!.Value;
        Type = element.Attribute(nameof(Type))!.Value.ToEnum<EmailAttachmentType>();
        FileToken = element.Attribute(nameof(FileToken))?.Let(t => new QueryTokenEmbedded(t.Value))!;
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(FileToken) && FileToken?.Token != null && !typeof(IFile).IsAssignableFrom(FileToken.Token.Type.CleanType()))
            return ValidationMessage._0ShouldBeOfType1.NiceToString(pi.NiceName(), "IFile");

        return base.PropertyValidation(pi);
    }
}

