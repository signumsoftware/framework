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

namespace Signum.React.Auth
{
    public class UserQueryController : ApiController
    {
        [Route("api/userQueries/fromQuery"), HttpGet]
        public IEnumerable<Lite<UserQueryEntity>> FromQuery(string queryKey)
        {
            return UserQueryLogic.GetUserQueries(QueryLogic.ToQueryName(queryKey));
        }

        [Route("api/userQueries/forEntityType"), HttpGet]
        public IEnumerable<Lite<UserQueryEntity>> FromEntityType(string typeName)
        {
            return UserQueryLogic.GetUserQueriesEntity(TypeLogic.GetType(typeName));
        }

        [Route("api/userQueries/parseFilters"), HttpPost]
        public List<FilterTS> ParseFilters(ParseFiltersRequest request)
        {
            var queryName = QueryLogic.ToQueryName(request.queryKey);
            var qd = DynamicQueryManager.Current.QueryDescription(queryName);
            var options = SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | (request.canAggregate ? SubTokensOptions.CanAggregate : 0);

            using (request.entity != null ? CurrentEntityConverter.SetCurrentEntity(request.entity.Retrieve()) : null)
            {
                return request.filters
                        .Select(f => new FilterTS
                        {
                            token = f.Token.TokenString,
                            operation = f.Operation,
                            value = FilterValueConverter.Parse(f.ValueString, QueryUtils.Parse(f.Token.TokenString, qd, options).Type, f.Operation.IsList())
                        })
                        .ToList();
            }
        }

        public class ParseFiltersRequest
        {
            public string queryKey;
            public bool canAggregate;
            public List<QueryFilterEntity> filters;
            public Lite<Entity> entity;
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