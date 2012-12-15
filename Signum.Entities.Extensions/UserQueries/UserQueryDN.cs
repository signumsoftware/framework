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
using Signum.Entities.Extensions.Properties;

namespace Signum.Entities.UserQueries
{
    [Serializable, EntityType(EntityType.Main)]
    public class UserQueryDN : Entity
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

        [ImplementedBy()]
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
                return Resources.ShouldBe1AllEmptyDefaultOrANumberGreaterThanZero;

            return base.PropertyValidation(pi);
        }

        internal void ParseData(QueryDescription description)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(description, this);

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(description, this);

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(description, this);
        }
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
        [StringLengthValidator(AllowNulls = false, Min = 1), AvoidLocalization]
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
                if (parseException != null)
                    throw parseException;
                return token;
            }
            set { if (Set(ref token, value, () => Token)) TokenChanged(); }
        }

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
        }

        protected override void PreSaving(ref bool graphModified)
        {
            tokenString = token.FullKey();
        }

        public virtual void ParseData(QueryDescription desc, IdentifiableEntity context)
        {
            ParseData(t => QueryUtils.SubTokens(t, desc.Columns), context);
        }

        public abstract void ParseData(Func<QueryToken, List<QueryToken>> subTokens, IdentifiableEntity context);

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Is(() => Token) && parseException != null)
            {
                return parseException.Message;
            }

            return base.PropertyValidation(pi);
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

        public override void ParseData(Func<QueryToken, List<QueryToken>> subTokens, IdentifiableEntity context)
        {
            try
            {
                Token = QueryUtils.Parse(tokenString, subTokens);
            }
            catch (Exception e)
            {
                parseException = new FormatException("{0} {1}: {2}\r\n{3}".Formato(context.GetType().Name, context.IdOrNull, context, e.Message));
            }
            CleanSelfModified();
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


        public override void ParseData(Func<QueryToken,List<QueryToken>> subTokens, IdentifiableEntity context)
        {
            try
            {
                Token = QueryUtils.Parse(tokenString, subTokens);
            }
            catch (Exception e)
            {
                parseException = new FormatException("{0} {1}: {2}\r\n{3}".Formato(context.GetType().Name, context.IdOrNull, context, e.Message));
            }
            CleanSelfModified();
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

        public override void ParseData(Func<QueryToken, List<QueryToken>> subTokens, IdentifiableEntity context)
        {
            try
            {
                Token = QueryUtils.Parse(tokenString, subTokens);
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
                    object val;
                    string error = FilterValueConverter.TryParse(ValueString, Token.Type, out val);
                    if (string.IsNullOrEmpty(error))
                        Value = val; //Executed on server only

                    CleanSelfModified();
                }
            }
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
                FilterType filterType = QueryUtils.GetFilterType(Token.Type);

                if (pi.Is(() => Operation))
                {
                    if (!QueryUtils.GetFilterOperations(filterType).Contains(operation))
                        return "The Filter Operation {0} is not compatible with {1}".Formato(operation, filterType);
                }

                if (pi.Is(() => ValueString))
                {
                    object val;
                    return FilterValueConverter.TryParse(ValueString, Token.Type, out val);
                }
            }

            return null;
        }
    }

    public static class UserQueryUtils
    {
        public static UserQueryDN ToUserQuery(this QueryRequest request, QueryDescription qd, QueryDN query, int defaultElementsPerPage)
        {
            var tuple = SmartColumns(request.Columns, qd.Columns);

            return new UserQueryDN
            {
                Query = query,
                Filters = request.Filters.Select(f => new QueryFilterDN
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

        public static Tuple<ColumnOptionsMode, MList<QueryColumnDN>> SmartColumns(List<Column> current, List<ColumnDescription> descriptions)
        {
            var ideal = (from cd in descriptions
                         where !cd.IsEntity
                         select new Column(cd)).ToList();

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
}
