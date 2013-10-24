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

namespace Signum.Web.Selenium
{
    [TestClass]
    public class SeleniumTestClass
    {
        protected static ISelenium selenium;
        protected static Process seleniumServerProcess;
        private static bool Cleaned = false;

        protected const string PageLoadTimeout = SeleniumExtensions.DefaultPageLoadTimeout;

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

        public SearchPageProxy SearchPage(object queryName)
        {
            var url = Url(FindRoute(queryName));

            selenium.Open(url);
            selenium.WaitForPageToLoad(PageLoadTimeout);

            return new SearchPageProxy(selenium);
        }
        
        protected virtual string ViewRoute(Type type, int? id)
        {
            return "View/{0}/{1}".Formato(
                TypeLogic.TypeToName.TryGetC(type) ?? Reflector.CleanTypeName(type),
                id.HasValue ? id.ToString() : "");
        }

        protected virtual string ViewRoute(Lite<IIdentifiable> lite)
        {
            return ViewRoute(lite.EntityType, lite.IdOrNull);
        }

        public NormalPage<T> NormalPage<T>(int? id = null) where T: IdentifiableEntity
        {
            var url = Url(ViewRoute(typeof(T), id));

            selenium.Open(url);
            selenium.WaitForPageToLoad(PageLoadTimeout);

            return new NormalPage<T>(selenium, null); 
        }
    }
}
