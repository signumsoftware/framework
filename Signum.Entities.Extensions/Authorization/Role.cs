using System.Security.Authentication;
using System.Collections.Specialized;
using Signum.Entities.Basics;

namespace Signum.Entities.Authorization;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class RoleEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 2, Max = 100)]
    public string Name { get; set; }

    public MergeStrategy MergeStrategy { get; set; }

    [NotifyCollectionChanged, NoRepeatValidator]
    public MList<Lite<RoleEntity>> InheritsFrom { get; set; } = new MList<Lite<RoleEntity>>();

    [StringLengthValidator(MultiLine = true)]
    public string? Description { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public static Lite<RoleEntity> Current
    {
        get
        {
            var userHolder = UserHolder.Current;
            if (userHolder == null)
                throw new AuthenticationException(LoginAuthMessage.NotUserLogged.NiceToString());

            return (Lite<RoleEntity>)userHolder.GetClaim("Role")!;
        }
    }
}

public enum MergeStrategy
{
    Union,
    Intersection,
}

[AutoInit]
public static class RoleOperation
{
    public static ExecuteSymbol<RoleEntity> Save;
    public static DeleteSymbol<RoleEntity> Delete;
}
