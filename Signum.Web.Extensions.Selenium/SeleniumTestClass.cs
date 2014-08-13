using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Selenium;
using System.Diagnostics;
using System.Threading;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities.Reflection;
using Signum.Entities;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using System.Linq.Expressions;

namespace Signum.Web.Selenium
{
    [TestClass]
    public class SeleniumTestClass
    {
        protected static ISelenium selenium;
        protected static Process seleniumServerProcess;
        private static bool Cleaned = false;

        public SeleniumTestClass()
        {

        }

        public static void LaunchSelenium()
        {
            try
            {
                seleniumServerProcess = SeleniumExtensions.LaunchSeleniumProcess();
                //Thread.Sleep(5000);
                selenium = SeleniumExtensions.InitializeSelenium();
            }
            catch (Exception)
            {
                MyTestCleanup();
                throw;
            }
        }

        [ClassCleanup]
        public static void MyTestCleanup()
        {
            try
            {
                if (!Cleaned)
                {
                    if (selenium != null)
                    {
                        selenium.Stop();
                        selenium.ShutDownSeleniumServer();
                    }
                    SeleniumExtensions.KillSelenium(seleniumServerProcess);
                }
                Cleaned = true;
            }
            catch (Exception)
            {
                // Ignore errors if unable to close the browser
                throw;
            }
        }

        protected virtual string Url(string url)
        {
            throw new InvalidOperationException("Implement this method returing something like: http://localhost/MyApp/ + url");
        }

      
        public SearchPageProxy SearchPage(object queryName)
        {
            var url = Url(FindRoute(queryName));

            selenium.Open(url);
            selenium.WaitForPageToLoad();

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
                return TypeLogic.TryGetCleanName((Type)queryName) ?? ((Type)queryName).Name;
            }

            return queryName.ToString();
        }


        public NormalPage<T> NormalPage<T>(int id) where T : IdentifiableEntity
        {
            return NormalPage<T>(Lite.Create<T>(id));
        }

        public NormalPage<T> NormalPage<T>() where T : IdentifiableEntity
        {
            var url = Url(NavigateRoute(typeof(T), null));

            return NormalPageUrl<T>(url);
        }

        public NormalPage<T> NormalPage<T>(Lite<T> lite) where T : IdentifiableEntity
        {
            if(lite != null && lite.EntityType != typeof(T))
                throw new InvalidOperationException("Use NormalPage<{0}> instead".Formato(lite.EntityType.Name));
            
            var url = Url(NavigateRoute(lite));

            return NormalPageUrl<T>(url);
        }

        public NormalPage<T> NormalPageUrl<T>(string url) where T : IdentifiableEntity
        {
            selenium.Open(url);
            selenium.WaitForPageToLoad();

            return new NormalPage<T>(selenium, null);
        }

        protected virtual string NavigateRoute(Type type, int? id)
        {
            var typeName = TypeLogic.TypeToName.TryGetC(type) ?? Reflector.CleanTypeName(type);

            if (id.HasValue)
                return "View/{0}/{1}".Formato(typeName, id.HasValue ? id.ToString() : "");
            else
                return "Create/{0}".Formato(typeName);
        }

        protected virtual string NavigateRoute(Lite<IIdentifiable> lite)
        {
            return NavigateRoute(lite.EntityType, lite.IdOrNull);
        }


        public virtual string GetCurrentUser()
        {
            if (selenium.IsElementPresent("jq=.sf-login:visible"))
                return null;

            if (!selenium.IsElementPresent("jq=a.sf-user"))
                throw new InvalidOperationException("No login or logout button found");

            var result = selenium.GetEval("window.$('.sf-user span').text()");

            return result;
        }

        public virtual void Logout()
        {
            selenium.Click("jq=a[href$='Auth/Logout']");
            selenium.WaitForPageToLoad();
            selenium.Open(Url("Auth/Login"));
            selenium.WaitForPageToLoad();
        }

        public virtual void Login(string username, string password)
        {
            selenium.Open(Url("Auth/Login"));
            selenium.WaitForPageToLoad();

            var currentUser = GetCurrentUser();
            if (currentUser == username)
                return;

            if (currentUser.HasText())
                Logout();

            selenium.Type("username", username);
            selenium.Type("password", password);
            selenium.Submit("jq=form");

            selenium.WaitForPageToLoad();
        }
    }
}
