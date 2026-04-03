using System.Collections.Concurrent;
using System.Data;
using System.Diagnostics;

namespace Signum.Authorization.Rules;

[EntityKind(EntityKind.System, EntityData.Master)]
public abstract class RuleEntity<R> : Entity
    where R : class
{
    public Lite<RoleEntity> Role { get; set; }

    public R Resource { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Role) && RoleEntity.RetrieveFromCache(Role).IsTrivialMerge)
            return AuthAdminMessage.Role0IsTrivialMerge.NiceToString(Role);

        return base.PropertyValidation(pi);
    }
}

public class RuleQueryEntity : RuleEntity<QueryEntity> 
{
    public QueryAllowed Allowed { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Resource} for {Role} <- {Allowed}");
}

public class RulePermissionEntity : RuleEntity<PermissionSymbol> 
{
    public bool Allowed { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Resource} for {Role} <- {Allowed}");
}

public class RuleOperationEntity : RuleEntity<OperationTypeEmbedded> 
{
    public OperationAllowed Fallback { get; set; }

    [PreserveOrder, Ignore, QueryableProperty]
    [BindParent]
    public MList<RuleOperationConditionEntity> ConditionRules { get; set; } = new MList<RuleOperationConditionEntity>();

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(ConditionRules))
        {
            var errors = NoRepeatValidatorAttribute.ByKey(ConditionRules, a => a.Conditions.OrderBy(a => a.ToString()).ToString(" & "));

            if (errors != null)
                return ValidationMessage._0HasSomeRepeatedElements1.NiceToString(this.Resource, errors);
        }

        return base.PropertyValidation(pi);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Resource} for {Role} <- {Fallback}");
}


public class OperationTypeEmbedded : EmbeddedEntity
{
    public OperationSymbol Operation { get; set; }

    public TypeEntity Type { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Operation}/{Type}");
}

[EntityKind(EntityKind.System, EntityData.Master)]
public class RuleOperationConditionEntity : Entity, ICanBeOrdered
{
    [NotNullValidator(Disabled = true)]
    public Lite<RuleOperationEntity> RuleOperation { get; set; }

    [PreserveOrder, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 0)]
    public MList<TypeConditionSymbol> Conditions { get; set; } = new MList<TypeConditionSymbol>();

    public OperationAllowed Allowed { get; set; }

    public int Order { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Allowed.ToString());
}

public class RulePropertyEntity : RuleEntity<PropertyRouteEntity> 
{
    public PropertyAllowed Fallback { get; set; }

    [PreserveOrder, Ignore, QueryableProperty]
    [BindParent]
    public MList<RulePropertyConditionEntity> ConditionRules { get; set; } = new MList<RulePropertyConditionEntity>();

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(ConditionRules))
        {
            var errors = NoRepeatValidatorAttribute.ByKey(ConditionRules, a => a.Conditions.OrderBy(a => a.ToString()).ToString(" & "));

            if (errors != null)
                return ValidationMessage._0HasSomeRepeatedElements1.NiceToString(this.Resource, errors);
        }

        return base.PropertyValidation(pi);
    }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Resource} for {Role} <- {Fallback}");
}

[EntityKind(EntityKind.System, EntityData.Master)]
public class RulePropertyConditionEntity : Entity, ICanBeOrdered
{
    [NotNullValidator(Disabled = true)]
    public Lite<RulePropertyEntity> RuleProperty { get; set; }

    [PreserveOrder, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 0)]
    public MList<TypeConditionSymbol> Conditions { get; set; } = new MList<TypeConditionSymbol>();

    public PropertyAllowed Allowed { get; set; }

    public int Order { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Allowed.ToString());

}


public class RuleTypeEntity : RuleEntity<TypeEntity>
{
    public TypeAllowed Fallback { get; set; }

    [PreserveOrder, Ignore, QueryableProperty]
    [BindParent]
    public MList<RuleTypeConditionEntity> ConditionRules { get; set; } = new MList<RuleTypeConditionEntity>();

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(ConditionRules))
        {
            var errors = NoRepeatValidatorAttribute.ByKey(ConditionRules, a => a.Conditions.OrderBy(a => a.ToString()).ToString(" & "));

            if (errors != null)
                return ValidationMessage._0HasSomeRepeatedElements1.NiceToString(this.Resource, errors);
        }

        return base.PropertyValidation(pi);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => $"{Resource} for {Role} <- {Fallback}");
}

[EntityKind(EntityKind.System, EntityData.Master)]
public class RuleTypeConditionEntity : Entity, ICanBeOrdered
{
    [NotNullValidator(Disabled = true)]
    public Lite<RuleTypeEntity> RuleType { get; set; }

