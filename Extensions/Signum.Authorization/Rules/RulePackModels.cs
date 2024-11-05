using System.Runtime.CompilerServices;

namespace Signum.Authorization.Rules;

public class DefaultDictionary<K, A>
    where K : notnull
{
    public DefaultDictionary(Func<K, A> defaultAllowed, Dictionary<K, A>? overridesDictionary)
    {
        this.DefaultAllowed = defaultAllowed;
        this.OverrideDictionary = overridesDictionary;
    }

    public Dictionary<K, A>? OverrideDictionary { get; private set; }
    public Func<K, A> DefaultAllowed { get; private set; }

    public A GetAllowed(K key)
    {
        if (OverrideDictionary != null && OverrideDictionary.TryGetValue(key, out A? result))
            return result;

        return DefaultAllowed(key);
    }
}

public class ConstantFunction<K, A>
{
    internal A Allowed;
    public ConstantFunction(A allowed)
    {
        this.Allowed = allowed;
    }

    public A GetValue(K key)
    {
        return this.Allowed;
    }

    public override string ToString()
    {
        return "Constant {0}".FormatWith(Allowed);
    }
}


public abstract class BaseRulePack<T> : ModelEntity
{

    public Lite<RoleEntity> Role { get; internal set; }

    [HiddenProperty]
    public MergeStrategy MergeStrategy { get; set; }

    [HiddenProperty]
    public MList<Lite<RoleEntity>> InheritFrom { get; set; } = new MList<Lite<RoleEntity>>();

    public string Strategy
    {
        get
        {
            return AuthMessage.DefaultAuthorization.NiceToString() +
                (InheritFrom.Count == 0 ? (MergeStrategy == MergeStrategy.Union ? AuthMessage.Everything : AuthMessage.Nothing).NiceToString() :
                InheritFrom.Count == 1 ? AuthMessage  .SameAs0.NiceToString(InheritFrom.Only()) :
                (MergeStrategy == MergeStrategy.Union ? AuthMessage.MaximumOfThe0 : AuthMessage.MinumumOfThe0).NiceToString(typeof(RoleEntity).NiceCount(InheritFrom.Count)));
        }
    }


    public MList<T> Rules { get; set; } = new MList<T>();
}

public abstract class AllowedRule<R, A> : ModelEntity
    where R : notnull
    where A : notnull
{
    A allowedBase;
    public A AllowedBase
    {
        get { return allowedBase; }
        set { allowedBase = value; }
    }

    A allowed;
    public A Allowed
    {
        get { return allowed; }
        set
        {
            if (Set(ref allowed, value))
            {
                Notify();
            }
        }
    }

    protected virtual void Notify()
    {
        Notify(() => Overriden);
    }

    [InTypeScript(false)]
    public bool Overriden
    {
        get { return !allowed.Equals(allowedBase); }
    }

    public R Resource { get; set; }

    public override string ToString()
    {
        return "{0} -> {1}".FormatWith(Resource, Allowed) + (Overriden ? "(overriden from {0})".FormatWith(AllowedBase) : "");
    }

}

public class TypeRulePack : BaseRulePack<TypeAllowedRule>
{
    public override string ToString()
    {
        return AuthAdminMessage._0RulesFor1.NiceToString().FormatWith(typeof(TypeEntity).NiceName(), Role);
    }
}

public class TypeAllowedRule : AllowedRule<TypeEntity, WithConditions<TypeAllowed>>
{
    public AuthThumbnail? Properties { get; set; }

    public AuthThumbnail? Operations { get; set; }

    public AuthThumbnail? Queries { get; set; }

    public List<TypeConditionSymbol> AvailableConditions { get; set; }
}

