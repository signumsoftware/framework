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
    public class EntityHelpDN : Entity
    {
        [NotNullable]
        TypeDN type;
        [NotNullValidator]
        public TypeDN Type
        {
            get { return type; }
            set { SetToStr(ref type, value); }
        }

        [NotNullable]
        CultureInfoDN culture;
        [NotNullValidator]
        public CultureInfoDN Culture
        {
            get { return culture; }
            set { Set(ref culture, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string description;
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
        }

        [NotNullable]
        MList<PropertyRouteHelpDN> properties = new MList<PropertyRouteHelpDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<PropertyRouteHelpDN> Properties
        {
            get { return properties; }
            set { Set(ref properties, value); }
        }

        [Ignore]
        MList<OperationHelpDN> operations = new MList<OperationHelpDN>();
        public MList<OperationHelpDN> Operations
        {
            get { return operations; }
            set { Set(ref operations, value); }
        }

        [Ignore]
        MList<QueryHelpDN> queries = new MList<QueryHelpDN>();
        public MList<QueryHelpDN> Queries
        {
            get { return queries; }
            set { Set(ref queries, value); }
        }

        static Expression<Func<EntityHelpDN, string>> ToStringExpression = e => e.Type.ToString();
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
        public static readonly ExecuteSymbol<EntityHelpDN> Save = OperationSymbol.Execute<EntityHelpDN>();
    }

    [Serializable]
    public class PropertyRouteHelpDN : EmbeddedEntity
    {
        [NotNullable]
        PropertyRouteDN property;
        [NotNullValidator]
        public PropertyRouteDN Property
        {
            get { return property; }
            set { Set(ref property, value); }
        }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        string description;
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
