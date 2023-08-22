using Microsoft.AspNetCore.Mvc;
using Signum.UserAssets;

namespace Signum.UserQueries;

public class UserQueryController : ControllerBase
{
    [HttpGet("api/userQueries/forEntityType/{typeName}")]
    public IEnumerable<Lite<UserQueryEntity>> FromEntityType(string typeName)
    {
        return UserQueryLogic.GetUserQueriesModel(TypeLogic.GetType(typeName));
    }

    [HttpGet("api/userQueries/forQuery/{queryKey}")]
    public IEnumerable<Lite<UserQueryEntity>> FromQuery(string queryKey)
    {
        return UserQueryLogic.GetUserQueries(QueryLogic.ToQueryName(queryKey), appendFilters: false);
    }

    [HttpGet("api/userQueries/forQueryAppendFilters/{queryKey}")]
    public IEnumerable<Lite<UserQueryEntity>> FromQueryAppendFilters(string queryKey)
    {
        return UserQueryLogic.GetUserQueries(QueryLogic.ToQueryName(queryKey), appendFilters : true);
    }
}
