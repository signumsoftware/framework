using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Operations;
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class DefaultDictionary<K, A>
    {
        public DefaultDictionary(A defaultAllowed, Dictionary<K, A> dictionary)
        {
            this.DefaultAllowed = defaultAllowed;
            this.dictionary = dictionary;
        }

        readonly Dictionary<K, A> dictionary;
        public readonly A DefaultAllowed;

        public A GetAllowed(K key)
        {
            return dictionary.TryGet(key, DefaultAllowed);
        }

        public IEnumerable<K> ExplicitKeys
        {
            get
            {
                return dictionary == null ? Enumerable.Empty<K>() : dictionary.Keys;
            }
        }
    }


    //Only for client-side communication
    [Serializable, AvoidLocalization]
    public abstract class BaseRulePack<T> : ModelEntity
    {
        Lite<RoleDN> role;
        [NotNullValidator]
        public Lite<RoleDN> Role
        {
            get { return role; }
            internal set { Set(ref role, value, () => Role); }
        }

        List<Lite<RoleDN>> subRoles;
        public List<Lite<RoleDN>> SubRoles
        {
            get { return subRoles; }
            set { Set(ref subRoles, value, () => SubRoles); }
        }

        public string DefaultLabel
        {
            get { return subRoles == null || subRoles.Empty() ? "Value" : "of " + subRoles.CommaAnd(); }
        }

        DefaultRule defaultRule;
        public DefaultRule DefaultRule
        {
            get { return defaultRule; }
            set { Set(ref defaultRule, value, () => DefaultRule); }
        } 

        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            internal set { Set(ref type, value, () => Type); }
        }

        MList<T> rules;
        public MList<T> Rules
        {
            get { return rules; }
            set { Set(ref rules, value, () => Rules); }
        }
    }

    public enum DefaultRule
    {
        Max, 
        Min,
    }

    [Serializable, AvoidLocalization]
    public abstract class AllowedRule<R, A> : EmbeddedEntity
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
            return Resources._0RulesFor1.Formato(typeof(TypeDN).NiceName(), Role);
        }
    }

    [Serializable]
    public class TypeAllowedRule : AllowedRule<TypeDN, TypeAllowed> 
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
    }

    public enum AuthThumbnail
    {
        All,
        Mix,
        None,
    }


    [Serializable]
    public class PropertyRulePack : BaseRulePack<PropertyAllowedRule>
    {
        public override string ToString()
        {
            return Resources._0RulesFor1.Formato(typeof(PropertyDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class PropertyAllowedRule : AllowedRule<PropertyDN, PropertyAllowed>{}


    [Serializable]
    public class QueryRulePack : BaseRulePack<QueryAllowedRule>
    {
        public override string ToString()
        {
            return Resources._0RulesFor1.Formato(typeof(QueryDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class QueryAllowedRule : AllowedRule<QueryDN, bool> { }


    [Serializable]
    public class OperationRulePack : BaseRulePack<OperationAllowedRule>
    {
        public override string ToString()
        {
            return Resources._0RulesFor1.Formato(typeof(OperationDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class OperationAllowedRule : AllowedRule<OperationDN, bool> { } 


    [Serializable]
    public class PermissionRulePack : BaseRulePack<PermissionAllowedRule>
    {
        public override string ToString()
        {
            return Resources._0RulesFor1.Formato(typeof(PermissionDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class PermissionAllowedRule : AllowedRule<PermissionDN, bool> { } 

    [Serializable]
    public class FacadeMethodRulePack : BaseRulePack<FacadeMethodAllowedRule>
    {
        public override string ToString()
        {
            return Resources._0RulesFor1.Formato(typeof(FacadeMethodDN).NiceName(), Role);
        }
    }
    [Serializable]
    public class FacadeMethodAllowedRule : AllowedRule<FacadeMethodDN, bool> { } 

    [Serializable]
    public class EntityGroupRulePack : BaseRulePack<EntityGroupAllowedRule>
    {
        public override string ToString()
        {
            return Resources._0RulesFor1.Formato(typeof(EntityGroupDN).NiceName(), Role);
        }
    }

    [Serializable]
    public class EntityGroupAllowedRule : AllowedRule<EntityGroupDN, EntityGroupAllowedDN> { }
}
