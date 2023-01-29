using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using System.ComponentModel;
using System.Xml.Linq;

namespace Signum.Entities.UserQueries;

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
            return PaginationMode == Signum.Entities.DynamicQuery.PaginationMode.Firsts ||
                PaginationMode == Signum.Entities.DynamicQuery.PaginationMode.Paginate;
        }
    }

    internal void ParseData(QueryDescription description)
    {
        var canAggregate = this.GroupResults ? SubTokensOptions.CanAggregate : 0;

        foreach (var f in Filters)
            f.ParseData(this, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate);

        foreach (var c in Columns)
            c.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanToArray | (canAggregate != 0 ? canAggregate : SubTokensOptions.CanOperation));

        foreach (var o in Orders)
            o.ParseData(this, description, SubTokensOptions.CanElement | canAggregate);
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("UserQuery",
            new XAttribute("Guid", Guid),
            new XAttribute("DisplayName", DisplayName),
            new XAttribute("Query", Query.Key),
            EntityType == null ? null! : new XAttribute("EntityType", ctx.RetrieveLite(EntityType).CleanName),
            Owner == null ? null! : new XAttribute("Owner", Owner.KeyLong()),
            !HideQuickLink ? null! : new XAttribute("HideQuickLink", HideQuickLink),
            IncludeDefaultFilters == null ? null! : new XAttribute("IncludeDefaultFilters", IncludeDefaultFilters.Value),
            !AppendFilters ? null! : new XAttribute("AppendFilters", AppendFilters),
            RefreshMode == RefreshMode.Auto ? null! : new XAttribute("RefreshMode", RefreshMode.ToString()),
            !GroupResults ? null! : new XAttribute("GroupResults", GroupResults),
            ElementsPerPage == null ? null! : new XAttribute("ElementsPerPage", ElementsPerPage),
            PaginationMode == null ? null! : new XAttribute("PaginationMode", PaginationMode),
            new XAttribute("ColumnsMode", ColumnsMode),
            Filters.IsNullOrEmpty() ? null! : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
            Columns.IsNullOrEmpty() ? null! : new XElement("Columns", Columns.Select(c => c.ToXml(ctx)).ToList()),
            Orders.IsNullOrEmpty() ? null! : new XElement("Orders", Orders.Select(o => o.ToXml(ctx)).ToList()),
            CustomDrilldowns.IsNullOrEmpty() ? null! : new XElement("CustomDrilldowns", CustomDrilldowns.Select(d => new XElement("CustomDrilldown", ctx.Include((Lite<IUserAssetEntity>)d))).ToList()));
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
        Filters.Synchronize(element.Element("Filters")?.Elements().ToList(), (f, x) => f.FromXml(x, ctx));
        Columns.Synchronize(element.Element("Columns")?.Elements().ToList(), (c, x) => c.FromXml(x, ctx));
        Orders.Synchronize(element.Element("Orders")?.Elements().ToList(), (o, x) => o.FromXml(x, ctx));
        CustomDrilldowns.Synchronize((element.Element("CustomDrilldowns")?.Elements("CustomDrilldown")).EmptyIfNull().Select(x => (Lite<Entity>)ctx.GetEntity(Guid.Parse(x.Value)).ToLite()).NotNull().ToMList());

        ParseData(ctx.GetQueryDescription(Query));
    }

    public Pagination? GetPagination()
    {
        switch (PaginationMode)
        {
            case Signum.Entities.DynamicQuery.PaginationMode.All: return new Pagination.All();
            case Signum.Entities.DynamicQuery.PaginationMode.Firsts: return new Pagination.Firsts(ElementsPerPage!.Value);
            case Signum.Entities.DynamicQuery.PaginationMode.Paginate: return new Pagination.Paginate(ElementsPerPage!.Value, 1);
            default: return null;
        }
    }
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


public class QueryOrderEmbedded : EmbeddedEntity
{

    public QueryTokenEmbedded Token { get; set; }

