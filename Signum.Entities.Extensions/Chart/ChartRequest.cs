using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;

namespace Signum.Entities.Chart;

public interface IChartBase
{
    ChartScriptSymbol ChartScript { get; set; }

    ChartScript GetChartScript();
    
    MList<ChartColumnEmbedded> Columns { get; }
    MList<ChartParameterEmbedded> Parameters { get; }
    MList<Lite<Entity>> CustomDrilldowns { get; }

    void FixParameters(ChartColumnEmbedded chartColumnEntity);
}

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
                this.GetChartScript().SynchronizeColumns(this, null);
            }
        }
    }

    public static Func<ChartScriptSymbol, ChartScript> GetChartScriptFunc;

    public ChartScript GetChartScript()
    {
        return GetChartScriptFunc(this.ChartScript);
    }

    [BindParent]
    public MList<ChartColumnEmbedded> Columns { get; set; } = new MList<ChartColumnEmbedded>();

    [NoRepeatValidator]
    public MList<ChartParameterEmbedded> Parameters { get; set; } = new MList<ChartParameterEmbedded>();

    [NoRepeatValidator, ImplementedBy(typeof(UserQueryEntity))]
    public MList<Lite<Entity>> CustomDrilldowns { get; set; } = new MList<Lite<Entity>>();

    public int? MaxRows { get; set; }

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
