using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Signum.Entities;
using Signum.Utilities;
using Signum.Entities.Reports;

namespace Signum.Web.Queries.Models
{
    [Serializable()]
    public class QueryTokenModel : Entity
    {
        public QueryTokenModel() { }

        public QueryTokenModel(QueryTokenDN token, string queryNameToStr)
        {
            QueryToken = token;
            QueryNameToStr = queryNameToStr;
        }

        QueryTokenDN queryToken;
        [NotNullValidator]
        public QueryTokenDN QueryToken
        {
            get { return queryToken; }
            set { Set(ref queryToken, value, () => QueryToken); }
        }

        string queryNameToStr;
        [StringLengthValidator(AllowNulls=false, Min=1)]
        public string QueryNameToStr
        {
            get { return queryNameToStr; }
            set { Set(ref queryNameToStr, value, () => QueryNameToStr); }
        }
    }
}
