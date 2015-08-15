using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.Basics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.SharedPart, EntityData.Master)]
    public class QueryHelpEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public QueryEntity Query { get; set; }

        [NotNullable]
        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = true, Min = 3, MultiLine = true)]
        public string Description { get; set; }

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<QueryColumnHelpEntity> Columns { get; set; } = new MList<QueryColumnHelpEntity>();

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Description) && Columns.IsEmpty(); }
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
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
    public class QueryColumnHelpEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ColumnName { get; set; }

        [SqlDbType(Size = int.MaxValue)]
        [StringLengthValidator(AllowNulls = true, Min = 3, MultiLine = true)]
        public string Description { get; set; }

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
