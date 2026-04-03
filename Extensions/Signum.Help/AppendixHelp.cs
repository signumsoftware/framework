using Signum.Basics;
using Signum.Utilities.Reflection;

namespace Signum.Help;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class AppendixHelpEntity : Entity, IHelpEntity
{
    [StringLengthValidator(Min = 3, Max = 100)]
    public string UniqueName { get; set; }
    
    public CultureInfoEntity Culture { get; set; }

    [StringLengthValidator(Max = 200)]
    public string Title { get; set; }

    [StringLengthValidator(Min = 3, MultiLine = true)]
    public string? Description { get; set; }

    bool IHelpEntity.ForeachHtmlField(Func<string, string> processHtml)
    {
        bool changed = false;
        if(Description != null)
        {
            var newDesc = processHtml(Description);
            if(newDesc != Description)
            {
                changed = true;
                Description = newDesc;
            }
        }
        return changed;
    }

    public override string ToString()
    {
        return Title;
    }
}

[AutoInit]
public static class AppendixHelpOperation
{
    public static ExecuteSymbol<AppendixHelpEntity> Save;
    public static DeleteSymbol<AppendixHelpEntity> Delete;
}
