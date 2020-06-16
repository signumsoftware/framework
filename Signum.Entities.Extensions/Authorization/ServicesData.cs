using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.Basics;
using Signum.Utilities;
using System.Collections.ObjectModel;

namespace Signum.Entities.Authorization
{
    [Serializable]
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
            if (OverrideDictionary != null && OverrideDictionary.TryGetValue(key, out A result))
                return result;

            return DefaultAllowed(key);
        }
    }

    [Serializable]
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

    [Serializable]
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


    [Serializable, InTypeScript(Undefined = false)]
    public abstract class BaseRulePack<T> : ModelEntity
    {
        
        public Lite<RoleEntity> Role { get; internal set; }

        [HiddenProperty]
        public MergeStrategy MergeStrategy { get; set; }

        [HiddenProperty]
        public MList<Lite<RoleEntity>> SubRoles { get; set; } = new MList<Lite<RoleEntity>>();

        public string Strategy
        {
            get
            {
                return AuthAdminMessage._0of1.NiceToString().FormatWith(
                    MergeStrategy.NiceToString(),
                    SubRoles.IsNullOrEmpty() ? "∅  -> " + (MergeStrategy == MergeStrategy.Union ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).NiceToString() :
                    SubRoles.CommaAnd());
            }
        }
        
        
        public MList<T> Rules { get; set; } = new MList<T>();
    }

    [Serializable, InTypeScript(Undefined = false)]
    public abstract class AllowedRule<R, A> : ModelEntity
        where R : notnull
        where A : notnull
    {
        A allowedBase;
        [InTypeScript(Null = false)]
        public A AllowedBase
        {
            get { return allowedBase; }
            set { allowedBase = value; }
        }

        A allowed;
        [InTypeScript(Null = false)]
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

        [InTypeScript(Null = false)]
        public R Resource { get; set; }

        public override string ToString()
        {
            return "{0} -> {1}".FormatWith(Resource, Allowed) + (Overriden ? "(overriden from {0})".FormatWith(AllowedBase) : "");
        }

    }

    [Serializable]
    public class TypeRulePack : BaseRulePack<TypeAllowedRule>
    {
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(TypeEntity).NiceName(), Role);
        }
    }

    [Serializable]
    public class TypeAllowedRule : AllowedRule<TypeEntity, TypeAllowedAndConditions>
    {
        public AuthThumbnail? Properties { get; set; }

        public AuthThumbnail? Operations { get; set; }

        public AuthThumbnail? Queries { get; set; }

        public ReadOnlyCollection<TypeConditionSymbol> AvailableConditions { get; set; }
    }

    [Serializable]
    public class TypeAllowedAndConditions : ModelEntity, IEquatable<TypeAllowedAndConditions>
    {
        private TypeAllowedAndConditions()
        {
        }

        public TypeAllowedAndConditions(TypeAllowed? fallback, IEnumerable<TypeConditionRuleEmbedded> conditions)
        {
            this.fallback = fallback;
            this.Conditions.AddRange(conditions);
        }

        public TypeAllowedAndConditions(TypeAllowed? fallback, params TypeConditionRuleEmbedded[] conditions)
        {
            this.fallback = fallback;
            this.Conditions.AddRange(conditions);
        }

        TypeAllowed? fallback;
        [InTypeScript(Undefined =false)]
        public TypeAllowed? Fallback
        {
            get { return fallback; }
            private set { fallback = value; }
        }

        [InTypeScript(false)]
        public TypeAllowed FallbackOrNone
        {
            get { return this.fallback ?? TypeAllowed.None; }
        }

        public MList<TypeConditionRuleEmbedded> Conditions { get; set; } = new MList<TypeConditionRuleEmbedded>();

        public override bool Equals(object? obj) => obj is TypeAllowedAndConditions tac && Equals(tac);
        public bool Equals(TypeAllowedAndConditions other)
        {
            return this.fallback.Equals(other.fallback) &&
                this.Conditions.SequenceEqual(other.Conditions);
        }

        public override int GetHashCode()
        {
            return this.fallback.GetHashCode();
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
            if (!Conditions.Any())
                return FallbackOrNone.GetUI();

            return (TypeAllowedBasic)Math.Min((int)fallback!.Value.GetUI(), Conditions.Select(a => (int)a.Allowed.GetUI()).Min());
        }

        public TypeAllowedBasic MaxUI()
        {
            if (!Conditions.Any())
                return FallbackOrNone.GetUI();

            return (TypeAllowedBasic)Math.Max((int)fallback!.Value.GetUI(), Conditions.Select(a => (int)a.Allowed.GetUI()).Max());
        }

        public TypeAllowedBasic MinDB()
        {
            if (!Conditions.Any())
                return FallbackOrNone.GetDB();

            return (TypeAllowedBasic)Math.Min((int)fallback!.Value.GetDB(), Conditions.Select(a => (int)a.Allowed.GetDB()).Min());
        }

        public TypeAllowedBasic MaxDB()
        {
            if (!Conditions.Any())
                return FallbackOrNone.GetDB();

            return (TypeAllowedBasic)Math.Max((int)fallback!.Value.GetDB(), Conditions.Select(a => (int)a.Allowed.GetDB()).Max());
        }

        public override string ToString()
        {
            if (Conditions.IsEmpty())
                return Fallback.ToString()!;

            return "{0} | {1}".FormatWith(Fallback, Conditions.ToString(c => "{0} {1}".FormatWith(c.TypeCondition, c.Allowed), " | "));
        }

        internal bool Exactly(TypeAllowed current)
        {
            return Fallback == current && Conditions.IsNullOrEmpty();
        }
    }

    [Serializable, InTypeScript(Undefined = false)]
    public class TypeConditionRuleEmbedded : EmbeddedEntity, IEquatable<TypeConditionRuleEmbedded>
    {
        private TypeConditionRuleEmbedded() { }

        public TypeConditionRuleEmbedded(TypeConditionSymbol typeCondition, TypeAllowed allowed)
        {
            this.TypeCondition = typeCondition;
            this.Allowed = allowed;
        }

        [InTypeScript(Null = false)]
        public TypeConditionSymbol TypeCondition { get; set; }

        public TypeAllowed Allowed { get; set; }

        public bool Equals(TypeConditionRuleEmbedded other)
        {
            return TypeCondition.Equals(other.TypeCondition) &&
                Allowed.Equals(other.Allowed);
        }
    }

    public enum AuthThumbnail
    {
        All,
        Mix,
        None,
    }

    [Serializable]
    public abstract class AllowedRuleCoerced<R, A> : AllowedRule<R, A>
        where R : notnull
        where A : notnull
    {
        public A[] CoercedValues { get; internal set; }
    }

    [Serializable]
    public class PropertyRulePack : BaseRulePack<PropertyAllowedRule>
    {
        
        public TypeEntity Type { get; internal set; }

        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(PropertyRouteEntity).NiceName(), Role);
        }
    }
    [Serializable]
    public class PropertyAllowedRule : AllowedRuleCoerced<PropertyRouteEntity, PropertyAllowed>
    {
    }


    [Serializable]
    public class QueryRulePack : BaseRulePack<QueryAllowedRule>
    {
        
        public TypeEntity Type { get; internal set; }

        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(QueryEntity).NiceName(), Role);
        }
    }
    [Serializable]
    public class QueryAllowedRule : AllowedRuleCoerced<QueryEntity, QueryAllowed> { }


    [Serializable]
    public class OperationRulePack : BaseRulePack<OperationAllowedRule>
    {
        
        public TypeEntity Type { get; internal set; }

        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(OperationSymbol).NiceName(), Role);
        }
    }
    [Serializable]
    public class OperationAllowedRule : AllowedRuleCoerced<OperationTypeEmbedded, OperationAllowed> { }

    [Serializable]
    public class PermissionRulePack : BaseRulePack<PermissionAllowedRule>
    {
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(PermissionSymbol).NiceName(), Role);
        }
    }
    [Serializable]
    public class PermissionAllowedRule : AllowedRule<PermissionSymbol, bool> { }
}
