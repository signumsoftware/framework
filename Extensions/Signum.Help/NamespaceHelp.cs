using Signum.Basics;

namespace Signum.Help;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class NamespaceHelpEntity : Entity, IHelpEntity
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
        return changed;
    }
}

[AutoInit]
public static class NamespaceHelpOperation
{
    public static ExecuteSymbol<NamespaceHelpEntity> Save;
    public static DeleteSymbol<NamespaceHelpEntity> Delete;
}


