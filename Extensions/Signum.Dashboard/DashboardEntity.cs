using System.ComponentModel;
using System.Collections.Specialized;
using Signum.UserAssets;
using System.Xml.Linq;
using Signum.Scheduler;
using Signum.UserAssets.QueryTokens;
using Signum.UserAssets.Queries;

namespace Signum.Dashboard;

[EntityKind(EntityKind.Main, EntityData.Master)]
public class DashboardEntity : Entity, IUserAssetEntity, IHasEntityType, ITaskEntity
{
    public DashboardEntity()
    {
        BindParent();
    }

    Lite<TypeEntity>? entityType;
    public Lite<TypeEntity>? EntityType
    {
        get { return entityType; }
        set
        {
            if (Set(ref entityType, value) && value == null)
                EmbeddedInEntity = null;
        }
    }

    public DashboardEmbedededInEntity? EmbeddedInEntity { get; set; }

    public Lite<Entity>? Owner { get; set; }

    public int? DashboardPriority { get; set; }

    [Unit("s"), NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 10)]
    public int? AutoRefreshPeriod { get; set; }

    [StringLengthValidator(Min = 2, Max = 200)]
    public string DisplayName { get; set; }

    public bool HideDisplayName { get; set; }

    public bool CombineSimilarRows { get; set; } = true;

    public CacheQueryConfigurationEmbedded? CacheQueryConfiguration { get; set; }

    [BindParent]
    [NoRepeatValidator]
    public MList<PanelPartEmbedded> Parts { get; set; } = new MList<PanelPartEmbedded>();

    [Ignore, QueryableProperty, BindParent]
    public MList<TokenEquivalenceGroupEntity> TokenEquivalencesGroups { get; set; } = new MList<TokenEquivalenceGroupEntity>();

    [UniqueIndex]
    public Guid Guid { get; set; } = Guid.NewGuid();

    [StringLengthValidator(Max = 200)]
    public string? Key { get; set; }

    public bool HideQuickLink { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? IconName { get; set; }

    [StringLengthValidator(Min = 3, Max = 20)]
    [Format(FormatAttribute.Color)]
    public string? IconColor { get; set; }

    [StringLengthValidator(Min = 3, Max = 20)]
    [Format(FormatAttribute.Color)]
    public string? TitleColor { get; set; }

    [AutoExpressionField]
    public bool ContainsContent(IPartEntity content) => 
        As.Expression(() => Parts.Any(p => p.Content.Is(content)));

    protected override string? ChildPropertyValidation(ModifiableEntity sender, PropertyInfo pi)
    {
        if (sender is PanelPartEmbedded part)
        {
            if (pi.Name == nameof(part.StartColumn))
            {
                if (part.StartColumn + part.Columns > 12)
                    return DashboardMessage.Part0IsTooLarge.NiceToString(part);

                var other = Parts.TakeWhile(p => p != part)
                    .FirstOrDefault(a => a.Row == part.Row && a.ColumnInterval().Overlaps(part.ColumnInterval()));

                if (other != null)
                    return DashboardMessage.Part0OverlapsWith1.NiceToString(part, other);
            }

            if (entityType != null && pi.Name == nameof(part.Content) && part.Content != null)
            {
                var idents = GraphExplorer.FromRoot((Entity)part.Content).OfType<Entity>();

                string errorsUserQuery = idents.OfType<IHasEntityType>()
                    .Where(uc => uc.EntityType != null && !uc.EntityType.Is(EntityType))
                    .ToString(uc => DashboardMessage._0Is1InstedOf2In3.NiceToString(NicePropertyName(() => EntityType), uc.EntityType, entityType, uc),
                    "\n");

                return errorsUserQuery.DefaultText(null!);
            }
        }

        return base.ChildPropertyValidation(sender, pi);
    }

    protected override void ChildCollectionChanged(object? sender, NotifyCollectionChangedEventArgs args)
    {
        if (sender == Parts)
            foreach (var pp in Parts)
                pp.NotifyRowColumn();

        base.ChildCollectionChanged(sender, args);
    }


    internal void ParseData(Func<QueryEntity, QueryDescription?> getDescription)
    {
        foreach (var f in TokenEquivalencesGroups)
        {
            foreach (var t in f.TokenEquivalences)
            {
                var description = getDescription(t.Query);
                if (description != null)
                    t.Token.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanAnyAll);
            }
        }

        foreach (var item in this.Parts.Select(a => a.Content).OfType<IPartParseDataEntity>())
        {
            item.ParseData(this);
        }
    }

    [Ignore]
    bool invalidating = false;
    protected override void ChildPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (!invalidating && sender is PanelPartEmbedded && (e.PropertyName == "Row" || e.PropertyName == "Column"))
        {
            invalidating = true;
            foreach (var pp in Parts)
                pp.NotifyRowColumn();
            invalidating = false;
        }

        base.ChildPropertyChanged(sender, e);
    }

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => DisplayName);

    public DashboardEntity Clone()
    {
        return new DashboardEntity
        {
            EntityType = this.EntityType,
            EmbeddedInEntity = this.EmbeddedInEntity,
            Owner = Owner,
            DashboardPriority = DashboardPriority,
            AutoRefreshPeriod = this.AutoRefreshPeriod,
            DisplayName = "Clone {0}".FormatWith(this.DisplayName),
            HideDisplayName = this.HideDisplayName,
            CombineSimilarRows = this.CombineSimilarRows,
            CacheQueryConfiguration = this.CacheQueryConfiguration?.Clone(),
            Parts = Parts.Select(p => p.Clone()).ToMList(),
            Key = this.Key
        };
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Dashboard",
            new XAttribute("Guid", Guid),
            new XAttribute("DisplayName", DisplayName),
            EntityType == null ? null! : new XAttribute("EntityType", ctx.RetrieveLite(EntityType).CleanName),
            Owner == null ? null! : new XAttribute("Owner", Owner.KeyLong()),
            HideDisplayName == false ? null! : new XAttribute("HideDisplayName", HideDisplayName.ToString()),
            DashboardPriority == null ? null! : new XAttribute("DashboardPriority", DashboardPriority.Value.ToString()),
            EmbeddedInEntity == null ? null! : new XAttribute("EmbeddedInEntity", EmbeddedInEntity.Value.ToString()),
            new XAttribute("CombineSimilarRows", CombineSimilarRows),
            IconName == null ? null! : new XAttribute("IconName", IconName),
            IconColor == null ? null! : new XAttribute("IconColor", IconColor),
            TitleColor == null ? null! : new XAttribute("TitleColor", TitleColor),
            CacheQueryConfiguration?.ToXml(ctx),
            new XElement("Parts", Parts.Select(p => p.ToXml(ctx))),
            new XElement(nameof(TokenEquivalencesGroups), TokenEquivalencesGroups.Select(teg => teg.ToXml(ctx)))
        );
    }


    public void FromXml(XElement element, IFromXmlContext ctx)
    {
        DisplayName = element.Attribute("DisplayName")!.Value;
        EntityType = element.Attribute("EntityType")?.Let(a => ctx.GetTypeLite(a.Value));
        Owner = element.Attribute("Owner")?.Let(a => ctx.ParseLite(a.Value, this, d => d.Owner));
        HideDisplayName = element.Attribute("HideDisplayName")?.Let(a => bool.Parse(a.Value)) ?? false;
        DashboardPriority = element.Attribute("DashboardPriority")?.Let(a => int.Parse(a.Value));
        EmbeddedInEntity = element.Attribute("EmbeddedInEntity")?.Let(a => a.Value.ToEnum<DashboardEmbedededInEntity>());
        CombineSimilarRows = element.Attribute("CombineSimilarRows")?.Let(a => bool.Parse(a.Value)) ?? false;
        IconName = element.Attribute("IconName")?.Value;
        IconColor = element.Attribute("IconColor")?.Value;
        TitleColor = element.Attribute("TitleColor")?.Value; 
        CacheQueryConfiguration = CacheQueryConfiguration.CreateOrAssignEmbedded(element.Element(nameof(CacheQueryConfiguration)), (cqc, elem) => cqc.FromXml(elem));
        Parts.Synchronize(element.Element("Parts")!.Elements().ToList(), (pp, x) => pp.FromXml(x, ctx));
        TokenEquivalencesGroups.Synchronize(element.Element(nameof(TokenEquivalencesGroups))?.Elements().ToList() ?? new List<XElement>(), (teg, x) => teg.FromXml(x, ctx));
        ParseData(q => ctx.GetQueryDescription(q));
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if (pi.Name == nameof(EmbeddedInEntity))
        {
            if (EmbeddedInEntity == null && EntityType != null)
                return ValidationMessage._0IsNecessary.NiceToString(pi.NiceName());

            if (EmbeddedInEntity != null && EntityType == null)
                return ValidationMessage._0IsNotAllowed.NiceToString(pi.NiceName());
        }

        if(pi.Name == nameof(CacheQueryConfiguration) && CacheQueryConfiguration != null && EntityType != null)
        {
            return ValidationMessage._0ShouldBeNullWhen1IsSet.NiceToString(pi.NiceName(), NicePropertyName(() => EntityType));
        }

        if(pi.Name == nameof(TokenEquivalencesGroups))
        {
            var dups = TokenEquivalencesGroups
                .SelectMany(a => a.TokenEquivalences).Select(a => a.Token.Token).NotNull()
                .GroupCount(a => a).Where(gr => gr.Value > 1).ToString(a => a.Value + " x " + a.Key.FullKey(), "\n");

            if (dups.HasText())
                return "Duplicated tokens: " + dups;
        }

        return base.PropertyValidation(pi);
    }

    protected override void PostRetrieving(PostRetrievingContext ctx)
    {
        base.PostRetrieving(ctx);

  
    }
}

