using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EntityHelpEntity : Entity
    {
        [NotNullValidator]
        public TypeEntity Type { get; set; }

        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

		[StringLengthValidator(AllowNulls = true, Min = 3, MultiLine = true)]
        public string Description { get; set; }

        [NotNullValidator, NoRepeatValidator]
        public MList<PropertyRouteHelpEmbedded> Properties { get; set; } = new MList<PropertyRouteHelpEmbedded>();

        [NotNullValidator, NoRepeatValidator]
        public MList<OperationHelpEmbedded> Operations { get; set; } = new MList<OperationHelpEmbedded>();

        [Ignore]
        public MList<QueryHelpEntity> Queries { get; set; } = new MList<QueryHelpEntity>();

        static Expression<Func<EntityHelpEntity, string>> ToStringExpression = e => e.Type.ToString();
        [ExpressionField]
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
            if (pi.Name == nameof(IsEmpty) && IsEmpty)
                return "IsEmpty is true";

            return base.PropertyValidation(pi);
        }
    }

    [AutoInit]
    public static class EntityHelpOperation
    {
        public static ExecuteSymbol<EntityHelpEntity> Save;
    }

    [Serializable]
    public class PropertyRouteHelpEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
        public PropertyRouteEntity Property { get; set; }

		[StringLengthValidator(AllowNulls = false, Min = 3, MultiLine = true)]
        public string Description { get; set; }

        public override string ToString()
        {
            return this.Property?.ToString();
        }
    }

    [Serializable]
    public class OperationHelpEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
        public OperationSymbol Operation { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 3, MultiLine = true)]
        public string Description { get; set; }

        public override string ToString()
        {
            return this.Operation?.ToString();
        }
    }

}
