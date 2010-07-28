using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Reports;
using Signum.Entities.DynamicQuery;

namespace Signum.Web.Queries.Models
{
    [Serializable()]
    public class QueryFilterModel : Entity
    {
        public QueryFilterModel() { }

        public QueryFilterModel(QueryFilterDN filter, string queryUrlName)
        {
            Operation = filter.Operation;
            ValueString = filter.ValueString;
            QueryToken = new QueryTokenModel((QueryTokenDN)filter, queryUrlName);
        }

        QueryTokenModel queryToken;
        [NotNullValidator]
        public QueryTokenModel QueryToken
        {
            get { return queryToken; }
            set { Set(ref queryToken, value, () => QueryToken); }
        }

        FilterOperation operation;
        public FilterOperation Operation
        {
            get { return operation; }
            set { Set(ref operation, value, () => Operation); }
        }
        
        string valueString;
        [StringLengthValidator(AllowNulls = true)]
        public string ValueString
        {
            get { return valueString; }
            set { SetToStr(ref valueString, value, () => ValueString); }
        }

        public QueryFilterDN ToQueryFilterDN()
        {
            return new QueryFilterDN
            {
                Token = this.QueryToken.QueryToken.Token,
                Operation = this.Operation,
                ValueString = this.ValueString
            };
        }
    }
}
