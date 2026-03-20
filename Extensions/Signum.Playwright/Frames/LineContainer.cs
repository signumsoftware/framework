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
    public ILocator ElementLocator { get; set; }
    public PropertyRoute Route { get; set; }
    public IPage Page { get; set; }

    public LineLocator(ILocator elementLocator, PropertyRoute route, IPage page)
    {
        ElementLocator = elementLocator;
        Route = route;
        Page = page;
    }
}

public class LineContainer<T> : ILineContainer<T> where T : IModifiableEntity
{
    public ILocator Element { get; private set; }
    public PropertyRoute Route { get; private set; }
    public IPage Page { get; private set; }

    public LineContainer(ILocator element, IPage page, PropertyRoute? route = null)
    {
        this.Element = element;
        this.Route = route ?? PropertyRoute.Root(typeof(T));
        this.Page = page;
    }

    public LineContainer<S> As<S>() where S : T
    {
        return new LineContainer<S>(this.Element, this.Page, PropertyRoute.Root(typeof(S)));
    }
}

public static class LineContainerExtensions
{
    public static LineLocator<S> LineLocator<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        PropertyRoute route = lineContainer.Route ?? PropertyRoute.Root(typeof(T));

        var locator = lineContainer.Element;

        foreach (var mi in Reflector.GetMemberList(property))
        {
            if (mi is MethodInfo && ((MethodInfo)mi).IsInstantiationOf(MixinDeclarations.miMixin))
            {
                route = route.Add(((MethodInfo)mi).GetGenericArguments()[0]);
            }
            else
            {
                var newRoute = route.Add(mi);

                if (newRoute.Parent != route && route != lineContainer.Route)
                    locator = locator.Locator($"[data-property-path='{route.PropertyString()}']");

                route = newRoute;
            }
        }

        return new LineLocator<S>(
            elementLocator: locator.Locator($"[data-property-path='{route.PropertyString()}']"),
            page: locator.Page,
            route: route
        );
    }

    public static bool IsVisible<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        return lineContainer.LineLocator(property).ElementLocator.IsVisibleAsync().Result;
    }

    public static bool IsPresent<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        return lineContainer.LineLocator(property).ElementLocator.IsVisibleAsync().Result;
    }

    public static async Task WaitVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Visible });
    }

    public static async Task WaitPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitForAsync();
    }

    public static async Task WaitNoVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Hidden });
    }

    public static async Task WaitNoPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitForAsync(new LocatorWaitForOptions { State = WaitForSelectorState.Detached });
    }

    public static LineContainer<S> SubContainer<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property, ILocator? element = null)
        where T : IModifiableEntity
        where S : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new LineContainer<S>(element ?? lineLocator.ElementLocator, lineLocator.Page);
    }

    public static CheckboxLineProxy CheckboxLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, bool>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new CheckboxLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task CheckboxLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, bool>> property, bool value)
        where T : IModifiableEntity
    {
        var line = lineContainer.CheckboxLine(property);
        await line.SetValueAsync(value);
    }

    public static DateTimeLineProxy DateTimeLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new DateTimeLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task DateTimeLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
        where V : IFormattable
    {
        var line = lineContainer.DateTimeLine(property);
        await line.SetValueAsync(value);
    }

    public static EnumLineProxy EnumLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EnumLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task EnumLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var line = lineContainer.EnumLine(property);
        await line.SetValueUntypedAsync(value);
    }

    public static GuidBoxLineProxy GuidLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, Guid?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new GuidBoxLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task GuidLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, Guid?>> property, Guid? value)
        where T : IModifiableEntity
    {
        var line = lineContainer.GuidLine(property);
        await line.SetValueAsync(value);
    }

    public static NumberLineProxy NumberLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new NumberLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task NumberLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
        where V : IFormattable
    {
        var line = lineContainer.NumberLine(property);
        await line.SetValueAsync(value);
    }

    public static HtmlLineProxy HtmlLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new HtmlLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task HtmlLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property, string? value)
        where T : IModifiableEntity
    {
        var line = lineContainer.HtmlLine(property);
        await line.SetValueUntypedAsync(value);
    }

    public static TextAreaLineProxy TextAreaLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new TextAreaLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task TextAreaLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property, string? value)
        where T : IModifiableEntity
    {
        var line = lineContainer.TextAreaLine(property);
        await line.SetValueAsync(value);
    }

    public static TextBoxLineProxy TextBoxLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new TextBoxLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task TextBoxLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property, string? value)
        where T : IModifiableEntity
    {
        var line = lineContainer.TextBoxLine(property);
        await line.SetValueAsync(value);
    }

    public static TimeLineProxy TimeLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new TimeLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task TimeLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
        where V : IFormattable
    {
        var line = lineContainer.TimeLine(property);
        await line.SetValueAsync(value);
    }

    public static BaseLineProxy AutoLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return EntityBaseProxy.AutoLine(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task AutoLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var line = lineContainer.AutoLine(property);
        await line.SetValueUntypedAsync(value);
    }

    public static EntityTableProxy EntityTable<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityTableProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }
}
