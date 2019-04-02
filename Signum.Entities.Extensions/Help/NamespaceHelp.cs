using System;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class NamespaceHelpEntity : Entity
    {
        [StringLengthValidator(Min = 3, Max = 300)]
        public string Name { get; set; }
        
        public CultureInfoEntity Culture { get; set; }

        [StringLengthValidator(Max = 200)]
        public string? Title { get; set; }

		[StringLengthValidator(Min = 3, MultiLine = true)]
        public string? Description { get; set; }

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
