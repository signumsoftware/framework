using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Utilities;
using System.Collections.ObjectModel;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class DefaultDictionary<K, A>
    {
        public DefaultDictionary(Func<K, A> defaultAllowed, Dictionary<K, A> overridesDictionary)
        {
            this.defaultAllowed = defaultAllowed;
            this.overrideDictionary = overridesDictionary;
        }

        readonly Dictionary<K, A> overrideDictionary;
        readonly Func<K, A> defaultAllowed;

        public A GetAllowed(K key)
        {
            A result;
            if (overrideDictionary != null && overrideDictionary.TryGetValue(key, out result))
                return result;

            return defaultAllowed(key);
        }

        public Dictionary<K, A> OverrideDictionary
        {
            get { return overrideDictionary; }
        }

        public Func<K, A> DefaultAllowed
        {
            get { return defaultAllowed; }
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
    public abstract class BaseRulePack<T> : ModelEntity
    {
        [NotNullValidator]
        public Lite<RoleEntity> Role { get; internal set; }

        [HiddenProperty]
        public MergeStrategy MergeStrategy { get; set; }

        [NotNullable]
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

        [NotNullValidator]
        public TypeEntity Type { get; internal set; }

        [NotNullable]
        public MList<T> Rules { get; set; } = new MList<T>();
    }

    [Serializable, DescriptionOptions(DescriptionOptions.None)]
    public abstract class AllowedRule<R, A> : ModelEntity
        where R : Entity
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

    [Serializable, DescriptionOptions(DescriptionOptions.None)]
    public class TypeAllowedAndConditions : ModelEntity, IEquatable<TypeAllowedAndConditions>
    {
        public TypeAllowedAndConditions(TypeAllowed? fallback, ReadOnlyCollection<TypeConditionRule> conditions)
        {
            this.fallback = fallback;
            this.conditions = conditions;
        }

        public TypeAllowedAndConditions(TypeAllowed? fallback, params TypeConditionRule[] conditions)
        {
            this.fallback = fallback;
            this.conditions = conditions.ToReadOnly();
        }

        readonly TypeAllowed? fallback;
        public TypeAllowed? Fallback
        {
            get { return fallback; }
        }

        public TypeAllowed FallbackOrNone
        {
            get { return this.fallback ?? TypeAllowed.None; }
        }

        readonly ReadOnlyCollection<TypeConditionRule> conditions;
        public ReadOnlyCollection<TypeConditionRule> Conditions
        {
            get { return conditions; }
        }

        public bool Equals(TypeAllowedAndConditions other)
        {
            return this.fallback.Equals(other.fallback) &&
                this.conditions.SequenceEqual(other.conditions);
        }

        public override bool Equals(object obj)
        {
            var other = obj as TypeAllowedAndConditions;
            return other != null && Equals(other);
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
            if (!conditions.Any())
                return FallbackOrNone.GetUI();

            return (TypeAllowedBasic)Math.Min((int)fallback.Value.GetUI(), conditions.Select(a => (int)a.Allowed.GetUI()).Min());
        }

        public TypeAllowedBasic MaxUI()
        {
            if (!conditions.Any())
                return FallbackOrNone.GetUI();

            return (TypeAllowedBasic)Math.Max((int)fallback.Value.GetUI(), conditions.Select(a => (int)a.Allowed.GetUI()).Max());
        }

        public TypeAllowedBasic MinDB()
        {
            if (!conditions.Any())
                return FallbackOrNone.GetDB();

            return (TypeAllowedBasic)Math.Min((int)fallback.Value.GetDB(), conditions.Select(a => (int)a.Allowed.GetDB()).Min());
        }

        public TypeAllowedBasic MaxDB()
        {
            if (!conditions.Any())
                return FallbackOrNone.GetDB();

            return (TypeAllowedBasic)Math.Max((int)fallback.Value.GetDB(), conditions.Select(a => (int)a.Allowed.GetDB()).Max());
        }

        public override string ToString()
        {
            if (conditions.IsEmpty())
                return Fallback.ToString();

            return "{0} | {1}".FormatWith(Fallback, conditions.ToString(c => "{0} {1}".FormatWith(c.TypeCondition, c.Allowed), " | "));
        }

        internal bool Exactly(TypeAllowed current)
        {
            return Fallback == current && Conditions.IsNullOrEmpty();
        }
    }

    [Serializable, DescriptionOptions(DescriptionOptions.None)]
    public class TypeConditionRule : EmbeddedEntity, IEquatable<TypeConditionRule>
    {
        public TypeConditionRule(TypeConditionSymbol typeCondition, TypeAllowed allowed)
        {
            this.TypeCondition = typeCondition;
            this.Allowed = allowed;
        }

        public TypeConditionSymbol TypeCondition { get; set; }

        public TypeAllowed Allowed { get; set; }

        public bool Equals(TypeConditionRule other)
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
         where R : Entity
    {
        public A[] CoercedValues { get; internal set; }
    }

    [Serializable]
    public class PropertyRulePack : BaseRulePack<PropertyAllowedRule>
    {
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
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(QueryEntity).NiceName(), Role);
        }
    }
    [Serializable]
    public class QueryAllowedRule : AllowedRuleCoerced<QueryEntity, bool> { }


    [Serializable]
    public class OperationRulePack : BaseRulePack<OperationAllowedRule>
    {
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().FormatWith(typeof(OperationSymbol).NiceName(), Role);
        }
    }
    [Serializable]
    public class OperationAllowedRule : AllowedRuleCoerced<OperationSymbol, OperationAllowed> { }


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
