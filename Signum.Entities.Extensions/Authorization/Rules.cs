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
        //In - Out
        NoneNone = 0,
        NoneRead = 1,
        NoneModify = 2,
        NoneCreate = 3,

        ReadNone = 4,
        ReadRead = 5,
        ReadModify = 6,
        ReadCreate = 7,

        ModifyNone = 8,
        ModifyRead = 9,
        ModifyModify = 10,
        ModifyCreate = 11,

        CreateNone = 12,
        CreateRead = 13,
        CreateModify = 14,
        CreateCreate = 15,
    }

    public static class EntityGroupAllowedUtils
    {
        public static EntityGroupAllowed FromInOut(TypeAllowed inAllowed, TypeAllowed outAllowed)
        {
            return (EntityGroupAllowed)(((int)inAllowed << 2) | (int)outAllowed);
        }

        public static TypeAllowed In(EntityGroupAllowed groupAllowed)
        {
            return (TypeAllowed)(((int)groupAllowed >> 2) & 0x3);
        }

        public static TypeAllowed Out(EntityGroupAllowed groupAllowed)
        {
            return (TypeAllowed)((int)groupAllowed & 0x3);
        }
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