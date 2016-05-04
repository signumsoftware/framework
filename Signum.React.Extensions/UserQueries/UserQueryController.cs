using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
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

namespace Signum.React.UserQueries
{
    public class UserQueryController : ApiController
    {
        [Route("api/userQueries/forQuery/{queryKey}"), HttpGet]
        public IEnumerable<Lite<UserQueryEntity>> FromQuery(string queryKey)
        {
            return UserQueryLogic.GetUserQueries(QueryLogic.ToQueryName(queryKey));
        }

        [Route("api/userQueries/forEntityType/{typeName}"), HttpGet]
        public IEnumerable<Lite<UserQueryEntity>> FromEntityType(string typeName)
        {
            return UserQueryLogic.GetUserQueriesEntity(TypeLogic.GetType(typeName));
        }

        [Route("api/userQueries/fromQueryRequest"), HttpPost]
        public UserQueryEntity FromQueryRequest(CreateRequest request)
        {
            var qr = request.queryRequest.ToQueryRequest();
            var qd = DynamicQueryManager.Current.QueryDescription(qr.QueryName);
            return qr.ToUserQuery(qd, QueryLogic.GetQueryEntity(qd.QueryName), request.defaultPagination.ToPagination(), withoutFilters: false);
        }

        public class CreateRequest
        {
            public QueryRequestTS queryRequest;
            public PaginationTS defaultPagination;
        }
    }
}