using Signum.Basics;

namespace Signum.Help;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class TypeHelpEntity : Entity, IHelpEntity
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
    public override string ToString() => As.Expression(() => $"{Type}");

    public bool IsEmpty
    {
        get { return string.IsNullOrEmpty(this.Description) && Properties.IsEmpty() && Operations.IsEmpty(); }
    }

    [Ignore]
    public string? Info { get; set; }

    protected override string? PropertyValidation(System.Reflection.PropertyInfo pi)
    {
        if (pi.Name == nameof(IsEmpty) && IsEmpty)
            return "IsEmpty is true";

        return base.PropertyValidation(pi);
    }

    bool IHelpEntity.ForeachHtmlField(Func<string, string> processHtml)
    {
        bool changed = false;
        if (Description != null)
        {
            var newDesc = processHtml(Description);
            if (newDesc != Description)
            {
                changed = true;
                Description = newDesc;
            }
        }
        foreach (var prop in Properties)
        {
            if (prop.Description != null)
            {
                var newDesc = processHtml(prop.Description);
                if (newDesc != prop.Description)
                {
                    changed = true;
                    prop.Description = newDesc;
                }
            }
        }
        foreach (var oper in Operations)
        {
            if (oper.Description != null)
            {
                var newDesc = processHtml(oper.Description);
                if (newDesc != oper.Description)
                {
                    changed = true;
                    oper.Description = newDesc;
                }
            }
        }
        return changed;
    }
}

[AutoInit]
public static class TypeHelpOperation
{
    public static ExecuteSymbol<TypeHelpEntity> Save;
    public static DeleteSymbol<TypeHelpEntity> Delete;
}

public class PropertyRouteHelpEmbedded : EmbeddedEntity
{
    public PropertyRouteEntity Property { get; set; }

    [Ignore]
    public string? Info { get; set; }

    [StringLengthValidator(MultiLine = true), ForceNotNullable]
    public string? Description { get; set; }

    public override string ToString()
    {
        return this.Property?.ToString() ?? "";
    }
}

public class OperationHelpEmbedded : EmbeddedEntity
{
    public OperationSymbol Operation { get; set; }

    [Ignore]
    public string? Info { get; set; }

    [StringLengthValidator(MultiLine = true), ForceNotNullable]
    public string? Description { get; set; }

    public override string ToString()
    {
        return this.Operation?.ToString() ?? "";
    }
}

