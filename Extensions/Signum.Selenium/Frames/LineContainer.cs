using Signum.Entities.Reflection;
using OpenQA.Selenium;
using Signum.UserAssets.QueryTokens;
using Signum.UserAssets.Queries;
using Signum.Selenium.LineProxies;

namespace Signum.Selenium;

public interface ILineContainer<T> : ILineContainer where T : IModifiableEntity
{
}

public interface ILineContainer
{
    IWebElement Element { get; }

    PropertyRoute Route { get; }
}

public  class LineLocator<T>
{
    public LineLocator(WebElementLocator elementLocator, PropertyRoute route)
    {
        ElementLocator = elementLocator;
        Route = route;
    }

    public WebElementLocator ElementLocator { get; set; }

    public PropertyRoute Route { get; set; }
}

public static class LineContainerExtensions
{
    public static bool HasError(this WebDriver selenium, string elementId)
    {
        return selenium.IsElementPresent(By.CssSelector("#{0}.input-validation-error".FormatWith(elementId)));
    }

    public static LineLocator<S> LineLocator<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property) where T : IModifiableEntity
    {
        PropertyRoute route = lineContainer.Route ?? PropertyRoute.Root(typeof(T));

        var element = lineContainer.Element;

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
                    element = element.FindElement(By.CssSelector("[data-property-path='" + route.PropertyString() + "']"));

                route = newRoute;
            }
        }

        return new LineLocator<S>(
            elementLocator : element.WithLocator(By.CssSelector("[data-property-path='" + route.PropertyString() + "']")),
            route : route
        );
    }


    public static bool IsVisible<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        return lineContainer.LineLocator(property).ElementLocator.IsVisible();
    }

    public static bool IsPresent<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        return lineContainer.LineLocator(property).ElementLocator.IsPresent();
    }

    public static void WaitVisible<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        lineContainer.LineLocator(property).ElementLocator.WaitVisible();
    }

    public static void WaitPresent<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        lineContainer.LineLocator(property).ElementLocator.WaitPresent();
    }

    public static void WaitNoVisible<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
   where T : IModifiableEntity
    {
        lineContainer.LineLocator(property).ElementLocator.WaitNoVisible();
    }

    public static void WaitNoPresent<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
        where T : IModifiableEntity
    {
        lineContainer.LineLocator(property).ElementLocator.WaitNoPresent();
    }

    public static LineContainer<S> SubContainer<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property, IWebElement? element = null)
        where T : IModifiableEntity
        where S : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new LineContainer<S>(element ?? lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }


    public static CheckboxLineProxy CheckboxLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, bool>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new CheckboxLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void CheckboxLineValue<T>(this ILineContainer<T> lineContainer, Expression<Func<T, bool>> property, bool value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.CheckboxLine(property);

        valueLine.SetValue(value);

        if (loseFocus)
            valueLine.CheckboxLocator.Find().LoseFocus();
    }

    public static DateTimeLineProxy DateTimeLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
    where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new DateTimeLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void DateTimeLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.DateTimeLine(property);

        valueLine.SetValue((IFormattable?)value);

        if (loseFocus)
            valueLine.InputLocator.Find().LoseFocus();
    }

    public static EnumLineProxy EnumLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
 where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EnumLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void EnumLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.EnumLine(property);

        valueLine.SetValue(value);

        if (loseFocus)
            valueLine.SelectLocator.Find().LoseFocus();
    }

    public static GuidBoxLineProxy GuidLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, Guid?>> property)
