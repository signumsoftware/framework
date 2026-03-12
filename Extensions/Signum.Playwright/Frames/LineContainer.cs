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

    public LineLocator(ILocator elementLocator, PropertyRoute route)
    {
        ElementLocator = elementLocator;
        Route = route;
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
            if (mi is MethodInfo methodInfo && methodInfo.IsInstantiationOf(MixinDeclarations.miMixin))
            {
                route = route.Add(methodInfo.GetGenericArguments()[0]);
            }
            else
            {
                var newRoute = route.Add(mi);

                if (newRoute.Parent != route && route != lineContainer.Route)
                {
                    element = element.Locator($"[data-property-path='{route.PropertyString()}']");
                }

                route = newRoute;
            }
        }

        return new LineLocator<S>(
            elementLocator: element.Locator($"[data-property-path='{route.PropertyString()}']"),
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
        return await lineContainer.LineLocator(property).ElementLocator.IsPresentAsync();
    }

    public static async Task WaitVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitVisibleAsync();
    }

    public static async Task WaitPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitVisibleAsync();
    }

    public static async Task WaitNotVisibleAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitNotVisibleAsync();
    }

    public static async Task WaitNotPresentAsync<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        await lineContainer.LineLocator(property).ElementLocator.WaitNotVisibleAsync();
    }

    public static LineContainer<S> SubContainer<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property, ILocator? element = null)
        where T : IModifiableEntity
        where S : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new LineContainer<S>(element ?? lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    // CheckboxLine
    public static CheckboxLineProxy CheckboxLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, bool>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new CheckboxLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task CheckboxLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, bool>> property, bool value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.CheckboxLine(property);
        await valueLine.SetCheckedAsync(value);
    }

    // DateTimeLine
    public static DateTimeLineProxy DateTimeLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new DateTimeLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task DateTimeLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.DateTimeLine(property);
        await valueLine.SetValueUntypedAsync(value);
    }

    // EnumLine
    public static EnumLineProxy EnumLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EnumLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task EnumLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.EnumLine(property);
        await valueLine.SetValueUntypedAsync(value);
    }

    // GuidLine
    public static GuidBoxLineProxy GuidLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, Guid?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new GuidBoxLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task GuidLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, Guid?>> property, Guid? value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.GuidLine(property);
        await valueLine.SetValueUntypedAsync(value);
    }

    // NumberLine
    public static NumberLineProxy NumberLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new NumberLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task NumberLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.NumberLine(property);
        await valueLine.SetValueUntypedAsync(value);
    }

    // HtmlLine
    public static HtmlLineProxy HtmlLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new HtmlLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task HtmlLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property, string? value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.HtmlLine(property);
        await valueLine.SetValueUntypedAsync(value);
    }

    // TextAreaLine
    public static TextAreaLineProxy TextAreaLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new TextAreaLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task TextAreaLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property, string? value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.TextAreaLine(property);
        await valueLine.SetValueAsync(value);
    }

    // TextBoxLine
    public static TextBoxLineProxy TextBoxLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new TextBoxLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task TextBoxLineValueAsync<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property, string? value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.TextBoxLine(property);
        await valueLine.SetValueAsync(value);
    }

    // TimeLine
    public static TimeLineProxy TimeLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new TimeLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task TimeLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.TimeLine(property);
        await valueLine.SetValueUntypedAsync(value);
    }

    // AutoLine
    public static async Task<BaseLineProxy> AutoLineAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return await LineProxyHelpers.AutoLineAsync(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task AutoLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = await lineContainer.AutoLineAsync(property);
        await valueLine.SetValueUntypedAsync(value);
    }

    public static async Task<V> GetAutoLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var valueLine = await lineContainer.AutoLineAsync(property);
        return (V)(await valueLine.GetValueUntypedAsync())!;
    }

    // FileLine
    public static FileLineProxy FileLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new FileLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    // EntityLine
    public static EntityLineProxy EntityLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityLineProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task<V> GetEntityLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lite = await lineContainer.EntityLine(property).GetLiteAsync();
        return lite is V ? (V)lite : (V)(object)(lite?.RetrieveAndRemember())!;
    }

    public static async Task EntityLineValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        await lineContainer.EntityLine(property).SetLiteAsync(value as Lite<IEntity> ?? ((IEntity?)value)?.ToLite());
    }

    // EntityCombo
    public static EntityComboProxy EntityCombo<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityComboProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    public static async Task<V> GetEntityComboValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lite = await lineContainer.EntityCombo(property).GetLiteAsync();
        return lite is V ? (V)lite : (V)(object)(lite?.RetrieveAndRemember())!;
    }

    public static async Task EntityComboValueAsync<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var combo = lineContainer.EntityCombo(property);
        await combo.SetLiteAsync(value as Lite<IEntity> ?? ((IEntity?)value)?.ToLite());
    }

    // EntityDetail
    public static EntityDetailProxy EntityDetail<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityDetailProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    // EntityRepeater
    public static EntityRepeaterProxy EntityRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityRepeaterProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    // EntityTabRepeater
    public static EntityTabRepeaterProxy EntityTabRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityTabRepeaterProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    // EntityStrip
    public static EntityStripProxy EntityStrip<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityStripProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }

    // EntityList
    public static EntityListProxy EntityList<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);
        return new EntityListProxy(lineLocator.ElementLocator, lineLocator.Route, lineContainer.Page);
    }
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

    public LineContainer<S> As<S>() where S : T
    {
        return new LineContainer<S>(this.Element, PropertyRoute.Root(typeof(S)), this.Page);
    }
}
