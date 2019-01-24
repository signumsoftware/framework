using System;
using System.Linq.Expressions;
using OpenQA.Selenium.Remote;
using Signum.Engine;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using OpenQA.Selenium;

namespace Signum.React.Selenium
{
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
        public WebElementLocator ElementLocator { get; set; }

        public PropertyRoute Route { get; set; }
    }

    public static class LineContainerExtensions
    {
        public static bool HasError(this RemoteWebDriver selenium, string elementId)
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

                    if (newRoute.Parent != route)
                        element = element.FindElement(By.CssSelector("[data-property-path='" + route.PropertyString() + "']"));

                    route = newRoute;
                }
            }

            return new LineLocator<S>
            {
                Route = route,
                ElementLocator = element.WithLocator(By.CssSelector("[data-property-path='" + route.PropertyString() + "']"))
            };
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

        public static LineContainer<S> SubContainer<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
            where T : IModifiableEntity
            where S : IModifiableEntity
        {
            var lineLocator = lineContainer.LineLocator(property);

            return new LineContainer<S>(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
        }

     


        public static ValueLineProxy ValueLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : IModifiableEntity
        {
            var lineLocator = lineContainer.LineLocator(property);

            return new ValueLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
        }

        public static void ValueLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
            where T : IModifiableEntity
        {
            var valueLine = lineContainer.ValueLine(property);

            valueLine.SetValue(value);

            if (loseFocus)
                valueLine.EditableElement.Find().LoseFocus();
        }

        public static FileLineProxy FileLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : IModifiableEntity
        {
            var lineLocator = lineContainer.LineLocator(property);

            return new FileLineProxy(lineLocator.ElementLocator.WaitVisible(), lineLocator.Route);
        }

        public static V ValueLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : IModifiableEntity
        {
            return (V)lineContainer.ValueLine(property).GetValue();
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

            return lite is V ? (V)lite : (V)(object)lite.Retrieve();
        }

        public static void EntityLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
            where T : IModifiableEntity
        {
            lineContainer.EntityLine(property).SetLite( value as Lite<IEntity> ?? ((IEntity)value)?.ToLite());
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

            return lite is V ? (V)lite : (V)(object)lite.Retrieve();
        }

        public static void EntityComboValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
            where T : IModifiableEntity
        {
            var combo = lineContainer.EntityCombo(property);

            combo.LiteValue = value as Lite<IEntity> ?? ((IEntity)value)?.ToLite();

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
            var element = lineContainer.Element.WaitElementVisible(By.CssSelector($"li.nav-item[data-eventkey={eventKey}] a"));

            element.ScrollTo();
            element.Click();
        }

        public static SearchControlProxy GetSearchControl(this ILineContainer lineContainer, object queryName)
        {
            string queryKey = QueryUtils.GetKey(queryName);

            var element = lineContainer.Element.FindElement(By.CssSelector("div.sf-search-control[data-query-key={0}]".FormatWith(queryKey)));

            return new SearchControlProxy(element);
        }
    }

    public class LineContainer<T> :ILineContainer<T> where T:IModifiableEntity
    {
        public IWebElement Element { get; private set; }

        public PropertyRoute Route { get; private set; }

        public LineContainer(IWebElement element, PropertyRoute route = null)
        {
            this.Element = element;
            this.Route = route ?? PropertyRoute.Root(typeof(T));
        }

        public LineContainer<S> As<S>() where S : T
        {
            return new LineContainer<S>(this.Element, PropertyRoute.Root(typeof(S)));
        }

    }
}
