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

        [NotNullable] //sometimes A is an EmbeddedEntity
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
    public class RuleEntityGroupDN : RuleDN<EntityGroupDN, EntityGroupAllowedDN> { }

    [Serializable]
    public class RuleTypeDN : RuleDN<TypeDN, TypeAllowed> { }

    [Serializable]
    public class EntityGroupAllowedDN : EmbeddedEntity, IEquatable<EntityGroupAllowedDN>
    {
        public static readonly EntityGroupAllowedDN CreateCreate = new EntityGroupAllowedDN(TypeAllowed.Create, TypeAllowed.Create);
        public static readonly EntityGroupAllowedDN NoneNone = new EntityGroupAllowedDN(TypeAllowed.None, TypeAllowed.None);

        private EntityGroupAllowedDN() { }

        public EntityGroupAllowedDN(TypeAllowed inGroup, TypeAllowed outGroup)
        {
            this.inGroup = inGroup;
            this.outGroup = outGroup;
        }

        TypeAllowed inGroup;
        public TypeAllowed InGroup
        {
            get { return inGroup; }
        }

        TypeAllowed outGroup;
        public TypeAllowed OutGroup
        {
            get { return outGroup; }
        }

        public override bool Equals(object obj)
        {
            if (obj is EntityGroupAllowedDN)
                return Equals((EntityGroupAllowedDN)obj);

            return false;
        }

        public bool Equals(EntityGroupAllowedDN other)
        {
            return this == other || this.InGroup == other.InGroup && this.OutGroup == other.OutGroup;
        }

        public override int GetHashCode()
        {
            return inGroup.GetHashCode() ^ OutGroup.GetHashCode() << 5;
        }

        public override string ToString()
        {
            return "[In = {0}, Out = {1}]".Formato(inGroup, outGroup);
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