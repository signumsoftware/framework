using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Entities.Chart
{
    public interface IChartBase
    {
        ChartScriptSymbol ChartScript { get; set; }

        ChartScript GetChartScript();
        
        MList<ChartColumnEmbedded> Columns { get; }
        MList<ChartParameterEmbedded> Parameters { get; }

        void InvalidateResults(bool needNewQuery);

        bool Invalidator { get; }

        void FixParameters(ChartColumnEmbedded chartColumnEntity);
    }

    [Serializable, InTypeScript(Undefined = false)]
    public class ChartRequestModel : ModelEntity, IChartBase
    {
        private ChartRequestModel()
        {
        }

        public ChartRequestModel(object queryName)
        {
            this.queryName = queryName;
        }

        object queryName;
        [InTypeScript(false)]
        public object QueryName
        {
            get { return queryName; }
            set { queryName = value; }
        }

        ChartScriptSymbol chartScript;
        public ChartScriptSymbol ChartScript
        {
            get { return chartScript; }
            set
            {
                if (Set(ref chartScript, value))
                {
                    var newQuery = this.GetChartScript().SynchronizeColumns(this);
                    NotifyAllColumns();
                    InvalidateResults(newQuery);
                }
            }
        }

        public static Func<ChartScriptSymbol, ChartScript> GetChartScriptFunc;

        public ChartScript GetChartScript()
        {
            return GetChartScriptFunc(this.ChartScript);
        }

        [NotifyCollectionChanged, NotifyChildProperty]
        public MList<ChartColumnEmbedded> Columns { get; set; } = new MList<ChartColumnEmbedded>();

        [NoRepeatValidator]
        public MList<ChartParameterEmbedded> Parameters { get; set; } = new MList<ChartParameterEmbedded>();

        public List<Column> GetQueryColumns()
        {
            return Columns.Where(c => c.Token != null).Select(t => t.CreateColumn()).ToList();
        }

        public List<Order> GetQueryOrders()
        {
            var result = Columns
                .Where(a => a.OrderByIndex != null && a.Token != null)
                .OrderBy(a => a.OrderByType!.Value)
                .Select(o => new Order(o.Token!.Token, o.OrderByType!.Value)).ToList();

            return result;
        }

        void NotifyAllColumns()
        {
            foreach (var item in Columns)
            {
                item.NotifyAll();
            }
        }

        [field: NonSerialized, Ignore]
        public event Action ChartRequestChanged;

        public void InvalidateResults(bool needNewQuery)
        {
            if (needNewQuery)
                this.NeedNewQuery = true;

            ChartRequestChanged?.Invoke();

            Notify(() => Invalidator);
        }

        public bool Invalidator { get { return false; } }

        [NonSerialized]
        bool needNewQuery;
        [HiddenProperty]
        public bool NeedNewQuery
        {
            get { return needNewQuery; }
            set { Set(ref needNewQuery, value); }
        }


        [InTypeScript(false)]
        public List<Filter> Filters { get; set; } = new List<Filter>();
        
        public List<QueryToken> AllTokens()
        {
            var allTokens = Columns.Select(a => a.Token?.Token).NotNull().ToList();

            if (Filters != null)
                allTokens.AddRange(Filters.SelectMany(a => a.GetFilterConditions()).Select(a => a.Token));
            
            return allTokens;
        }

        [InTypeScript(false)]
        public List<CollectionElementToken> Multiplications
        {
            get { return  CollectionElementToken.GetElements(new HashSet<QueryToken>(AllTokens())); }
        }
        
        public void FixParameters(ChartColumnEmbedded chartColumn)
        {
            ChartUtils.FixParameters(this, chartColumn);
        }

        public bool HasAggregates()
        {
            return Filters.Any(a=>a.IsAggregate()) || Columns.Any(a=>a.Token?.Token is AggregateToken);
        }
    }
}
