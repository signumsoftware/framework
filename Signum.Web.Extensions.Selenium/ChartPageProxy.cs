using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Entities.Chart;
using Signum.Utilities;
using Signum.Web.Selenium;

namespace Signum.Web.Selenium
{
    public class ChartPageProxy : IDisposable
    {
        public ISelenium Selenium { get; private set; }

        public ResultTableProxy Results { get ; private set; }

        public FiltersProxy Filters { get; private set; }

        public QueryTokenBuilderProxy GetColumnTokenBuilder(int index)
        {
            return new QueryTokenBuilderProxy(this.Selenium, "Columns_{0}_Token_".Formato(index)); 
        }

        public ChartPageProxy(ISelenium selenium)
        {
            this.Selenium = selenium;
            this.Results = new ResultTableProxy(selenium, null, a => a(), hasDataEntity: false);
            this.Filters = new FiltersProxy(selenium, null); 
        }

        public string DrawButtonLocator
        {
            get { return "jq=#qbDraw"; }
        }

        public void Draw()
        {
            Selenium.Click(DrawButtonLocator); 
        }

      

        public string DataTabLocator
        {
            get { return "jq=a[href='#sfChartData']"; }
        }

        public void DataTab()
        {
            Selenium.Click(DataTabLocator);
            Selenium.WaitElementPresent(Results.ResultTableLocator);
        }

        public string ChartTabLocator
        {
            get { return "jq=a[href='#sfChartContainer']"; }
        }

        public void ChartTab()
        {
            Selenium.Click(ChartTabLocator);
            Selenium.WaitElementPresent(ChartContianerLocator);
        }

        public string ChartContianerLocator
        {
            get { return "jq=#sf-chart-container"; }
        }

        public string MenuOptionLocator(string menuId, string optionId)
        {
            return "jq=a#{0}".Formato(optionId);
        }

        public string MenuOptionLocatorByAttr(string menuId, string optionLocator)
        {
            return "jq=a[{0}]".Formato(optionLocator);
        }

        public NormalPage<UserChartDN> NewUserChart()
        {
            Selenium.MouseUp(MenuOptionLocator("tmUserCharts", "qbUserChartNew"));
            Selenium.WaitForPageToLoad();
            return new NormalPage<UserChartDN>(Selenium); 
        }

        public void SelectUserChart(string userChartName)
        {
            Selenium.Click(MenuOptionLocatorByAttr("tmUserCharts", "title=" + userChartName));
            Selenium.WaitForPageToLoad();
        }

        public NormalPage<UserChartDN> EditUserChart()
        {
            Selenium.Click(MenuOptionLocator("tmUserCharts", "qbUserChartEdit"));
            Selenium.WaitForPageToLoad();
            return new NormalPage<UserChartDN>(Selenium); 
        }

        public void Dispose()
        {
        }

    }

    public static class SearchControlChartExtensions
    {
        public static ChartPageProxy OpenChart(this SearchControlProxy searchControl)
        {
            searchControl.Selenium.MouseUp("jq=#qbChartNew");
            searchControl.Selenium.WaitForPageToLoad();
            return new ChartPageProxy(searchControl.Selenium);
        }
    }
}
