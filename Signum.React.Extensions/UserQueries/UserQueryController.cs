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
    public class UserQueryController : ControllerBase
    {


        [HttpGet("api/userQueries/forEntityType/{typeName}")]
        public IEnumerable<Lite<UserQueryEntity>> FromEntityType(string typeName)
        {
            return UserQueryLogic.GetUserQueriesEntity(TypeLogic.GetType(typeName));
        }

        [HttpGet("api/userQueries/forQuery/{queryKey}")]
        public IEnumerable<Lite<UserQueryEntity>> FromQuery(string queryKey)
        {
            return UserQueryLogic.GetUserQueries(QueryLogic.ToQueryName(queryKey));
        }

        [HttpGet("api/userQueries/forQueryAppendFilters/{queryKey}")]
        public IEnumerable<Lite<UserQueryEntity>> FromQueryAppendFilters(string queryKey)
        {
            return UserQueryLogic.GetUserQueries(QueryLogic.ToQueryName(queryKey), appendFilterOnly : true);
        }

    }
}
