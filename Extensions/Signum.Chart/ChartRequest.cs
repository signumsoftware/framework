using Signum.DynamicQuery.Tokens;
using Signum.UserAssets;
using System.Xml.Linq;

namespace Signum.Chart;

public interface IChartBase
{
    ChartScriptSymbol ChartScript { get; set; }

    ChartScript GetChartScript();
    
    MList<ChartColumnEmbedded> Columns { get; }
    MList<ChartParameterEmbedded> Parameters { get; }

    ChartTimeSeriesEmbedded? ChartTimeSeries { get; }

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

    public int? MaxRows { get; set; }

    public ChartTimeSeriesEmbedded? ChartTimeSeries { get; set; }

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
            allTokens.AddRange(Filters.SelectMany(a => a.GetAllFilters()).SelectMany(f => f.GetTokens()));
        
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

public class ChartTimeSeriesEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 100)]
    public string? StartDate { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? EndDate { get; set; }

    public TimeSeriesUnit? TimeSeriesUnit { get; set; }

    [NumberIsValidator(ComparisonType.GreaterThan, 0)]
    public int? TimeSeriesStep { get; set; }

    [NumberIsValidator(ComparisonType.GreaterThan, 0)]
    public int? TimeSeriesMaxRowsPerStep { get; set; }

    internal ChartTimeSeriesEmbedded? FromXml(XElement xml)
    {
        StartDate = xml.Attribute("StartDate")?.Value;
        EndDate = xml.Attribute("EndDate")?.Value;
        TimeSeriesUnit = xml.Attribute("TimeSeriesUnit")?.Value.ToEnum<TimeSeriesUnit>();
        TimeSeriesStep = xml.Attribute("TimeSeriesStep")?.Value.ToInt();
        TimeSeriesMaxRowsPerStep = xml.Attribute("TimeSeriesMaxRowsPerStep")?.Value.ToInt();
        return this;
    }

    internal XElement ToXml()
    {
        return new XElement("SystemTime",
            StartDate == null ? null : new XAttribute("StartDate", StartDate),
            EndDate == null ? null : new XAttribute("EndDate", EndDate),
            TimeSeriesUnit == null ? null : new XAttribute("TimeSeriesUnit", TimeSeriesUnit.ToString()!),
            TimeSeriesStep == null ? null : new XAttribute("TimeSeriesStep", TimeSeriesStep.ToString()!),
            TimeSeriesMaxRowsPerStep == null ? null : new XAttribute("TimeSeriesMaxRowsPerStep", TimeSeriesMaxRowsPerStep.ToString()!)
        );
    }

    internal SystemTimeRequest ToSystemTimeRequest() => new SystemTimeRequest
    {
        mode = SystemTimeMode.TimeSeries,
        joinMode = SystemTimeJoinMode.AllCompatible,
        endDate = ParseDate(this.EndDate),
        startDate = ParseDate(this.StartDate),
        timeSeriesStep = this.TimeSeriesStep,
        timeSeriesUnit = this.TimeSeriesUnit,
        timeSeriesMaxRowsPerStep = this.TimeSeriesMaxRowsPerStep,
    };

    DateTime? ParseDate(string? date)
    {
        if (date.IsNullOrEmpty())
            return null;


        return (DateTime)FilterValueConverter.Parse(date, typeof(DateTime), false)!;
    }
}
