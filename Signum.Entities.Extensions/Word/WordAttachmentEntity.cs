using Signum.Entities.Mailing;
using Signum.Entities.Templating;
using Signum.Entities.UserAssets;
using System.Xml.Linq;

namespace Signum.Entities.Word;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class WordAttachmentEntity : Entity, IAttachmentGeneratorEntity
{
    string fileName;
    [StringLengthValidator(Min = 3, Max = 100), FileNameValidator]
    public string FileName
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
    public Lite<Entity> OverrideModel { get; set; }

    public ModelConverterSymbol ModelConverter { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => FileName);

    public XElement ToXml(IToXmlContext ctx)
    {
        throw new NotImplementedException();
    }

    static WordAttachmentEntity()
    {
        AttachmentFromXmlExtensions.TypeMapping.Add("WordAttachment", typeof(WordAttachmentEntity));
    }

    public void FromXml(XElement element, IFromXmlContext ctx, IUserAssetEntity userAsset)
    {
        throw new NotImplementedException();
    }
}
