using Microsoft.Playwright;
using Signum.Entities.Reflection;
using Signum.Playwright.LineProxies;

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

public class LineLocator<T>
{
    public LineLocator(ILocator elementLocator, PropertyRoute route, IPage page)
    {
        ElementLocator = elementLocator;
        Route = route;
        Page = page;
    }

    public ILocator ElementLocator { get; set; }
    public PropertyRoute Route { get; set; }
    public IPage Page { get; set; }
}

public class LineContainer<T> : ILineContainer<T> where T : IModifiableEntity
{
    public ILocator Element { get; private set; }
    public PropertyRoute Route { get; private set; }
    public IPage Page { get; private set; }

    public LineContainer(ILocator element, IPage page, PropertyRoute? route = null)
    {
        Element = element;
        Route = route ?? PropertyRoute.Root(typeof(T));
        Page = page;
    }

    public LineContainer<S> As<S>() where S : T
    {
        return new LineContainer<S>(Element, Page, PropertyRoute.Root(typeof(S)));
    }
}

public static class LineContainerExtensions
{
    public static LineLocator<S> LineLocator<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        PropertyRoute route = lineContainer.Route ?? PropertyRoute.Root(typeof(T));
        var element = lineContainer.Element;

        foreach (var mi in Reflector.GetMemberList(property))
        {
            if (mi is MethodInfo miInfo && miInfo.IsInstantiationOf(MixinDeclarations.miMixin))
                route = route.Add(miInfo.GetGenericArguments()[0]);
            else
            {
                var newRoute = route.Add(mi);
                if (newRoute.Parent != route && route != lineContainer.Route)
                    element = element.Locator($"[data-property-path='{route.PropertyString()}']");
                route = newRoute;
            }
        }

        var locator = element.Locator($"[data-property-path='{route.PropertyString()}']");
        return new LineLocator<S>(locator, route, lineContainer.Page);
    }

    public static ILocator LineLocatorElement<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        return lineContainer.LineLocator(property).ElementLocator;
    }

    public static async Task<bool> IsVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        return await lineContainer.LineLocatorElement(property).IsVisibleAsync();
    }

    public static async Task<bool> IsPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        try
        {
            return await lineContainer.LineLocatorElement(property).CountAsync() > 0;
        }
        catch
        {
            return false;
        }
    }

    public static async Task WaitVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocatorElement(property).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public static async Task WaitPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocatorElement(property).WaitForAsync();
    }

    public static async Task WaitNoVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocatorElement(property).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }

    public static async Task WaitNoPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocatorElement(property).WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    public static LineContainer<S> SubContainer<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property, ILocator? element = null)
        where T : IModifiableEntity
        where S : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new LineContainer<S>(element ?? lineLocator.ElementLocator, lineContainer.Page, lineLocator.Route);
    }
}