    public OrderType OrderType { get; set; }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Orden",
            new XAttribute("Token", Token.Token.FullKey()),
            new XAttribute("OrderType", OrderType));
    }

    internal void FromXml(XElement element, IFromXmlContext ctx)
    {
        Token = new QueryTokenEmbedded(element.Attribute("Token")!.Value);
        OrderType = element.Attribute("OrderType")!.Value.ToEnum<OrderType>();
    }

    public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
    {
        Token.ParseData(context, description, options & ~SubTokensOptions.CanAnyAll);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Token) && Token != null && Token.ParseException == null)
        {
            return QueryUtils.CanOrder(Token.Token);
        }

        return base.PropertyValidation(pi);
    }

    public override string ToString()
    {
        return "{0} {1}".FormatWith(Token, OrderType);
    }
}

public class QueryColumnEmbedded : EmbeddedEntity
{
    public QueryTokenEmbedded Token { get; set; }

    string? displayName;
    public string? DisplayName
    {
        get { return displayName.DefaultToNull(); }
        set { Set(ref displayName, value); }
    }

    public QueryTokenEmbedded? SummaryToken { get; set; }

    public bool HiddenColumn { get; set; }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Column",
            new XAttribute("Token", Token.Token.FullKey()),
            SummaryToken != null ? new XAttribute("SummaryToken", SummaryToken.Token.FullKey()) : null!,
            DisplayName.HasText() ? new XAttribute("DisplayName", DisplayName) : null!,
            HiddenColumn ? new XAttribute("HiddenColumn", HiddenColumn) : null!);
    }

    internal void FromXml(XElement element, IFromXmlContext ctx)
    {
        Token = new QueryTokenEmbedded(element.Attribute("Token")!.Value);
        SummaryToken = element.Attribute("SummaryToken")?.Value.Let(val => new QueryTokenEmbedded(val));
        DisplayName = element.Attribute("DisplayName")?.Value;
        HiddenColumn = element.Attribute("HiddenColumn")?.Value.ToBool() ?? false;
    }

    public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
    {
        Token.ParseData(context, description, options);
        SummaryToken?.ParseData(context, description, options | SubTokensOptions.CanAggregate);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(Token) && Token != null && Token.ParseException == null)
        {
            return QueryUtils.CanColumn(Token.Token);
        }

        if (pi.Name == nameof(SummaryToken) && SummaryToken != null && SummaryToken.ParseException == null)
        {
            return QueryUtils.CanColumn(SummaryToken.Token) ??
                (SummaryToken.Token is not AggregateToken ? SearchMessage.SummaryHeaderMustBeAnAggregate.NiceToString() : null);
        }

        return base.PropertyValidation(pi);
    }

    public override string ToString()
    {
        return "{0} {1}".FormatWith(Token, displayName);
    }
}

public class QueryFilterEmbedded : EmbeddedEntity
{
    public QueryFilterEmbedded() { }

    QueryTokenEmbedded? token;
    public QueryTokenEmbedded? Token
    {
        get { return token; }
        set
        {
            if (Set(ref token, value))
            {
                Notify(() => Operation);
                Notify(() => ValueString);
            }
        }
    }

    public bool IsGroup { get; set; }

    public FilterGroupOperation? GroupOperation { get; set; }

    public FilterOperation? Operation { get; set; }

    [StringLengthValidator(Max = 300)]
    public string? ValueString { get; set; }

    public PinnedQueryFilterEmbedded? Pinned { get; set; }

    public DashboardBehaviour? DashboardBehaviour { get; set; }

