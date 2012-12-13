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

        protected virtual string FindRoute(Type tipo)
        {
            return "Find/" + (TypeLogic.TypeToName.TryGetC(tipo) ?? Reflector.CleanTypeName(tipo));
        }

        protected virtual string ViewRoute(Type tipo, int? id)
        {
            return "View/{0}/{1}".Formato(
                TypeLogic.TypeToName.TryGetC(tipo) ?? Reflector.CleanTypeName(tipo),
                id.HasValue ? id.ToString() : "");
        }

        protected virtual string ViewRoute(Lite<IIdentifiable> lite)
        {
            return ViewRoute(lite.EntityType, lite.IdOrNull);
        }
    }
}
