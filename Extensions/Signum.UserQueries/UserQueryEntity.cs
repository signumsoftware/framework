using Microsoft.AspNetCore.Mvc;
using Signum.Basics;
using Signum.Dashboard;
using Signum.DynamicQuery;
using Signum.Omnibox;
using Signum.UserAssets;
using Signum.UserAssets.Queries;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;

namespace Signum.UserQueries;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class UserQueryEntity : Entity, IUserAssetEntity, IHasEntityType
{
    public UserQueryEntity()
    {
        this.BindParent();
    }

    public UserQueryEntity(object queryName) : this()
    {
        this.queryName = queryName;
    }

    [Ignore]
    internal object queryName;


    public QueryEntity Query { get; set; }

    public bool GroupResults { get; set; }

    public Lite<TypeEntity>? EntityType { get; set; }

    public bool HideQuickLink { get; set; }

    public bool? IncludeDefaultFilters { get; set; }

    public Lite<Entity>? Owner { get; set; }

    [StringLengthValidator(Min = 1, Max = 200)]
    public string DisplayName { get; set; }

    public bool AppendFilters { get; set; }

    public RefreshMode RefreshMode { get; set; } = RefreshMode.Auto;

    [PreserveOrder, BindParent]
    public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

    [PreserveOrder]
    public MList<QueryOrderEmbedded> Orders { get; set; } = new MList<QueryOrderEmbedded>();

    public ColumnOptionsMode ColumnsMode { get; set; }

    [PreserveOrder]
    public MList<QueryColumnEmbedded> Columns { get; set; } = new MList<QueryColumnEmbedded>();

    public PaginationMode? PaginationMode { get; set; }

