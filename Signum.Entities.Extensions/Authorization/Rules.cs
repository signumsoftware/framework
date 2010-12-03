using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.Operations;
using System.Text.RegularExpressions;

namespace Signum.Entities.Authorization
{
    [Serializable, AvoidLocalization]
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

    [Serializable, AvoidLocalization]
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

        public bool IsActive
        {
            get { return !this.Equals(CreateCreate); }
        }

        public static EntityGroupAllowedDN Parse(string str)
        {
            Match m = Regex.Match(str, @"^\[In = (?<in>.*?), Out = (?<out>.*?)\]$");

            if (!m.Success)
                throw new FormatException("'{0}' is not a valid {1}".Formato(str, typeof(EntityGroupAllowedDN).Name));

            return new EntityGroupAllowedDN(EnumExtensions.ToEnum<TypeAllowed>(m.Groups["in"].Value), 
                                            EnumExtensions.ToEnum<TypeAllowed>(m.Groups["out"].Value)); 
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
        None =             TypeAllowedBasic.None << 2 | TypeAllowedBasic.None,
        
        DBReadUINone =     TypeAllowedBasic.Read << 2 | TypeAllowedBasic.None,
        Read =             TypeAllowedBasic.Read << 2 | TypeAllowedBasic.Read,

        DBModifyUINone =   TypeAllowedBasic.Modify << 2 | TypeAllowedBasic.None,
        DBModifyUIRead =   TypeAllowedBasic.Modify << 2 | TypeAllowedBasic.Read,
        Modify =           TypeAllowedBasic.Modify << 2 | TypeAllowedBasic.Modify,

        DBCreateUINone =   TypeAllowedBasic.Create << 2 | TypeAllowedBasic.None,
        DBCreateUIRead =   TypeAllowedBasic.Create << 2 | TypeAllowedBasic.Read,
        DBCreateUIModify = TypeAllowedBasic.Create << 2 | TypeAllowedBasic.Modify,
        Create =           TypeAllowedBasic.Create << 2 | TypeAllowedBasic.Create,
    }

    public static class TypeAllowedExtensions
    {
        public static TypeAllowedBasic GetDB(this TypeAllowed allowed)
        {
            return (TypeAllowedBasic)(((int)allowed >> 2) & 0x03);
        }

        public static TypeAllowedBasic GetUI(this TypeAllowed allowed)
        {
            return (TypeAllowedBasic)((int)allowed & 0x03);
        }

        public static TypeAllowedBasic Get(this TypeAllowed allowed, bool userInterface)
        {
            return userInterface ? allowed.GetUI() : allowed.GetDB(); 
        }

        public static TypeAllowed Create(bool create, bool modify, bool read, bool none)
        {
            TypeAllowedBasic[] result = new[]
            {
                create? TypeAllowedBasic.Create: (TypeAllowedBasic?)null, 
                modify? TypeAllowedBasic.Modify: (TypeAllowedBasic?)null, 
                read? TypeAllowedBasic.Read: (TypeAllowedBasic?)null, 
                none? TypeAllowedBasic.None: (TypeAllowedBasic?)null, 
            }.NotNull().OrderByDescending(a=>a).ToArray();

            if (result.Length != 1 && result.Length != 2)
                throw new FormatException();

            return Create(result.Max(), result.Min());
        }

        public static TypeAllowed Create(TypeAllowedBasic database, TypeAllowedBasic ui)
        {
            TypeAllowed result = (TypeAllowed)(((int)database << 2) | (int)ui);
            
            if (!Enum.IsDefined(typeof(TypeAllowed), result))
                throw new FormatException("Invalid TypeAllowed");

            return result;
        }

        public static bool IsActive(this TypeAllowed allowed, TypeAllowedBasic basicAllowed)
        {
            return allowed.GetDB() == basicAllowed || allowed.GetUI() == basicAllowed; 
        }

        public static string ToStringParts(this TypeAllowed allowed)
        {
            TypeAllowedBasic db = allowed.GetDB();
            TypeAllowedBasic ui = allowed.GetUI();

            if (db == ui)
                return db.ToString();

            return "{0},{1}".Formato(db, ui); 
        }
    }

    public enum TypeAllowedBasic
    {
        None = 0,
        Read = 1,
        Modify = 2 ,
        Create = 3
    }
}