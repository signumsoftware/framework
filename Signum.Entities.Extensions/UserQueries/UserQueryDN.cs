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
using Signum.Entities.Reports;
using System.Linq.Expressions;
using System.ComponentModel;
using Signum.Entities.Authorization;
using System.Xml.Linq;

namespace Signum.Entities.UserQueries
{
    [Serializable, EntityKind(EntityKind.Main)]
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
            set { Set(ref query, value, () => Query); }
        }

        Lite<TypeDN> entityType;
        public Lite<TypeDN> EntityType
        {
            get { return entityType; }
            set { Set(ref entityType, value, () => EntityType); }
        }

        Lite<IdentifiableEntity> related;
        public Lite<IdentifiableEntity> Related
        {
            get { return related; }
            set { Set(ref related, value, () => Related); }
        }

        [NotNullable]
        string displayName;
        [StringLengthValidator(Min = 1)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetToStr(ref displayName, value, () => DisplayName); }
        }

        bool withoutFilters;
        public bool WithoutFilters
        {
            get { return withoutFilters; }
            set
            {
                if (Set(ref withoutFilters, value, () => WithoutFilters) && withoutFilters)
                    filters.Clear();
            }
        }

        [NotNullable]
        MList<QueryFilterDN> filters = new MList<QueryFilterDN>();
        public MList<QueryFilterDN> Filters
        {
            get { return filters; }
            set { Set(ref filters, value, () => Filters); }
        }

        [NotNullable]
        MList<QueryOrderDN> orders = new MList<QueryOrderDN>();
        public MList<QueryOrderDN> Orders
        {
            get { return orders; }
            set { Set(ref orders, value, () => Orders); }
        }

        ColumnOptionsMode columnsMode;
        public ColumnOptionsMode ColumnsMode
        {
            get { return columnsMode; }
            set { Set(ref columnsMode, value, () => ColumnsMode); }
        }

        [NotNullable]
        MList<QueryColumnDN> columns = new MList<QueryColumnDN>();
        public MList<QueryColumnDN>  Columns
        {
            get { return columns; }
            set { Set(ref columns, value, () => Columns); }
        }

        int? elementsPerPage;
        [NumberIsValidator(ComparisonType.GreaterThanOrEqual, -1)]
        public int? ElementsPerPage
        {
            get { return elementsPerPage; }
            set { Set(ref elementsPerPage, value, () => ElementsPerPage); }
        }

        [UniqueIndex]
        Guid guid = Guid.NewGuid();
        public Guid Guid
        {
            get { return guid; }
            set { Set(ref guid, value, () => Guid); }
        }

        protected override void PostRetrieving()
        {
            if (Orders != null)
                Orders.Sort(a => a.Index);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            base.PreSaving(ref graphModified);

            if (Orders != null)
                for (int i = 0; i < Orders.Count; i++)
                    Orders[i].Index = i;
        }

        static readonly Expression<Func<UserQueryDN, string>> ToStringExpression = e => e.displayName;
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => ElementsPerPage) && ElementsPerPage <= 0 && ElementsPerPage != -1)
                return UserQueryMessage.ShouldBe1AllEmptyDefaultOrANumberGreaterThanZero.NiceToString();

            if (pi.Is(() => Filters) && WithoutFilters && Filters.Any())
                return "{0} should be empty if {1} is set".Formato(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => WithoutFilters).NiceName());

            return base.PropertyValidation(pi);
        }

        internal void ParseData(QueryDescription description)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(this, description, canAggregate: false);

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(this, description, canAggregate: false);

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(this, description, canAggregate: false);
        }

        public void SetFilterValues()
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.SetValue();
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("UserQuery",
                new XAttribute("Guid", Guid),
                new XAttribute("DisplayName", DisplayName),
                new XAttribute("Query", Query.Name),
                EntityType == null ? null : new XAttribute("EntityType", EntityType.Key()),
                Related == null ? null : new XAttribute("Related", Related.Key()),
                WithoutFilters == true ? null : new XAttribute("WithoutFilters", true),
                ElementsPerPage != null ? null : new XAttribute("ElementsPerPage", ElementsPerPage),
                new XAttribute("ColumnsMode", ColumnsMode),
                Filters.IsNullOrEmpty() ? null : new XElement("Filters", Filters.Select(f => f.ToXml(ctx)).ToList()),
                Columns.IsNullOrEmpty() ? null : new XElement("Columns", Columns.Select(c => c.ToXml(ctx)).ToList()),
                Orders.IsNullOrEmpty() ? null : new XElement("Orders", Orders.Select(o => o.ToXml(ctx)).ToList()));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            Query = ctx.GetQuery(element.Attribute("Query").Value);
            DisplayName = element.Attribute("DisplayName").Value;
            EntityType = element.Attribute("EntityType").TryCC(a => Lite.Parse<TypeDN>(a.Value));
            Related = element.Attribute("Related").TryCC(a => Lite.Parse(a.Value));
            WithoutFilters = element.Attribute("WithoutFilters").TryCS(a => a.Value == true.ToString()) ?? false;
            ElementsPerPage = element.Attribute("ElementsPerPage").TryCS(a => int.Parse(a.Value));
            ColumnsMode = element.Attribute("ElementsPerPage").Value.ToEnum<ColumnOptionsMode>();
            Filters.Syncronize(element.Element("Filters").TryCC(fs => fs.Elements()).EmptyIfNull().ToList(), (f, x)=>f.FromXml(x, ctx));
            Columns.Syncronize(element.Element("Columns").TryCC(fs => fs.Elements()).EmptyIfNull().ToList(), (c, x)=>c.FromXml(x, ctx));
            Orders.Syncronize(element.Element("Orders").TryCC(fs => fs.Elements()).EmptyIfNull().ToList(), (o, x)=>o.FromXml(x, ctx));   
        }
    }

    public interface IToXmlContext
    {
        Guid Include(IUserAssetEntity content);

        string TypeToName(Lite<TypeDN> type);
    }

    public interface IFromXmlContext
    {
        QueryDN GetQuery(string queryName);

        IUserAssetEntity GetEntity(Guid guid);

        Lite<TypeDN> NameToType(string cleanName);
    }

    public interface IUserAssetEntity : IIdentifiable
    {
        Guid Guid { get; set; }

        XElement ToXml(IToXmlContext ctx);

        void FromXml(XElement element, IFromXmlContext ctx);
    }

    public enum UserQueryPermission
    {
        ViewUserQuery
    }

    public enum UserQueryOperation
    { 
        Save, 
        Delete
    }

    [Serializable]
    public abstract class QueryTokenDN : EmbeddedEntity
    {
        [NotNullable]
        protected string tokenString;
        [StringLengthValidator(AllowNulls = false, Min = 1)]
        public string TokenString
        {
            get { return tokenString; }
            set { SetToStr(ref tokenString, value, () => TokenString); }
        }

        [Ignore]
        protected QueryToken token;
        [HiddenProperty]
        public QueryToken Token
        {
            get
            {
                if (parseException != null && token == null)
                    throw parseException;

                return token;
            }
            set { if (Set(ref token, value, () => Token)) TokenChanged(); }
        }

        [HiddenProperty]
        public QueryToken TryToken
        {
            get { return token; }
            set { if (Set(ref token, value, () => Token)) TokenChanged(); }
        }

        [Ignore]
        protected Exception parseException;
        [HiddenProperty]
        public Exception ParseException
        {
            get { return parseException; }
        }

        public virtual void TokenChanged()
        {
            parseException = null;
            Notify(() => Token);
            Notify(() => TryToken);
        }

        protected override void PreSaving(ref bool graphModified)
        {
            if (token != null)
                tokenString = token.FullKey();
        }

        public abstract void ParseData(IdentifiableEntity context, QueryDescription description, bool canAggregate);

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => TokenString) && token == null)
            {
                return parseException != null ? parseException.Message : ValidationMessage._0IsNotSet.NiceToString().Formato(pi.NiceName());
            }

            return base.PropertyValidation(pi);
        }
    }

    public static class FromXmlExtensions
    {
        public static void Syncronize<T>(this MList<T> entities, List<XElement> xElements, Action<T, XElement> syncAction)
            where T : new()
        {
            for (int i = 0; i < xElements.Count; i++)
			{
                T entity;
                if(entities.Count == i)
                {
                    entity = new T();
                    entities.Add(entity); 
                }
                else
                    entity = entities[i];

                syncAction(entity, xElements[i]); 
			}

            if(entities.Count > xElements.Count)
            {
                entities.RemoveRange(entities.Count, entities.Count - xElements.Count); 
            }
        }
    }

    [Serializable]
    public class QueryOrderDN : QueryTokenDN
    {
        public QueryOrderDN() {}

        public QueryOrderDN(string columnName, OrderType type)
        {
            this.TokenString = columnName;
            orderType = type;
        }

        int index;
        public int Index
        {
            get { return index; }
            set { Set(ref index, value, () => Index); }
        }

        OrderType orderType;
        public OrderType OrderType
        {
            get { return orderType; }
            set { Set(ref orderType, value, () => OrderType); }
        }

        public override void ParseData(IdentifiableEntity context, QueryDescription description, bool canAggregate)
        {
            try
            {
                token = QueryUtils.Parse(tokenString, description, canAggregate);
            }
            catch (Exception e)
            {
                parseException = new FormatException("{0} {1}: {2}\r\n{3}".Formato(context.GetType().Name, context.IdOrNull, context, e.Message));
            }
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Orden",
                new XAttribute("Token", Token.FullKey()),
                new XAttribute("OrderType", OrderType));
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            TokenString = element.Attribute("Token").Value;
            OrderType = element.Attribute("OrderType").Value.ToEnum<OrderType>();
        }
    }

    [Serializable]
    public class QueryColumnDN : QueryTokenDN
    {
        public QueryColumnDN(){}

        public QueryColumnDN(string columnName)
        {
            this.TokenString = columnName;
        }

        public QueryColumnDN(Column col)
        {
            Token = col.Token;
            if (col.DisplayName != col.Token.NiceName())
                DisplayName = col.DisplayName;
        }

        [SqlDbType(Size = 100)]
        string displayName;
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string DisplayName
        {
            get { return displayName; }
            set { SetToStr(ref displayName, value, () => DisplayName); }
        }


        public override void ParseData(IdentifiableEntity context, QueryDescription description, bool canAggregate)
        {
            try
            {
                token = QueryUtils.Parse(tokenString, description, canAggregate);
            }
            catch (Exception e)
            {
                parseException = new FormatException("{0} {1}: {2}\r\n{3}".Formato(context.GetType().Name, context.IdOrNull, context, e.Message));
            }
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Column",
                new XAttribute("Token", Token.FullKey()),
                DisplayName != null ? new XAttribute("DisplayName", DisplayName) : null);
        }

        internal void FromXml(XElement element, IFromXmlContext ctx)
        {
            TokenString = element.Attribute("Token").Value;
            DisplayName = element.Attribute("DisplayName").TryCC(a => a.Value);
        }
    }

    [Serializable]
    public class QueryFilterDN : QueryTokenDN
    {
        public QueryFilterDN() { }

        public QueryFilterDN(string columnName)
        {
            this.TokenString = columnName;
        }

        public QueryFilterDN(string columnName, object value)
        {
            this.TokenString = columnName;
            this.value = value;
            this.operation = FilterOperation.EqualTo;
        }

        FilterOperation operation;
        public FilterOperation Operation
        {
            get { return operation; }
            set { Set(ref operation, value, () => Operation); }
        }

        [SqlDbType(Size = 100)]
        string valueString;
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string ValueString
        {
            get { return valueString; }
            set { SetToStr(ref valueString, value, () => ValueString); }
        }

        [Ignore]
        object value;
        public object Value
        {
            get { return value; }
            set { this.value = value; }
        }

        public override void ParseData(IdentifiableEntity context, QueryDescription description, bool canAggregate)
        {
            try
            {
                token = QueryUtils.Parse(tokenString, description, canAggregate);
            }
            catch (Exception e)
            {
                parseException = new FormatException("{0} {1}: {2}\r\n{3}".Formato(context.GetType().Name, context.IdOrNull, context, e.Message));
            }

            if (token != null)
            {
                if (value != null)
                {
                    if (valueString.HasText())
                        throw new InvalidOperationException("Value and ValueString defined at the same time");

                    ValueString = FilterValueConverter.ToString(value, Token.Type);
                }
                else
                {
                    SetValue();
                }
            }
        }

        public void SetValue()
        {
            object val;
            string error = FilterValueConverter.TryParse(ValueString, Token.Type, out val);
            if (string.IsNullOrEmpty(error))
                Value = val; //Executed on server only
        }

        public override void TokenChanged()
        {
            Notify(() => Operation);
            Notify(() => ValueString);

            base.TokenChanged();
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (token != null)
            {
                if (pi.Is(() => Operation))
                {
                    FilterType? filterType = QueryUtils.TryGetFilterType(Token.Type);

                    if (filterType == null)
                        return UserQueryMessage._0IsNotFilterable.NiceToString().Formato(token);

                    if (!QueryUtils.GetFilterOperations(filterType.Value).Contains(operation))
                        return UserQueryMessage.TheFilterOperation0isNotCompatibleWith1.NiceToString().Formato(operation, filterType);
                }

                if (pi.Is(() => ValueString))
                {
                    object val;
                    return FilterValueConverter.TryParse(ValueString, Token.Type, out val);
                }
            }

            return null;
        }

        public XElement ToXml(IToXmlContext ctx)
        {
            return new XElement("Filter",
                new XAttribute("Token", Token.FullKey()),
                new XAttribute("Operation", Operation),
                new XAttribute("Value", ValueString));
        }

        public void FromXml(XElement element, IFromXmlContext ctx)
        {
            TokenString = element.Attribute("Token").Value;
            Operation = element.Attribute("Operation").Value.ToEnum<FilterOperation>();
            ValueString = element.Attribute("Value").Value;
        }
    }

    public static class UserQueryUtils
    {
        public static Func<Lite<IdentifiableEntity>> DefaultRelated = () => UserDN.Current.ToLite();

        public static UserQueryDN ToUserQuery(this QueryRequest request, QueryDescription qd, QueryDN query, int defaultElementsPerPage, bool withoutFilters)
        {
            var tuple = SmartColumns(request.Columns, qd);

            return new UserQueryDN
            {
                Query = query,
                WithoutFilters = withoutFilters,
                Related = DefaultRelated(),
                Filters = withoutFilters ? new MList<QueryFilterDN>() : request.Filters.Select(f => new QueryFilterDN
                {
                    Token = f.Token,
                    Operation = f.Operation,
                    ValueString = FilterValueConverter.ToString(f.Value, f.Token.Type)
                }).ToMList(),
                ColumnsMode = tuple.Item1,
                Columns = tuple.Item2,
                Orders = request.Orders.Select(oo => new QueryOrderDN
                {
                    Token = oo.Token,
                    OrderType = oo.OrderType
                }).ToMList(),
                ElementsPerPage = (request.ElementsPerPage == defaultElementsPerPage) ? (int?)null : request.ElementsPerPage,
            };
        }

        public static Tuple<ColumnOptionsMode, MList<QueryColumnDN>> SmartColumns(List<Column> current, QueryDescription qd)
        {
            var ideal = (from cd in qd.Columns
                         where !cd.IsEntity
                         select new Column(cd, qd.QueryName)).ToList();

            if (current.Count < ideal.Count)
            {
                List<Column> toRemove = new List<Column>();
                int j = 0;
                for (int i = 0; i < ideal.Count; i++)
                {
                    if (j < current.Count && current[j].Equals(ideal[i]))
                        j++;
                    else
                        toRemove.Add(ideal[i]);
                }

                if (toRemove.Count + current.Count == ideal.Count)
                    return Tuple.Create(ColumnOptionsMode.Remove, toRemove.Select(c => new QueryColumnDN { Token = c.Token }).ToMList());
            }
            else
            {
                if (current.Zip(ideal).All(t => t.Item1.Equals(t.Item2)))
                    return Tuple.Create(ColumnOptionsMode.Add, current.Skip(ideal.Count).Select(c => new QueryColumnDN(c)).ToMList());

            }

            return Tuple.Create(ColumnOptionsMode.Replace, current.Select(c => new QueryColumnDN(c)).ToMList());
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
        [Description("{0} should be -1 (all), empty (default), or a number greater than zero")]
        ShouldBe1AllEmptyDefaultOrANumberGreaterThanZero,
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