public class DashboardLiteModel : ModelEntity
{
    public string DisplayName { get; set; }
    public bool HideQuickLink { get; set; }

    internal static DashboardLiteModel Translated(DashboardEntity d) => new DashboardLiteModel
    {
        DisplayName = PropertyRouteTranslationLogic.TranslatedField(d, d => d.DisplayName),
        HideQuickLink = d.HideQuickLink,
    };

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => DisplayName);
}

public class CacheQueryConfigurationEmbedded : EmbeddedEntity
{
    [Unit("s")]
    public int TimeoutForQueries { get; set; } = 5 * 60;

    public int MaxRows { get; set; } = 1000 * 1000;

    [Unit("m")]
    public int? AutoRegenerateWhenOlderThan { get; set; }

    internal CacheQueryConfigurationEmbedded Clone() => new CacheQueryConfigurationEmbedded
    {
        TimeoutForQueries = TimeoutForQueries,
        MaxRows = MaxRows,
        AutoRegenerateWhenOlderThan = AutoRegenerateWhenOlderThan,
    };

    internal XElement ToXml(IToXmlContext ctx) => new XElement("CacheQueryConfiguration",
        new XAttribute(nameof(TimeoutForQueries), TimeoutForQueries),
        new XAttribute(nameof(MaxRows), MaxRows),
        AutoRegenerateWhenOlderThan == null ? null : new XAttribute(nameof(AutoRegenerateWhenOlderThan), AutoRegenerateWhenOlderThan)
    );

