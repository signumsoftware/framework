using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Selenium;
using Signum.Entities.Chart;
using Signum.Entities.ControlPanel;
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
            this.Results = new ResultTableProxy(selenium, null, () => { }, hasDataEntity: false);
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
            return "jq=#{0}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-query-button#{1}".Formato(menuId, optionId);
        }

        public string MenuOptionLocatorByAttr(string menuId, string optionLocator)
        {
            return "jq=#{0}.sf-dropdown ul.sf-menu-button li.ui-menu-item a.sf-query-button[{1}]".Formato(menuId, optionLocator);
        }

        public NormalPage<UserChartDN> NewUserChart()
        {
            Selenium.Click(MenuOptionLocator("tmUserCharts", "qbUserChartNew"));
            Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);
            return new NormalPage<UserChartDN>(Selenium); 
        }

        public void SelectUserChart(string userChartName)
        {
            Selenium.Click(MenuOptionLocatorByAttr("tmUserCharts", "title=" + userChartName));
            Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);
        }

        public NormalPage<UserChartDN> EditUserChart()
        {
            Selenium.Click(MenuOptionLocator("tmUserCharts", "qbUserChartEdit"));
            Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);
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
            searchControl.Selenium.Click("jq=#qbChartNew");
            searchControl.Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);
            return new ChartPageProxy(searchControl.Selenium);
        }

        public static LineContainer<PanelPartDN> CreateNewPart<T>(this ILineContainer<ControlPanelDN> controlPanel, int index)
        {
            controlPanel.Selenium.EntityButtonClick("CreatePart");

            SelectorPopup sp = new SelectorPopup(controlPanel.Selenium);
            controlPanel.Selenium.WaitElementPresent(sp.PopupVisibleLocator);
            sp.Select<T>();
            controlPanel.Selenium.WaitForPageToLoad(SeleniumExtensions.DefaultPageLoadTimeout);
            return controlPanel.EntityRepeater(a => a.Parts).Details<PanelPartDN>(index);
        }
    }
}
