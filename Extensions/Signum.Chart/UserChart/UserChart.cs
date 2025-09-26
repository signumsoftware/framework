using Signum.Dashboard;
using Signum.UserAssets;
using Signum.UserAssets.Queries;
using Signum.UserQueries;
using System.Xml.Linq;

namespace Signum.Chart.UserChart;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class UserChartEntity : Entity, IChartBase, IHasEntityType, IUserAssetEntity
{
    public UserChartEntity()
    {
        BindParent();
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
    
    public ChartTimeSeriesEmbedded? ChartTimeSeries { get; set; }

    ChartScriptSymbol chartScript;
    public ChartScriptSymbol ChartScript
    {
        get { return chartScript; }
        set
        {
            if (Set(ref chartScript, value))
            {
                GetChartScript().SynchronizeColumns(this, null);
            }
        }
    }

    public ChartScript GetChartScript()
    {
        return ChartRequestModel.GetChartScriptFunc(ChartScript);
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
            f.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll | SubTokensOptions.CanAggregate | (ChartTimeSeries != null ? SubTokensOptions.CanTimeSeries : 0));

        foreach (var c in Columns)
        {            
            c.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate | (ChartTimeSeries != null ? SubTokensOptions.CanTimeSeries : 0));
        }
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
        ((IMListPrivate)this.Columns).ExecutePostRetrieving(ctx);

        try
        {
            GetChartScript().SynchronizeColumns(this, ctx);
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
            EntityType == null ? null : new XAttribute("EntityType", ctx.RetrieveLite(EntityType).CleanName),
            new XAttribute("HideQuickLink", HideQuickLink),
            Owner == null ? null : new XAttribute("Owner", Owner.KeyLong()),
            IncludeDefaultFilters == null ? null : new XAttribute("IncludeDefaultFilters", IncludeDefaultFilters.Value),
            new XAttribute("ChartScript", ChartScript.Key),
            MaxRows == null ? null : new XAttribute("MaxRows", MaxRows.Value),
            Filters.IsNullOrEmpty() ? null : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
            new XElement("Columns", Columns.Select(f => f.ToXml(ctx)).ToList()),
            Parameters.IsNullOrEmpty() ? null : new XElement("Parameters", Parameters.Select(f => f.ToXml(ctx)).ToList()),
            CustomDrilldowns.IsNullOrEmpty() ? null : new XElement("CustomDrilldowns", CustomDrilldowns.Select(d => new XElement("CustomDrilldown", ctx.Include((Lite<IUserAssetEntity>)d))).ToList()));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        DisplayName = element.Attribute("DisplayName")!.Value;
        Query = ctx.GetQuery(element.Attribute("Query")!.Value);
        EntityType = element.Attribute("EntityType")?.Let(a => ctx.GetTypeLite(a.Value));
        HideQuickLink = element.Attribute("HideQuickLink")?.Let(a => bool.Parse(a.Value)) ?? false;
        Owner = element.Attribute("Owner")?.Let(a => ctx.ParseLite(a.Value, this, uc => uc.Owner))!;
        IncludeDefaultFilters = element.Attribute("IncludeDefaultFilters")?.Let(a => bool.Parse(a.Value));
        ChartScript = SymbolLogic<ChartScriptSymbol>.ToSymbol(element.Attribute("ChartScript")!.Value);
        MaxRows = element.Attribute("MaxRows")?.Let(at => at.Value.ToInt());

        var valuePr = PropertyRoute.Construct((UserChartEntity wt) => wt.Filters[0].ValueString);
        Filters.Synchronize(element.Element("Filters")?.Elements().ToList(), (f, x) => f.FromXml(x, ctx, this, valuePr));
        Columns.Synchronize(element.Element("Columns")?.Elements().ToList(), (c, x) => c.FromXml(x, ctx));
        CustomDrilldowns.Synchronize((element.Element("CustomDrilldowns")?.Elements("CustomDrilldown")).EmptyIfNull().Select(x => (Lite<Entity>)ctx.GetEntity(Guid.Parse(x.Value)).ToLiteFat()).NotNull().ToMList());
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
                    GetChartScript().AllParameters(),
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

public class UserChartLiteModel : ModelEntity
{
    public string DisplayName { get; set; }
    public QueryEntity Query { get; set; }
    public bool HideQuickLink { get; set; }

    internal static UserChartLiteModel Translated(UserChartEntity uc) => new UserChartLiteModel
    {
        DisplayName = PropertyRouteTranslationLogic.TranslatedField(uc, d => d.DisplayName),
        HideQuickLink = uc.HideQuickLink,
        Query = uc.Query
    };

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => DisplayName);
}


[EntityKind(EntityKind.Part, EntityData.Master)]
public class UserChartPartEntity : Entity, IPartEntity
{
    public UserChartEntity UserChart { get; set; }

    public bool IsQueryCached { get; set; }

    public bool ShowData { get; set; } = false;

    public bool AllowChangeShowData { get; set; } = false;

    public bool CreateNew { get; set; } = false;

    public bool AutoRefresh { get; set; } = false;

