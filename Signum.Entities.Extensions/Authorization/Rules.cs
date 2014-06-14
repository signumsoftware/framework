using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Text.RegularExpressions;
using System.Reflection;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master)]
    public class RuleDN<R, A> : Entity
        where R: IdentifiableEntity
    {
        Lite<RoleDN> role;
        [NotNullValidator]
        public Lite<RoleDN> Role
        {
            get { return role; }
            set { Set(ref role, value); }
        }

        [NotNullable]
        R resource;
        [NotNullValidator]
        public R Resource
        {
            get { return resource; }
            set { Set(ref resource, value); }
        }

        A allowed;
        public A Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value); }
        }

        public override string ToString()
        {
            return "{0} for {1} <- {2}".Formato(resource, role, allowed);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            this.toStr = this.ToString();
        }
    }

    [Serializable]
    public class RuleQueryDN : RuleDN<QueryDN, bool> { }

    [Serializable]
    public class RulePermissionDN : RuleDN<PermissionSymbol, bool> { }

    [Serializable]
    public class RuleOperationDN : RuleDN<OperationSymbol, OperationAllowed> { }

    [Serializable]
    public class RulePropertyDN : RuleDN<PropertyRouteDN, PropertyAllowed> { }
   
    [Serializable]
    public class RuleTypeDN : RuleDN<TypeDN, TypeAllowed> 
    {
        [ValidateChildProperty, NotNullable, PreserveOrder]
        MList<RuleTypeConditionDN> conditions = new MList<RuleTypeConditionDN>();
        public MList<RuleTypeConditionDN> Conditions
        {
            get { return conditions; }
            set { Set(ref conditions, value); }
        }
    }

    [Serializable]
    public class RuleTypeConditionDN : EmbeddedEntity, IEquatable<RuleTypeConditionDN> 
    {
        TypeConditionSymbol condition;
        [NotNullValidator]
        public TypeConditionSymbol Condition
        {
            get { return condition; }
            set { Set(ref condition, value); }
        }

        TypeAllowed allowed;
        public TypeAllowed Allowed
        {
            get { return allowed; }
            set { Set(ref allowed, value); }
        }

        public bool Equals(RuleTypeConditionDN other)
        {
            return this.condition.Equals(other.condition) 
                && this.allowed == other.allowed; 
        }

        public override string ToString()
        {
            return "{0} ({1})".Formato(condition, allowed);
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum OperationAllowed
    {
        None = 0, 
        DBOnly = 1,
        Allow = 2,
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum PropertyAllowed
    {
        None,
        Read,
        Modify,
    }

    [DescriptionOptions(DescriptionOptions.Members)]
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

        public static PropertyAllowed ToPropertyAllowed(this TypeAllowedBasic ta)
        {
            PropertyAllowed pa =
                ta == TypeAllowedBasic.None ? PropertyAllowed.None :
                ta == TypeAllowedBasic.Read ? PropertyAllowed.Read : PropertyAllowed.Modify;
            return pa;
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum TypeAllowedBasic
    {
        None = 0,
        Read = 1,
        Modify = 2 ,
        Create = 3
    }
}