    internal void FromXml(XElement elem)
    {
        TimeoutForQueries = elem.Attribute(nameof(TimeoutForQueries))?.Value.ToInt() ?? 5 * 60;
        MaxRows = elem.Attribute(nameof(MaxRows))?.Value.ToInt() ?? 1000 * 1000;
        AutoRegenerateWhenOlderThan = elem.Attribute(nameof(AutoRegenerateWhenOlderThan))?.Value.ToInt();
    }
}

[AutoInit]
public static class DashboardPermission
{
    public static PermissionSymbol ViewDashboard;
}

[AutoInit]
public static class DashboardOperation
{
    public static ExecuteSymbol<DashboardEntity> Save;
    public static ExecuteSymbol<DashboardEntity> RegenerateCachedQueries;
    public static ConstructSymbol<DashboardEntity>.From<DashboardEntity> Clone;
    public static DeleteSymbol<DashboardEntity> Delete;
}

public enum DashboardMessage
{
    CreateNewPart,

    [Description("Title must be specified for {0}")]
    DashboardDN_TitleMustBeSpecifiedFor0,

    Preview,
    [Description("{0} is {1} (instead of {2}) in {3}")]
    _0Is1InstedOf2In3,

    [Description("Part {0} is too large")]
    Part0IsTooLarge,

