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
    public class QueryColumnModel : Entity
    {
        public QueryColumnModel() { }

        public QueryColumnModel(QueryColumnDN column, string queryUrlName)
        {
            DisplayName = column.DisplayName;
            QueryToken = new QueryTokenModel(column, queryUrlName);
        }

        string displayName;
        [StringLengthValidator(AllowNulls = false, Min = 3)]
        public string DisplayName
        {
            get { return displayName; }
            set { Set(ref displayName, value, () => DisplayName); }
        }

        QueryTokenModel queryToken;
        [NotNullValidator]
        public QueryTokenModel QueryToken
        {
            get { return queryToken; }
            set { Set(ref queryToken, value, () => QueryToken); }
        }

        public QueryColumnDN ToQueryColumnDN()
        {
            return new QueryColumnDN
            {
                Token = this.QueryToken.QueryToken.Token,
                DisplayName = this.DisplayName
            };
        }
    }
}
