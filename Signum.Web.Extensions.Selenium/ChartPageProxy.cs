using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Entities.Chart;
using Signum.Utilities;
using Signum.Web.Selenium;

namespace Signum.Web.Selenium
{
    public class ChartPageProxy : IDisposable
    {
        public RemoteWebDriver Selenium { get; private set; }

        public ResultTableProxy Results { get ; private set; }

        public FiltersProxy Filters { get; private set; }

        public QueryTokenBuilderProxy GetColumnTokenBuilder(int index)
        {
            return new QueryTokenBuilderProxy(this.Selenium, "Columns_{0}_Token_".FormatWith(index)); 
        }

        public ChartPageProxy(RemoteWebDriver selenium)
        {
            this.Selenium = selenium;
            this.Results = new ResultTableProxy(selenium, null, a => a(), hasDataEntity: false);
            this.Filters = new FiltersProxy(selenium, null); 
        }

        public By DrawButtonLocator
        {
            get { return By.CssSelector("#qbDraw"); }
        }

        public void Draw()
        {
            Selenium.FindElement(DrawButtonLocator).Click(); 
        }

        public By DataTabLocator
        {
            get { return By.CssSelector("a[href='#sfChartData']"); }
        }

        public void DataTab()
        {
            Selenium.FindElement(DataTabLocator).Click();
            Selenium.WaitElementVisible(Results.ResultTableLocator);
        }

        public By ChartTabLocator
        {
            get { return By.CssSelector("a[href='#sfChartContainer']"); }
        }

        public void ChartTab()
        {
            Selenium.FindElement(ChartTabLocator).Click();
            Selenium.WaitElementVisible(ChartContianerLocator);
        }

        public By ChartContianerLocator
        {
            get { return By.CssSelector("#sf-chart-container"); }
        }

        public By MenuOptionLocator(string menuId, string optionId)
        {
            return By.CssSelector("a#{0}".FormatWith(optionId));
        }

        public By MenuOptionLocatorByAttr(string menuId, string optionLocator)
        {
            return By.CssSelector("a[{0}]".FormatWith(optionLocator));
        }

        public NormalPage<UserChartEntity> NewUserChart()
        {
            Selenium.FindElement(MenuOptionLocator("tmUserCharts", "qbUserChartNew")).ButtonClick();
            return new NormalPage<UserChartEntity>(Selenium).WaitLoaded(); 
        }

        public void SelectUserChart(string userChartName)
        {
            Selenium.FindElement(MenuOptionLocatorByAttr("tmUserCharts", "title=" + userChartName)).ButtonClick();
            this.WaitLoaded();
        }

        public NormalPage<UserChartEntity> EditUserChart()
        {
            Selenium.FindElement(MenuOptionLocator("tmUserCharts", "qbUserChartEdit")).ButtonClick();
            return new NormalPage<UserChartEntity>(Selenium).WaitLoaded(); 
        }

        public void Dispose()
        {
        }


        public ChartPageProxy WaitLoaded()
        {
            this.Selenium.WaitElementPresent(DrawButtonLocator);
            return this;
        }
    }

    public static class SearchControlChartExtensions
    {
        public static ChartPageProxy OpenChart(this SearchControlProxy searchControl)
        {
            searchControl.Selenium.FindElement(By.CssSelector("#qbChartNew")).Click();
            return new ChartPageProxy(searchControl.Selenium).WaitLoaded();
        }
    }
}
