using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Signum.Entities;
using Signum.React.ApiControllers;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Engine.Basics;
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
