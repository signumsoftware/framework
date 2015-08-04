using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EntityHelpEntity : Entity
    {
        [NotNullable]
        TypeEntity type;
        [NotNullValidator]
        public TypeEntity Type
        {
            get { return type; }
            set { SetToStr(ref type, value); }
        }

        [NotNullable]
        CultureInfoEntity culture;
        [NotNullValidator]
        public CultureInfoEntity Culture
        {
            get { return culture; }
            set { Set(ref culture, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string description;
        [StringLengthValidator(AllowNulls = true, Min = 3, MultiLine = true)]
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
        }

        [NotNullable]
        MList<PropertyRouteHelpEntity> properties = new MList<PropertyRouteHelpEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<PropertyRouteHelpEntity> Properties
        {
            get { return properties; }
            set { Set(ref properties, value); }
        }

        [Ignore]
        MList<OperationHelpEntity> operations = new MList<OperationHelpEntity>();
        public MList<OperationHelpEntity> Operations
        {
            get { return operations; }
            set { Set(ref operations, value); }
        }

        [Ignore]
        MList<QueryHelpEntity> queries = new MList<QueryHelpEntity>();
        public MList<QueryHelpEntity> Queries
        {
            get { return queries; }
            set { Set(ref queries, value); }
        }

        static Expression<Func<EntityHelpEntity, string>> ToStringExpression = e => e.Type.ToString();
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Description) && Properties.IsEmpty() && Operations.IsEmpty(); }
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => IsEmpty) && IsEmpty)
                return "IsEmpty is true";

            return base.PropertyValidation(pi);
        }
    }

    public static class EntityHelpOperation
    {
        public static readonly ExecuteSymbol<EntityHelpEntity> Save = OperationSymbol.Execute<EntityHelpEntity>();
    }

    [Serializable]
    public class PropertyRouteHelpEntity : EmbeddedEntity
    {
        [NotNullable]
        PropertyRouteEntity property;
        [NotNullValidator]
        public PropertyRouteEntity Property
        {
            get { return property; }
            set { Set(ref property, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string description;
        [StringLengthValidator(AllowNulls = false, Min = 3, MultiLine = true)]
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
        }

        public override string ToString()
        {
            return this.Property.TryToString();
        }
    }


}
