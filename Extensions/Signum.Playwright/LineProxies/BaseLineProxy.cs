using Signum.Entities.Reflection;
namespace Signum.Playwright.LineProxies;

public abstract class BaseLineProxy
{
    public ILocator Element { get; }
    public PropertyRoute Route { get; }
    public IPage Page => Element.Page;

    protected BaseLineProxy(ILocator element, PropertyRoute route)
    {
        Element = element;
        Route = route;
    }

    public abstract Task SetValueUntypedAsync(object? value);
    public abstract Task<object?> GetValueUntypedAsync();
    public abstract Task<bool> IsReadonlyAsync();

    public static BaseLineProxy AutoLine(ILocator element, PropertyRoute route)
    {
        var type = route.Type.UnNullify();
        var imp = route.TryGetImplementations();

        if (type.ElementType() != null && type != typeof(string))
        {
            if (imp != null)
            {
                if (imp.Value.IsByAll)
                    return new EntityStripProxy(element, route);

                if (imp.Value.Types.Count() == 1 && !type.IsLite() &&
                    (type.IsModelEntity() ||
                     EntityKindCache.GetEntityKind(imp.Value.Types.SingleEx())
                        is EntityKind.Part or EntityKind.SharedPart))
                    return new EntityTableProxy(element, route);

                if (imp.Value.Types.All(t =>
                        EntityKindCache.GetEntityKind(t)
                        is EntityKind.Part or EntityKind.SharedPart))
                    return new EntityRepeaterProxy(element, route);

                if (imp.Value.Types.All(t =>
                        EntityKindCache.IsLowPopulation(t)))
                    return new EntityListCheckBoxProxy(element, route);

                return new EntityStripProxy(element, route);
            }

            if (type.IsEmbeddedEntity())
                return new EntityTableProxy(element, route);

            if (type.IsEnum)
                throw new InvalidOperationException("EnumCheckBoxListProxy not implemented");

            throw new InvalidOperationException("MultiValueLineProxy not implemented");
        }
        else
        {
            if (type == typeof(PrimaryKey))
                type = PrimaryKey.Type(route.RootType);

            if (imp != null)
            {
                if (imp.Value.IsByAll)
                    return new EntityLineProxy(element, route);

                if (imp.Value.Types.All(t =>
                        EntityKindCache.GetEntityKind(t)
                        is EntityKind.Part or EntityKind.SharedPart))
                    return new EntityDetailProxy(element, route);

                if (imp.Value.Types.All(t =>
                        EntityKindCache.IsLowPopulation(t)))
                    return new EntityComboProxy(element, route);

                return new EntityLineProxy(element, route);
            }

            if (type.IsEntity())
                return new EntityLineProxy(element, route);

            if (type.IsEmbeddedEntity())
                return new EntityDetailProxy(element, route);

            if (type.IsEnum)
                return new EnumLineProxy(element, route);

            if (type == typeof(bool))
                return route.Type.IsNullable()
                    ? new EnumLineProxy(element, route)
                    : new CheckboxLineProxy(element, route);

            if (type == typeof(DateTime) ||
                type == typeof(DateOnly) ||
                type == typeof(DateTimeOffset))
                return new DateTimeLineProxy(element, route);

            if (type == typeof(string))
            {
                var multiLine =
                    Validator.TryGetPropertyValidator(route)?
                        .Validators.Any(a =>
                            a is StringLengthValidatorAttribute slv &&
                            slv.MultiLine);

                if (multiLine == true)
                    return new TextAreaLineProxy(element, route);

                if (Reflector.GetFormatString(route) == "Color")
                    return new ColorBoxLineProxy(element, route);

                return new TextBoxLineProxy(element, route);
            }

            if (type == typeof(int) || type == typeof(decimal))
                return new NumberLineProxy(element, route);

            if (type == typeof(Guid))
                return new GuidBoxLineProxy(element, route);

            if (type == typeof(TimeSpan) || type == typeof(TimeOnly))
                return new TimeLineProxy(element, route);

            throw new UnexpectedValueException(type);
        }
    }
}