public class WithConditions<A> : ModelEntity, IEquatable<WithConditions<A>>
    where A : struct, Enum
{
    private WithConditions()
    {
    }

    public WithConditions(A fallback, IEnumerable<ConditionRule<A>> conditions)
    {
        this.fallback = fallback;
        this.ConditionRules.AddRange(conditions);
    }

    public WithConditions(A fallback, params ConditionRule<A>[] conditions)
    {
        this.fallback = fallback;
        this.ConditionRules.AddRange(conditions);
    }

    A fallback;
    public A Fallback
    {
        get { return fallback; }
        private set { fallback = value; }
    }

    public MList<ConditionRule<A>> ConditionRules { get; set; } = new MList<ConditionRule<A>>();

    public override bool Equals(object? obj) => obj is WithConditions<A> tac && Equals(tac);
    public bool Equals(WithConditions<A>? other)
    {
        if (other == null)
            return false;

        return this.fallback.Equals(other.fallback) &&
            this.ConditionRules.SequenceEqual(other.ConditionRules);
    }

    public override int GetHashCode()
    {
        return this.fallback.GetHashCode();
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(ConditionRules))
        {
            var errors = NoRepeatValidatorAttribute.ByKey(ConditionRules, a => a.TypeConditions.OrderBy(a => a.ToString()).ToString(" & "));

            if (errors != null)
                return ValidationMessage._0HasSomeRepeatedElements1.NiceToString(pi.NiceName(), errors);
        }

        return base.PropertyValidation(pi);
    }


    public override string ToString()
    {
        if (ConditionRules.IsEmpty())
            return Fallback.ToString()!;

        return "{0} | {1}".FormatWith(Fallback, ConditionRules.ToString(" | "));
    }

    internal bool Exactly(A current)
    {
        return Fallback.Equals(current) && ConditionRules.IsNullOrEmpty();
    }

    public WithConditions<A> WithoutCondition(TypeConditionSymbol typeCondition)
    {
        return new WithConditions<A>(this.Fallback, this.ConditionRules.Select(a => a.WithoutCondition(typeCondition)).NotNull().ToMList());
    }
}

static class TypeAllowAndConditionsExtensions
{

    public static TypeAllowedBasic Min(this WithConditions<TypeAllowed> taac, bool inUserInterface)
    {
        return inUserInterface ? taac.MinUI() : taac.MinDB();
    }

    public static TypeAllowedBasic Max(this WithConditions<TypeAllowed> taac, bool inUserInterface)
    {
        return inUserInterface ? taac.MaxUI() : taac.MaxDB();
    }

    public static TypeAllowed MinCombined(this WithConditions<TypeAllowed> taac)
    {
        return TypeAllowedExtensions.Create(taac.MinDB(), taac.MinUI());
    }

    public static TypeAllowed MaxCombined(this WithConditions<TypeAllowed> taac)
    {
        return TypeAllowedExtensions.Create(taac.MaxDB(), taac.MaxUI());
    }

    public static TypeAllowedBasic MinUI(this WithConditions<TypeAllowed> taac)
    {
        if (!taac.ConditionRules.Any())
            return taac.Fallback.GetUI();

        return (TypeAllowedBasic)Math.Min((int)taac.Fallback.GetUI(), taac.ConditionRules.Select(a => (int)a.Allowed.GetUI()).Min());
    }

    public static TypeAllowedBasic MaxUI(this WithConditions<TypeAllowed> taac)
    {
        if (!taac.ConditionRules.Any())
            return taac.Fallback.GetUI();

        return (TypeAllowedBasic)Math.Max((int)taac.Fallback.GetUI(), taac.ConditionRules.Select(a => (int)a.Allowed.GetUI()).Max());
    }

    public static TypeAllowedBasic MinDB(this WithConditions<TypeAllowed> taac)
    {
        if (!taac.ConditionRules.Any())
            return taac.Fallback.GetDB();

        return (TypeAllowedBasic)Math.Min((int)taac.Fallback.GetDB(), taac.ConditionRules.Select(a => (int)a.Allowed.GetDB()).Min());
    }

    public static TypeAllowedBasic MaxDB(this WithConditions<TypeAllowed> taac)
    {
        if (!taac.ConditionRules.Any())
            return taac.Fallback.GetDB();

        return (TypeAllowedBasic)Math.Max((int)taac.Fallback.GetDB(), taac.ConditionRules.Select(a => (int)a.Allowed.GetDB()).Max());
    }
}