where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new GuidBoxLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void GuidLineValue<T>(this ILineContainer<T> lineContainer, Expression<Func<T, Guid?>> property, Guid? value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.GuidLine(property);

        valueLine.SetValue(value);

        if (loseFocus)
            valueLine.InputLocator.Find().LoseFocus();
    }

    public static NumberLineProxy NumberLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new NumberLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void NumberLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.NumberLine(property);

        valueLine.SetValue((IFormattable?)value);

        if (loseFocus)
            valueLine.InputLocator.Find().LoseFocus();
    }

    public static TextAreaLineProxy TextAreaLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property)
    where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new TextAreaLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void TextAreaLineValue<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property, string? value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.TextAreaLine(property);

        valueLine.SetValue((string?)value);

        if (loseFocus)
            valueLine.TextAreaLocator.Find().LoseFocus();
    }

    public static TextBoxLineProxy TextBoxLine<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new TextBoxLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void TextBoxLineValue<T>(this ILineContainer<T> lineContainer, Expression<Func<T, string?>> property, string? value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.TextBoxLine(property);

        valueLine.SetValue((string?)value);

        if (loseFocus)
            valueLine.InputLocator.Find().LoseFocus();
    }


    public static TimeLineProxy TimeLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new TimeLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void TimeLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.TimeLine(property);

        valueLine.SetValue((IFormattable?)value);

        if (loseFocus)
            valueLine.InputLocator.Find().LoseFocus();
    }

    public static BaseLineProxy AutoLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return EntityBaseProxy.AutoLine(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static void AutoLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.AutoLine(property);

        valueLine.SetValueUntyped(value);
    }

    public static V AutoLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var valueLine = lineContainer.AutoLine(property);

        return (V)valueLine.GetValueUntyped()!;
    }


    public static FileLineProxy FileLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
    where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new FileLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static EntityLineProxy EntityLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
      where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static V EntityLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lite = lineContainer.EntityLine(property).GetLite();

        return lite is V ? (V)lite : (V)(object)lite?.RetrieveAndRemember()!;
    }

    public static void EntityLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
        where T : IModifiableEntity
    {
        lineContainer.EntityLine(property).SetLite(value as Lite<IEntity> ?? ((IEntity?)value)?.ToLite());
    }

    public static EntityComboProxy EntityCombo<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
      where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityComboProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static V EntityComboValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lite = lineContainer.EntityCombo(property).LiteValue;
        
        return lite is V ? (V)lite : (V)(object)lite?.RetrieveAndRemember()!;
    }

    public static void EntityComboValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
        where T : IModifiableEntity
    {
        var combo = lineContainer.EntityCombo(property);

        combo.LiteValue = value as Lite<IEntity> ?? ((IEntity?)value)?.ToLite();

        if (loseFocus)
            combo.ComboElement.WrappedElement.LoseFocus();
    }

    public static EntityDetailProxy EntityDetail<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
      where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityDetailProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static EntityRepeaterProxy EntityRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
      where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityRepeaterProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static EntityTabRepeaterProxy EntityTabRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
       where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityTabRepeaterProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static EntityStripProxy EntityStrip<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityStripProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static EntityListProxy EntityList<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
      where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityListProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static EntityTableProxy EntityTable<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityTableProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }

    public static EntityListCheckBoxProxy EntityListCheckBox<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new EntityListCheckBoxProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
    }


    public static QueryTokenBuilderProxy QueryTokenBuilder<T>(this ILineContainer<T> lineContainer, Expression<Func<T, QueryTokenEmbedded>> property)
        where T : IModifiableEntity
    {
        var lineLocator = lineContainer.LineLocator(property);

        return new QueryTokenBuilderProxy(lineLocator.ElementLocator.WaitVisible());
    }

    public static void SelectTab(this ILineContainer lineContainer, string eventKey)
    {
        var element = lineContainer.Element.WaitElementVisible(By.CssSelector($".nav-tabs .nav-item .nav-link[data-rr-ui-event-key='{eventKey}']"));

        element.ScrollTo();
        element.Click();
    }

    public static SearchControlProxy GetSearchControl(this ILineContainer lineContainer, object queryName)
    {
        string queryKey = QueryUtils.GetKey(queryName);

        var element = lineContainer.Element.WaitElementVisible(By.CssSelector("div.sf-search-control[data-query-key={0}]".FormatWith(queryKey)));

        return new SearchControlProxy(element);
    }

    public static SearchValueLineProxy GetSearchValueLine(this ILineContainer lineContainer, object queryName)
    {
        string queryKey = QueryUtils.GetKey(queryName);

        var element = lineContainer.Element.WaitElementVisible(By.CssSelector("[data-value-query-key={0}]".FormatWith(queryKey)));

        return new SearchValueLineProxy(element);
    }
}

public class LineContainer<T> :ILineContainer<T> where T:IModifiableEntity
{
    public IWebElement Element { get; private set; }

    public PropertyRoute Route { get; private set; }

    public LineContainer(IWebElement element, PropertyRoute? route = null)
    {
        this.Element = element;
        this.Route = route ?? PropertyRoute.Root(typeof(T));
    }

    public LineContainer<S> As<S>() where S : T
    {
        return new LineContainer<S>(this.Element, PropertyRoute.Root(typeof(S)));
    }

}
