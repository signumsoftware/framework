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

namespace Signum.Web.Selenium
{
    public interface ILineContainer<T> :ILineContainer where T : ModifiableEntity
    {
      
    }

    public interface ILineContainer
    {
        RemoteWebDriver Selenium { get; }

        string Prefix { get; }

        PropertyRoute Route { get; }
    }

    public static class LineContainerExtensions
    {
        public static bool HasError(this RemoteWebDriver selenium, string elementId)
        {
            return selenium.IsElementPresent(By.CssSelector("#{0}.input-validation-error".FormatWith(elementId)));
        }

        public static PropertyRoute GetRoute<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property, out string newPrefix) where T : ModifiableEntity
        {
            newPrefix = lineContainer.Prefix;

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
                    if (newPrefix.HasText())
                        newPrefix += "_";
                    newPrefix += mi.Name;
                }
            }
            return result;
        }

        public static bool HasError<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property)
            where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return lineContainer.Selenium.HasError(newPrefix);
        }

        public static LineContainer<S> SubContainer<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property) 
            where T : ModifiableEntity
            where S : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new LineContainer<S>(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static ValueLineProxy ValueLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new ValueLineProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static void ValueLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
            where T : ModifiableEntity
        {
            lineContainer.ValueLine(property).Value = value;
        }

        public static FileLineProxy FileLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new FileLineProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static V ValueLineValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            return (V)lineContainer.ValueLine(property).Value;
        }

        public static EntityLineProxy EntityLine<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityLineProxy(lineContainer.Selenium, newPrefix, newRoute);
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
            lineContainer.EntityLine(property).LiteValue = value is Lite<IEntity> ? (Lite<IEntity>)value : ((IEntity)value).ToLite();
        }

        public static EntityComboProxy EntityCombo<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityComboProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static V EntityComboValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
        where T : ModifiableEntity
        {
            var lite = lineContainer.EntityLine(property).LiteValue;

            return lite is V ? (V)lite : (V)(object)lite.Retrieve();
        }

        public static void EntityComboValue<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
            where T : ModifiableEntity
        {
            lineContainer.EntityCombo(property).LiteValue = value is Lite<IEntity> ? (Lite<IEntity>)value : ((IEntity)value).ToLite();
        }

        public static EntityDetailProxy EntityDetail<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityDetailProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static EntityRepeaterProxy EntityRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityRepeaterProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static EntityTabRepeaterProxy EntityTabRepeater<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
           where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityTabRepeaterProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static EntityStripProxy EntityStrip<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityStripProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static EntityListProxy EntityList<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
          where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityListProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static EntityListDetailProxy EntityListDetail<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityListDetailProxy(lineContainer.Selenium, newPrefix, newRoute);
        }

        public static EntityListCheckBoxProxy EntityListCheckBox<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property)
            where T : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new EntityListCheckBoxProxy(lineContainer.Selenium, newPrefix, newRoute);
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
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            return new QueryTokenBuilderProxy(lineContainer.Selenium, newPrefix + "_");
        }

        public static void SelectTab(this ILineContainer lineContainer, string tabId)
        {
            var fullTabId = lineContainer.PrefixUnderscore() + tabId;
            lineContainer.Selenium.FindElement(By.CssSelector("a[href='#{0}']".FormatWith(fullTabId))).Click();
            lineContainer.Selenium.Wait(() => lineContainer.Selenium.IsElementVisible(By.Id(fullTabId)));

        }

        public static string PrefixUnderscore(this ILineContainer lineContainer)
        {
            if (lineContainer.Prefix.HasText())
                return lineContainer.Prefix + "_";

            return "";
        }

        public static string[] Errors(this ILineContainer lineContainer)
        {
            var result = (string)lineContainer.Selenium
                .ExecuteScript("return $('#" + lineContainer.PrefixUnderscore() + "sfGlobalValidationSummary > ul > li').toArray().map(function(e){return $(e).text()}).join('\\r\\n');");

            return result.SplitNoEmpty("\r\n" );
        }

        public static SearchControlProxy GetSearchControl(this ILineContainer lineContainer, object queryName)
        {
            string query = QueryUtils.GetQueryUniqueKey(queryName);

            var prefix = (string)lineContainer.Selenium.ExecuteScript("return $('div.sf-search-control[data-queryname=\"{0}\"]').data('prefix')".FormatWith(query));

            return new SearchControlProxy(lineContainer.Selenium, prefix);
        }

        public static SearchControlProxy GetSearchControlSuffix(this ILineContainer lineContainer, string suffix)
        {
            return new SearchControlProxy(lineContainer.Selenium, lineContainer.PrefixUnderscore() + suffix); 
        }
    }

    public class LineContainer<T> :ILineContainer<T> where T:ModifiableEntity
    {
        public RemoteWebDriver Selenium { get; private set; }

        public string Prefix { get; private set; }

        public PropertyRoute Route { get; private set; }

        public LineContainer(RemoteWebDriver selenium, string prefix = null, PropertyRoute route = null)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.Route = route == null || route.IsImplementation(typeof(T)) ? PropertyRoute.Root(typeof(T)) : route;
        }
    }

    public class NormalPage<T> : ILineContainer<T>, IEntityButtonContainer<T>, IWidgetContainer, IDisposable where T : ModifiableEntity
    {
        public RemoteWebDriver Selenium { get; private set; }

        public string Prefix { get; private set; }

        public PropertyRoute Route { get; private set; }

        public NormalPage(RemoteWebDriver selenium, string prefix = null)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.Route = PropertyRoute.Root(typeof(T));
        }

        public By ContainerLocator()
        {
            return By.CssSelector("#divMainPage");
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
            this.Selenium.Wait(() => { var ri = this.RuntimeInfo(); return ri != null && ri.EntityType == typeof(T); });

            return this;
        }

        public NormalPage<T> WaitLoadedAndId()
        {
            this.Selenium.Wait(() => {var ri = this.RuntimeInfo(); return ri != null && ri.EntityType == typeof(T) && ri.IdOrNull.HasValue;});

            return this;
        }

        public string Title()
        {
            return (string)Selenium.ExecuteScript("return $('#divMainPage > h3 > .sf-entity-title').html()");
        }

        public RuntimeInfoProxy RuntimeInfo()
        {
            var ri = (string)Selenium.ExecuteScript("return $('#sfRuntimeInfo').val()");

            if (ri == null)
                return null;

            return RuntimeInfoProxy.FromFormValue(ri);
        }

        public T RetrieveEntity()
        {
            var lite = this.RuntimeInfo().ToLite();
            return (T)(IEntity)lite.Retrieve();
        }

        public string EntityState()
        {
            if ((long)Selenium.ExecuteScript("return $('#sfEntityState').length".FormatWith(Prefix)) == 0)
                return null;

            return (string)Selenium.ExecuteScript("return $('#sfEntityState')[0].value".FormatWith(Prefix));
        }
    }
}