    [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 1)]
    public int? ElementsPerPage { get; set; }

    public SystemTimeEmbedded? SystemTime { get; set; }

    public HealthCheckEmbedded? HealthCheck { get; set; }

    [PreserveOrder, NoRepeatValidator]
    [ImplementedBy(typeof(UserQueryEntity))]
    public MList<Lite<Entity>> CustomDrilldowns { get; set; } = new MList<Lite<Entity>>();

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => DisplayName);

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(ElementsPerPage))
        {
            return (pi, ElementsPerPage).IsSetOnlyWhen(PaginationMode == DynamicQuery.PaginationMode.Firsts || PaginationMode == DynamicQuery.PaginationMode.Paginate);
        }

        return base.PropertyValidation(pi);
    }

    [HiddenProperty]
    public bool ShouldHaveElements
    {
        get
        {
            return PaginationMode == Signum.DynamicQuery.PaginationMode.Firsts ||
                PaginationMode == Signum.DynamicQuery.PaginationMode.Paginate;
        }
    }



    internal void ParseData(QueryDescription description)
    {
        var canAggregate = this.GroupResults ? SubTokensOptions.CanAggregate : 0;
        var canTimeSeries = this.SystemTime?.Mode == SystemTimeMode.TimeSeries ? SubTokensOptions.CanTimeSeries : 0;

        foreach (var f in Filters)
            f.ParseData(this, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate | canTimeSeries);

        foreach (var c in Columns)
            c.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanSnippet | SubTokensOptions.CanToArray | (canAggregate != 0 ? canAggregate : SubTokensOptions.CanOperation | SubTokensOptions.CanManual) | canTimeSeries);

        foreach (var o in Orders)
            o.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanSnippet | canAggregate);
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("UserQuery",
            new XAttribute("Guid", Guid),
            new XAttribute("DisplayName", DisplayName),
            new XAttribute("Query", Query.Key),
            EntityType == null ? null : new XAttribute("EntityType", ctx.RetrieveLite(EntityType).CleanName),
            Owner == null ? null : new XAttribute("Owner", Owner.KeyLong()),
            !HideQuickLink ? null : new XAttribute("HideQuickLink", HideQuickLink),
            IncludeDefaultFilters == null ? null : new XAttribute("IncludeDefaultFilters", IncludeDefaultFilters.Value),
            !AppendFilters ? null : new XAttribute("AppendFilters", AppendFilters),
            RefreshMode == RefreshMode.Auto ? null : new XAttribute("RefreshMode", RefreshMode.ToString()),
            !GroupResults ? null : new XAttribute("GroupResults", GroupResults),
            ElementsPerPage == null ? null : new XAttribute("ElementsPerPage", ElementsPerPage),
            PaginationMode == null ? null : new XAttribute("PaginationMode", PaginationMode),
            new XAttribute("ColumnsMode", ColumnsMode),
            Filters.IsNullOrEmpty() ? null : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
            Columns.IsNullOrEmpty() ? null : new XElement("Columns", Columns.Select(c => c.ToXml(ctx)).ToList()),
            Orders.IsNullOrEmpty() ? null : new XElement("Orders", Orders.Select(o => o.ToXml(ctx)).ToList()),
            SystemTime?.ToXml(),
            CustomDrilldowns.IsNullOrEmpty() ? null : new XElement("CustomDrilldowns", CustomDrilldowns.Select(d => new XElement("CustomDrilldown", ctx.Include((Lite<IUserAssetEntity>)d))).ToList()));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        Query = ctx.GetQuery(element.Attribute("Query")!.Value);
        DisplayName = element.Attribute("DisplayName")!.Value;
        EntityType = element.Attribute("EntityType")?.Let(a => ctx.GetType(a.Value).ToLite());
        Owner = element.Attribute("Owner")?.Let(a => ctx.ParseLite(a.Value, this, uq => uq.Owner))!;
        HideQuickLink = element.Attribute("HideQuickLink")?.Let(a => bool.Parse(a.Value)) ?? false;
        IncludeDefaultFilters = element.Attribute("IncludeDefaultFilters")?.Let(a => bool.Parse(a.Value));
        AppendFilters = element.Attribute("AppendFilters")?.Let(a => bool.Parse(a.Value)) ?? false;
        RefreshMode = element.Attribute("RefreshMode")?.Let(a => a.Value.ToEnum<RefreshMode>()) ?? RefreshMode.Auto;
        GroupResults = element.Attribute("GroupResults")?.Let(a => bool.Parse(a.Value)) ?? false;
        ElementsPerPage = element.Attribute("ElementsPerPage")?.Let(a => int.Parse(a.Value));
        PaginationMode = element.Attribute("PaginationMode")?.Let(a => a.Value.ToEnum<PaginationMode>());
        ColumnsMode = element.Attribute("ColumnsMode")!.Value.Let(cm => cm == "Replace" ? "ReplaceAll" : cm).ToEnum<ColumnOptionsMode>();

        var valuePr = PropertyRoute.Construct((UserQueryEntity wt) => wt.Filters[0].ValueString);
        Filters.Synchronize(element.Element("Filters")?.Elements().ToList(), (f, x) => f.FromXml(x, ctx, this, valuePr));
        Columns.Synchronize(element.Element("Columns")?.Elements().ToList(), (c, x) => c.FromXml(x, ctx));
        Orders.Synchronize(element.Element("Orders")?.Elements().ToList(), (o, x) => o.FromXml(x, ctx));
        CustomDrilldowns.Synchronize((element.Element("CustomDrilldowns")?.Elements("CustomDrilldown")).EmptyIfNull().Select(x => (Lite<Entity>)ctx.GetEntity(Guid.Parse(x.Value)).ToLiteFat()).NotNull().ToMList());
        SystemTime = element.Element("SystemTime")?.Let(xml => (SystemTime ?? new SystemTimeEmbedded()).FromXml(xml));
        ParseData(ctx.GetQueryDescription(Query));
    }

    public Pagination? GetPagination()
    {
        switch (PaginationMode)
        {
            case Signum.DynamicQuery.PaginationMode.All: return new Pagination.All();
            case Signum.DynamicQuery.PaginationMode.Firsts: return new Pagination.Firsts(ElementsPerPage!.Value);
            case Signum.DynamicQuery.PaginationMode.Paginate: return new Pagination.Paginate(ElementsPerPage!.Value, 1);
            default: return null;
        }
    }
}

