using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Operations;

namespace Signum.Entities.Authorization
{
    //Only for client-side communication
    [Serializable]
    public abstract class BaseRulePack<T> : IdentifiableEntity
        where T : EmbeddedEntity
    {
        Lite<RoleDN> role;
        [NotNullValidator]
        public Lite<RoleDN> Role
        {
            get { return role; }
            internal set { Set(ref role, value, () => Role); }
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

    [Serializable]
    public abstract class AllowedRule<R, A> : EmbeddedEntity
        where R : IdentifiableEntity
        where A : struct
    {
        A allowedBase;
        public A AllowedBase
        {
            get { return allowedBase; }
            internal set { allowedBase = value; }
        }

        A? allowedOverride;
        public A Allowed
        {
            get { return allowedOverride ?? allowedBase; }
            set
            {
                A? val = value.Equals(allowedBase) ? (A?)null : value;

                if (Set(ref allowedOverride, val, () => Allowed))
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
            get { return allowedOverride.HasValue; }
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
    public class TypeRulePack : BaseRulePack<TypeAllowedRule> { }
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
    public class PropertyRulePack : BaseRulePack<PropertyAllowedRule> { }
    [Serializable]
    public class PropertyAllowedRule : AllowedRule<PropertyDN, PropertyAllowed>{}


    [Serializable]
    public class QueryRulePack : BaseRulePack<QueryAllowedRule> { }
    [Serializable]
    public class QueryAllowedRule : AllowedRule<QueryDN, bool> { }


    [Serializable]
    public class OperationRulePack : BaseRulePack<OperationAllowedRule> { }
    [Serializable]
    public class OperationAllowedRule : AllowedRule<OperationDN, bool> { } 


    [Serializable]
    public class PermissionRulePack : BaseRulePack<PermissionAllowedRule> { }
    [Serializable]
    public class PermissionAllowedRule : AllowedRule<PermissionDN, bool> { } 

    [Serializable]
    public class FacadeMethodRulePack : BaseRulePack<FacadeMethodAllowedRule> { }
    [Serializable]
    public class FacadeMethodAllowedRule : AllowedRule<FacadeMethodDN, bool> { } 

    [Serializable]
    public class EntityGroupRulePack : BaseRulePack<EntityGroupAllowedRule> { }
    [Serializable]
    public class EntityGroupAllowedRule : AllowedRule<EntityGroupDN, EntityGroupAllowed>
    {
        public TypeAllowed In
        {
            get { return EntityGroupAllowedUtils.In(Allowed); }
            set
            {
                Allowed = EntityGroupAllowedUtils.FromInOut(value, EntityGroupAllowedUtils.Out(Allowed));
                Notify(() => In);
            }
        }

        public TypeAllowed Out
        {
            get { return EntityGroupAllowedUtils.Out(Allowed); }
            set
            {
                Allowed = EntityGroupAllowedUtils.FromInOut(EntityGroupAllowedUtils.In(Allowed), value);
                Notify(() => Out);
            }
        }

        public TypeAllowed InBase
        {
            get { return EntityGroupAllowedUtils.In(AllowedBase); }
        }

        public TypeAllowed OutBase
        {
            get { return EntityGroupAllowedUtils.Out(AllowedBase); }
        }
    }
}
