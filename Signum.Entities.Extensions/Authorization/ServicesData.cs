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
            return "Constant {0}".Formato(Allowed);
        }
    }

    public static class ConstantFunction
    {
        public static A GetConstantValue<K, A>(Func<K, A> defaultConstant)
        {
            return ((ConstantFunction<K, A>)defaultConstant.Target).Allowed;
        }
    }


    [Serializable]
    public abstract class BaseRulePack<T> : ModelEntity
    {
        Lite<RoleDN> role;
        [NotNullValidator]
        public Lite<RoleDN> Role
        {
            get { return role; }
            internal set { Set(ref role, value, () => Role); }
        }

        MergeStrategy mergeStrategy;
        public MergeStrategy MergeStrategy
        {
            get { return mergeStrategy; }
            set { Set(ref mergeStrategy, value, () => MergeStrategy); }
        }

        [NotNullable]
        MList<Lite<RoleDN>> subRoles = new MList<Lite<RoleDN>>();
        public MList<Lite<RoleDN>> SubRoles
        {
            get { return subRoles; }
            set { Set(ref subRoles, value, () => SubRoles); }
        }

        public string Strategy
        {
            get
            {
                return AuthAdminMessage._0of1.NiceToString().Formato(
                    mergeStrategy.NiceToString(),
                    subRoles.IsNullOrEmpty() ? "∅  -> " + (mergeStrategy == MergeStrategy.Union ? AuthAdminMessage.Nothing : AuthAdminMessage.Everything).NiceToString() :
                    subRoles.CommaAnd());
            }
        }

        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            internal set { Set(ref type, value, () => Type); }
        }

        [NotNullable]
        MList<T> rules = new MList<T>();
        public MList<T> Rules
        {
            get { return rules; }
            set { Set(ref rules, value, () => Rules); }
        }
    }

    [Serializable, DescriptionOptions(DescriptionOptions.None)]
    public abstract class AllowedRule<R, A> : ModelEntity
        where R : IdentifiableEntity
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
                if (Set(ref allowed, value, () => Allowed))
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
            set { Set(ref resource, value, () => Resource); }
        }

        public override string ToString()
        {
            return "{0} -> {1}".Formato(resource, Allowed) + (Overriden ? "(overriden from {0})".Formato(AllowedBase) : "");
        }

    }

    [Serializable]
    public class TypeRulePack : BaseRulePack<TypeAllowedRule>
    {
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().Formato(typeof(TypeDN).NiceName(), Role);
        }
    }

    [Serializable]
    public class TypeAllowedRule : AllowedRule<TypeDN, TypeAllowedAndConditions> 
    {
        AuthThumbnail? properties;
        public AuthThumbnail? Properties
        {
            get { return properties; }
            set { Set(ref properties, value, () => Properties); }
        }

        AuthThumbnail? operations;
        public AuthThumbnail? Operations
        {
            get { return operations; }
            set { Set(ref operations, value, () => Operations); }
        }

        AuthThumbnail? queries;
        public AuthThumbnail? Queries
        {
            get { return queries; }
            set { Set(ref queries, value, () => Queries); }
        }

        ReadOnlyCollection<Enum> availableConditions;
        public ReadOnlyCollection<Enum> AvailableConditions
        {
            get { return availableConditions; }
            set { Set(ref availableConditions, value, () => AvailableConditions); }
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

            return "{0} | {1}".Formato(Fallback, conditions.ToString(c=>"{0} {1}".Formato(c.ConditionName, c.Allowed), " | "));
        }

        internal bool Exactly(TypeAllowed current)
        {
            return Fallback == current && Conditions.IsNullOrEmpty();
        }
    }

    [Serializable, DescriptionOptions(DescriptionOptions.None)]
    public class TypeConditionRule : EmbeddedEntity, IEquatable<TypeConditionRule>
    {
        public TypeConditionRule(Enum conditionName, TypeAllowed allowed)
        {
            this.conditionName = conditionName;
            this.allowed = allowed;
        }

        Enum conditionName;
        public Enum ConditionName
        {
            get { return conditionName; }
            set { Set(ref conditionName, value, () => ConditionName); }
        }

        TypeAllowed allowed;
        public TypeAllowed Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value, () => Allowed); }
        }

        public bool Equals(TypeConditionRule other)
        {
            return conditionName.Equals(other.conditionName) && 
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
         where R : IdentifiableEntity
    {
        A[] coercedValues;
        public A[] CoercedValues
        {
            get { return coercedValues; }
            internal set { Set(ref coercedValues, value, () => CoercedValues); }
        }
    }

    [Serializable]
    public class PropertyRulePack : BaseRulePack<PropertyAllowedRule>
    {
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().Formato(typeof(PropertyRouteDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class PropertyAllowedRule : AllowedRuleCoerced<PropertyRouteDN, PropertyAllowed>
    {
    }


    [Serializable]
    public class QueryRulePack : BaseRulePack<QueryAllowedRule>
    {
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().Formato(typeof(QueryDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class QueryAllowedRule : AllowedRuleCoerced<QueryDN, bool> { }


    [Serializable]
    public class OperationRulePack : BaseRulePack<OperationAllowedRule>
    {
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().Formato(typeof(OperationDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class OperationAllowedRule : AllowedRuleCoerced<OperationDN, OperationAllowed> { } 


    [Serializable]
    public class PermissionRulePack : BaseRulePack<PermissionAllowedRule>
    {
        public override string ToString()
        {
            return AuthMessage._0RulesFor1.NiceToString().Formato(typeof(PermissionDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class PermissionAllowedRule : AllowedRule<PermissionDN, bool> { } 
}