public static class ProperyAllowedAndConditionsExtensions
{
    public static PropertyAllowed Min(this WithConditions<PropertyAllowed> paac)
    {
        if (!paac.ConditionRules.Any())
            return paac.Fallback;

        return (PropertyAllowed)Math.Min((int)paac.Fallback, paac.ConditionRules.Select(a => (int)a.Allowed).Min());
    }

    public static PropertyAllowed Max(this WithConditions<PropertyAllowed> paac)
    {
        if (!paac.ConditionRules.Any())
            return paac.Fallback;

        return (PropertyAllowed)Math.Min((int)paac.Fallback, paac.ConditionRules.Select(a => (int)a.Allowed).Min());
    }
}

public class ConditionRule<A> : ModelEntity, IEquatable<ConditionRule<A>>
    where A: struct, Enum
{
    private ConditionRule() { }

    public ConditionRule(IEnumerable<TypeConditionSymbol> typeConditions, A allowed)
    {
        this.TypeConditions = typeConditions.ToMList();
        this.Allowed = allowed;
    }

    [PreserveOrder, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 0)]
    public MList<TypeConditionSymbol> TypeConditions { get; set; } = new MList<TypeConditionSymbol>();

    public A Allowed { get; set; }

    public override int GetHashCode() => TypeConditions.Count ^ Allowed.GetHashCode();
    public override bool Equals(object? obj) => obj is ConditionRule<A> rm && Equals(rm);

    public bool Equals(ConditionRule<A>? other)
    {
        if (other == null)
            return false;

        return TypeConditions.ToHashSet().SetEquals(other.TypeConditions) && Allowed.Equals(other.Allowed);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => TypeConditions.ToString(" & ") + " => " + Allowed);

    internal ConditionRule<A>? WithoutCondition(TypeConditionSymbol typeCondition)
    {
        if (!TypeConditions.Contains(typeCondition))
            return this;

        if (TypeConditions.Count == 1)
            return null;

        return new ConditionRule<A> { TypeConditions = TypeConditions.Where(tc => !tc.Is(typeCondition)).ToMList() };
    }
}

public enum AuthThumbnail
{
    All,
    Mix,
    None,
}

public abstract class AllowedRuleCoerced<R, A> : AllowedRule<R, A>
    where R : notnull
    where A : notnull
{
    public A[] CoercedValues { get; internal set; }
}

public class PropertyRulePack : BaseRulePack<PropertyAllowedRule>
{
    public TypeEntity Type { get; internal set; }

    public override string ToString()
    {
        return AuthAdminMessage._0RulesFor1.NiceToString().FormatWith(typeof(PropertyRouteEntity).NiceName(), Role);
    }
}

public class PropertyAllowedRule : AllowedRuleCoerced<PropertyRouteEntity, WithConditions<PropertyAllowed>>
{
}


public class QueryRulePack : BaseRulePack<QueryAllowedRule>
{

    public TypeEntity Type { get; internal set; }

    public override string ToString()
    {
        return AuthAdminMessage._0RulesFor1.NiceToString().FormatWith(typeof(QueryEntity).NiceName(), Role);
    }
}
public class QueryAllowedRule : AllowedRuleCoerced<QueryEntity, QueryAllowed> { }


public class OperationRulePack : BaseRulePack<OperationAllowedRule>
{

    public TypeEntity Type { get; internal set; }

    public override string ToString()
    {
        return AuthAdminMessage._0RulesFor1.NiceToString().FormatWith(typeof(OperationSymbol).NiceName(), Role);
    }
}
public class OperationAllowedRule : AllowedRuleCoerced<OperationTypeEmbedded, OperationAllowed> { }

public class PermissionRulePack : BaseRulePack<PermissionAllowedRule>
{
    public override string ToString()
    {
        return AuthAdminMessage._0RulesFor1.NiceToString().FormatWith(typeof(PermissionSymbol).NiceName(), Role);
    }
}
public class PermissionAllowedRule : AllowedRule<PermissionSymbol, bool> { }
