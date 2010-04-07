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
    public abstract class BaseRulePack<R, A> : IdentifiableEntity
        where R : IdentifiableEntity
        where A : struct
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

        MList<AllowedRule<R, A>> rules;
        public MList<AllowedRule<R, A>> Rules
        {
            get { return rules; }
            set { Set(ref rules, value, () => Rules); }
        }
    }

    [Serializable]
    public class AllowedRule<R, A> : EmbeddedEntity
        where R : IdentifiableEntity
        where A : struct
    {
        public AllowedRule(A allowedBase)
        {
            this.allowedBase = allowedBase;
        }

        A allowedBase;
        public A AllowedBase
        {
            get { return allowedBase; }
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
    public class TypeRulePack : BaseRulePack<TypeDN, TypeAllowed> { }

    [Serializable]
    public class PropertyRulePack : BaseRulePack<PropertyDN, PropertyAllowed> { }

    [Serializable]
    public class QueryRulePack : BaseRulePack<QueryDN, bool> { }

    [Serializable]
    public class OperationRulePack : BaseRulePack<OperationDN, bool> { }

    [Serializable]
    public class PermissionRulePack : BaseRulePack<PermissionDN, bool> { }

    [Serializable]
    public class FacadeMethodRulePack : BaseRulePack<FacadeMethodDN, bool> { }

    [Serializable]
    public class EntityGroupRulePack : BaseRulePack<EntityGroupDN, EntityGroupAllowed> { }
}
