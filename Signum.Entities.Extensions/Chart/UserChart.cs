using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserQueries;
using System.Xml.Linq;
using Signum.Entities.UserAssets;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml.Spreadsheet;

namespace Signum.Entities.Chart;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class UserChartEntity : Entity, IChartBase, IHasEntityType, IUserAssetEntity
{
    public UserChartEntity()
    {
        this.BindParent();
    }

    public UserChartEntity(object queryName) : this()
    {
        this.queryName = queryName;
    }

    [HiddenProperty]
    public object QueryName
    {
        get { return ToQueryName(Query); }
        set { Query = ToQueryEntity(value); }
    }

    [Ignore]
    internal object queryName;


    public QueryEntity Query { get; set; }

    public Lite<TypeEntity>? EntityType { get; set; }

    public bool HideQuickLink { get; set; }

    public Lite<Entity>? Owner { get; set; }

    [StringLengthValidator(Min = 3, Max = 200)]
    public string DisplayName { get; set; }

    public bool? IncludeDefaultFilters { get; set; }

    public int? MaxRows { get; set; }

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

    public ChartScript GetChartScript()
    {
        return ChartRequestModel.GetChartScriptFunc(this.ChartScript);
    }

    [NoRepeatValidator]
    public MList<ChartParameterEmbedded> Parameters { get; set; } = new MList<ChartParameterEmbedded>();

    [BindParent, PreserveOrder]
    public MList<ChartColumnEmbedded> Columns { get; set; } = new MList<ChartColumnEmbedded>();

    [BindParent, PreserveOrder]
    public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

    [NoRepeatValidator, PreserveOrder]
    [ImplementedBy(typeof(UserQueryEntity))]
    public MList<Lite<Entity>> CustomDrilldowns { get; set; } = new MList<Lite<Entity>>();

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => DisplayName);

    internal void ParseData(QueryDescription description)
    {
        foreach (var f in Filters)
            f.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | SubTokensOptions.CanAggregate);

        foreach (var c in Columns)
            c.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate);
    }

    static Func<QueryEntity, object> ToQueryName;
    static Func<object, QueryEntity> ToQueryEntity;

    public static void SetConverters(Func<QueryEntity, object> toQueryName, Func<object, QueryEntity> toQueryEntity)
    {
        ToQueryName = toQueryName;
        ToQueryEntity = toQueryEntity;
    }

    protected override void PostRetrieving(PostRetrievingContext ctx)
    {
        base.PostRetrieving(ctx);

        try
        {
            this.GetChartScript().SynchronizeColumns(this, ctx);
        }
        catch (InvalidOperationException e) when (e.Message.Contains("sealed"))
        {
            throw new InvalidOperationException($"Error Synchronizing columns for '{this}'. Maybe the ChartScript has changed. Consider opening UserChart and saving it again.");
        }
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("UserChart",
            new XAttribute("Guid", Guid),
            new XAttribute("DisplayName", DisplayName),
            new XAttribute("Query", Query.Key),
            EntityType == null ? null! : new XAttribute("EntityType", ctx.RetrieveLite(EntityType).CleanName),
            new XAttribute("HideQuickLink", HideQuickLink),
            Owner == null ? null! : new XAttribute("Owner", Owner.KeyLong()),
            IncludeDefaultFilters == null ? null! : new XAttribute("IncludeDefaultFilters", IncludeDefaultFilters.Value),
            new XAttribute("ChartScript", this.ChartScript.Key),
            MaxRows == null ? null! : new XAttribute("MaxRows", MaxRows.Value),
            Filters.IsNullOrEmpty() ? null! : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
            new XElement("Columns", Columns.Select(f => f.ToXml(ctx)).ToList()),
            Parameters.IsNullOrEmpty() ? null! : new XElement("Parameters", Parameters.Select(f => f.ToXml(ctx)).ToList()),
            CustomDrilldowns.IsNullOrEmpty() ? null! : new XElement("CustomDrilldowns", CustomDrilldowns.Select(d => new XElement("CustomDrilldown", ctx.Include((Lite<IUserAssetEntity>)d))).ToList()));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        DisplayName = element.Attribute("DisplayName")!.Value;
        Query = ctx.GetQuery(element.Attribute("Query")!.Value);
        EntityType = element.Attribute("EntityType")?.Let(a => ctx.GetTypeLite(a.Value));
        HideQuickLink = element.Attribute("HideQuickLink")?.Let(a => bool.Parse(a.Value)) ?? false;
        Owner = element.Attribute("Owner")?.Let(a => ctx.ParseLite(a.Value, this, uc => uc.Owner))!;
        IncludeDefaultFilters = element.Attribute("IncludeDefaultFilters")?.Let(a => bool.Parse(a.Value));
        ChartScript = ctx.ChartScript(element.Attribute("ChartScript")!.Value);
        MaxRows = element.Attribute("MaxRows")?.Let(at => at.Value.ToInt());
        Filters.Synchronize(element.Element("Filters")?.Elements().ToList(), (f, x) => f.FromXml(x, ctx));
        Columns.Synchronize(element.Element("Columns")?.Elements().ToList(), (c, x) => c.FromXml(x, ctx));
        CustomDrilldowns.Synchronize((element.Element("CustomDrilldowns")?.Elements("CustomDrilldown")).EmptyIfNull().Select(x => (Lite<Entity>)ctx.GetEntity(Guid.Parse(x.Value)).ToLite()).NotNull().ToMList());
        var paramsXml = (element.Element("Parameters")?.Elements()).EmptyIfNull().ToDictionary(a => a.Attribute("Name")!.Value);
        Parameters.ForEach(p =>
        {
            var pxml = paramsXml.TryGetC(p.Name);
            if (pxml != null)
                p.FromXml(pxml, ctx);
        });

        ParseData(ctx.GetQueryDescription(Query));
    }

    public void FixParameters(ChartColumnEmbedded chartColumn)
    {
        ChartUtils.FixParameters(this, chartColumn);
    }

    protected override void PreSaving(PreSavingContext ctx)
    {
        Columns.ForEach(c =>
        {
            if (c.Token == null)
            {
                c.OrderByIndex = null;
                c.OrderByType = null;
            }
        });
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Parameters) && Parameters != null && ChartScript != null)
        {
            try
            {
                EnumerableExtensions.JoinStrict(
                    Parameters,
                    this.GetChartScript().AllParameters(),
                    p => p.Name,
                    ps => ps.Name,
                    (p, ps) => new { p, ps }, pi.NiceName());
            }
            catch (Exception e)
            {
                return e.Message;
            }
        }

        return base.PropertyValidation(pi);
    }

}

[AutoInit]
public static class UserChartOperation
{
    public static ExecuteSymbol<UserChartEntity> Save;
    public static DeleteSymbol<UserChartEntity> Delete;
}
