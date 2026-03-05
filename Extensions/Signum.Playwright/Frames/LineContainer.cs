using Microsoft.Playwright;

namespace Signum.Playwright.Frames;

public interface ILineContainer<T> : ILineContainer where T : IModifiableEntity
{
}

public interface ILineContainer
{
    ILocator Element { get; }
    PropertyRoute Route { get; }
    IPage Page { get; }
}

public interface IEntityButtonContainer<T> where T : ModifiableEntity
{
    ILocator Element { get; }
    IPage Page { get; }
}

public interface IWidgetContainer
{
    ILocator Element { get; }
    IPage Page { get; }
}

public interface IValidationSummaryContainer
{
    ILocator Element { get; }
    IPage Page { get; }
}

public class LineContainer<T> : ILineContainer<T> where T : IModifiableEntity
{
    public ILocator Element { get; }
    public PropertyRoute Route { get; }
    public IPage Page { get; }

    public LineContainer(ILocator element, PropertyRoute route, IPage page)
    {
        Element = element;
        Route = route;
        Page = page;
    }

    public LineContainer(ILocator element, IPage page)
        : this(element, PropertyRoute.Root(typeof(T)), page)
    {
    }
}
