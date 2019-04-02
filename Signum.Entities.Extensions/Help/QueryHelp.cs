using System;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.SharedPart, EntityData.Master)]
    public class QueryHelpEntity : Entity
    {
        
        public QueryEntity Query { get; set; }

        
        public CultureInfoEntity Culture { get; set; }

        [StringLengthValidator(Min = 3, MultiLine = true)]
        public string? Description { get; set; }

        [PreserveOrder]
        [NoRepeatValidator]
        public MList<QueryColumnHelpEmbedded> Columns { get; set; } = new MList<QueryColumnHelpEmbedded>();

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Description) && Columns.IsEmpty(); }
        }

        protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Name == nameof(IsEmpty) && IsEmpty)
                return "IsEmpty is true";

            return base.PropertyValidation(pi);
        }

        static Expression<Func<QueryHelpEntity, string>> ToStringExpression = e => e.Query.ToString();
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class QueryColumnHelpEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Min = 3, Max = 100)]
        public string ColumnName { get; set; }

        [StringLengthValidator(Min = 3, MultiLine = true)]
        public string? Description { get; set; }

        public override string ToString()
        {
            return ColumnName;
        }
    }

    [AutoInit]
    public static class QueryHelpOperation
    {
        public static ExecuteSymbol<QueryHelpEntity> Save;
    }
}
