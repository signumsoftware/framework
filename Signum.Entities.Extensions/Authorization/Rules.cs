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
    public class RuleFacadeMethodDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        FacadeMethodDN serviceOperation;
        [NotNullValidator]
        public FacadeMethodDN ServiceOperation
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
    public class RuleOperationDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        OperationDN operation;
        [NotNullValidator]
        public OperationDN Operation
        {
            get { return operation; }
            set { Set(ref operation, value, "Operation"); }
        }

        bool allowed;
        public bool Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value, "Allowed"); }
        }
    }

    [Serializable]
    public class RulePropertyDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        PropertyDN property;
        [NotNullValidator]
        public PropertyDN Property
        {
            get { return property; }
            set { Set(ref property, value, "Property"); }
        }

        Access access;
        public Access Access
        {
            get { return access; }
            set { Set(ref access, value, "Access"); }
        }
    }

    //[Serializable]
    //public class RuleEntityGroupDN : IdentifiableEntity
    //{
    //    RoleDN role;
    //    [NotNullValidator]
    //    public RoleDN Role
    //    {
    //        get { return role; }
    //        set { Set(ref role, value, "Role"); }
    //    }

    //    EntityGroupDN group;
    //    [NotNullValidator]
    //    public EntityGroupDN Group
    //    {
    //        get { return group; }
    //        set { Set(ref group, value, "Group"); }
    //    }

    //    Access inGroupAccess;
    //    public Access InGroupAccess
    //    {
    //        get { return inGroupAccess; }
    //        set { Set(ref inGroupAccess, value, "InGroupAccess"); }
    //    }

    //    Access notInGroupAccess;
    //    public Access NotInGroupAccess
    //    {
    //        get { return notInGroupAccess; }
    //        set { Set(ref notInGroupAccess, value, "NotInGroupAccess"); }
    //    }
    //}

    public enum Access
    {
        None,
        Read,
        Modify,
    }

    [Serializable]
    public class RuleTypeDN : IdentifiableEntity
    {
        RoleDN role;
        [NotNullValidator]
        public RoleDN Role
        {
            get { return role; }
            set { Set(ref role, value, "Role"); }
        }

        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            set { Set(ref type, value, "Type"); }
        }

        TypeAccess access;
        public TypeAccess Access
        {
            get { return access; }
            set { Set(ref access, value, "TypeAccess"); }
        }
    }

    public enum TypeAccess
    {
        None,
        Read,
        Modify,
        Create,
    }
}