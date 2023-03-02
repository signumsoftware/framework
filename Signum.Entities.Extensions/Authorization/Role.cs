using System.Security.Authentication;
using System.Collections.Specialized;
using Signum.Entities.Basics;

namespace Signum.Entities.Authorization;

[EntityKind(EntityKind.Shared, EntityData.Master)]
public class RoleEntity : Entity
{
    [UniqueIndex]
    [StringLengthValidator(Min = 2, Max = 200)]
    public string Name { get; set; }

    public MergeStrategy MergeStrategy { get; set; }

    public bool IsTrivialMerge { get; set; }

    [BindParent, NoRepeatValidator]
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

    internal static Func<Lite<RoleEntity>, RoleEntity> RetrieveFromCache = r => throw new NotImplementedException("RetrieveFromCache not set");

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (IsTrivialMerge)
        {
            if (pi.Name == nameof(InheritsFrom) && InheritsFrom.Count < 2)
            {
                return ValidationMessage._0ShouldBe12.NiceToString(pi.NiceName(), ComparisonType.GreaterThan, 2);
            }

            if(pi.Name == nameof(Description) && Description.HasText())
            {
                return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());
            }

            if (pi.Name == nameof(MergeStrategy) && MergeStrategy != MergeStrategy.Union)
            {
                return ValidationMessage._0ShouldBe1.NiceToString(MergeStrategy.Union.NiceToString());
            }
        }

        return base.PropertyValidation(pi);
    }

    protected override void PreSaving(PreSavingContext ctx)
    {
        if (IsTrivialMerge)
        {
            Name = CalculateTrivialMergeName(this.InheritsFrom);
        }

        base.PreSaving(ctx);
    }

    public static string CalculateTrivialMergeName(IEnumerable<Lite<RoleEntity>> roles)
    {
        var name = roles.OrderBy(a => a.ToString()).ToString(" + ");

        return (StringHashEncoder.Codify(name) + ": " + name).Etc(200);
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
