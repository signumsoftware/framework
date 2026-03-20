using Microsoft.Playwright;
using Signum.Entities.Reflection;
using Signum.Playwright.LineProxies;
using Signum.Playwright.Search;
using Signum.UserAssets.Queries;

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

    public static async Task<bool> IsVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        return await lineContainer.LineLocator(property).ElementLocator.IsVisibleAsync();
    }

    public static async Task<bool> IsPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        return await lineContainer.LineLocator(property).ElementLocator.CountAsync() > 0;
    }

    public static async Task WaitVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Visible
        });
    }

    public static async Task WaitPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitForAsync();
    }

    public static async Task WaitNoVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Hidden
        });
    }

    public static async Task WaitNoPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitForAsync(new LocatorWaitForOptions
        {
            State = WaitForSelectorState.Detached
        });
    }

    public static LineContainer<S> SubContainer<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property, ILocator? element = null)
        where T : IModifiableEntity
        where S : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new LineContainer<S>(element ?? lineLocator.ElementLocator, lineLocator.Page, lineLocator.Route);
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
        var valueLine = lineContainer.CheckboxLine(property);
        await valueLine.SetValueAsync(value);
    }

    public static DateTimeLineProxy DateTimeLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new DateTimeLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task DateTimeLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.DateTimeLine(property);
        await valueLine.SetValueAsync((IFormattable?)value);
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
        var valueLine = lineContainer.EnumLine(property);
        await valueLine.SetValueUntypedAsync(value);
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
        var valueLine = lineContainer.GuidLine(property);
        await valueLine.SetValueAsync(value);
    }

    public static NumberLineProxy NumberLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new NumberLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task NumberLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.NumberLine(property);
        await valueLine.SetValueAsync((IFormattable?)value);
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
        var valueLine = lineContainer.HtmlLine(property);
        await valueLine.SetValueUntypedAsync(value);
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
        var valueLine = lineContainer.TextAreaLine(property);
        await valueLine.SetValueAsync(value);
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
        var valueLine = lineContainer.TextBoxLine(property);
        await valueLine.SetValueAsync(value);
    }

    public static TimeLineProxy TimeLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new TimeLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static async Task TimeLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.TimeLine(property);
        await valueLine.SetValueAsync((IFormattable?)value);
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
        var valueLine = lineContainer.AutoLine(property);
        await valueLine.SetValueUntypedAsync(value);
    }

    public static async Task<V> AutoLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.AutoLine(property);
        return (V)(await valueLine.GetValueUntypedAsync())!;
    }

    public static FileLineProxy FileLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new FileLineProxy(lineLocator.ElementLocator, lineLocator.Page, lineLocator.Route);
    }

    public static EntityLineProxy EntityLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static EntityComboProxy EntityCombo<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityComboProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static EntityDetailProxy EntityDetail<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityDetailProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static EntityRepeaterProxy EntityRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityRepeaterProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static EntityTabRepeaterProxy EntityTabRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityTabRepeaterProxy(lineLocator.ElementLocator, lineLocator.Page, lineLocator.Route);
    }

    public static EntityStripProxy EntityStrip<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityStripProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static EntityListProxy EntityList<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityListProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static EntityTableProxy EntityTable<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityTableProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static EntityListCheckBoxProxy EntityListCheckBox<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityListCheckBoxProxy(lineLocator.ElementLocator, lineLocator.Route, lineLocator.Page);
    }

    public static QueryTokenBuilderProxy QueryTokenBuilder<T>(this ILineContainer<T> lineContainer, Expression<Func<T, QueryTokenEmbedded>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new QueryTokenBuilderProxy(lineLocator.ElementLocator);
    }

    public static async Task SelectTabAsync(this ILineContainer lineContainer, string eventKey)
    {
        var element = lineContainer.Element.Locator($".nav-tabs .nav-item .nav-link[data-rr-ui-event-key='{eventKey}']");
        await element.ClickAsync();
    }

    public static SearchControlProxy GetSearchControl(this ILineContainer lineContainer, object queryName)
    {
        string queryKey = QueryUtils.GetKey(queryName);
        var element = lineContainer.Element.Locator($"div.sf-search-control[data-query-key='{queryKey}']");
        return new SearchControlProxy(element, lineContainer.Page);
    }

    public static SearchValueLineProxy GetSearchValueLine(this ILineContainer lineContainer, object queryName)
    {
        string queryKey = QueryUtils.GetKey(queryName);
        var element = lineContainer.Element.Locator($"[data-value-query-key='{queryKey}']");
        return new SearchValueLineProxy(element, lineContainer.Page);
    }
}
