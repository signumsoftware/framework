using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Selenium;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities;

namespace Signum.Web.Selenium
{
    public interface ILineContainer<T> where T : ModifiableEntity
    {
        ISelenium Selenium { get; }

        string Prefix { get; }

        PropertyRoute Route { get; }
    }

  

    public static class LineContainerExtensions
    {
        public static bool HasError(this ISelenium selenium, string elementId)
        {
            return selenium.IsElementPresent("jq=#{0}.input-validation-error".Formato(elementId));
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


        public static void Type<T, V>(this ILineContainer<T> lineContainer, Expression<Func<T, V>> property, V value)
            where T : ModifiableEntity
            where V : ModifiableEntity
        {
            string newPrefix;
            PropertyRoute newRoute = lineContainer.GetRoute(property, out newPrefix);

            string stringValue = value == null ? null :
                    value is IFormattable ? ((IFormattable)value).ToString(Reflector.FormatString(newRoute) ?? Reflector.FormatString(newRoute.Type), null) :
                    value.ToString();

            lineContainer.Selenium.Type(newPrefix, stringValue);
        }

        public static PropertyRoute GetRoute<T, S>(this ILineContainer<T> lineContainer, Expression<Func<T, S>> property, out string newPrefix) where T : ModifiableEntity
        {
            newPrefix = lineContainer.Prefix;

            PropertyRoute result = lineContainer.Route ?? PropertyRoute.Root(typeof(T));

            foreach (var mi in Reflector.GetMemberList(property))
            {
                result = result.Add(mi);
                newPrefix += "_" + newPrefix;
            }
            return result;
        }
    }


   

    public class LineContainer<T> :ILineContainer<T> where T:ModifiableEntity
    {
        public ISelenium Selenium { get; private set; }

        public string Prefix { get; private set; }

        public PropertyRoute Route { get; private set; }

        public LineContainer(ISelenium selenium, string prefix = null, PropertyRoute route = null)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.Route = route ?? PropertyRoute.Root(typeof(T));
        }
    }

    public class NormalPage<T> : ILineContainer<T>, IEntityButtonContainer, IWidgetContainer where T : ModifiableEntity
    {
        public ISelenium Selenium { get; private set; }

        public string Prefix { get; private set; }

        public PropertyRoute Route { get; private set; }

        public NormalPage(ISelenium selenium, string prefix = null)
        {
            this.Selenium = selenium;
            this.Prefix = prefix;
            this.Route = PropertyRoute.Root(typeof(T));
        }
    }
}