    [Description("Part {0} overlaps with {1}")]
    Part0OverlapsWith1,

    [Description("Row[s] selected")]
    RowsSelected,

    ForPerformanceReasonsThisDashboardMayShowOutdatedInformation,

    [Description("Last update was on {0}")]
    LasUpdateWasOn0,

    [Description("The User Query '{0}' has no column with summary header")]
    TheUserQuery0HasNoColumnWithSummaryHeader,

    Edit,


    [Description("Click in one chart to filter in the others")]
    CLickInOneChartToFilterInTheOthers,


    [Description("[Ctrl] + Click to filter by multiple elements")]
    CtrlClickToFilterByMultipleElements,


    [Description("[Alt] + Click to open results in a modal window")]
    AltClickToOpenResultsInAModalWindow,

    CopyHealthCheckDashboardData,

    [Description("{0} can only be used in a {1} with {2}")]
    _0CanOnlyBeUserInA1With2,

    InteractiveDashboard,
}

public enum DashboardEmbedededInEntity
{
    None,
    Top,
    Bottom,
    Tab
}

[EntityKind(EntityKind.Part, EntityData.Master)]
public class TokenEquivalenceGroupEntity : Entity
{
    [NotNullValidator(Disabled = true)]
    public Lite<DashboardEntity> Dashboard { get; set; }

    public InteractionGroup? InteractionGroup { get; set; }

    [PreserveOrder, NoRepeatValidator, CountIsValidator(ComparisonType.GreaterThan, 1)]
    public MList<TokenEquivalenceEmbedded> TokenEquivalences { get; set; } = new MList<TokenEquivalenceEmbedded>();

    internal void FromXml(XElement x, IFromXmlContext ctx)
    {
        InteractionGroup = x.Attribute("InteractionGroup")?.Value.ToEnum<InteractionGroup>();
        TokenEquivalences.Synchronize(x.Elements("TokenEquivalence").ToList(), (teg, x) => teg.FromXml(x, ctx));
    }

    internal XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("TokenEquivalenceGroup",
            InteractionGroup == null ? null : new XAttribute(nameof(InteractionGroup), InteractionGroup.Value.ToString()),
            TokenEquivalences.Select(te => te.ToXml(ctx)));
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        if(pi.Name == nameof(TokenEquivalences))
        {
            var list = TokenEquivalences.Select(a => a.Token.Token.Type.UnNullify().CleanType()).Distinct().ToList();
            if(list.Count > 1)
            {
                if (!list.Any(t => list.All(t2 => t.IsAssignableFrom(t2))))
                    return "Types " + list.CommaAnd(t => t.TypeName()) + " are not compatible";
            }
        }    

        return base.PropertyValidation(pi);
    }
}

public class TokenEquivalenceEmbedded : EmbeddedEntity
{
    public QueryEntity Query { get; set; }

    public QueryTokenEmbedded Token { get; set; }

    internal void FromXml(XElement element, IFromXmlContext ctx)
    {
        Query = ctx.GetQuery(element.Attribute("Query")!.Value);
        Token = new QueryTokenEmbedded(element.Attribute("Token")!.Value);
    }

    internal XElement ToXml(IToXmlContext ctx) => new XElement("TokenEquivalence",
        new XAttribute("Query", Query.Key),
        new XAttribute("Token", Token.Token.FullKey())
    );
}
