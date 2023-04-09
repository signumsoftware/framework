using Signum.Authorization.Rules;
using Signum.DynamicQuery.Tokens;
using Signum.UserAssets;
using Signum.UserAssets.Queries;
using System.ComponentModel;
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
        CustomDrilldowns.Synchronize((element.Element("CustomDrilldowns")?.Elements("CustomDrilldown")).EmptyIfNull().Select(x => (Lite<Entity>)ctx.GetEntity(Guid.Parse(x.Value)).ToLiteFat()).NotNull().ToMList());

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
}
