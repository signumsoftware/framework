using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Entities.Basics;
using System.Text.RegularExpressions;
using System.Reflection;
using Signum.Entities;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master)]
    public abstract class RuleEntity<R, A> : Entity
    {
        [NotNullValidator]
        public Lite<RoleEntity> Role { get; set; }

        [NotNullValidator]
        public R Resource { get; set; }

        public A Allowed { get; set; }

        public override string ToString()
        {
            return "{0} for {1} <- {2}".FormatWith(Resource, Role, Allowed);
        }

        protected override void PreSaving(PreSavingContext ctx)
        {
            this.toStr = this.ToString();
        }
    }

    [Serializable]
    public class RuleQueryEntity : RuleEntity<QueryEntity, QueryAllowed> { }

    [Serializable]
    public class RulePermissionEntity : RuleEntity<PermissionSymbol, bool> { }

    [Serializable]
    public class RuleOperationEntity : RuleEntity<OperationTypeEmbedded, OperationAllowed> { }


    [Serializable, InTypeScript(Undefined = false)]
    public class OperationTypeEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
        public OperationSymbol Operation { get; set; }

        //[NotNullable] While transition
        [NotNullValidator]
        public TypeEntity Type { get; set; }

        public override string ToString()
        {
            return $"{Operation}/{Type}";
        }
    }

    [Serializable]
    public class RulePropertyEntity : RuleEntity<PropertyRouteEntity, PropertyAllowed> { }

    [Serializable]
    public class RuleTypeEntity : RuleEntity<TypeEntity, TypeAllowed>
    {
        [NotNullValidator, PreserveOrder]
        public MList<RuleTypeConditionEmbedded> Conditions { get; set; } = new MList<RuleTypeConditionEmbedded>();
    }

    [Serializable]
    public class RuleTypeConditionEmbedded : EmbeddedEntity, IEquatable<RuleTypeConditionEmbedded>
    {
        [NotNullValidator]
        public TypeConditionSymbol Condition { get; set; }

        public TypeAllowed Allowed { get; set; }

        public bool Equals(RuleTypeConditionEmbedded other)
        {
            return this.Condition.Equals(other.Condition)
                && this.Allowed == other.Allowed;
        }

        public override string ToString()
        {
            return "{0} ({1})".FormatWith(Condition, Allowed);
        }
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum QueryAllowed
    {
        None = 0,
        EmbeddedOnly = 1,
        Allow = 2,
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
            }.NotNull().OrderByDescending(a => a).ToArray();

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

            return "{0},{1}".FormatWith(db, ui);
        }

        public static PropertyAllowed ToPropertyAllowed(this TypeAllowedBasic ta)
        {
            PropertyAllowed pa =
                ta == TypeAllowedBasic.None ? PropertyAllowed.None :
                ta == TypeAllowedBasic.Read ? PropertyAllowed.Read : PropertyAllowed.Modify;
            return pa;
        }
    }

    [InTypeScript(true)]
    [DescriptionOptions(DescriptionOptions.Members)]
    public enum TypeAllowedBasic
    {
        None = 0,
        Read = 1,
        Modify = 2,
        Create = 3
    }
}