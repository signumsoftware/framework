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

namespace Signum.Entities.UserQueries
{
    [Serializable]
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

        int? maxItems;
        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int? MaxItems
        {
            get { return maxItems; }
            set { Set(ref maxItems, value, () => MaxItems); }
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

        public override string ToString()
        {
            return displayName;
        }

        internal void ParseData(QueryDescription description)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(description);

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(description);

            if (Orders != null)
                foreach (var o in Orders)
                    o.ParseData(description);
        }
    }

    [Serializable]
    public abstract class QueryTokenDN : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        protected string tokenString;
        [StringLengthValidator(AllowNulls = false, Min = 1, Max = 100), AvoidLocalization]
        public string TokenString
        {
            get { return tokenString; }
            set { SetToStr(ref tokenString, value, () => TokenString); }
        }

        [Ignore]
        protected QueryToken token;
        [NotNullValidator]
        public QueryToken Token
        {
            get { return token; }
            set { if (Set(ref token, value, () => Token)) TokenChanged(); }
        }

        protected virtual void TokenChanged()
        {

        }

        protected override void PreSaving(ref bool graphModified)
        {
            tokenString = token.FullKey();
        }

        public abstract void ParseData(QueryDescription queryDescription);
    }

    [Serializable]
    public class QueryOrderDN : QueryTokenDN
    {
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

        public override void ParseData(QueryDescription queryDescription)
        {
            Token = QueryUtils.Parse(tokenString, queryDescription);
            CleanSelfModified();
        }

    }

    [Serializable]
    public class QueryColumnDN : QueryTokenDN
    {
        public QueryColumnDN(){}

        public QueryColumnDN(Column col)
        {
            Token = col.Token;
            if(col.DisplayName != col.Token.NiceName())
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


        public override void ParseData(QueryDescription queryDescription)
        {
            Token = QueryUtils.Parse(tokenString, queryDescription);
            CleanSelfModified();
        }

        public static Tuple<ColumnOptionsMode, MList<QueryColumnDN>> SmartColumns(List<Column> current, List<ColumnDescription> descriptions)
        {
            var ideal = (from cd in descriptions
                         where !cd.IsEntity
                         select new Column(cd)).ToList();

            if(current.Count < ideal.Count)
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

                if(toRemove.Count + current.Count == ideal.Count)
                    return Tuple.Create(ColumnOptionsMode.Remove, toRemove.Select(c=> new QueryColumnDN{ Token = c.Token }).ToMList());
            }
            else
            {
                if(current.Zip(ideal).All(t=>t.Item1.Equals(t.Item2)))
                    return Tuple.Create(ColumnOptionsMode.Add, current.Skip(ideal.Count).Select(c=> new QueryColumnDN(c)).ToMList()); 

            }

            return Tuple.Create(ColumnOptionsMode.Replace, current.Select(c=> new QueryColumnDN(c)).ToMList()); 
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

        public override void ParseData(QueryDescription queryDescription)
        {
            Token = QueryUtils.Parse(tokenString, queryDescription);

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

        protected override void TokenChanged()
        {
            Notify(() => Operation);
            Notify(() => ValueString);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (Token != null)
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
}
