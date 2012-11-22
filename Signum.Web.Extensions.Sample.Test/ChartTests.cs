using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Web.Selenium;
using Selenium;
using System.Diagnostics;
using Signum.Engine;
using Signum.Entities.Authorization;
using Signum.Test;
using Signum.Web.Extensions.Sample.Test.Properties;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Signum.Utilities;
using Signum.Entities.Chart;
using System.Text.RegularExpressions;

namespace Signum.Web.Extensions.Sample.Test
{
    [TestClass]
    public class ChartTests : Common
    {
        public ChartTests()
        {

        }

        [ClassInitialize]
        public static void ClassInitialize(TestContext testContext)
        {
            Common.Start();
        }

        [ClassCleanup]
        public static void ClassCleanup()
        {
            Common.MyTestCleanup();
        }

        void OpenChart()
        {
            selenium.Click("jq=#qbChartNew");
            selenium.WaitForPageToLoad(PageLoadTimeout);
        }

        void Draw()
        {
            selenium.Click("jq=#qbDraw");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#tblResults"));
        }

        [TestMethod]
        public void Chart001_Open_Filter_Draw_CreateUC()
        {
            CheckLoginAndOpen(FindRoute("Album"));

            //add filter of simple value
            selenium.FilterSelectToken(0, "label=Id", false);
            selenium.AddFilter(0);
            selenium.FilterSelectOperation(0, "value=GreaterThan");
            selenium.Type("value_0", "1");

            OpenChart();

            //filter is maintained
            Assert.IsTrue(selenium.IsElementPresent("jq=#value_0[value=1]"));

            //set chart tokens
            selenium.FilterSelectToken(0, "value=Author", true, "Columns_0_");
            selenium.FilterSelectToken(0, "value=Count", false, "Columns_1_");

            Draw();
            //3 filas
            selenium.WaitAjaxFinished(() => 
                selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 3)) && 
                !selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 4)));                

            //add aggregate filter
            selenium.FilterSelectToken(0, "value=Count", false);
            selenium.AddFilter(1);
            selenium.FilterSelectOperation(1, "value=GreaterThan");
            selenium.Type("value_1", "2");

            //check aggregate filter present with dot dot, but do not add
            selenium.FilterSelectToken(0, "value=Id", true);
            selenium.FilterSelectToken(1, "value=Average", false);

            Draw();
            //2 filas
            selenium.WaitAjaxFinished(() =>
                selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 2)) &&
                !selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 3)));       

            //Sort
            selenium.Click("jq=a[href='#sfChartData']");
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("jq=#sfChartData:visible"));
            selenium.Sort(1, true);
            selenium.WaitAjaxFinished(() => selenium.IsElementPresent("{0}:contains('Michael')".Formato(SearchTestExtensions.RowSelector(selenium, 1))));

            //Create User Chart
            selenium.QueryMenuOptionClick("tmUserCharts", "qbUserChartNew");
            selenium.WaitForPageToLoad(PageLoadTimeout);

            string userChartName = "uctest" + DateTime.Now.Ticks.ToString();
            selenium.Type("DisplayName", userChartName);

            Assert.IsTrue(selenium.IsElementPresent("jq=#Filters_1_sfRuntimeInfo"));
            Assert.IsTrue(selenium.IsElementPresent("jq=#Orders_0_sfRuntimeInfo"));

            selenium.EntityOperationClick(UserChartOperation.Save);
            selenium.WaitForPageToLoad(PageLoadTimeout);

            //Load user chart
            selenium.Open(FindRoute("Album"));
            selenium.WaitForPageToLoad(PageLoadTimeout);
            OpenChart();

            selenium.Click(SearchTestExtensions.QueryMenuOptionLocatorByAttr("tmUserCharts", "title=" + userChartName));
            selenium.WaitForPageToLoad(PageLoadTimeout);

            Draw();
            //2 filas
            selenium.WaitAjaxFinished(() =>
                selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 2)) &&
                !selenium.IsElementPresent(SearchTestExtensions.RowSelector(selenium, 3)));
            //check order was set
            Assert.IsTrue(selenium.IsElementPresent("{0}:contains('Michael')".Formato(SearchTestExtensions.RowSelector(selenium, 1))));

            selenium.QueryMenuOptionClick("tmUserCharts", "qbUserChartEdit");
            selenium.WaitForPageToLoad(PageLoadTimeout);

            selenium.EntityOperationClick(UserChartOperation.Delete);
            Assert.IsTrue(Regex.IsMatch(selenium.GetConfirmation(), ".*"));
            selenium.WaitForPageToLoad(PageLoadTimeout);
            Assert.IsTrue(selenium.IsElementPresent(SearchTestExtensions.SearchSelector("")));
        }
    }
}
