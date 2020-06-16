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

        [Ignore]
        public string Info { get; set; }

        [StringLengthValidator(MultiLine = true)]
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

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Query.ToString());
    }

    [Serializable]
    public class QueryColumnHelpEmbedded : EmbeddedEntity
    {
        [StringLengthValidator(Max = 100)]
        public string ColumnName { get; set; }

        [StringLengthValidator(MultiLine = true)]
        public string? Description { get; set; }

        [Ignore]
        public string NiceName { get; set; }

        [Ignore]
        public string Info { get; set; }

        public override string ToString()
        {
            return ColumnName;
        }
    }

    [AutoInit]
    public static class QueryHelpOperation
    {
        public static ExecuteSymbol<QueryHelpEntity> Save;
        public static DeleteSymbol<QueryHelpEntity> Delete;
    }
}
