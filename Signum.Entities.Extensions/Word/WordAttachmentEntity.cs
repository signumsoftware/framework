using Signum.Entities.Mailing;
using Signum.Entities.Templating;
using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Entities.Word;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class WordAttachmentEntity : Entity, IAttachmentGeneratorEntity
{
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

    [Ignore]
    internal object? FileNameNode;

    
    public Lite<WordTemplateEntity> WordTemplate { get; set; }

    [ImplementedByAll]
    public Lite<Entity>? OverrideModel { get; set; }

    public ModelConverterSymbol? ModelConverter { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{WordTemplate}");

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("WordAttachment",
            new XAttribute(nameof(WordTemplate), ctx.Include(WordTemplate)),
            FileName?.Let(fn => new XAttribute(nameof(FileName), fn)),
            OverrideModel?.Let(om => new XAttribute(nameof(OverrideModel), om.KeyLong())),
            ModelConverter?.Let(om => new XAttribute(nameof(ModelConverter), om.Key))
        );
    }

    static WordAttachmentEntity()
    {
        AttachmentFromXmlExtensions.TypeMapping.Add("WordAttachment", typeof(WordAttachmentEntity));
    }

    public void FromXml(XElement element, IFromXmlContext ctx, IUserAssetEntity userAsset)
    {
        WordTemplate = (Lite<WordTemplateEntity>)ctx.GetEntity(Guid.Parse(element.Attribute(nameof(WordTemplate))!.Value)).ToLiteFat();
        FileName = element.Attribute(nameof(FileName))?.Value;
        OverrideModel = element.Attribute(nameof(OverrideModel))?.Let(om => ctx.ParseLite(om.Value, userAsset, PropertyRoute.Construct((WordAttachmentEntity wa) => wa.OverrideModel)));
        ModelConverter = element.Attribute(nameof(ModelConverter))?.Let(om => ctx.GetSymbol<ModelConverterSymbol>(om.Value));
    }
}
