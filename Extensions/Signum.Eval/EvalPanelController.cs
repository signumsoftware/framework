using Microsoft.AspNetCore.Mvc;
using Signum.API;
using Signum.API.Filters;
using Signum.API.Json;
using System.ComponentModel.DataAnnotations;

namespace Signum.Eval;

[ValidateModelFilter]
public class EvalPanelController : ControllerBase
{
    [HttpPost("api/eval/evalErrors")]
    public async Task<List<EvalEntityError>> GetEvalErrors([Required, FromBody] QueryEntitiesRequestTS request)
    {
        EvalPanelPermission.ViewDynamicPanel.AssertAuthorized();

        var allEntities = await QueryLogic.Queries.GetEntitiesLite(request.ToQueryEntitiesRequest(request.queryKey, SignumServer.JsonSerializerOptions)).Select(a => a.Entity).ToListAsync();

        return allEntities.Select(entity =>
        {
            GraphExplorer.PreSaving(() => GraphExplorer.FromRoot(entity));

            return new EvalEntityError
            {
                lite = entity.ToLite(),
                error = entity.FullIntegrityCheck().EmptyIfNull().Select(a => a.Value).SelectMany(a => a.Errors.Values).ToString("\n")
            };
        })
        .Where(ee => ee.error.HasText())
        .ToList();
    }
}


public class EvalEntityError
{
    public Lite<Entity> lite;
    public string error;
}
