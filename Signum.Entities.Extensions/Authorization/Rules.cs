using System;
using System.Linq;
using Signum.Utilities;
using Signum.Entities.Basics;

namespace Signum.Entities.Authorization
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Master)]
    public abstract class RuleEntity<R, A> : Entity
    {   
        public Lite<RoleEntity> Role { get; set; }

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
        public OperationSymbol Operation { get; set; }
        
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
        [PreserveOrder]
        public MList<RuleTypeConditionEmbedded> Conditions { get; set; } = new MList<RuleTypeConditionEmbedded>();
    }

    [Serializable]
    public class RuleTypeConditionEmbedded : EmbeddedEntity, IEquatable<RuleTypeConditionEmbedded>
    {
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

    [DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
    public enum QueryAllowed
    {
        None = 0,
        EmbeddedOnly = 1,
        Allow = 2,
    }

    [DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
    public enum OperationAllowed
    {
        None = 0,
        DBOnly = 1,
        Allow = 2,
    }

    [DescriptionOptions(DescriptionOptions.Members), InTypeScript(true)]
    public enum PropertyAllowed
    {
        None,
        Read,
        Write,
    }

    [DescriptionOptions(DescriptionOptions.Members)]
    public enum TypeAllowed
    {
        None =             TypeAllowedBasic.None << 2 | TypeAllowedBasic.None,

        DBReadUINone =     TypeAllowedBasic.Read << 2 | TypeAllowedBasic.None,
        Read =             TypeAllowedBasic.Read << 2 | TypeAllowedBasic.Read,

        DBWriteUINone =   TypeAllowedBasic.Write << 2 | TypeAllowedBasic.None,
        DBWriteUIRead =   TypeAllowedBasic.Write << 2 | TypeAllowedBasic.Read,
        Write =           TypeAllowedBasic.Write << 2 | TypeAllowedBasic.Write
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
                ta == TypeAllowedBasic.Read ? PropertyAllowed.Read : PropertyAllowed.Write;
            return pa;
        }
    }

    [InTypeScript(true)]
    [DescriptionOptions(DescriptionOptions.Members)]
    public enum TypeAllowedBasic
    {
        None = 0,
        Read = 1,
        Write = 2,
    }
}
