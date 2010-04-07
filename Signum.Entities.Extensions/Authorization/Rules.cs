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
    public class RuleDN<R, A> : IdentifiableEntity
        where R: IdentifiableEntity
        where A : struct 
    {
        Lite<RoleDN> role;
        [NotNullValidator]
        public Lite<RoleDN> Role
        {
            get { return role; }
            set { Set(ref role, value, () => Role); }
        }

        R resource;
        [NotNullValidator]
        public R Resource
        {
            get { return resource; }
            set { Set(ref resource, value, () => Resource); }
        }

        A allowed;
        public A Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value, () => Allowed); }
        }
    }

    [Serializable]
    public class RuleQueryDN : RuleDN<QueryDN, bool> { }

    [Serializable]
    public class RuleFacadeMethodDN : RuleDN<FacadeMethodDN, bool> { }

    [Serializable]
    public class RulePermissionDN : RuleDN<PermissionDN, bool> { }

    [Serializable]
    public class RuleOperationDN : RuleDN<OperationDN, bool> { }

    [Serializable]
    public class RulePropertyDN : RuleDN<PropertyDN, PropertyAllowed> { }

    [Serializable]
    public class RuleEntityGroupDN : RuleDN<EntityGroupDN, EntityGroupAllowed> { }

    [Serializable]
    public class RuleTypeDN : RuleDN<TypeDN, TypeAllowed> { }

    public enum EntityGroupAllowed
    {
        None = 0,
        In = 1,
        Out  = 2,
        All = 3
    }

    public enum PropertyAllowed
    {
        None,
        Read,
        Modify,
    }

    public enum TypeAllowed
    {
        None = 0,
        Read = 1,
        Modify = 2,
        Create = 3,
    }
}