    [PreserveOrder, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 0)]
    public MList<TypeConditionSymbol> Conditions { get; set; } = new MList<TypeConditionSymbol>();

    public TypeAllowed Allowed { get; set; }

    public int Order { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Allowed.ToString());

}

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum QueryAllowed
{
    None = 0,
    EmbeddedOnly = 1,
    Allow = 2,
}

[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum OperationAllowed
{
    None = 0,
    DBOnly = 1,
    Allow = 2,
}



[DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
public enum PropertyAllowed
{
    None,
    Read,
    Write,
}

[DescriptionOptions(DescriptionOptions.Members)]
public enum TypeAllowed
{
    None = TypeAllowedBasic.None << 2 | TypeAllowedBasic.None,

    DBReadUINone = TypeAllowedBasic.Read << 2 | TypeAllowedBasic.None,
    Read = TypeAllowedBasic.Read << 2 | TypeAllowedBasic.Read,

    DBWriteUINone = TypeAllowedBasic.Write << 2 | TypeAllowedBasic.None,
    DBWriteUIRead = TypeAllowedBasic.Write << 2 | TypeAllowedBasic.Read,
    Write = TypeAllowedBasic.Write << 2 | TypeAllowedBasic.Write
}

public static class TypeAllowedExtensions
{
    public static TypeAllowedBasic GetDB(this TypeAllowed allowed)
    {
        return (TypeAllowedBasic)(((int)allowed >> 2) & 0x03);
    }

    public static TypeAllowedBasic GetUI(this TypeAllowed allowed)
    {
        return (TypeAllowedBasic)((int)allowed & 0x03);
    }

    public static TypeAllowedBasic Get(this TypeAllowed allowed, bool userInterface)
    {
        return userInterface ? allowed.GetUI() : allowed.GetDB();
    }

    public static TypeAllowed Create(TypeAllowedBasic database, TypeAllowedBasic ui)
    {
        TypeAllowed result = (TypeAllowed)(((int)database << 2) | (int)ui);

        if (!Enum.IsDefined(typeof(TypeAllowed), result))
            throw new FormatException("Invalid TypeAllowed");

        return result;
    }

    public static bool IsActive(this TypeAllowed allowed, TypeAllowedBasic basicAllowed)
    {
        return allowed.GetDB() == basicAllowed || allowed.GetUI() == basicAllowed;
    }

    public static string ToStringParts(this TypeAllowed allowed)
    {
        TypeAllowedBasic db = allowed.GetDB();
        TypeAllowedBasic ui = allowed.GetUI();

        if (db == ui)
            return db.ToString();

        return "{0},{1}".FormatWith(db, ui);
    }

    public static PropertyAllowed ToPropertyAllowed(this TypeAllowedBasic ta)
    {
        PropertyAllowed pa =
            ta == TypeAllowedBasic.None ? PropertyAllowed.None :
            ta == TypeAllowedBasic.Read ? PropertyAllowed.Read : PropertyAllowed.Write;
        return pa;
    }

    public static bool EqualsForRead(this WithConditions<PropertyAllowed> a, WithConditions<PropertyAllowed> b)
    {
        bool CanRead(PropertyAllowed pa) => pa >= PropertyAllowed.Read;
        if (CanRead(a.Fallback) != CanRead(b.Fallback))
            return false;

        for (int i = 0; i < a.ConditionRules.Count; i++)
        {
            Debug.Assert(a.ConditionRules[i].TypeConditions.SetEquals(b.ConditionRules[i].TypeConditions));

            if (CanRead(a.ConditionRules[i].Allowed) != CanRead(b.ConditionRules[i].Allowed))
                return false;
        }

        return true;
    }

    static ConcurrentDictionary<WithConditions<TypeAllowed>, WithConditions<PropertyAllowed>> cache = new ConcurrentDictionary<WithConditions<TypeAllowed>, WithConditions<PropertyAllowed>>();
    public static WithConditions<PropertyAllowed> ToPropertyAllowed(this WithConditions<TypeAllowed> taac)
    {
        return cache.GetOrAdd(taac, taac =>
            new WithConditions<PropertyAllowed>(taac.Fallback.GetUI().ToPropertyAllowed(), taac.ConditionRules.Select(cr => new ConditionRule<PropertyAllowed>(cr.TypeConditions, cr.Allowed.GetUI().ToPropertyAllowed())).ToReadOnly()).Intern()
        );
    }
}

[InTypeScript(true)]
[AllowUnauthenticated]
[DescriptionOptions(DescriptionOptions.Members)]
public enum TypeAllowedBasic
{
    None = 0,
    Read = 1,
    Write = 2,
}
