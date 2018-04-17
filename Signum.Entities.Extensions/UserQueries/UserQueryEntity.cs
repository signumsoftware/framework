using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml.Linq;

namespace Signum.Entities.UserQueries
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class UserQueryEntity : Entity, IUserAssetEntity
    {
        public UserQueryEntity() { }
        public UserQueryEntity(object queryName)
        {
            this.queryName = queryName;
        }

        [Ignore]
        internal object queryName;

        [NotNullValidator]
        public QueryEntity Query { get; set; }

        public bool GroupResults { get; set; }

        public Lite<TypeEntity> EntityType { get; set; }

        public bool HideQuickLink { get; set; }

        public Lite<Entity> Owner { get; set; }

        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 200)]
        public string DisplayName { get; set; }

        public bool AppendFilters { get; set; }
        
        [NotNullValidator, PreserveOrder]
        public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

        [NotNullValidator, PreserveOrder]
        public MList<QueryOrderEmbedded> Orders { get; set; } = new MList<QueryOrderEmbedded>();

        public ColumnOptionsMode ColumnsMode { get; set; }

        [NotNullValidator, PreserveOrder]
        public MList<QueryColumnEmbedded> Columns { get; set; } = new MList<QueryColumnEmbedded>();
        
        public bool SearchOnLoad { get; set; } = true;

        public bool ShowFilterButton { get; set; } = true;
        
        PaginationMode? paginationMode;
        public PaginationMode? PaginationMode
        {
            get { return paginationMode; }
            set { if (Set(ref paginationMode, value)) Notify(() => ShouldHaveElements); }
        }

        [NumberIsValidator(ComparisonType.GreaterThanOrEqualTo, 1)]
        public int? ElementsPerPage { get; set; }

        [UniqueIndex]
        public Guid Guid { get; set; } = Guid.NewGuid();

        static readonly Expression<Func<UserQueryEntity, string>> ToStringExpression = e => e.DisplayName;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(ElementsPerPage))
            {
                if (ElementsPerPage != null && !ShouldHaveElements)
                    return UserQueryMessage._0ShouldBeNullIf1Is2.NiceToString().FormatWith(pi.NiceName(), NicePropertyName(() => PaginationMode), PaginationMode?.Let(pm => pm.NiceToString()) ?? "");

                if (ElementsPerPage == null && ShouldHaveElements)
                    return UserQueryMessage._0ShouldBeSetIf1Is2.NiceToString().FormatWith(pi.NiceName(), NicePropertyName(() => PaginationMode), PaginationMode.NiceToString());
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

            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(this, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate);

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(this, description, SubTokensOptions.CanElement | canAggregate);

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(this, description, SubTokensOptions.CanElement | canAggregate);
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserQuery",
                new XAttribute("Guid", Guid),
                new XAttribute("DisplayName", DisplayName),
                new XAttribute("Query", Query.Key),
                EntityType == null ? null : new XAttribute("EntityType", ctx.TypeToName(EntityType)),
                new XAttribute("HideQuickLink", HideQuickLink),
                Owner == null ? null : new XAttribute("Owner", Owner.Key()),
                AppendFilters == true ? null : new XAttribute("AppendFilters", true),
                ElementsPerPage == null ? null : new XAttribute("ElementsPerPage", ElementsPerPage),
                PaginationMode == null ? null : new XAttribute("PaginationMode", PaginationMode),
                new XAttribute("ColumnsMode", ColumnsMode),
                Filters.IsNullOrEmpty() ? null : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
                Columns.IsNullOrEmpty() ? null : new XElement("Columns", Columns.Select(c => c.ToXml(ctx)).ToList()),
                Orders.IsNullOrEmpty() ? null : new XElement("Orders", Orders.Select(o => o.ToXml(ctx)).ToList()));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Query = ctx.GetQuery(element.Attribute("Query").Value);
            DisplayName = element.Attribute("DisplayName").Value;
            EntityType = element.Attribute("EntityType")?.Let(a => ctx.GetType(a.Value));
            HideQuickLink = element.Attribute("HideQuickLink")?.Let(a => bool.Parse(a.Value)) ?? false;
            Owner = element.Attribute("Owner")?.Let(a => Lite.Parse(a.Value));
            AppendFilters = element.Attribute("AppendFilters")?.Let(a => a.Value == true.ToString()) ?? false;
            ElementsPerPage = element.Attribute("ElementsPerPage")?.Let(a => int.Parse(a.Value));
            PaginationMode = element.Attribute("PaginationMode")?.Let(a => a.Value.ToEnum<PaginationMode>());
            ColumnsMode = element.Attribute("ColumnsMode").Value.ToEnum<ColumnOptionsMode>();
            Filters.Synchronize(element.Element("Filters")?.Elements().ToList(), (f, x) => f.FromXml(x, ctx));
            Columns.Synchronize(element.Element("Columns")?.Elements().ToList(), (c, x) => c.FromXml(x, ctx));
            Orders.Synchronize(element.Element("Orders")?.Elements().ToList(), (o, x) => o.FromXml(x, ctx));
            ParseData(ctx.GetQueryDescription(Query));
        }

        public Pagination GetPagination()
        {
            switch (PaginationMode)
            {
                case Signum.Entities.DynamicQuery.PaginationMode.All: return new Pagination.All();
                case Signum.Entities.DynamicQuery.PaginationMode.Firsts: return new Pagination.Firsts(ElementsPerPage.Value);
                case Signum.Entities.DynamicQuery.PaginationMode.Paginate: return new Pagination.Paginate(ElementsPerPage.Value, 1);
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


    [Serializable]
    public class QueryOrderEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
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
            Token = new QueryTokenEmbedded(element.Attribute("Token").Value);
            OrderType = element.Attribute("OrderType").Value.ToEnum<OrderType>();
        }

        public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
        {
            Token.ParseData(context, description, options & ~SubTokensOptions.CanAnyAll);
        }

        protected override string PropertyValidation(PropertyInfo pi)
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

    [Serializable]
    public class QueryColumnEmbedded : EmbeddedEntity
    {
        [NotNullValidator]
        public QueryTokenEmbedded Token { get; set; }

        string displayName;
        public string DisplayName
        {
            get { return displayName.DefaultText(null); }
            set { Set(ref displayName, value); }
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Column",
                new XAttribute("Token", Token.Token.FullKey()),
                DisplayName.HasText() ? new XAttribute("DisplayName", DisplayName) : null);
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            Token = new QueryTokenEmbedded(element.Attribute("Token").Value);
            DisplayName = element.Attribute("DisplayName")?.Value;
        }

        public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
        {
            Token.ParseData(context, description, options);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(Token) && Token != null && Token.ParseException == null)
            {
                return QueryUtils.CanColumn(Token.Token);
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            return "{0} {1}".FormatWith(Token, displayName);
        }
    }

    [Serializable]
    public class QueryFilterEmbedded : EmbeddedEntity
    {
        public QueryFilterEmbedded() { }

        QueryTokenEmbedded token;
        [NotNullValidator]
        public QueryTokenEmbedded Token
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

        public FilterOperation Operation { get; set; }

        [StringLengthValidator(AllowNulls = true, Max = 300)]
        public string ValueString { get; set; }

        public void ParseData(ModifiableEntity context, QueryDescription description, SubTokensOptions options)
        {
            token.ParseData(context, description, options);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (token != null && token.ParseException == null)
            {
                if (pi.Name == nameof(Token))
                {
                    return QueryUtils.CanFilter(token.Token);
                }

                if (pi.Name == nameof(Operation))
                {
                    FilterType? filterType = QueryUtils.TryGetFilterType(Token.Token.Type);

                    if (filterType == null)
                        return UserQueryMessage._0IsNotFilterable.NiceToString().FormatWith(token);

                    if (!QueryUtils.GetFilterOperations(filterType.Value).Contains(Operation))
                        return UserQueryMessage.TheFilterOperation0isNotCompatibleWith1.NiceToString().FormatWith(Operation, filterType);
                }

                if (pi.Name == nameof(ValueString))
                {
                    var result = FilterValueConverter.TryParse(ValueString, Token.Token.Type, Operation.IsList(), allowSmart: true);
                    return result is Result<object>.Error e ? e.ErrorText : null;
                }
            }

            return null;
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Filter",
                new XAttribute("Token", Token.Token.FullKey()),
                new XAttribute("Operation", Operation),
                new XAttribute("Value", ValueString ?? ""));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Token = new QueryTokenEmbedded(element.Attribute("Token").Value);
            Operation = element.Attribute("Operation").Value.ToEnum<FilterOperation>();
            ValueString = element.Attribute("Value").Value;
        }

        public override string ToString()
        {
            return "{0} {1} {2}".FormatWith(token, Operation, ValueString);
        }

        internal QueryFilterEmbedded Clone() => new QueryFilterEmbedded
        {
            Token = Token.Clone(),
            Operation = Operation,
            ValueString = ValueString,
        };
    }

    public static class UserQueryUtils
    {
        public static Func<Lite<Entity>> DefaultOwner = () => (Lite<Entity>)UserHolder.Current?.ToLite();

        public static UserQueryEntity ToUserQuery(this QueryRequest request, QueryDescription qd, QueryEntity query, Pagination defaultPagination, bool withoutFilters)
        {
            var tuple = SmartColumns(request.Columns, qd);

            var defaultMode = defaultPagination.GetMode();
            var defaultElementsPerPage = defaultPagination.GetElementsPerPage();

            var mode = request.Pagination.GetMode();
            var elementsPerPage = request.Pagination.GetElementsPerPage();

            bool isDefaultPaginate = defaultMode == mode && defaultElementsPerPage == elementsPerPage;

            return new UserQueryEntity
            {
                Query = query,
                AppendFilters = withoutFilters,
                Owner = DefaultOwner(),
                GroupResults = request.GroupResults,
                Filters = withoutFilters ? new MList<QueryFilterEmbedded>() : request.Filters.Select(f => new QueryFilterEmbedded
                {
                    Token = new QueryTokenEmbedded(f.Token),
                    Operation = f.Operation,
                    ValueString = FilterValueConverter.ToString(f.Value, f.Token.Type, allowSmart: true)
                }).ToMList(),
                ColumnsMode = tuple.mode,
                Columns = tuple.columns,
                Orders = request.Orders.Select(oo => new QueryOrderEmbedded
                {
                    Token = new QueryTokenEmbedded(oo.Token),
                    OrderType = oo.OrderType
                }).ToMList(),
                PaginationMode = isDefaultPaginate ? (PaginationMode?)null : mode,
                ElementsPerPage = isDefaultPaginate ? (int?)null : elementsPerPage,
            };
        }

        public static (ColumnOptionsMode mode, MList<QueryColumnEmbedded> columns) SmartColumns(List<Column> current, QueryDescription qd)
        {
            var ideal = (from cd in qd.Columns
                         where !cd.IsEntity
                         select cd).ToList();

            foreach (var item in current)
            {
                if (item.Token.NiceName() == item.DisplayName)
                    item.DisplayName = null;
            }

            if (current.Count < ideal.Count)
            {
                List<Column> toRemove = new List<Column>();
                int j = 0;
                for (int i = 0; i < ideal.Count; i++)
                {
                    if (j < current.Count && current[j].Similar(ideal[i]))
                        j++;
                    else
                        toRemove.Add(new Column(ideal[i], qd.QueryName));
                }

                if (toRemove.Count + current.Count == ideal.Count)
                    return (mode: ColumnOptionsMode.Remove, columns: toRemove.Select(c => new QueryColumnEmbedded { Token = new QueryTokenEmbedded(c.Token) }).ToMList());
            }
            else
            {
                if (current.Zip(ideal).All(t => t.first.Similar(t.second)))
                    return (mode: ColumnOptionsMode.Add, columns: current.Skip(ideal.Count).Select(c => new QueryColumnEmbedded
                    {
                        Token = new QueryTokenEmbedded(c.Token),
                        DisplayName = c.DisplayName
                    }).ToMList());

            }

            return (mode: ColumnOptionsMode.Replace, columns: current.Select(c => new QueryColumnEmbedded
            {
                Token = new QueryTokenEmbedded(c.Token),
                DisplayName = c.DisplayName
            }).ToMList());
        }

        static bool Similar(this Column column, ColumnDescription other)
        {
            return column.Token is ColumnToken && ((ColumnToken)column.Token).Column.Name == other.Name && column.DisplayName == null;
        }

    }

    public enum UserQueryMessage
    {
        [Description("Are you sure to remove '{0}'?")]
        AreYouSureToRemove0,
        Edit,
        [Description("My Queries")]
        MyQueries,
        [Description("Remove User Query?")]
        RemoveUserQuery,
        [Description("{0} should be empty if {1} is set")]
        _0ShouldBeEmptyIf1IsSet,
        [Description("{0} should be null if {1} is '{2}'")]
        _0ShouldBeNullIf1Is2,
        [Description("{0} should be set if {1} is '{2}'")]
        _0ShouldBeSetIf1Is2,
        [Description("Create")]
        UserQueries_CreateNew,
        [Description("Edit")]
        UserQueries_Edit,
        [Description("User Queries")]
        UserQueries_UserQueries,
        [Description("The Filter Operation {0} is not compatible with {1}")]
        TheFilterOperation0isNotCompatibleWith1,
        [Description("{0} is not filterable")]
        _0IsNotFilterable,
        [Description("Use {0} to filter current entity")]
        Use0ToFilterCurrentEntity,
        Preview
    }
}