    [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 0)]
    public int Indentation { get; set; }

    public void ParseData(ModifiableEntity context, QueryDescription description, SubTokensOptions options)
    {
        token?.ParseData(context, description, options);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (IsGroup)
        {
            if (pi.Name == nameof(GroupOperation) && GroupOperation == null)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());


            if (token != null && token.ParseException == null)
            {
                if (pi.Name == nameof(Token))
                {
                    return QueryUtils.CanFilter(token.Token);
                }
            }
        }
        else
        {
            if (pi.Name == nameof(Operation) && Operation == null)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            if (pi.Name == nameof(Token) && Token == null)
                return ValidationMessage._0IsNotSet.NiceToString(pi.NiceName());

            if (token != null && token.ParseException == null)
            {
                if (pi.Name == nameof(Token))
                {
                    return QueryUtils.CanFilter(token.Token);
                }

                if (pi.Name == nameof(Operation) && Operation != null)
                {
                    FilterType? filterType = QueryUtils.TryGetFilterType(Token!.Token.Type);

                    if (filterType == null)
                        return UserQueryMessage._0IsNotFilterable.NiceToString().FormatWith(token);

                    if (!QueryUtils.GetFilterOperations(filterType.Value).Contains(Operation.Value))
                        return UserQueryMessage.TheFilterOperation0isNotCompatibleWith1.NiceToString().FormatWith(Operation, filterType);
                }

                if (pi.Name == nameof(ValueString))
                {
                    var parent = this.TryGetParentEntity<ModifiableEntity>() as IHasEntityType;

                    var result = FilterValueConverter.IsValidExpression(ValueString, Token!.Token.Type, Operation!.Value.IsList(), parent?.EntityType?.ToType());
                    return result is Result<Type>.Error e ? e.ErrorText : null;
                }
            }
        }

        return null;
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        if (this.GroupOperation.HasValue)
        {
            return new XElement("Filter",
               new XAttribute("Indentation", Indentation),
               new XAttribute("GroupOperation", GroupOperation),
               Token == null ? null! : new XAttribute("Token", Token.Token.FullKey()),
               DashboardBehaviour == null ? null! : new XAttribute("DashboardBehaviour", DashboardBehaviour),
               Pinned?.ToXml(ctx)!);

        }
        else
        {
            return new XElement("Filter",
                new XAttribute("Indentation", Indentation),
                new XAttribute("Token", Token!.Token.FullKey()),
                new XAttribute("Operation", Operation!),
                ValueString == null ? null! : new XAttribute("Value", ValueString),
                DashboardBehaviour == null ? null! : new XAttribute("DashboardBehaviour", DashboardBehaviour),
                Pinned?.ToXml(ctx)!);
        }
    }

    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        IsGroup = element.Attribute("GroupOperation") != null;
        Indentation = element.Attribute("Indentation")?.Value.ToInt() ?? 0;
        GroupOperation = element.Attribute("GroupOperation")?.Value.ToEnum<FilterGroupOperation>();
        Operation = element.Attribute("Operation")?.Value.ToEnum<FilterOperation>();
        Token = element.Attribute("Token")?.Let(t => new QueryTokenEmbedded(t.Value));
        ValueString = element.Attribute("Value")?.Value;
        DashboardBehaviour = element.Attribute("DashboardBehaviour")?.Value.ToEnum<DashboardBehaviour>();
        Pinned = element.Element("Pinned")?.Let(p => (this.Pinned ?? new PinnedQueryFilterEmbedded()).FromXml(p, ctx));
    }

    public override string ToString()
    {
        return "{0} {1} {2}".FormatWith(token, Operation, ValueString);
    }

    public QueryFilterEmbedded Clone() => new QueryFilterEmbedded
    {
        Indentation = Indentation,
        GroupOperation = GroupOperation,
        IsGroup = IsGroup,
        Pinned = Pinned?.Clone(),
        Token = Token?.Clone(),
        Operation = Operation,
        ValueString = ValueString,
    };
}


public class PinnedQueryFilterEmbedded : EmbeddedEntity
{
    [StringLengthValidator(Max = 100)]
    public string? Label { get; set; }

    public int? Column { get; set; }

    public int? Row { get; set; }

    public PinnedFilterActive Active { get; set; }

    public bool SplitText { get; set; }

    internal PinnedQueryFilterEmbedded Clone() => new PinnedQueryFilterEmbedded
    {
        Label = Label,
        Column = Column,
        Row = Row,
        Active = Active,
        SplitText = SplitText,
    };

