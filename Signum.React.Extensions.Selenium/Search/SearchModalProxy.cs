using OpenQA.Selenium;
using Signum.Entities;
using Signum.Entities.DynamicQuery;

namespace Signum.React.Selenium
{
    public class SearchModalProxy : ModalProxy
    {
        public SearchControlProxy SearchControl { get; private set; }
        public ResultTableProxy Results { get { return SearchControl.Results; } }
        public FiltersProxy Filters { get { return SearchControl.Filters; } }
        public PaginationSelectorProxy Pagination { get { return SearchControl.Pagination; } }

        public SearchModalProxy(IWebElement element)
            : base(element)
        {
            this.SearchControl = new SearchControlProxy(element);
        }

        public void SelectLite(Lite<IEntity> lite)
        {
            if (!this.SearchControl.FiltersVisible)
                this.SearchControl.ToggleFilters(true);

            this.SearchControl.Filters.AddFilter("Id", FilterOperation.EqualTo, lite.Id);

            this.SearchControl.Search();

            this.SearchControl.Results.SelectRow(lite);

            this.OkWaitClosed();

            this.Dispose();
        }

        public void SelectByPosition(int rowIndex)
        {
            this.SearchControl.Results.SelectRow(rowIndex);

            this.OkWaitClosed();

            this.Dispose();
        }

        public void SelectByPositionOrderById(int rowIndex)
        {
            this.Results.OrderBy("Id");

            this.SearchControl.Results.SelectRow(rowIndex);

            this.OkWaitClosed();

            this.Dispose();
        }

        public void SelectById(PrimaryKey id)
        {
            this.SearchControl.Filters.AddFilter("Id", FilterOperation.EqualTo, id);
            this.SearchControl.Search();
            this.Results.SelectRow(0);

            this.OkWaitClosed();

            this.Dispose();
        }

        public void SelectByPosition(params int[] rowIndexes)
        {
            this.SearchControl.Search();

            foreach (var index in rowIndexes)
                this.SearchControl.Results.SelectRow(index);

            this.OkWaitClosed();

            this.Dispose();
        }

        public FrameModalProxy<T> Create<T>() where T : ModifiableEntity
        {
            return SearchControl.Create<T>();
        }

        public void Search()
        {
            this.SearchControl.Search();
        }
    }
}
