using System;
using OpenQA.Selenium.Remote;
using System.Threading;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Engine.Basics;
using OpenQA.Selenium;
using System.Globalization;
using System.Linq.Expressions;

namespace Signum.React.Selenium
{
    public class BrowserProxy
    {
        public readonly RemoteWebDriver Selenium;

        public BrowserProxy(RemoteWebDriver selenium)
        {
            this.Selenium = selenium;
        }

        public virtual string Url(string url)
        {
            throw new InvalidOperationException("Implement this method returing something like: http://localhost/MyApp/ + url");
        }


        public SearchPageProxy SearchPage(object queryName)
        {
            var url = Url(FindRoute(queryName));

            Selenium.Url = url;

            return new SearchPageProxy(Selenium);
        }

        public virtual string FindRoute(object queryName)
        {
            return "Find/" + GetWebQueryName(queryName);
        }

        public string GetWebQueryName(object queryName)
        {
            if (queryName is Type)
            {
                return TypeLogic.TryGetCleanName((Type)queryName) ?? Reflector.CleanTypeName((Type)queryName);
            }

            return queryName.ToString();
        }


        public FramePageProxy<T> NormalPage<T>(PrimaryKey id) where T : Entity
        {
            return NormalPage<T>(Lite.Create<T>(id));
        }

        public FramePageProxy<T> NormalPage<T>() where T : Entity
        {
            var url = Url(NavigateRoute(typeof(T), null));

            return AsNormalPage<T>(url);
        }

        public FramePageProxy<T> NormalPage<T>(Lite<T> lite) where T : Entity
        {
            if(lite != null && lite.EntityType != typeof(T))
                throw new InvalidOperationException("Use NormalPage<{0}> instead".FormatWith(lite.EntityType.Name));

            var url = Url(NavigateRoute(lite));

            return AsNormalPage<T>(url);
        }

        public FramePageProxy<T> AsNormalPage<T>(string url) where T : Entity
        {
            Selenium.Url = url;

            return new FramePageProxy<T>(Selenium);
        }

        public virtual string NavigateRoute(Type type, PrimaryKey? id)
        {
            var typeName = TypeLogic.TypeToName.TryGetC(type) ?? Reflector.CleanTypeName(type);

            if (id.HasValue)
                return "view/{0}/{1}".FormatWith(typeName, id.HasValue ? id.ToString() : "");
            else
                return "create/{0}".FormatWith(typeName);
        }

        public virtual string NavigateRoute(Lite<IEntity> lite)
        {
            return NavigateRoute(lite.EntityType, lite.IdOrNull);
        }


        public virtual string GetCurrentUser()
        {
            var element = Selenium.WaitElementPresent(By.CssSelector("#sfUserDropDown, .sf-login"));

            if (element.HasClass("sf-login"))
                return null;

            var result = element.Text;

            return result;
        }

        public virtual void Logout()
        {
            Selenium.FindElement(By.Id("sfUserDropDown")).Click();
            Selenium.FindElement(By.Id("sf-auth-logout")).Click();    //SelectElement();
            Selenium.Wait(() => GetCurrentUser() == null);
            Selenium.Url = Url("Auth/Login");
            Selenium.WaitElementVisible(By.CssSelector(".sf-login"));
        }

        public virtual void Login(string username, string password)
        {
            Selenium.Url = Url("Auth/Login");
            Selenium.WaitElementPresent(By.Id("login"));

            var currentUser = GetCurrentUser();
            if (currentUser == username)
                return;

            if (currentUser.HasText())
                Logout();

            Selenium.FindElement(By.Id("userName")).SafeSendKeys(username);
            Selenium.FindElement(By.Id("password")).SafeSendKeys(password);
           // Selenium.FindElement(By.Id("login")).Submit();

            Selenium.FindElement(By.Id("login")).Click();
            Selenium.WaitElementNotPresent(By.Id("login"));

            Selenium.WaitElementPresent(By.Id("sfUserDropDown"));

            SetCurrentCulture();
        }

        public virtual void SetCurrentCulture()
        {
            string culture = Selenium.WaitElementPresent(By.Id("cultureDropdown")).GetAttribute("data-culture");

            Thread.CurrentThread.CurrentCulture = Thread.CurrentThread.CurrentUICulture = new CultureInfo(culture);
        }

        public T Wait<T>(Expression<Func<T>> expression)
        {
            var condition = expression.Compile();

            return Selenium.Wait(condition, () => expression.ToString());
        }
    }
}
