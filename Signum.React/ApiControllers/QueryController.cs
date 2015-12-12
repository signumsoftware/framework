using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;

namespace Signum.React.ApiControllers
{
    public class QueryController : ApiController
    {
        [Route("api/query/description/{queryName}")]
        public QueryDescription GetQueryDescription(string queryName)
        {
            var qn = QueryLogic.ToQueryName(queryName);
            return DynamicQueryManager.Current.QueryDescription(qn);
        }
    }
}