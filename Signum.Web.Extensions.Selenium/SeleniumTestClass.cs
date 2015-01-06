using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium.Remote;
using System.Diagnostics;
using System.Threading;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;
using OpenQA.Selenium;

namespace Signum.Web.Selenium
{
    public class SeleniumTestClass
    {
        protected static RemoteWebDriver selenium;
        protected static Process seleniumServerProcess;
        private static bool Cleaned = false;

        protected virtual string Url(string url)
        {
            throw new InvalidOperationException("Implement this method returing something like: http://localhost/MyApp/ + url");
        }

      
        public SearchPageProxy SearchPage(object queryName)
        {
            var url = Url(FindRoute(queryName));

            selenium.Url = url;

            return new SearchPageProxy(selenium);
        }

        protected virtual string FindRoute(object queryName)
        {
            return "Find/" + GetWebQueryName(queryName);
        }

        protected string GetWebQueryName(object queryName)
        {
            if (queryName is Type)
            {
                return TypeLogic.TryGetCleanName((Type)queryName) ?? Reflector.CleanTypeName((Type)queryName);
            }

            return queryName.ToString();
        }


        public NormalPage<T> NormalPage<T>(PrimaryKey id) where T : Entity
        {
            return NormalPage<T>(Lite.Create<T>(id));
        }

        public NormalPage<T> NormalPage<T>() where T : Entity
        {
            var url = Url(NavigateRoute(typeof(T), null));

            return NormalPageUrl<T>(url);
        }

        public NormalPage<T> NormalPage<T>(Lite<T> lite) where T : Entity
        {
            if(lite != null && lite.EntityType != typeof(T))
                throw new InvalidOperationException("Use NormalPage<{0}> instead".FormatWith(lite.EntityType.Name));
            
            var url = Url(NavigateRoute(lite));

            return NormalPageUrl<T>(url);
        }

        public NormalPage<T> NormalPageUrl<T>(string url) where T : Entity
        {
            selenium.Url = url;

            return new NormalPage<T>(selenium, null).WaitLoaded();
        }

        protected virtual string NavigateRoute(Type type, PrimaryKey? id)
        {
            var typeName = TypeLogic.TypeToName.TryGetC(type) ?? Reflector.CleanTypeName(type);

            if (id.HasValue)
                return "View/{0}/{1}".FormatWith(typeName, id.HasValue ? id.ToString() : "");
            else
                return "Create/{0}".FormatWith(typeName);
        }

        protected virtual string NavigateRoute(Lite<IEntity> lite)
        {
            return NavigateRoute(lite.EntityType, lite.IdOrNull);
        }


        public virtual string GetCurrentUser()
        {
            if (selenium.IsElementVisible(By.CssSelector(".sf-login")))
                return null;

            if (!selenium.IsElementPresent(By.CssSelector("a.sf-user")))
                throw new InvalidOperationException("No login or logout button found");

            var result = (string)selenium.ExecuteScript("return $('.sf-user span').text()");

            return result;
        }

        public virtual void Logout()
        {
            selenium.FindElement(By.CssSelector("a[href$='Auth/Logout']")).ButtonClick();
            selenium.Wait(() => GetCurrentUser() == null);

            selenium.Url = Url("Auth/Login");
            selenium.WaitElementVisible(By.CssSelector(".sf-login"));
        }

        public virtual void Login(string username, string password)
        {
            selenium.Url = Url("Auth/Login");
            selenium.WaitElementPresent(By.Id("login"));

            var currentUser = GetCurrentUser();
            if (currentUser == username)
                return;

            if (currentUser.HasText())
                Logout();

            selenium.FindElement(By.Id("username")).SafeSendKeys(username);
            selenium.FindElement(By.Id("password")).SafeSendKeys(password);
            selenium.FindElement(By.Id("login")).Submit();

            selenium.Wait(() => GetCurrentUser() != null);
        }
    }
}
