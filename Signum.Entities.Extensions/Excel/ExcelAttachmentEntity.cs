using Signum.Entities.UserQueries;
using Signum.Entities.Mailing;
using System.Xml.Linq;
using Signum.Entities.UserAssets;

namespace Signum.Entities.Excel;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class ExcelAttachmentEntity : Entity, IAttachmentGeneratorEntity
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

    string? title;
    [StringLengthValidator(Min = 3, Max = 300)]
    public string? Title
    {
        get { return title; }
        set
        {
            if (Set(ref title, value))
                TitleNode = null;
        }
    }


    [Ignore]
    internal object? TitleNode;

    
    public Lite<UserQueryEntity> UserQuery { get; set; }

    [ImplementedByAll]
    public Lite<Entity>? Related { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => FileName);

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ExcelAttachment",
            new XAttribute(nameof(FileName), FileName),
            Title == null ? null : new XAttribute(nameof(Title), Title),
            new XAttribute(nameof(UserQuery), ctx.Include(UserQuery)),
            Related == null ? null : new XAttribute(nameof(Related), Related.KeyLong())
        );
    }

    static ExcelAttachmentEntity()
    {
        AttachmentFromXmlExtensions.TypeMapping.Add("ExcelAttachment", typeof(ExcelAttachmentEntity));
    }

    public void FromXml(XElement element, IFromXmlContext ctx, IUserAssetEntity userAsset)
    {
        FileName = element.Attribute(nameof(FileName))!.Value;
        Title = element.Attribute(nameof(Title))?.Value;
        UserQuery = (Lite<UserQueryEntity>)ctx.GetEntity(Guid.Parse(element.Attribute(nameof(UserQuery))!.Value)).ToLiteFat();
        Related = element.Attribute(nameof(Related))?.Let(a => a == null ? null : ctx.ParseLite(a.Value, userAsset, PropertyRoute.Construct((ExcelAttachmentEntity a) => a.Related)));
    }
}
