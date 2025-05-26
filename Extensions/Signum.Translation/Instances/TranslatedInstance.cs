using Signum.Basics;

namespace Signum.Translation.Instances;

[EntityKind(EntityKind.System, EntityData.Master)]
public class TranslatedInstanceEntity : Entity
{
    public CultureInfoEntity Culture { get; set; }

    [ImplementedByAll]
    public Lite<Entity> Instance { get; set; }

    public PropertyRouteEntity PropertyRoute { get; set; }

    public string? RowId { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string TranslatedText { get; set; }

    [StringLengthValidator(MultiLine = true)]
    public string OriginalText { get; set; }

    public override string ToString()
    {
        return "{0} {1} {2}".FormatWith(Culture, Instance, PropertyRoute);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(RowId) && PropertyRoute != null)
        {
            if (RowId == null && PropertyRoute.Path.Contains("/"))
                return "{0} should be set for route {1}".FormatWith(pi.NiceName(), PropertyRoute);

            if (RowId != null && !PropertyRoute.Path.Contains("/"))
                return "{0} should be null for route {1}".FormatWith(pi.NiceName(), PropertyRoute);
        }

        return null;
    }
}



[InTypeScript(true)]
public enum TranslatedSummaryState
{
    Completed,
    Pending,
    None,
}


[AutoInit]
public static class TranslatedInstanceOperation
{
    public static DeleteSymbol<TranslatedInstanceEntity> Delete;
}
