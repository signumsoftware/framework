using Signum.Dashboard;
using Signum.UserAssets;
using Signum.UserQueries;
using System.Xml.Linq;

namespace Signum.Tree;

[EntityKind(EntityKind.Part, EntityData.Master)]
public class UserTreePartEntity : Entity, IPartEntity
{
    public UserQueryEntity UserQuery { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => UserQuery + "");

    public bool RequiresTitle
    {
        get { return false; }
    }

    public IPartEntity Clone() => new UserTreePartEntity
    {
        UserQuery = this.UserQuery,
    };

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("UserTreePart",
            new XAttribute(nameof(UserQuery), ctx.Include(UserQuery))
            );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery")!.Value));
    }
}
