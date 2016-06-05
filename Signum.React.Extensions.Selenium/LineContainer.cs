using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using OpenQA.Selenium.Remote;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System.Reflection;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using OpenQA.Selenium;
using Signum.React.Selenium;

namespace Signum.React.Selenium
{
    public interface ILineContainer<T> : ILineContainer where T : ModifiableEntity
    {
    }

    public interface ILineContainer
    {
        IWebElement Element { get; }

        PropertyRoute Route { get; }
    }

    public static class LineContainerExtensions
    {
        public static bool HasError(this RemoteWebDriver selenium, string elementId)
        {
            return selenium.IsElementPresent(By.CssSelector("#{0}.input-validation-error".FormatWith(elementId)));
        }

        public static PropertyRoute GetRoute<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property, out IWebElement element) where T : ModifiableEntity
        {
            PropertyRoute result = lineContainer.Route ?? PropertyRoute.Root(typeof(T));

            foreach (var mi in Reflector.GetMemberList(property))
            {
                if (mi is MethodInfo && ((MethodInfo)mi).IsInstantiationOf(MixinDeclarations.miMixin))
                {
                    result = result.Add(((MethodInfo)mi).GetGenericArguments()[0]);
                }
                else
                {
                    result = result.Add(mi);
                }
            }

            element = lineContainer.Element.FindElement(By.CssSelector("[data-propertyroute=" + result.PropertyString() + "]"));

            return result;
        }


        public static LineContainer<S> SubContainer<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property) 
            where T : ModifiableEntity
            where S : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new LineContainer<S>(element, newRoute);
        }

        public static ValueLineProxy ValueLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new ValueLineProxy(element, newRoute);
        }

        public static void ValueLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
            where T : ModifiableEntity
        {
            var valueLine = lineContainer.ValueLine(property);

            valueLine.Value = value;

            if (loseFocus)
                valueLine.MainElement.Find().LoseFocus();
        }

        public static FileLineProxy FileLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new FileLineProxy(element, newRoute);
        }

        public static V ValueLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            return (V)lineContainer.ValueLine(property).Value;
        }

        public static EntityLineProxy EntityLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityLineProxy(element, newRoute);
        }

        public static V EntityLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : ModifiableEntity
        {
            var lite = lineContainer.EntityLine(property).LiteValue;

            return lite is V ? (V)lite : (V)(object)lite.Retrieve();
        }

        public static void EntityLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
            where T : ModifiableEntity
        {
            lineContainer.EntityLine(property).LiteValue = value as Lite<IEntity> ?? ((IEntity)value)?.ToLite();
        }

        public static EntityComboProxy EntityCombo<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityComboProxy(element, newRoute);
        }

        public static V EntityComboValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : ModifiableEntity
        {
            var lite = lineContainer.EntityCombo(property).LiteValue;

            return lite is V ? (V)lite : (V)(object)lite.Retrieve();
        }

        public static void EntityComboValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
            where T : ModifiableEntity
        {
            var combo = lineContainer.EntityCombo(property);

            combo.LiteValue = value as Lite<IEntity> ?? ((IEntity)value)?.ToLite();

            if (loseFocus)
                combo.ComboElement.WrappedElement.LoseFocus();
        }

        public static EntityDetailProxy EntityDetail<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityDetailProxy(element, newRoute);
        }

        public static EntityRepeaterProxy EntityRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityRepeaterProxy(element, newRoute);
        }

        public static EntityTabRepeaterProxy EntityTabRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
           where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityTabRepeaterProxy(element, newRoute);
        }

        public static EntityStripProxy EntityStrip<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityStripProxy(element, newRoute);
        }

        public static EntityListProxy EntityList<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityListProxy(element, newRoute);
        }

        public static EntityListCheckBoxProxy EntityListCheckBox<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityListCheckBoxProxy(element, newRoute);
        }

        public static bool IsImplementation(this PropertyRoute route, Type type)
        {
            if (!typeof(Entity).IsAssignableFrom(type))
                return false;

            var routeType = route.Type.CleanType();

            return routeType.IsAssignableFrom(type);
        }

        public static QueryTokenBuilderProxy QueryTokenBuilder<T>(this ILineContainer<T> lineContainer, Expression<Func<T, QueryTokenEntity>> property)
            where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new QueryTokenBuilderProxy(element);
        }

        public static void SelectTab(this ILineContainer lineContainer, string title)
        {
            var tabs = lineContainer.Element.FindElement(By.CssSelector("ul[role=tablist]"));

            var tab = tabs.FindElements(By.CssSelector("a[role=tab]")).Single(a => a.Text.Contains(title));

        }

        public static SearchControlProxy GetSearchControl(this ILineContainer lineContainer, object queryName)
        {
            string queryKey = QueryUtils.GetKey(queryName);
            
            var element = lineContainer.Element.FindElement(By.CssSelector("div.sf-search-control[data-query-key={0}]".FormatWith(queryKey)));

            return new SearchControlProxy(element);
        }
    }

    public class LineContainer<T> :ILineContainer<T> where T:ModifiableEntity
    {
        public IWebElement Element { get; private set; }

        public PropertyRoute Route { get; private set; }

        public LineContainer(IWebElement element, PropertyRoute route = null)
        {
            this.Element = element;
            this.Route = route == null || route.IsImplementation(typeof(T)) ? PropertyRoute.Root(typeof(T)) : route;
        }
    }

    public class NormalPage<T> : ILineContainer<T>, IEntityButtonContainer<T>, IWidgetContainer, IDisposable where T : ModifiableEntity
    {
        public RemoteWebDriver Selenium { get; private set; }

        public IWebElement Element { get; private set; }

        public PropertyRoute Route { get; private set; }

        public NormalPage(RemoteWebDriver selenium)
        {
            this.Selenium = selenium;
            this.Element = selenium.WaitElementPresent(By.CssSelector(".normal-control"));
            this.Route = PropertyRoute.Root(typeof(T));
        }

        public IWebElement ContainerElement()
        {
            return this.Element;
        }

        public void Dispose()
        {
        }

        public NormalPage<T> WaitLoadedAndId()
        {
            this.Selenium.Wait(() => {var ri = this.EntityInfo(); return ri != null && ri.EntityType == typeof(T) && ri.IdOrNull.HasValue;});

            return this;
        }

        public string Title()
        {
            return (string)Selenium.ExecuteScript("return $('#divMainPage > h3 > .sf-entity-title').html()");
        }

        public EntityInfoProxy EntityInfo()
        {
            return EntityInfoProxy.Parse(this.Element.FindElement(By.CssSelector("sf-main-control")).GetAttribute("data-main-entity"));
        }

        public T RetrieveEntity()
        {
            var lite = this.EntityInfo().ToLite();
            return (T)(IEntity)lite.Retrieve();
        }

        public NormalPage<T> WaitLoaded()
        {
            this.Element.GetDriver().Wait(() => this.EntityInfo() != null);
            return this;
        }
    }
}
