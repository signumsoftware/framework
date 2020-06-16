using System;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class NamespaceHelpEntity : Entity
    {
        [StringLengthValidator(Max = 300)]
        public string Name { get; set; }
        
        public CultureInfoEntity Culture { get; set; }

        [StringLengthValidator(Max = 200)]
        public string? Title { get; set; }

		[StringLengthValidator(MultiLine = true)]
        public string? Description { get; set; }

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Name);
    }

    [AutoInit]
    public static class NamespaceHelpOperation
    {
        public static ExecuteSymbol<NamespaceHelpEntity> Save;
        public static DeleteSymbol<NamespaceHelpEntity> Delete;
    }


}
