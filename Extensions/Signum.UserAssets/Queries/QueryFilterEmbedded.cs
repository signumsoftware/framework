using Signum.DynamicQuery.Tokens;
using Signum.UserAssets;
using Signum.UserAssets.QueryTokens;
using System.Xml.Linq;

namespace Signum.UserAssets.Queries;

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

    [StringLengthValidator(Max = int.MaxValue)]
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
                        return UserAssetMessage._0IsNotFilterable.NiceToString().FormatWith(token);

                    if (!QueryUtils.GetFilterOperations(Token!.Token).Contains(Operation.Value))
                        return UserAssetMessage.TheFilterOperation0isNotCompatibleWith1.NiceToString().FormatWith(Operation, filterType);
                }

                if (pi.Name == nameof(ValueString))
                {
                    var parent = TryGetParentEntity<ModifiableEntity>() as IHasEntityType;

                    var result = FilterValueConverter.IsValidExpression(ValueString, Token!.Token.Type, Operation!.Value.IsList(), parent?.EntityType?.ToType());
                    return result is Result<Type>.Error e ? e.ErrorText : null;
                }
            }
        }

        return null;
    }

    public XElement ToXml(IToXmlContext ctx)
    {
        if (GroupOperation.HasValue)
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

    public void FromXml(XElement element, IFromXmlContext ctx, IUserAssetEntity parentEntity, PropertyRoute valuePr)
    {
        IsGroup = element.Attribute("GroupOperation") != null;
        Indentation = element.Attribute("Indentation")?.Value.ToInt() ?? 0;
        GroupOperation = element.Attribute("GroupOperation")?.Value.ToEnum<FilterGroupOperation>();
        Operation = element.Attribute("Operation")?.Value.ToEnum<FilterOperation>();
        Token = element.Attribute("Token")?.Let(t => new QueryTokenEmbedded(t.Value));
        ValueString = element.Attribute("Value")?.Value;
        if(ValueString.HasText() && Lite.TryParseLite(ValueString, out var result) == null)
        {
            var lite = ctx.ParseLite(ValueString, parentEntity, valuePr);
            if (lite?.KeyLong() != ValueString)
                ValueString = lite?.KeyLong();

        }

        DashboardBehaviour = element.Attribute("DashboardBehaviour")?.Value.ToEnum<DashboardBehaviour>();
        Pinned = element.Element("Pinned")?.Let(p => (Pinned ?? new PinnedQueryFilterEmbedded()).FromXml(p, ctx));
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

    public int? ColSpan { get; set; }

    public int? Row { get; set; }

    public PinnedFilterActive Active { get; set; }

    public bool SplitValue { get; set; }

    internal PinnedQueryFilterEmbedded Clone() => new PinnedQueryFilterEmbedded
    {
        Label = Label,
        Column = Column,
        ColSpan = ColSpan,
        Row = Row,
        Active = Active,
        SplitValue = SplitValue,
    };

    internal PinnedQueryFilterEmbedded FromXml(XElement p, IFromXmlContext ctx)
    {
        Label = p.Attribute("Label")?.Value;
        Column = p.Attribute("Column")?.Value.ToInt();
        ColSpan = p.Attribute("ColSpan")?.Value.ToInt();
        Row = p.Attribute("Row")?.Value.ToInt();
        Active = ModernizeActive(p.Attribute("Active")?.Value)?.ToEnum<PinnedFilterActive>() ?? PinnedFilterActive.Always;
        SplitValue = p.Attribute("SplitValue")?.Value.ToBool() ?? p.Attribute("SplitText")?.Value.ToBool() ?? false;
        return this;
    }

    internal XElement ToXml(IToXmlContext ctx)
    {
        return new XElement("Pinned",
            Label.DefaultToNull()?.Let(l => new XAttribute("Label", l))!,
            Column?.Let(l => new XAttribute("Column", l))!,
            ColSpan?.Let(l => new XAttribute("ColSpan", l))!,
            Row?.Let(l => new XAttribute("Row", l))!,
            Active == PinnedFilterActive.Always ? null! : new XAttribute("Active", Active.ToString())!,
            SplitValue == false ? null! : new XAttribute("SplitValue", SplitValue)
        );
    }

    private string? ModernizeActive(string? str) => str switch
    {
        "Checkbox_StartChecked" => "Checkbox_Checked",
        "Checkbox_StartUnchecked" => "Checkbox_Unchecked",
        "NotCheckbox_StartChecked" => "NotCheckbox_Checked",
        "NotCheckbox_StartUnchecked" => "NotCheckbox_Unchecked",
        _ => str
    };

}


public static class QueryFilterUtils
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
                if (filter.Pinned.Active == PinnedFilterActive.Checkbox_Unchecked)
                    return null;

                if (filter.Pinned.Active == PinnedFilterActive.NotCheckbox_Checked)
                    return null;

                if (filter.Pinned.SplitValue && !filter.ValueString.HasText())
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

public enum UserAssetQueryMessage
{
    SwitchToValue,
    SwitchToExpression,
}