public class SystemTimeEmbedded : EmbeddedEntity
{
    public SystemTimeMode Mode { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? StartDate { get; set; }

    [StringLengthValidator(Max = 100)]
    public string? EndDate { get; set; }

    public SystemTimeJoinMode? JoinMode { get; set; }

    public TimeSeriesUnit? TimeSeriesUnit { get; set; }

    [NumberIsValidator(ComparisonType.GreaterThan, 0)]
    public int? TimeSeriesStep { get; set; }

    [NumberIsValidator(ComparisonType.GreaterThan, 0)]
    public int? TimeSeriesMaxRowsPerStep { get; set; }

    public bool SplitQueries { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        return stateValidator.Validate(this, pi) ??  base.PropertyValidation(pi);
    }

    static StateValidator<SystemTimeEmbedded, SystemTimeMode> stateValidator = new StateValidator<SystemTimeEmbedded, SystemTimeMode>
        (a => a.Mode, a => a.StartDate, a => a.EndDate, a => a.JoinMode, a => a.TimeSeriesUnit, a => a.TimeSeriesStep, a => a.TimeSeriesMaxRowsPerStep)
    {
 { SystemTimeMode.AsOf,       true,          false,            false,          false,                false,                false  },
 { SystemTimeMode.Between,    true,          true,             true,           false,                false,                false  },
 { SystemTimeMode.ContainedIn,true,          true,             true,           false,                false,                false  },
 { SystemTimeMode.All,        false,         false,            true,           false,                false,                false  },
 { SystemTimeMode.TimeSeries,  true,          true,             false,          true,                 true,                 true  },


    };

    internal SystemTimeEmbedded? FromXml(XElement xml)
    {
        Mode = xml.Attribute("Mode")!.Value.ToEnum<SystemTimeMode>();
        StartDate = xml.Attribute("StartDate")?.Value;
        EndDate = xml.Attribute("EndDate")?.Value;
        JoinMode = xml.Attribute("JoinMode")?.Value.ToEnum<SystemTimeJoinMode>();
        TimeSeriesUnit = xml.Attribute("TimeSeriesUnit")?.Value.ToEnum<TimeSeriesUnit>();
        TimeSeriesStep = xml.Attribute("TimeSeriesStep")?.Value.ToInt();
        TimeSeriesMaxRowsPerStep = xml.Attribute("TimeSeriesMaxRowsPerStep")?.Value.ToInt();
        SplitQueries = xml.Attribute("SplitQueries")?.Value.ToBool() ?? false;
        return this;
    }

    internal XElement ToXml()
    {
        return new XElement("SystemTime",
            new XAttribute("Mode", Mode.ToString()),
            StartDate == null ? null : new XAttribute("StartDate", StartDate),
            EndDate == null ? null : new XAttribute("EndDate", EndDate),
            JoinMode == null ? null : new XAttribute("JoinMode", JoinMode.ToString()!),
            TimeSeriesUnit == null ? null : new XAttribute("TimeSeriesUnit", TimeSeriesUnit.ToString()!),
            TimeSeriesStep == null ? null : new XAttribute("TimeSeriesStep", TimeSeriesStep.ToString()!),
            TimeSeriesMaxRowsPerStep == null ? null : new XAttribute("TimeSeriesMaxRowsPerStep", TimeSeriesMaxRowsPerStep.ToString()!),
            SplitQueries  == false ? null : new XAttribute("SplitQueries", SplitQueries.ToString()!)
        );
    }

    internal SystemTimeRequest ToSystemTimeRequest() => new SystemTimeRequest
    {
        mode = this.Mode,
        joinMode = this.JoinMode,
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

public class UserQueryLiteModel : ModelEntity
{
    public string DisplayName { get; set; }
    public QueryEntity Query { get; set; }
    public bool HideQuickLink { get; set; }

    internal static UserQueryLiteModel Translated(UserQueryEntity uq) => new UserQueryLiteModel
    {
        DisplayName = uq.TranslatedField(a => a.DisplayName),
        Query = uq.Query,
        HideQuickLink = uq.HideQuickLink,
    };

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => DisplayName);
}

[AutoInit]
public static class UserQueryPermission
{
    public static PermissionSymbol ViewUserQuery;
}

[AutoInit]
public static class UserQueryOperation
{
    public static ExecuteSymbol<UserQueryEntity> Save;
    public static DeleteSymbol<UserQueryEntity> Delete;
}



[EntityKind(EntityKind.Part, EntityData.Master)]
public class ValueUserQueryListPartEntity : Entity, IPartEntity
{
    public MList<ValueUserQueryElementEmbedded> UserQueries { get; set; } = new MList<ValueUserQueryElementEmbedded>();

    public override string ToString()
    {
        return "{0} {1}".FormatWith(UserQueries.Count, typeof(UserQueryEntity).NicePluralName());
    }

    public bool RequiresTitle
    {
        get { return true; }
    }

    public IPartEntity Clone() => new ValueUserQueryListPartEntity
    {
        UserQueries = this.UserQueries.Select(e => e.Clone()).ToMList(),
    };

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ValueUserQueryListPart",
            UserQueries.Select(cuqe => cuqe.ToXml(ctx)));
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        UserQueries.Synchronize(element.Elements().ToList(), (cuqe, x) => cuqe.FromXml(x, ctx));
    }
}

