using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Microsoft.AspNetCore.Mvc;
using Signum.Entities.UserAssets;

namespace Signum.React.UserQueries;

public class UserQueryController : ControllerBase
{


    [HttpGet("api/userQueries/forEntityType/{typeName}")]
    public IEnumerable<UserAssetModel<UserQueryEntity>> FromEntityType(string typeName)
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
