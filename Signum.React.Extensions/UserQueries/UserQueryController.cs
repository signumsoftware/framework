using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using Signum.Engine.Authorization;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Services;
using Signum.Utilities;
using Signum.React.Facades;
using Signum.React.Authorization;
using Signum.React.ApiControllers;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Engine.Basics;
using Signum.Entities.UserAssets;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine;
using Microsoft.AspNetCore.Mvc;

namespace Signum.React.UserQueries
{
    public class UserQueryController : ApiController
    {
        [HttpGet("api/userQueries/forQuery/{queryKey}")]
        public IEnumerable<Lite<UserQueryEntity>> FromQuery(string queryKey)
        {
            return UserQueryLogic.GetUserQueries(QueryLogic.ToQueryName(queryKey));
        }

        [HttpGet("api/userQueries/forEntityType/{typeName}")]
        public IEnumerable<Lite<UserQueryEntity>> FromEntityType(string typeName)
        {
            return UserQueryLogic.GetUserQueriesEntity(TypeLogic.GetType(typeName));
        }

        [HttpPost("api/userQueries/fromQueryRequest")]
        public UserQueryEntity FromQueryRequest([Required, FromBody]CreateRequest request)
        {
            var qr = request.queryRequest.ToQueryRequest();
            var qd = QueryLogic.Queries.QueryDescription(qr.QueryName);
            return qr.ToUserQuery(qd, QueryLogic.GetQueryEntity(qd.QueryName), request.defaultPagination.ToPagination());
        }

        public class CreateRequest
        {
            public QueryRequestTS queryRequest;
            public PaginationTS defaultPagination;
        }
    }
}
