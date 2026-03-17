using Signum.Entities.Reflection;
namespace Signum.Playwright.LineProxies;

public abstract class BaseLineProxy
{
    public ILocator Element { get; }
    public PropertyRoute Route { get; }
    public IPage Page { get; }

    protected BaseLineProxy(ILocator element, PropertyRoute route, IPage page)
    {
        Element = element;
        Route = route;
        Page = page;
    }

    public abstract Task SetValueUntypedAsync(object? value);
    public abstract Task<object?> GetValueUntypedAsync();
    public abstract Task<bool> IsReadonlyAsync();

    public static BaseLineProxy AutoLine(ILocator element, PropertyRoute route, IPage page)
    {
        var type = route.Type.UnNullify();
        var imp = route.TryGetImplementations();

        if (type.ElementType() != null && type != typeof(string))
        {
            if (imp != null)
            {
                if (imp.Value.IsByAll)
                    return new EntityStripProxy(element, route, page);

                if (imp.Value.Types.Count() == 1 && !type.IsLite() &&
                    (type.IsModelEntity() ||
                     EntityKindCache.GetEntityKind(imp.Value.Types.SingleEx())
                        is EntityKind.Part or EntityKind.SharedPart))
                    return new EntityTableProxy(element, route, page);

                if (imp.Value.Types.All(t =>
                        EntityKindCache.GetEntityKind(t)
                        is EntityKind.Part or EntityKind.SharedPart))
                    return new EntityRepeaterProxy(element, route, page);

                if (imp.Value.Types.All(t =>
                        EntityKindCache.IsLowPopulation(t)))
                    return new EntityListCheckBoxProxy(element, route, page);

                return new EntityStripProxy(element, route, page);
            }

            if (type.IsEmbeddedEntity())
                return new EntityTableProxy(element, route, page);

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
                    return new EntityLineProxy(element, route, page);

                if (imp.Value.Types.All(t =>
                        EntityKindCache.GetEntityKind(t)
                        is EntityKind.Part or EntityKind.SharedPart))
                    return new EntityDetailProxy(element, route, page);

                if (imp.Value.Types.All(t =>
                        EntityKindCache.IsLowPopulation(t)))
                    return new EntityComboProxy(element, route, page);

                return new EntityLineProxy(element, route, page);
            }

            if (type.IsEntity())
                return new EntityLineProxy(element, route, page);

            if (type.IsEmbeddedEntity())
                return new EntityDetailProxy(element, route, page);

            if (type.IsEnum)
                return new EnumLineProxy(element, route, page);

            if (type == typeof(bool))
                return route.Type.IsNullable()
                    ? new EnumLineProxy(element, route, page)
                    : new CheckboxLineProxy(element, route, page);

            if (type == typeof(DateTime) ||
                type == typeof(DateOnly) ||
                type == typeof(DateTimeOffset))
                return new DateTimeLineProxy(element, route, page);

            if (type == typeof(string))
            {
                var multiLine =
                    Validator.TryGetPropertyValidator(route)?
                        .Validators.Any(a =>
                            a is StringLengthValidatorAttribute slv &&
                            slv.MultiLine);

                if (multiLine == true)
                    return new TextAreaLineProxy(element, route, page);

                if (Reflector.GetFormatString(route) == "Color")
                    return new ColorBoxLineProxy(element, route, page);

                return new TextBoxLineProxy(element, route, page);
            }

            if (type == typeof(int) || type == typeof(decimal))
                return new NumberLineProxy(element, route, page);

            if (type == typeof(Guid))
                return new GuidBoxLineProxy(element, route, page);

            if (type == typeof(TimeSpan) || type == typeof(TimeOnly))
                return new TimeLineProxy(element, route, page);

            throw new UnexpectedValueException(type);
        }
    }
}
