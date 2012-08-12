using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using Signum.Utilities;
using System.Linq.Expressions;
using Signum.Utilities.DataStructures;
using Signum.Entities.Extensions.Properties;
using System.ComponentModel;
using Signum.Utilities.Reflection;
using Signum.Entities.Basics;
using Signum.Entities.Reports;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.UserQueries;

namespace Signum.Entities.Chart
{
    public interface IChartBase 
    {
        ChartScriptDN ChartScript { get; }

        bool GroupResults { get; }

        void NotifyChange(bool needNewQuery);
    }

    [Serializable]
    public class ChartRequest : ModelEntity, IChartBase
    {
        public ChartRequest(object queryName)
        {
            this.queryName = queryName;
        }

        object queryName;
        [NotNullValidator]
        public object QueryName
        {
            get { return queryName; }
        }

        ChartScriptDN chartScript;
        [NotNullValidator]
        public ChartScriptDN ChartScript
        {
            get { return chartScript; }
            set
            {
                if (Set(ref chartScript, value, () => ChartScript))
                {
                    chartScript.SyncronizeColumns(columns);
                }
            }
        }

        bool groupResults = true;
        public bool GroupResults
        {
            get { return groupResults; }
            set
            {
                if (Set(ref groupResults, value, () => GroupResults))
                {
                    NotifyAllColumns();
                    NotifyChange(true);
                }
            }
        }

        [NotifyCollectionChanged, ValidateChildProperty]
        MList<ChartColumnDN> columns = new MList<ChartColumnDN>();
        public MList<ChartColumnDN> Columns
        {
            get { return columns; }
            set { Set(ref columns, value, () => Columns); }
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

        public void NotifyChange(bool needNewQuery)
        {
            if (needNewQuery)
                this.NeedNewQuery = true;

            if (ChartRequestChanged != null)
                ChartRequestChanged();
        }

        [NonSerialized]
        bool needNewQuery;
        [AvoidLocalization]
        public bool NeedNewQuery
        {
            get { return needNewQuery; }
            set { Set(ref needNewQuery, value, () => NeedNewQuery); }
        }


        List<Filter> filters = new List<Filter>();
        public List<Filter> Filters
        {
            get { return filters; }
            set { Set(ref filters, value, () => Filters); }
        }

        List<Order> orders = new List<Order>();
        public List<Order> Orders
        {
            get { return orders; }
            set { Set(ref orders, value, () => Orders); }
        }

        public List<QueryToken> AllTokens()
        {
            var allTokens = Columns.Select(a => a.Token).ToList();

            if (Filters != null)
                allTokens.AddRange(Filters.Select(a => a.Token));

            if (Orders != null)
                allTokens.AddRange(Orders.Select(a => a.Token));

            return allTokens;
        }

        public List<CollectionElementToken> Multiplications
        {
            get { return CollectionElementToken.GetElements(AllTokens().ToHashSet()); }
        }
    }
}
