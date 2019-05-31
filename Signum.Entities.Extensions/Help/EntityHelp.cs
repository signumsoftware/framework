using System;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class EntityHelpEntity : Entity
    {
        
        public TypeEntity Type { get; set; }

        
        public CultureInfoEntity Culture { get; set; }

		[StringLengthValidator(Min = 3, MultiLine = true)]
        public string? Description { get; set; }

        [NoRepeatValidator]
        public MList<PropertyRouteHelpEmbedded> Properties { get; set; } = new MList<PropertyRouteHelpEmbedded>();

        [NoRepeatValidator]
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

        protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
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
        public PropertyRouteEntity Property { get; set; }

		[StringLengthValidator(Min = 3, MultiLine = true)]
        public string? Description { get; set; }

        public override string ToString()
        {
            return this.Property?.ToString() ?? "";
        }
    }

    [Serializable]
    public class OperationHelpEmbedded : EmbeddedEntity
    {
        public OperationSymbol Operation { get; set; }

        [StringLengthValidator(Min = 3, MultiLine = true)]
        public string? Description { get; set; }

        public override string ToString()
        {
            return this.Operation?.ToString() ?? "";
        }
    }

}
