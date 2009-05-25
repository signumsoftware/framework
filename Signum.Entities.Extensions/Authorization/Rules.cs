using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Operations;

namespace Signum.Entities.Authorization
{
    [Serializable]
    public class RuleQueryDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value, "Query"); }
        }

        bool allowed;
        public bool Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value, "Allowed"); }
        }
    }

    [Serializable]
    public class RuleServiceOperationDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        ServiceOperationDN serviceOperation;
        [NotNullValidator]
        public ServiceOperationDN ServiceOperation
        {
            get { return serviceOperation; }
            set { Set(ref serviceOperation, value, "ServiceOperation"); }
        }

        bool allowed;
        public bool Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value, "Allowed"); }
        }
    }

    [Serializable]
    public class RulePermissionDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        PermissionDN permission;
        [NotNullValidator]
        public PermissionDN Permission
        {
            get { return permission; }
            set { Set(ref permission, value, "Permission"); }
        }

        bool allowed;
        public bool Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value, "Allowed"); }
        }
    }

    [Serializable]
    public class RuleActionDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        ActionDN action;
        [NotNullValidator]
        public ActionDN Action
        {
            get { return action; }
            set { Set(ref action, value, "Action"); }
        }

        bool allowed;
        public bool Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value, "Allowed"); }
        }
    }

    //Only for client-side communication
    [Serializable]
    public class AllowedRule : EmbeddedEntity
    {
        public AllowedRule(bool allowedBase)
        {
            this.allowedBase = allowedBase;
        }

        bool allowedBase;
        public bool AllowedBase
        {
            get { return allowedBase; }
        }

        bool? allowedOverride;
        public bool Allowed
        {
            get { return allowedOverride ?? allowedBase; }
            set
            {
                bool? val = value == allowedBase ? (bool?)null : value;

                if (Set(ref allowedOverride, val, "Allowed"))
                {
                    Notify("Overriden");
                }
            }
        }

        public bool Overriden
        {
            get { return allowedOverride.HasValue; }
        }

        IdentifiableEntity resource;
        public IdentifiableEntity Resource
        {
            get { return resource; }
            set { Set(ref resource, value, "Resource"); }
        }
    }
}