using Signum.Entities.Basics;
using Signum.Utilities.DataStructures;

namespace Signum.Entities.Authorization;

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

public class ConstantFunctionButEnums
{
    internal TypeAllowedAndConditions Allowed;
    public ConstantFunctionButEnums(TypeAllowedAndConditions allowed)
    {
        this.Allowed = allowed;
    }

    public TypeAllowedAndConditions GetValue(Type type)
    {
        if (EnumEntity.Extract(type) != null)
            return new TypeAllowedAndConditions(TypeAllowed.Read);

        return Allowed;
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
            return AuthAdminMessage.DefaultAuthorization.NiceToString() +
                (InheritFrom.Count == 0 ? (MergeStrategy == MergeStrategy.Union ? AuthAdminMessage.Everything : AuthAdminMessage.Nothing).NiceToString() :
                InheritFrom.Count == 1 ? AuthAdminMessage.SameAs0.NiceToString(InheritFrom.Only()) :
                (MergeStrategy == MergeStrategy.Union ? AuthAdminMessage.MaximumOfThe0 : AuthAdminMessage.MinumumOfThe0).NiceToString(typeof(RoleEntity).NiceCount(InheritFrom.Count)));
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

public class TypeAllowedRule : AllowedRule<TypeEntity, TypeAllowedAndConditions>
{
    public AuthThumbnail? Properties { get; set; }

    public AuthThumbnail? Operations { get; set; }

    public AuthThumbnail? Queries { get; set; }

    public List<TypeConditionSymbol> AvailableConditions { get; set; }
}

  

public class TypeAllowedAndConditions : ModelEntity, IEquatable<TypeAllowedAndConditions>
{
    private TypeAllowedAndConditions()
    {
    }

    public TypeAllowedAndConditions(TypeAllowed fallback, IEnumerable<TypeConditionRuleModel> conditions)
    {
        this.fallback = fallback;
        this.ConditionRules.AddRange(conditions);
    }

    public TypeAllowedAndConditions(TypeAllowed fallback, params TypeConditionRuleModel[] conditions)
    {
        this.fallback = fallback;
        this.ConditionRules.AddRange(conditions);
    }

    TypeAllowed fallback;
    public TypeAllowed Fallback
    {
        get { return fallback; }
        private set { fallback = value; }
    }

    public MList<TypeConditionRuleModel> ConditionRules { get; set; } = new MList<TypeConditionRuleModel>();

    public override bool Equals(object? obj) => obj is TypeAllowedAndConditions tac && Equals(tac);
    public bool Equals(TypeAllowedAndConditions? other)
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
        if(pi.Name == nameof(ConditionRules))
        {
            var errors = NoRepeatValidatorAttribute.ByKey(ConditionRules, a => a.TypeConditions.OrderBy(a => a.ToString()).ToString(" & "));

            if (errors != null)
                return ValidationMessage._0HasSomeRepeatedElements1.NiceToString(pi.NiceName(), errors);
        }

        return base.PropertyValidation(pi);
    }

    public TypeAllowedBasic Min(bool inUserInterface)
    {
        return inUserInterface ? MinUI() : MinDB();
    }

    public TypeAllowedBasic Max(bool inUserInterface)
    {
        return inUserInterface ? MaxUI() : MaxDB();
    }

    public TypeAllowed MinCombined()
    {
        return TypeAllowedExtensions.Create(MinDB(), MinUI());
    }

    public TypeAllowed MaxCombined()
    {
        return TypeAllowedExtensions.Create(MaxDB(), MaxUI());
    }

    public TypeAllowedBasic MinUI()
    {
        if (!ConditionRules.Any())
            return Fallback.GetUI();

        return (TypeAllowedBasic)Math.Min((int)Fallback.GetUI(), ConditionRules.Select(a => (int)a.Allowed.GetUI()).Min());
    }

    public TypeAllowedBasic MaxUI()
    {
        if (!ConditionRules.Any())
            return Fallback.GetUI();

        return (TypeAllowedBasic)Math.Max((int)Fallback.GetUI(), ConditionRules.Select(a => (int)a.Allowed.GetUI()).Max());
    }

    public TypeAllowedBasic MinDB()
    {
        if (!ConditionRules.Any())
            return Fallback.GetDB();

        return (TypeAllowedBasic)Math.Min((int)Fallback.GetDB(), ConditionRules.Select(a => (int)a.Allowed.GetDB()).Min());
    }

    public TypeAllowedBasic MaxDB()
    {
        if (!ConditionRules.Any())
            return Fallback.GetDB();

        return (TypeAllowedBasic)Math.Max((int)Fallback.GetDB(), ConditionRules.Select(a => (int)a.Allowed.GetDB()).Max());
    }

    public override string ToString()
    {
        if (ConditionRules.IsEmpty())
            return Fallback.ToString()!;

        return "{0} | {1}".FormatWith(Fallback, ConditionRules.ToString(" | "));
    }

    internal bool Exactly(TypeAllowed current)
    {
        return Fallback == current && ConditionRules.IsNullOrEmpty();
    }

    public TypeAllowedAndConditions WithoutCondition(TypeConditionSymbol typeCondition)
    {
        return new TypeAllowedAndConditions(this.Fallback, this.ConditionRules.Select(a => a.WithoutCondition(typeCondition)).NotNull().ToMList());
    }
}

public class TypeConditionRuleModel : ModelEntity, IEquatable<TypeConditionRuleModel>
{
    private TypeConditionRuleModel() { }

    public TypeConditionRuleModel(IEnumerable<TypeConditionSymbol> typeConditions, TypeAllowed allowed)
    {
        this.TypeConditions = typeConditions.ToMList();
        this.Allowed = allowed;
    }

    [PreserveOrder, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 0)]
    public MList<TypeConditionSymbol> TypeConditions { get; set; } = new MList<TypeConditionSymbol>();

    public TypeAllowed Allowed { get; set; }

    public override int GetHashCode() => TypeConditions.Count ^ Allowed.GetHashCode();
    public override bool Equals(object? obj) => obj is TypeConditionRuleModel rm && Equals(rm);

    public bool Equals(TypeConditionRuleModel? other)
    {
        if (other == null)
            return false;

        return TypeConditions.ToHashSet().SetEquals(other.TypeConditions) && Allowed == other.Allowed;
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => TypeConditions.ToString(" & ") + " => " + Allowed);

    internal TypeConditionRuleModel? WithoutCondition(TypeConditionSymbol typeCondition)
    {
        if (!TypeConditions.Contains(typeCondition))
            return this;

        if (TypeConditions.Count == 1)
            return null;

        return new TypeConditionRuleModel { TypeConditions = TypeConditions.Where(tc => !tc.Is(typeCondition)).ToMList() };
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
public class PropertyAllowedRule : AllowedRuleCoerced<PropertyRouteEntity, PropertyAllowed>
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