public class ValueUserQueryElementEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 200)]
    public string? Label { get; set; }

    public UserQueryEntity UserQuery { get; set; }

    public bool IsQueryCached { get; set; }

    [StringLengthValidator(Max = 200)]
    public string? Href { get; set; }

    public ValueUserQueryElementEmbedded Clone()
    {
        return new ValueUserQueryElementEmbedded
        {
            Href = this.Href,
            Label = this.Label,
            UserQuery = UserQuery,
            IsQueryCached = this.IsQueryCached,
        };
    }

    internal XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("ValueUserQueryElement",
            Label == null ? null : new XAttribute(nameof(Label), Label),
            Href == null ? null : new XAttribute(nameof(Href), Href),
            IsQueryCached == false ? null : new XAttribute(nameof(IsQueryCached), IsQueryCached),
            new XAttribute("UserQuery", ctx.Include(UserQuery)));
    }

    internal void FromXml(XElement element, IFromXmlContext ctx)
    {
        Label = element.Attribute(nameof(Label))?.Value;
        Href = element.Attribute(nameof(Href))?.Value;
        IsQueryCached = element.Attribute(nameof(IsQueryCached))?.Value.ToBool() ?? false;
        UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute(nameof(UserQuery))!.Value));
    }
}

[EntityKind(EntityKind.Part, EntityData.Master)]
public class BigValuePartEntity : Entity, IPartParseDataEntity
{
    public QueryTokenEmbedded? ValueToken { get; set; }

    public UserQueryEntity? UserQuery { get; set; }

    public bool RequiresTitle => false;

    public string? CustomBigValue { get; set; }

    public bool Navigate { get; set; }

    public string? CustomUrl { get; set; }

    public bool? IsClickable { get; set; }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("BigValuePart",
           UserQuery == null ? null : new XAttribute(nameof(UserQuery), ctx.Include(UserQuery)),
           ValueToken == null ? null : new XAttribute(nameof(ValueToken), ValueToken.Token.FullKey()),
           CustomBigValue == null ? null : new XAttribute(nameof(CustomBigValue), CustomBigValue),
           !Navigate ? null: new XAttribute(nameof(Navigate), Navigate),
           CustomUrl == null ? null : new XAttribute(nameof(CustomUrl), CustomUrl),
           IsClickable == null ? null : new XAttribute(nameof(IsClickable), IsClickable.Value)
           );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        var uq = element.Attribute(nameof(UserQuery))?.Value;
        UserQuery = uq == null ? null : (UserQueryEntity)ctx.GetEntity(Guid.Parse(uq));
        ValueToken = element.Attribute(nameof(ValueToken))?.Let(t => new QueryTokenEmbedded(t.Value));
        CustomBigValue = element.Attribute(nameof(CustomBigValue))?.Value;
        Navigate = element.Attribute(nameof(Navigate))?.Value.ToBool() ?? false;
        CustomUrl = element.Attribute(nameof(CustomUrl))?.Value;
        IsClickable = element.Attribute(nameof(IsClickable))?.Value.ToBool();
    }

    public void ParseData(DashboardEntity dashboardEntity)
    {
        if (ValueToken != null)
        {
            var queryKey = this.UserQuery != null ? this.UserQuery.Query.Key : dashboardEntity.EntityType?.ToString();

            if (queryKey != null)
            {
                var qn = QueryLogic.ToQueryName(queryKey);
                var qd = QueryLogic.Queries.QueryDescription(qn);
                this.ValueToken.ParseData(this, qd, (this.UserQuery != null ? SubTokensOptions.CanElement | SubTokensOptions.CanAggregate : 0));
            }
        }

    }

    public IPartEntity Clone() => new BigValuePartEntity
    {
        ValueToken = ValueToken,
        UserQuery = UserQuery,
        CustomBigValue = CustomBigValue,
        IsClickable = IsClickable
    };

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if(pi.Name == nameof(ValueToken))
        {
            if (ValueToken != null && UserQuery == null && this.GetDashboard().EntityType == null)
                return ValidationMessage._0ShouldBeNull.NiceToString(pi.NiceName());
        }

        if(this.GetDashboard().EntityType == null)
        {
            if (pi.Name == nameof(UserQuery))
            {
                if (UserQuery == null)
                    return ValidationMessage._0IsNotSet.NiceToString(NicePropertyName(() => UserQuery));
            }
        }
        else
        {
            if (pi.Name == nameof(UserQuery) || pi.Name == nameof(ValueToken))
            {
                if (UserQuery == null && ValueToken == null)
                    return ValidationMessage._0Or1ShouldBeSet.NiceToString(NicePropertyName(() => UserQuery), NicePropertyName(() => ValueToken));
            }
        }

       

        return base.PropertyValidation(pi);
    }
}

