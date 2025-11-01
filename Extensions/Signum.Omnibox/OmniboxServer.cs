using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.API.Controllers;

namespace Signum.Omnibox;

public static class OmniboxServer
{
    public static void Start(WebServerBuilder wsb, params IOmniboxResultGenerator[] generators)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        QueryTokenJsonConverter.GetQueryTokenTS = qt => new QueryTokenTS(qt, true);
        QueryNameJsonConverter.GetQueryKey = qn => QueryUtils.GetKey(qn);

        ReflectionServer.RegisterLike(typeof(OmniboxMessage), () => OmniboxPermission.ViewOmnibox.IsAuthorized());

        foreach (var g in generators)
        {
            OmniboxParser.Generators.Add(g);
        }
    }
}
