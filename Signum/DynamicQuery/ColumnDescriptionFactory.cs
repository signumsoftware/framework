using Signum.Engine.Maps;

namespace Signum.DynamicQuery;

public class ColumnDescriptionFactory
{
    readonly internal Meta? Meta;
    public Func<string>? OverrideDisplayName { get; set; }
    public Func<string?>? OverrideIsAllowed { get; set; }

    public string Name { get; internal set; }
    public Type Type { get; internal set; }

    public string? Format { get; set; }
    public string? Unit { get; set; }
    Implementations? implementations;
    public Implementations? Implementations
    {
        get { return implementations; }

        set
        {
            if (value != null && !value.Value.IsByAll)
            {
                var ct = Type.CleanType();
                string errors = value.Value.Types.Where(t => !ct.IsAssignableFrom(t)).ToString(a => a.Name, ", ");

                if (errors.Any())
                    throw new InvalidOperationException("Column {0} Implenentations should be assignable to {1}: {2}".FormatWith(Name, ct.Name, errors));
            }

            implementations = value;
        }
    }

    PropertyRoute[]? propertyRoutes;
    public PropertyRoute[]? PropertyRoutes
    {
        get { return propertyRoutes; }
        set
        {
            propertyRoutes = value;

            if (propertyRoutes != null && propertyRoutes.Any() /*Out of IB casting*/)
            {
                Format = GetFormat(propertyRoutes);
                Unit = GetUnit(propertyRoutes);
                if (Implementations == null)
                    Implementations = propertyRoutes.FirstEx().TryGetImplementations();
                processedType = null;
            }
        }
    }



    internal static string? GetUnit(PropertyRoute[] routes)
    {
        switch (routes[0].PropertyRouteType)
        {
            case PropertyRouteType.LiteEntity:
            case PropertyRouteType.Root:
                return null;
            case PropertyRouteType.FieldOrProperty:
                return routes.Select(pr => GetUnit(pr)).Distinct().Only();
            case PropertyRouteType.MListItems:
                return null;
        }

        throw new InvalidOperationException();
    }

    private static string? GetUnit(PropertyRoute pr)
    {
        return Schema.Current.Settings.FieldAttribute<UnitAttribute>(pr)?.UnitName;
    }

    internal static string? GetFormat(PropertyRoute[] routes)
    {
        switch (routes[0].PropertyRouteType)
        {
            case PropertyRouteType.LiteEntity:
            case PropertyRouteType.Root:
                return null;
            case PropertyRouteType.FieldOrProperty:
                return routes.Select(pr => Reflector.FormatString(pr)).Distinct().Only();
            case PropertyRouteType.MListItems:
                return Reflector.FormatString(routes[0].Type);
        }

        throw new InvalidOperationException();
    }

    public ColumnDescriptionFactory(int index, MemberInfo mi, Meta? meta)
    {
        Name = mi.Name;

        Type = mi.ReturningType();
        Meta = meta;

        //if (Type.IsIEntity())
        //    throw new InvalidOperationException("The Type of column {0} is a subtype of IEntity, use a Lite instead".FormatWith(mi.MemberName()));

        if (IsEntity && !Type.CleanType().IsIEntity())
            throw new InvalidOperationException("Entity must be a Lite or an IEntity");

        if (meta != null)
        {
            Implementations = meta.Implementations;

            if (meta is CleanMeta cm)
                PropertyRoutes = cm.PropertyRoutes;
        }
    }

    public string DisplayName()
    {
        if (OverrideDisplayName != null)
            return OverrideDisplayName();

        if (IsEntity)
            return this.Type.CleanType().NiceName();

        if (propertyRoutes != null &&
            propertyRoutes[0].PropertyRouteType == PropertyRouteType.FieldOrProperty &&
            propertyRoutes[0].PropertyInfo!.Name == Name)
        {
            var result = propertyRoutes.Select(pr => pr.PropertyInfo!.NiceName()).Only();
            if (result != null)
                return result;
        }

        return Name.SpacePascalOrUnderscores();
    }

    public void SetPropertyRoutes<T>(params Expression<Func<T, object>>[] propertyRoutes)
        where T : Entity
    {
        PropertyRoutes = propertyRoutes.Select(exp => PropertyRoute.Construct(exp)).ToArray();
    }

    public bool IsEntity
    {
        get { return this.Name == ColumnDescription.Entity; }
    }

    public string? IsAllowed()
    {
        if (OverrideIsAllowed != null)
            return OverrideIsAllowed();

        if (propertyRoutes != null)
        {
            var result = propertyRoutes.Select(a => a.IsAllowed()).NotNull();
            if (result.IsEmpty())
                return null;

            return result.CommaAnd();
        }

        if (Meta != null)
            return Meta.IsAllowed();

        return null;
    }

    Type? processedType;
    Type ProcessedType
    {
        get
        {
            return processedType ?? (processedType =
                (Reflector.IsIEntity(Type) ? Lite.Generate(Type) :
                Type.UnNullify() == typeof(PrimaryKey) ? UnwrapFromPropertRoutes().Nullify() :
                Type.Nullify()));
        }
    }

    private Type UnwrapFromPropertRoutes()
    {
        if (propertyRoutes == null || propertyRoutes.Length == 0)
            throw new InvalidOperationException($"Impossible to determine the underlying type of the PrimaryKey of column {this.Name} if PropertyRoutes is not set");

        return propertyRoutes.Select(a => PrimaryKey.Type(a.RootType)).Distinct().SingleEx();
    }

    public ColumnDescription BuildColumnDescription()
    {
        return new ColumnDescription(Name, ProcessedType, DisplayName())
        {
            PropertyRoutes = propertyRoutes,
            Implementations = Implementations,

            Format = Format,
            Unit = Unit,
        };
    }
}