[EntityKind(EntityKind.Part, EntityData.Master)]
public class UserQueryPartEntity : Entity, IPartEntity
{
    public UserQueryEntity UserQuery { get; set; }

    public bool IsQueryCached { get; set; }

    public AutoUpdate AutoUpdate { get; set; }

    public bool AllowSelection { get; set; }

    public bool ShowFooter { get; set; }

    public bool CreateNew { get; set; } = false;

    public bool AllowMaxHeight { get; set; } = false;

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => UserQuery + "");

    public bool RequiresTitle
    {
        get { return false; }
    }

    public IPartEntity Clone()
    {
        return new UserQueryPartEntity
        {
            UserQuery = this.UserQuery,
            AllowSelection = this.AllowSelection,
            ShowFooter = this.ShowFooter,
            CreateNew = this.CreateNew,
            IsQueryCached = this.IsQueryCached,
            AllowMaxHeight = this.AllowMaxHeight,
        };
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("UserQueryPart",
            new XAttribute(nameof(UserQuery), ctx.Include(UserQuery)),
            new XAttribute(nameof(AllowSelection), AllowSelection),
            ShowFooter ? new XAttribute(nameof(ShowFooter), ShowFooter) : null,
            CreateNew ? new XAttribute(nameof(CreateNew), CreateNew) : null,
            IsQueryCached ? new XAttribute(nameof(IsQueryCached), IsQueryCached) : null,
            AllowMaxHeight ? new XAttribute(nameof(AllowMaxHeight), AllowMaxHeight) : null
            );
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        UserQuery = (UserQueryEntity)ctx.GetEntity(Guid.Parse(element.Attribute("UserQuery")!.Value));
        AllowSelection = element.Attribute(nameof(AllowSelection))?.Value.ToBool() ?? true;
        ShowFooter = element.Attribute(nameof(ShowFooter))?.Value.ToBool() ?? false;
        CreateNew = element.Attribute(nameof(CreateNew))?.Value.ToBool() ?? false;
        IsQueryCached = element.Attribute(nameof(IsQueryCached))?.Value.ToBool() ?? false;
        AllowMaxHeight = element.Attribute(nameof(AllowMaxHeight))?.Value.ToBool() ?? false;
    }
}

public enum AutoUpdate
{
    None,
    InteractionGroup,
    Dashboard,
}


public enum UserQueryPartRenderMode
{
    SearchControl,
    BigValue,
}

public enum UserQueryMessage
{
    Edit,
    [Description("Create")]
    CreateNew,
    [Description("Back to Default")]
    BackToDefault,
    [Description("Apply changes")]
    ApplyChanges,
    [Description("Use {0} to filter current entity")]
    Use0ToFilterCurrentEntity,
    Preview,
    [Description("Makes the {0} available for Custom Drilldowns and in the contextual menu when grouping {1}")]
    MakesThe0AvailableForCustomDrilldownsAndInContextualMenuWhenGrouping0,
    [Description("Makes the {0} available as Quick Link of {1}")]
    MakesThe0AvailableAsAQuickLinkOf1,
    [Description("the selected {0}")]
    TheSelected0,
    Date,
    Pagination,
    [Description("{0} count of {1} is {2} than {3}")]
    _0CountOf1Is2Than3,
}

public class HealthCheckEmbedded : EmbeddedEntity
{
    public HealthCheckConditionEmbedded? FailWhen { get; set; }
    public HealthCheckConditionEmbedded? DegradedWhen { get; set; }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if(pi.Name == nameof(DegradedWhen) && DegradedWhen == null && FailWhen == null)
        {
            return ValidationMessage._0Or1ShouldBeSet.NiceToString(NicePropertyName(() => FailWhen), pi.NiceName());
        }

        return base.PropertyValidation(pi);
    }
}

public class HealthCheckConditionEmbedded : EmbeddedEntity
{
    public FilterOperation Operation { get; set; }

    public int Value { get; set; }

    [Ignore]
    internal Func<int, bool> _CachedPredicate;
}
