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
    public class QueryHelpDN : Entity
    {
        [NotNullable]
        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { SetToStr(ref query, value); }
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
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = int.MaxValue)]
        public string Description
        {
            get { return description; }
            set { Set(ref description, value); }
        }

        [NotNullable, PreserveOrder]
        MList<QueryColumnHelpDN> columns = new MList<QueryColumnHelpDN>();
        [NotNullValidator, NoRepeatValidator]
        public MList<QueryColumnHelpDN> Columns
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

        static Expression<Func<QueryHelpDN, string>> ToStringExpression = e => e.Query.ToString();
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class QueryColumnHelpDN : EmbeddedEntity
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
        public static readonly ExecuteSymbol<QueryHelpDN> Save = OperationSymbol.Execute<QueryHelpDN>();
    }
}