    internal PinnedQueryFilterEmbedded FromXml(XElement p, IFromXmlContext ctx)
    {
        Label = p.Attribute("Label")?.Value;
        Column = p.Attribute("Column")?.Value.ToInt();
        Row = p.Attribute("Row")?.Value.ToInt();
        Active = p.Attribute("Active")?.Value.ToEnum<PinnedFilterActive>() ?? (p.Attribute("DisableOnNull")?.Value.ToBool() == true ? PinnedFilterActive.WhenHasValue : PinnedFilterActive.Always);
        SplitText = p.Attribute("SplitText")?.Value.ToBool() ?? false;
        return this;
    }

    internal XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Pinned",
            Label.DefaultToNull()?.Let(l => new XAttribute("Label", l))!,
            Column?.Let(l => new XAttribute("Column", l))!,
            Row?.Let(l => new XAttribute("Row", l))!,
            Active == PinnedFilterActive.Always ? null! : new XAttribute("Active", Active.ToString())!,
            SplitText == false ? null! : new XAttribute("SplitText", SplitText)
        );
    }
}



public static class UserQueryUtils
{
    public static List<Filter> ToFilterList(this IEnumerable<QueryFilterEmbedded> filters, int indent = 0)
    {
        return filters.GroupWhen(filter => filter.Indentation == indent).Select(gr =>
        {
            var filter = gr.Key;

            if (filter.DashboardBehaviour == DashboardBehaviour.UseAsInitialSelection ||
               filter.DashboardBehaviour == DashboardBehaviour.UseWhenNoFilters /*TODO, works for CachedQueries but maybe not in other cases*/)
                return null;


            if (filter.Pinned != null)
            {
                if (filter.Pinned.Active == PinnedFilterActive.Checkbox_StartUnchecked)
                    return null;

                if (filter.Pinned.Active == PinnedFilterActive.NotCheckbox_StartChecked)
                    return null;

                if (filter.Pinned.SplitText && !filter.ValueString.HasText())
                    return null;
            }

            if (!filter.IsGroup)
            {
                if (gr.Count() != 0)
                    throw new InvalidOperationException("Unexpected childrens of condition");

                var value = FilterValueConverter.Parse(filter.ValueString, filter.Token!.Token.Type, filter.Operation!.Value.IsList());

                if (filter.Pinned?.Active == PinnedFilterActive.WhenHasValue && value == null)
                    return null;

                return (Filter)new FilterCondition(filter.Token.Token, filter.Operation.Value, value);
            }
            else
            {
                if (filter.Pinned?.Active == PinnedFilterActive.WhenHasValue /*TODO, works for empty groups */)
                    return null;

                return (Filter)new FilterGroup(filter.GroupOperation!.Value, filter.Token?.Token, gr.ToFilterList(indent + 1).ToList());
            }
        }).NotNull().ToList();
    }

    public static List<(QueryToken, bool prototedToDashboard)> GetDashboardPinnedFilterTokens(this IEnumerable<QueryFilterEmbedded> filters, int indent = 0)
    {
        return filters.GroupWhen(filter => filter.Indentation == indent).SelectMany(gr =>
        {
            var filter = gr.Key;

            if (filter.Pinned != null)
            {
                var promotedToDashboard = filter.DashboardBehaviour == DashboardBehaviour.PromoteToDasboardPinnedFilter;
                return gr.PreAnd(filter).Select(a => a.Token?.Token).NotNull().Distinct().Select(t => (t, promotedToDashboard));
            }

            if (filter.IsGroup)
                return gr.GetDashboardPinnedFilterTokens(indent + 1);
            else
                return Enumerable.Empty<(QueryToken, bool prototedToDashboard)>();
        }).ToList();
    }
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
    [Description("The Filter Operation {0} is not compatible with {1}")]
    TheFilterOperation0isNotCompatibleWith1,
    [Description("{0} is not filterable")]
    _0IsNotFilterable,
    [Description("Use {0} to filter current entity")]
    Use0ToFilterCurrentEntity,
    Preview,
    [Description("Makes the {0} available in the contextual menu when grouping {1}")]
    MakesThe0AvailableInContextualMenuWhenGrouping0,
    [Description("Makes the {0} available as Quick Link of {1}")]
    MakesThe0AvailableAsAQuickLinkOf1,
    [Description("the selected {0}")]
    TheSelected0,
}
