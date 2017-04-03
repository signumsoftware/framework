using System;
using System.Linq;
using OpenQA.Selenium;
using OpenQA.Selenium.Remote;
using Signum.Entities;
using Signum.Utilities;
using Signum.React.Selenium;

namespace Signum.React.Selenium
{
    public class SearchPageProxy : IDisposable
    {
        public RemoteWebDriver Selenium { get; private set; }
        public SearchControlProxy SearchControl { get; private set; }
        public ResultTableProxy Results { get { return SearchControl.Results; } }
        public FiltersProxy Filters { get { return SearchControl.Filters; } }
        public PaginationSelectorProxy Pagination { get { return SearchControl.Pagination; } }

        public SearchPageProxy(RemoteWebDriver selenium)
        {
            this.Selenium = selenium;
            this.SearchControl = new SearchControlProxy(selenium.WaitElementVisible(By.ClassName("sf-search-control")));
        }

        public FrameModalProxy<T> Create<T>() where T : ModifiableEntity
        {
            var popup = SearchControl.CreateButton.Find().CaptureOnClick();

            if (SelectorModalProxy.IsSelector(popup))
                popup = popup.GetDriver().CapturePopup(() => SelectorModalProxy.Select(popup, typeof(T)));

            return new FrameModalProxy<T>(popup);
        }

        public FramePageProxy<T> CreateInTab<T>() where T : ModifiableEntity
        {
            var oldCount = Selenium.WindowHandles.Count;
            
            SearchControl.CreateButton.Find().Click();

            Selenium.Wait(() => Selenium.WindowHandles.Count > oldCount);

            var windowHandles = Selenium.WindowHandles;

            var currentIndex = windowHandles.IndexOf(Selenium.CurrentWindowHandle);

            Selenium.SwitchTo().Window(windowHandles[currentIndex +1]);

            var result = new FramePageProxy<T>(this.Selenium);

            result.OnDisposed += () =>
            {
                Selenium.SwitchTo().Window(windowHandles[currentIndex]);
            };

            return result;
        }


        public void Dispose()
        {
        }

        public void Search()
        {
            this.SearchControl.Search();
        }

        internal SearchPageProxy WaitLoaded()
        {
            this.Selenium.Wait(() => this.SearchControl.SearchButton != null);
            return this;
        }
    }
}
