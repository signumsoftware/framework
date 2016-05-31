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
    public interface ILineContainer<T> :ILineContainer where T : ModifiableEntity
    {
      
    }

    public interface ILineContainer
    {
        RemoteWebDriver Selenium { get; }

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

            return new LineContainer<S>(lineContainer.Selenium, element, newRoute);
        }

        public static ValueLineProxy ValueLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new ValueLineProxy(lineContainer.Selenium, element, newRoute);
        }

        public static void ValueLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value, bool loseFocus = false)
            where T : ModifiableEntity
        {
            var valueLine = lineContainer.ValueLine(property);

            valueLine.Value = value;

            if (loseFocus)
                lineContainer.Selenium.LoseFocus(valueLine.MainElement());
        }

        public static FileLineProxy FileLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new FileLineProxy(lineContainer.Selenium, element, newRoute);
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

            return new EntityLineProxy(lineContainer.Selenium, element, newRoute);
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

            return new EntityComboProxy(lineContainer.Selenium, element, newRoute);
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
                lineContainer.Selenium.LoseFocus(combo.ComboElement.WrappedElement);
        }

        public static EntityDetailProxy EntityDetail<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityDetailProxy(lineContainer.Selenium, element, newRoute);
        }

        public static EntityRepeaterProxy EntityRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityRepeaterProxy(lineContainer.Selenium, element, newRoute);
        }

        public static EntityTabRepeaterProxy EntityTabRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
           where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityTabRepeaterProxy(lineContainer.Selenium, element, newRoute);
        }

        public static EntityStripProxy EntityStrip<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityStripProxy(lineContainer.Selenium, element, newRoute);
        }

        public static EntityListProxy EntityList<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityListProxy(lineContainer.Selenium, element, newRoute);
        }

        public static EntityListCheckBoxProxy EntityListCheckBox<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            IWebElement element;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out element);

            return new EntityListCheckBoxProxy(lineContainer.Selenium, element, newRoute);
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

            return new QueryTokenBuilderProxy(lineContainer.Selenium, element);
        }

        public static void SelectTab(this ILineContainer lineContainer, string tabId)
        {
            
            lineContainer.Selenium.NotImplemented().Click();
            throw new NotImplementedException();
            //lineContainer.Selenium.Wait(() => lineContainer.Selenium.IsElementVisible(By.Id(fullTabId)));

        }

        public static string[] Errors(this ILineContainer lineContainer)
        {
            //var result = (string)lineContainer.Selenium
            //    .ExecuteScript("return $('#" + lineContainer.PrefixUnderscore() + "sfGlobalValidationSummary > ul > li').toArray().map(function(e){return $(e).text()}).join('\\r\\n');");

            //return result.SplitNoEmpty("\r\n" );

            throw new InvalidOperationException();
        }

        public static SearchControlProxy GetSearchControl(this ILineContainer lineContainer, object queryName)
        {
            string query = QueryUtils.GetKey(queryName);

            throw new NotImplementedException();
            //var prefix = (string)lineContainer.Selenium.ExecuteScript("return $('div.sf-search-control[data-queryname=\"{0}\"]').data('prefix')".FormatWith(query));

            //return new SearchControlProxy(lineContainer.Selenium,  prefix);
        }
    }

    public class LineContainer<T> :ILineContainer<T> where T:ModifiableEntity
    {
        public RemoteWebDriver Selenium { get; private set; }

        public IWebElement Element { get; private set; }

        public PropertyRoute Route { get; private set; }

        public LineContainer(RemoteWebDriver selenium, IWebElement element = null, PropertyRoute route = null)
        {
            this.Selenium = selenium;
            this.Element = element;
            this.Route = route == null || route.IsImplementation(typeof(T)) ? PropertyRoute.Root(typeof(T)) : route;
        }
    }

    public class NormalPage<T> : ILineContainer<T>, IEntityButtonContainer<T>, IWidgetContainer, IDisposable where T : ModifiableEntity
    {
        public RemoteWebDriver Selenium { get; private set; }

        public IWebElement Element { get; private set; }

        public PropertyRoute Route { get; private set; }

        public NormalPage(RemoteWebDriver selenium, IWebElement element = null)
        {
            this.Selenium = selenium;
            this.Element = element;
            this.Route = PropertyRoute.Root(typeof(T));
        }

        public IWebElement ContainerElement()
        {
            return this.Selenium.NotImplemented(); // By.CssSelector("#divMainPage");
        }

        public void Dispose()
        {
        }

        public bool HasId()
        {
            return Selenium.IsElementPresent(By.CssSelector("#divMainPage[data-isnew=false]"));
        }

        public NormalPage<T> WaitLoaded()
        {
            this.Selenium.Wait(() => { var ri = this.EntityInfo(); return ri != null && ri.EntityType == typeof(T); });

            return this;
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
            var ri = (string)Selenium.ExecuteScript("return $('#sfRuntimeInfo').val()");

            if (ri == null)
                return null;

            throw new NotImplementedException();
        }

        public T RetrieveEntity()
        {
            var lite = this.EntityInfo().ToLite();
            return (T)(IEntity)lite.Retrieve();
        }
    }
}
