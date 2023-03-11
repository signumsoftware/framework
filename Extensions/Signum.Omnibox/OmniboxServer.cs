using Signum.Entities.Omnibox;
using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.API.Controllers;

namespace Signum.React.Omnibox;

public static class OmniboxServer
{
    public static void Start(IApplicationBuilder app, params IOmniboxResultGenerator[] generators)
    {
        SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());
        QueryTokenJsonConverter.GetQueryTokenTS = qt => new QueryTokenTS(qt, true);
        QueryNameJsonConverter.GetQueryKey = qn => QueryUtils.GetKey(qn);
        OmniboxParser.Manager = new ReactOmniboxManager();

        ReflectionServer.RegisterLike(typeof(OmniboxMessage), () => OmniboxPermission.ViewOmnibox.IsAuthorized());

        foreach (var g in generators)
        {
            OmniboxParser.Generators.Add(g);
        }
    }
}

public class ReactOmniboxManager : OmniboxManager
{
    public override bool AllowedType(Type type)
    {
        return Schema.Current.IsAllowed(type, true) == null;
    }

    public override bool AllowedPermission(PermissionSymbol permission)
    {
        return permission.IsAuthorized();
    }

    public override bool AllowedQuery(object queryName)
    {
        return QueryLogic.Queries.QueryAllowed(queryName, true);
    }

    public override Lite<Entity>? RetrieveLite(Type type, PrimaryKey id)
    {
        if (!Database.Exists(type, id))
            return null;

        return Database.FillLiteModel(Lite.Create(type, id));
    }

    public override QueryDescription GetDescription(object queryName)
    {
        return QueryLogic.Queries.QueryDescription(queryName);
    }

    public override List<Lite<Entity>> Autocomplete(Implementations implementations, string subString, int count)
    {
        if (string.IsNullOrEmpty(subString))
            return new List<Lite<Entity>>();

        return AutocompleteUtils.FindLiteLike(implementations, subString, 5);
    }

    protected override IEnumerable<object> GetAllQueryNames()
    {
        return QueryLogic.Queries.GetQueryNames();
    }

    protected override IEnumerable<Type> GetAllTypes()
    {
        return Schema.Current.Tables.Keys;
    }
}
