using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.SharedPart, EntityData.Master)]
    public class QueryHelpEntity : Entity
    {
        [NotNullable]
        QueryEntity query;
        [NotNullValidator]
        public QueryEntity Query
        {
            get { return query; }
            set { SetToStr(ref query, value); }
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
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = int.MaxValue)]
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
        }

        [NotNullable, PreserveOrder]
        MList<QueryColumnHelpEntity> columns = new MList<QueryColumnHelpEntity>();
        [NotNullValidator, NoRepeatValidator]
        public MList<QueryColumnHelpEntity> Columns
        {
            get { return columns; }
            set { Set(ref columns, value); }
        }

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Description) && Columns.IsEmpty(); }
        }

        protected override string PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Is(() => IsEmpty) && IsEmpty)
                return "IsEmpty is true";

            return base.PropertyValidation(pi);
        }

        static Expression<Func<QueryHelpEntity, string>> ToStringExpression = e => e.Query.ToString();
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class QueryColumnHelpEntity : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        string columnName;
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string ColumnName
        {
            get { return columnName; }
            set { Set(ref columnName, value); }
        }

        [SqlDbType(Size = int.MaxValue)]
        string description;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = int.MaxValue)]
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
        }

        public override string ToString()
        {
            return ColumnName;
        }
    }

    public static class QueryHelpOperation
    {
        public static readonly ExecuteSymbol<QueryHelpEntity> Save = OperationSymbol.Execute<QueryHelpEntity>();
    }
}
