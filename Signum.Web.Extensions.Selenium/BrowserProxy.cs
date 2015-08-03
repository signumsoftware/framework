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
            Selenium.Url = url;

            return new NormalPage<T>(Selenium, null).WaitLoaded();
        }

        public virtual string NavigateRoute(Type type, PrimaryKey? id)
        {
            var typeName = TypeLogic.TypeToName.TryGetC(type) ?? Reflector.CleanTypeName(type);

            if (id.HasValue)
                return "View/{0}/{1}".FormatWith(typeName, id.HasValue ? id.ToString() : "");
            else
                return "Create/{0}".FormatWith(typeName);
        }

        public virtual string NavigateRoute(Lite<IEntity> lite)
        {
            return NavigateRoute(lite.EntityType, lite.IdOrNull);
        }


        public virtual string GetCurrentUser()
        {
            var element = Selenium.WaitElementPresent(By.CssSelector("a.sf-user, .sf-login"));
            if (element.HasClass("sf-login"))
                return null;
            
            var result = (string)Selenium.ExecuteScript("return $('.sf-user span').text()");

            return result;
        }

        public virtual void Logout()
        {
            Selenium.FindElement(By.CssSelector("a[href$='Auth/Logout']")).ButtonClick();
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

            Selenium.FindElement(By.Id("username")).SafeSendKeys(username);
            Selenium.FindElement(By.Id("password")).SafeSendKeys(password);
            Selenium.FindElement(By.Id("login")).Submit();

            Selenium.Wait(() => GetCurrentUser() != null);
        }
    }
}
