using System;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class NamespaceHelpEntity : Entity
    {
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 300)]
        public string Name { get; set; }

        [NotNullValidator]
        public CultureInfoEntity Culture { get; set; }

                public string Title { get; set; }

		[StringLengthValidator(AllowNulls = true, Min = 3, MultiLine = true)]
        public string Description { get; set; }

        static Expression<Func<NamespaceHelpEntity, string>> ToStringExpression = e => e.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [AutoInit]
    public static class NamespaceHelpOperation
    {
        public static ExecuteSymbol<NamespaceHelpEntity> Save;
    }


}