    [Unit("px")]
    public int? MinHeight { get; set; }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => UserChart + "");

    public bool RequiresTitle
    {
        get { return false; }
    }

    public IPartEntity Clone() => new UserChartPartEntity
    {
        UserChart = this.UserChart,
        IsQueryCached = this.IsQueryCached,
        ShowData = this.ShowData,
        AllowChangeShowData = this.AllowChangeShowData,
        CreateNew = this.CreateNew,
        AutoRefresh = this.AutoRefresh,
        MinHeight = this.MinHeight,
    };

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("UserChartPart",
            new XAttribute(nameof(UserChart), ctx.Include(UserChart)),
            ShowData ? new XAttribute(nameof(ShowData), ShowData) : null,
            AllowChangeShowData ? new XAttribute(nameof(AllowChangeShowData), AllowChangeShowData) : null,
            IsQueryCached ? new XAttribute(nameof(IsQueryCached), IsQueryCached) : null,
            CreateNew ? new XAttribute(nameof(CreateNew), CreateNew) : null,
            AutoRefresh ? new XAttribute(nameof(AutoRefresh), AutoRefresh) : null,
            MinHeight.HasValue ? new XAttribute(nameof(MinHeight), MinHeight.Value) : null
            );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        UserChart = (UserChartEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserChart")!.Value));
        ShowData = element.Attribute(nameof(ShowData))?.Value.ToBool() ?? false;
        AllowChangeShowData = element.Attribute(nameof(AllowChangeShowData))?.Value.ToBool() ?? false;
        IsQueryCached = element.Attribute(nameof(IsQueryCached))?.Value.ToBool() ?? false;
        CreateNew = element.Attribute(nameof(CreateNew))?.Value.ToBool() ?? false;
        AutoRefresh = element.Attribute(nameof(AutoRefresh))?.Value.ToBool() ?? false;
        MinHeight = element.Attribute(nameof(MinHeight))?.Value.ToInt();
    }
}

[EntityKind(EntityKind.Part, EntityData.Master)]
public class CombinedUserChartPartEntity : Entity, IPartEntity
{
    [PreserveOrder, NoRepeatValidator]
    public MList<CombinedUserChartElementEmbedded> UserCharts { get; set; } = new MList<CombinedUserChartElementEmbedded>();

    public bool ShowData { get; set; } = false;

    public bool AllowChangeShowData { get; set; } = false;

    public bool CombinePinnedFiltersWithSameLabel { get; set; } = true;

    public bool UseSameScale { get; set; }

    [Unit("px")]
    public int? MinHeight { get; set; }

    public override string ToString()
    {
        return UserCharts.ToString(", ");
    }

    public bool RequiresTitle
    {
        get { return true; }
    }

    public IPartEntity Clone() => new CombinedUserChartPartEntity
    {
        UserCharts = this.UserCharts.Select(a => a.Clone()).ToMList(),
        ShowData = ShowData,
        AllowChangeShowData = AllowChangeShowData,
        CombinePinnedFiltersWithSameLabel = CombinePinnedFiltersWithSameLabel,
        UseSameScale = UseSameScale
    };

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("CombinedUserChartPart",
            ShowData ? new XAttribute(nameof(ShowData), ShowData) : null,
            AllowChangeShowData ? new XAttribute(nameof(AllowChangeShowData), AllowChangeShowData) : null,
            CombinePinnedFiltersWithSameLabel ? new XAttribute(nameof(CombinePinnedFiltersWithSameLabel), CombinePinnedFiltersWithSameLabel) : null,
            UseSameScale ? new XAttribute(nameof(UseSameScale), UseSameScale) : null,
            MinHeight.HasValue ? new XAttribute(nameof(MinHeight), MinHeight) : null,
            UserCharts.Select(uc => new XElement("UserChart",
                new XAttribute("Guid", ctx.Include(uc.UserChart)),
                uc.IsQueryCached ? new XAttribute(nameof(uc.IsQueryCached), uc.IsQueryCached) : null))
        );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        ShowData = element.Attribute(nameof(ShowData))?.Value.ToBool() ?? false;
        AllowChangeShowData = element.Attribute(nameof(AllowChangeShowData))?.Value.ToBool() ?? false;
        CombinePinnedFiltersWithSameLabel = element.Attribute(nameof(CombinePinnedFiltersWithSameLabel))?.Value.ToBool() ?? false;
        UseSameScale = element.Attribute(nameof(UseSameScale))?.Value.ToBool() ?? false;
        MinHeight = element.Attribute(nameof(MinHeight))?.Value.ToInt();
        UserCharts.Synchronize(element.Elements("UserChart").ToList(), (cuce, elem) =>
        {
            cuce.UserChart = (UserChartEntity)ctx.GetEntity(Guid.Parse(elem.Attribute("Guid")!.Value));
            cuce.IsQueryCached = elem.Attribute(nameof(cuce.IsQueryCached))?.Value.ToBool() ?? false;
        });
    }
}

public class CombinedUserChartElementEmbedded : EmbeddedEntity
{
    public UserChartEntity UserChart { get; set; }

    public bool IsQueryCached { get; set; }

    internal CombinedUserChartElementEmbedded Clone() => new CombinedUserChartElementEmbedded
    {
        UserChart = UserChart,
        IsQueryCached = IsQueryCached,
    };
}
