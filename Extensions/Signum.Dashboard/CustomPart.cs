using Signum.UserAssets;
using System.Xml.Linq;

namespace Signum.Dashboard;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class CustomPartEntity : Entity, IPartEntity
{
    public bool RequiresTitle => false;

    [StringLengthValidator(Max = 100)]
    public string CustomPartName { get; set; }

    public IPartEntity Clone()
    {
        return new CustomPartEntity
        {
        };
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("CustomPart",
             new XAttribute(nameof(CustomPartName), CustomPartName)
            );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        CustomPartName = element.Attribute(nameof(CustomPartName))?.Value!;
    }
}
