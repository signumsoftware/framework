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
        Lite<RoleEntity> role;
        [NotNullValidator]
        public Lite<RoleEntity> Role
        {
            get { return role; }
            internal set { Set(ref role, value); }
        }

        MergeStrategy mergeStrategy;
        [HiddenProperty]
        public MergeStrategy MergeStrategy
        {
            get { return mergeStrategy; }
            set { Set(ref mergeStrategy, value); }
        }

        [NotNullable]
        MList<Lite<RoleEntity>> subRoles = new MList<Lite<RoleEntity>>();
        [HiddenProperty]
        public MList<Lite<RoleEntity>> SubRoles
        {
            get { return subRoles; }
            set { Set(ref subRoles, value); }
        }

        public string Strategy
        {
            get
            {
                return AuthAdminMessage._0of1.NiceToString().FormatWith(
                    mergeStrategy.NiceToString(),
                    subRoles.IsNullOrEmpty() ? "∅  -> " + (mergeStrategy == MergeStrategy.Union ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).NiceToString() :
                    subRoles.CommaAnd());
            }
        }

        TypeEntity type;
        [NotNullValidator]
        public TypeEntity Type
        {
            get { return type; }
            internal set { Set(ref type, value); }
        }

        [NotNullable]
        MList<T> rules = new MList<T>();
        public MList<T> Rules
        {
            get { return rules; }
            set { Set(ref rules, value); }
        }
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

        R resource;
        public R Resource
        {
            get { return resource; }
            set { Set(ref resource, value); }
        }

        public override string ToString()
        {
            return "{0} -> {1}".FormatWith(resource, Allowed) + (Overriden ? "(overriden from {0})".FormatWith(AllowedBase) : "");
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
        AuthThumbnail? properties;
        public AuthThumbnail? Properties
        {
            get { return properties; }
            set { Set(ref properties, value); }
        }

        AuthThumbnail? operations;
        public AuthThumbnail? Operations
        {
            get { return operations; }
            set { Set(ref operations, value); }
        }

        AuthThumbnail? queries;
        public AuthThumbnail? Queries
        {
            get { return queries; }
            set { Set(ref queries, value); }
        }

        ReadOnlyCollection<TypeConditionSymbol> availableConditions;
        public ReadOnlyCollection<TypeConditionSymbol> AvailableConditions
        {
            get { return availableConditions; }
            set { Set(ref availableConditions, value); }
        }
    }

    [Serializable, DescriptionOptions(DescriptionOptions.None)]
    public class TypeAllowedAndConditions : ModelEntity, IEquatable<TypeAllowedAndConditions>
    {
        public TypeAllowedAndConditions(TypeAllowed fallback, ReadOnlyCollection<TypeConditionRule> conditions)
        {
            this.fallback = fallback;
            this.conditions = conditions;
        }

        public TypeAllowedAndConditions(TypeAllowed fallback, params TypeConditionRule[] conditions)
        {
            this.fallback = fallback;
            this.conditions = conditions.ToReadOnly();
        }

        readonly TypeAllowed fallback;
        public TypeAllowed Fallback
        {
            get { return fallback; }
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
                return fallback.GetUI();

            return (TypeAllowedBasic)Math.Min((int)fallback.GetUI(), conditions.Select(a => (int)a.Allowed.GetUI()).Min());
        }

        public TypeAllowedBasic MaxUI()
        {
            if (!conditions.Any())
                return fallback.GetUI();

            return (TypeAllowedBasic)Math.Max((int)fallback.GetUI(), conditions.Select(a => (int)a.Allowed.GetUI()).Max());
        }

        public TypeAllowedBasic MinDB()
        {
            if (!conditions.Any())
                return fallback.GetDB();

            return (TypeAllowedBasic)Math.Min((int)fallback.GetDB(), conditions.Select(a => (int)a.Allowed.GetDB()).Min());
        }

        public TypeAllowedBasic MaxDB()
        {
            if (!conditions.Any())
                return fallback.GetDB();

            return (TypeAllowedBasic)Math.Max((int)fallback.GetDB(), conditions.Select(a => (int)a.Allowed.GetDB()).Max());
        }

        public override string ToString()
        {
            if (conditions.IsEmpty())
                return Fallback.ToString();

            return "{0} | {1}".FormatWith(Fallback, conditions.ToString(c=>"{0} {1}".FormatWith(c.TypeCondition, c.Allowed), " | "));
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
            this.typeCondition = typeCondition;
            this.allowed = allowed;
        }

        TypeConditionSymbol typeCondition;
        public TypeConditionSymbol TypeCondition
        {
            get { return typeCondition; }
            set { Set(ref typeCondition, value); }
        }

        TypeAllowed allowed;
        public TypeAllowed Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value); }
        }

        public bool Equals(TypeConditionRule other)
        {
            return typeCondition.Equals(other.typeCondition) && 
                allowed.Equals(other.allowed);
        }
    }

    public enum AuthThumbnail
    {
        All,
        Mix,
        None,
    }

    [Serializable]
    public abstract class AllowedRuleCoerced<R, A> : AllowedRule<R,A>
         where R : Entity
    {
        A[] coercedValues;
        public A[] CoercedValues
        {
            get { return coercedValues; }
            internal set { Set(ref coercedValues, value); }
        }
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
