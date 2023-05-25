using System.Collections.ObjectModel;

namespace Signum.DynamicQuery;

public abstract class Meta
{
    public readonly Implementations? Implementations;

    public abstract string? IsAllowed();

    protected Meta(Implementations? implementations)
    {
        this.Implementations = implementations;
    }
}

public class CleanMeta : Meta
{
    public readonly PropertyRoute[] PropertyRoutes;

    public CleanMeta(Implementations? implementations, params PropertyRoute[] propertyRoutes)
        : base(implementations)
    {
        this.PropertyRoutes = propertyRoutes;
    }

    public override string? IsAllowed()
    {
        var result = PropertyRoutes.Select(a => a.IsAllowed()).NotNull();
        if (result.IsEmpty())
            return null;

        return result.CommaAnd();
    }

    public override string ToString()
    {
        return "CleanMeta({0})".FormatWith(PropertyRoutes.ToString(", "));
    }

}

public class DirtyMeta : Meta
{
    public readonly ReadOnlyCollection<CleanMeta> CleanMetas;

    public DirtyMeta(Implementations? implementations, Meta[] properties)
        : base(implementations)
    {
        CleanMetas = properties.OfType<CleanMeta>().Concat(
            properties.OfType<DirtyMeta>().SelectMany(d => d.CleanMetas))
            .ToReadOnly();
    }

    public override string? IsAllowed()
    {
        var result = CleanMetas.Select(a => a.IsAllowed()).NotNull();
        if (result.IsEmpty())
            return null;

        return result.CommaAnd();
    }

    public override string ToString()
    {
        return "DirtyMeta({0})".FormatWith(CleanMetas.ToString(", "));
    }
}
