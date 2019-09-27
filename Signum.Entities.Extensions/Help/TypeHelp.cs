using System;
using System.Linq.Expressions;
using Signum.Entities.Basics;
using Signum.Utilities;

namespace Signum.Entities.Help
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class TypeHelpEntity : Entity
    {   
        public TypeEntity Type { get; set; }

        public CultureInfoEntity Culture { get; set; }

		[StringLengthValidator(MultiLine = true)]
        public string? Description { get; set; }

        [NoRepeatValidator]
        public MList<PropertyRouteHelpEmbedded> Properties { get; set; } = new MList<PropertyRouteHelpEmbedded>();

        [NoRepeatValidator]
        public MList<OperationHelpEmbedded> Operations { get; set; } = new MList<OperationHelpEmbedded>();

        [Ignore]
        public MList<QueryHelpEntity> Queries { get; set; } = new MList<QueryHelpEntity>();

        [AutoExpressionField]
        public override string ToString() => As.Expression(() => Type.ToString());

        public bool IsEmpty
        {
            get { return string.IsNullOrEmpty(this.Description) && Properties.IsEmpty() && Operations.IsEmpty(); }
        }

        [Ignore]
        public string Info { get; set; }

        protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
        {
            if (pi.Name == nameof(IsEmpty) && IsEmpty)
                return "IsEmpty is true";

            return base.PropertyValidation(pi);
        }
    }

    [AutoInit]
    public static class TypeHelpOperation
    {
        public static ExecuteSymbol<TypeHelpEntity> Save;
        public static DeleteSymbol<TypeHelpEntity> Delete;
    }

    [Serializable]
    public class PropertyRouteHelpEmbedded : EmbeddedEntity
    {
        public PropertyRouteEntity Property { get; set; }

        [Ignore]
        public string Info { get; set; }

        [StringLengthValidator(MultiLine = true), ForceNotNullable]
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

        [Ignore]
        public string Info { get; set; }

        [StringLengthValidator(MultiLine = true), ForceNotNullable]
        public string? Description { get; set; }

        public override string ToString()
        {
            return this.Operation?.ToString() ?? "";
        }
    }

}
