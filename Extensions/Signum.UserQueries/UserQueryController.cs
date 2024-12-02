using Microsoft.AspNetCore.Mvc;
using Signum.API.Filters;
using Signum.API;
using Signum.UserAssets;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using System.Threading;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Signum.Authorization;

namespace Signum.UserQueries;

public class UserQueryController : ControllerBase
{
    [HttpGet("api/userQueries/forEntityType/{typeName}")]
    public IEnumerable<Lite<UserQueryEntity>> FromEntityType(string typeName)
    {
        return UserQueryLogic.GetUserQueriesModel(TypeLogic.GetType(typeName));
    }

    [HttpPost("api/userQueries/translated")]
    public UserQueryLiteModel Translated([FromBody]Lite<UserQueryEntity> lite)
    {
        var uq = UserQueryLogic.UserQueries.Value[lite];

        return UserQueryLiteModel.Translated(uq);
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

    [HttpGet("api/userQueries/healthCheck/{id}"), SignumAllowAnonymous]
    public async Task<SignumHealthResult> HealthCheck(string id, CancellationToken cancellationToken)
    {
        var pId = PrimaryKey.Parse(id, typeof(UserQueryEntity));
        var uq = UserQueryLogic.UserQueries.Value.TryGetC(Lite.Create<UserQueryEntity>(pId));

        if (uq == null || uq.HealthCheck == null)
            throw new InvalidOperationException($"UserQuery with id {id} does not exist or is not a health check");

        int value;
        using (AuthLogic.Disable())
        {
            var qr = UserQueryLogic.ToQueryRequestValue(uq);

            var rt = await QueryLogic.Queries.ExecuteQueryAsync(qr, cancellationToken);

            var row = rt.Rows.SingleEx();

            value = (int)row[rt.Columns.SingleEx()]!;
        }

        string? errorMessage = CheckWhen(uq.HealthCheck.FailWhen, value);
        if (errorMessage != null)
            return new SignumHealthResult(new HealthCheckResult(HealthStatus.Unhealthy, errorMessage));

        string? warningMessage = CheckWhen(uq.HealthCheck.DegradedWhen, value);
        if (warningMessage != null)
            return new SignumHealthResult(new HealthCheckResult(HealthStatus.Degraded, warningMessage));

        return new SignumHealthResult(new HealthCheckResult(HealthStatus.Healthy, $"{value}"));
    }

    string? CheckWhen(HealthCheckConditionEmbedded? condition, int value)
    {
        if (condition == null)
            return null;

        if (condition._CachedPredicate == null)
        {
            var p = Expression.Parameter(typeof(int));
            var exp = Expression.Lambda<Func<int, bool>>(QueryUtils.GetCompareExpression(condition.Operation, p, Expression.Constant(condition.Value)), p);
            condition._CachedPredicate = exp.Compile();
        }

        if (condition._CachedPredicate(value))
        {
            return $"{value} ({condition.Operation} {condition.Value})";
        }

        return null;
    }
}
