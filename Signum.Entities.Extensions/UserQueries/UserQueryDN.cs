using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System.Globalization;
using System.Reflection;
using System.Linq.Expressions;
using System.ComponentModel;
using Signum.Entities.Authorization;
using System.Xml.Linq;
using Signum.Entities.UserAssets;

namespace Signum.Entities.UserQueries
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Master)]
    public class UserQueryDN : Entity, IUserAssetEntity
    {
        public UserQueryDN() { }
        public UserQueryDN(object queryName)
        {
            this.queryName = queryName;
        }

        [Ignore]
        internal object queryName;
        
        QueryDN query;
        [NotNullValidator]
        public QueryDN Query
        {
            get { return query; }
            set { Set(ref query, value); }
        }

        Lite<TypeDN> entityType;
        public Lite<TypeDN> EntityType
        {
            get { return entityType; }
            set { Set(ref entityType, value); }
        }

        Lite<IdentifiableEntity> owner;
        public Lite<IdentifiableEntity> Owner
        {
            get { return owner; }
            set { Set(ref owner, value); }
        }

        [NotNullable]
        string displayName;
        [StringLengthValidator(Min = 1)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetToStr(ref displayName, value); }
        }

        bool withoutFilters;
        public bool WithoutFilters
        {
            get { return withoutFilters; }
            set
            {
                if (Set(ref withoutFilters, value) && withoutFilters)
                    filters.Clear();
            }
        }

        [NotNullable, PreserveOrder]
        MList<QueryFilterDN> filters = new MList<QueryFilterDN>();
        public MList<QueryFilterDN> Filters
        {
            get { return filters; }
            set { Set(ref filters, value); }
        }

        [NotNullable, PreserveOrder]
        MList<QueryOrderDN> orders = new MList<QueryOrderDN>();
        public MList<QueryOrderDN> Orders
        {
            get { return orders; }
            set { Set(ref orders, value); }
        }

        ColumnOptionsMode columnsMode;
        public ColumnOptionsMode ColumnsMode
        {
            get { return columnsMode; }
            set { Set(ref columnsMode, value); }
        }

        [NotNullable, PreserveOrder]
        MList<QueryColumnDN> columns = new MList<QueryColumnDN>();
        public MList<QueryColumnDN>  Columns
        {
            get { return columns; }
            set { Set(ref columns, value); }
        }

        PaginationMode? paginationMode;
        public PaginationMode? PaginationMode
        {
            get { return paginationMode; }
            set { if (Set(ref paginationMode, value)) Notify(() => ShouldHaveElements); }
        }

        int? elementsPerPage;
        [NumberIsValidator(ComparisonType.GreaterThanOrEqual, 1)]
        public int? ElementsPerPage
        {
            get { return elementsPerPage; }
            set { Set(ref elementsPerPage, value); }
        }

        [UniqueIndex]
        Guid guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return guid; }
            set { Set(ref guid, value); }
        }

        static readonly Expression<Func<UserQueryDN, string>> ToStringExpression = e => e.displayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => ElementsPerPage))
            {
                if (ElementsPerPage != null && !ShouldHaveElements)
                    return UserQueryMessage._0ShouldBeNullIf1Is2.NiceToString().Formato(pi.NiceName(), NicePropertyName(() => PaginationMode), PaginationMode.Try(pm=>pm.NiceToString()) ?? "" );

                if (ElementsPerPage == null && ShouldHaveElements)
                    return UserQueryMessage._0ShouldBeSetIf1Is2.NiceToString().Formato(pi.NiceName(), NicePropertyName(() => PaginationMode), PaginationMode.NiceToString());
            }

            if (pi.Is(() => Filters) && WithoutFilters && Filters.Any())
                return UserQueryMessage._0ShouldBeEmptyIf1IsSet.NiceToString().Formato(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => WithoutFilters).NiceName());

            return base.PropertyValidation(pi);
        }

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
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(this, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement);

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(this, description, SubTokensOptions.CanElement);

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(this, description, SubTokensOptions.CanElement);
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserQuery",
                new XAttribute("Guid", Guid),
                new XAttribute("DisplayName", DisplayName),
                new XAttribute("Query", Query.Key),
                EntityType == null ? null : new XAttribute("EntityType", ctx.TypeToName(EntityType)),
                Owner == null ? null : new XAttribute("Owner", Owner.Key()),
                WithoutFilters == true ? null : new XAttribute("WithoutFilters", true),
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
            EntityType = element.Attribute("EntityType").Try(a => ctx.GetType(a.Value));
            Owner = element.Attribute("Owner").Try(a => Lite.Parse(a.Value));
            WithoutFilters = element.Attribute("WithoutFilters").Try(a => a.Value == true.ToString()) ?? false;
            ElementsPerPage = element.Attribute("ElementsPerPage").Try(a => int.Parse(a.Value));
            PaginationMode = element.Attribute("PaginationMode").Try(a => a.Value.ToEnum<PaginationMode>());
            ColumnsMode = element.Attribute("ColumnsMode").Value.ToEnum<ColumnOptionsMode>();
            Filters.Syncronize(element.Element("Filters").Try(fs => fs.Elements()).EmptyIfNull().ToList(), (f, x)=>f.FromXml(x, ctx));
            Columns.Syncronize(element.Element("Columns").Try(fs => fs.Elements()).EmptyIfNull().ToList(), (c, x)=>c.FromXml(x, ctx));
            Orders.Syncronize(element.Element("Orders").Try(fs => fs.Elements()).EmptyIfNull().ToList(), (o, x)=>o.FromXml(x, ctx));
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

    public static class UserQueryPermission
    {
        public static readonly PermissionSymbol ViewUserQuery = new PermissionSymbol();
    }

    public static class UserQueryOperation
    {
        public static readonly ExecuteSymbol<UserQueryDN> Save = OperationSymbol.Execute<UserQueryDN>();
        public static readonly DeleteSymbol<UserQueryDN> Delete = OperationSymbol.Delete<UserQueryDN>();
    }


    [Serializable]
    public class QueryOrderDN : EmbeddedEntity
    {
        [NotNullable]
        QueryTokenDN token;
        [NotNullValidator]
        public QueryTokenDN Token
        {
            get { return token; }
            set { Set(ref token, value); }
        }


        OrderType orderType;
        public OrderType OrderType
        {
            get { return orderType; }
            set { Set(ref orderType, value); }
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Orden",
                new XAttribute("Token", Token.Token.FullKey()),
                new XAttribute("OrderType", OrderType));
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            Token = new QueryTokenDN(element.Attribute("Token").Value);
            OrderType = element.Attribute("OrderType").Value.ToEnum<OrderType>();
        }

        public void ParseData(IdentifiableEntity context, QueryDescription description, SubTokensOptions options)
        {
            token.ParseData(context, description, options & ~SubTokensOptions.CanAnyAll);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Token) && token != null && token.Token != null)
            {
                return QueryUtils.CanOrder(token.Token);
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(token, orderType);
        }
    }

    [Serializable]
    public class QueryColumnDN : EmbeddedEntity
    {
        [NotNullable]
        QueryTokenDN token;
        [NotNullValidator]
        public QueryTokenDN Token
        {
            get { return token; }
            set { Set(ref token, value); }
        }

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
            Token = new QueryTokenDN(element.Attribute("Token").Value);
            DisplayName = element.Attribute("DisplayName").Try(a => a.Value);
        }

        public void ParseData(IdentifiableEntity context, QueryDescription description, SubTokensOptions options)
        {
            token.ParseData(context, description, options);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Token) && token != null && token.Token != null)
            {
                return QueryUtils.CanColumn(token.Token);
            }

            return base.PropertyValidation(pi);
        }

        public override string ToString()
        {
            return "{0} {1}".Formato(token, displayName);
        }
    }

    [Serializable]
    public class QueryFilterDN : EmbeddedEntity
    {
        public QueryFilterDN() { }

        [NotNullable]
        QueryTokenDN token;
        [NotNullValidator]
        public QueryTokenDN Token
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

        FilterOperation operation;
        public FilterOperation Operation
        {
            get { return operation; }
            set { Set(ref operation, value); }
        }

        [SqlDbType(Size = 300)]
        string valueString;
        [StringLengthValidator(AllowNulls = true, Max = 300)]
        public string ValueString
        {
            get { return valueString; }
            set { SetToStr(ref valueString, value); }
        }

        public void ParseData(IdentifiableEntity context, QueryDescription description, SubTokensOptions options)
        {
            token.ParseData(context, description, options);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (token != null)
            {
                if (pi.Is(() => Token) && token.Token != null)
                {
                    return QueryUtils.CanFilter(token.Token);
                }

                if (pi.Is(() => Operation))
                {
                    FilterType? filterType = QueryUtils.TryGetFilterType(Token.Token.Type);

                    if (filterType == null)
                        return UserQueryMessage._0IsNotFilterable.NiceToString().Formato(token);

                    if (!QueryUtils.GetFilterOperations(filterType.Value).Contains(operation))
                        return UserQueryMessage.TheFilterOperation0isNotCompatibleWith1.NiceToString().Formato(operation, filterType);
                }

                if (pi.Is(() => ValueString))
                {
                    object val;
                    return FilterValueConverter.TryParse(ValueString, Token.Token.Type, out val, operation == FilterOperation.IsIn);
                }
            }

            return null;
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Filter",
                new XAttribute("Token", Token.Token.FullKey()),
                new XAttribute("Operation", Operation),
                new XAttribute("Value", ValueString));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Token = new QueryTokenDN(element.Attribute("Token").Value);
            Operation = element.Attribute("Operation").Value.ToEnum<FilterOperation>();
            ValueString = element.Attribute("Value").Value;
        }

        public override string ToString()
        {
            return "{0} {1} {2}".Formato(token, operation, ValueString);
        }

      
    }

    public static class UserQueryUtils
    {
        public static Func<Lite<IdentifiableEntity>> DefaultRelated = () => UserDN.Current.ToLite();

        public static UserQueryDN ToUserQuery(this QueryRequest request, QueryDescription qd, QueryDN query, Pagination defaultPagination, bool withoutFilters)
        {
            var tuple = SmartColumns(request.Columns, qd);

            var defaultMode = defaultPagination.GetMode();
            var defaultElementsPerPage = defaultPagination.GetElementsPerPage();

            var mode = request.Pagination.GetMode();
            var elementsPerPage = request.Pagination.GetElementsPerPage();

            bool isDefaultPaginate = defaultMode == mode && defaultElementsPerPage == elementsPerPage;

            return new UserQueryDN
            {
                Query = query,
                WithoutFilters = withoutFilters,
                Owner = DefaultRelated(),
                Filters = withoutFilters ? new MList<QueryFilterDN>() : request.Filters.Select(f => new QueryFilterDN
                {
                    Token = new QueryTokenDN(f.Token),
                    Operation = f.Operation,
                    ValueString = FilterValueConverter.ToString(f.Value, f.Token.Type)
                }).ToMList(),
                ColumnsMode = tuple.Item1,
                Columns = tuple.Item2,
                Orders = request.Orders.Select(oo => new QueryOrderDN
                {
                    Token = new QueryTokenDN(oo.Token),
                    OrderType = oo.OrderType
                }).ToMList(),
                PaginationMode = isDefaultPaginate ? (PaginationMode?)null : mode,
                ElementsPerPage = isDefaultPaginate ? (int?)null : elementsPerPage,
            };
        }

        public static Tuple<ColumnOptionsMode, MList<QueryColumnDN>> SmartColumns(List<Column> current, QueryDescription qd)
        {
            var ideal = (from cd in qd.Columns
                         where !cd.IsEntity
                         select cd).ToList();

            foreach (var item in current)
            {
                if(item.Token.NiceName() == item.DisplayName)
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
                    return Tuple.Create(ColumnOptionsMode.Remove, toRemove.Select(c => new QueryColumnDN { Token = new QueryTokenDN(c.Token) }).ToMList());
            }
            else
            {
                if (current.Zip(ideal).All(t => t.Item1.Similar(t.Item2)))
                    return Tuple.Create(ColumnOptionsMode.Add, current.Skip(ideal.Count).Select(c => new QueryColumnDN
                    {
                        Token = new QueryTokenDN(c.Token),
                        DisplayName = c.DisplayName
                    }).ToMList());

            }

            return Tuple.Create(ColumnOptionsMode.Replace, current.Select(c => new QueryColumnDN
            {
                Token = new QueryTokenDN(c.Token),
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
        Use0ToFilterCurrentEntity
    }
}
