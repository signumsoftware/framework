using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Entities.Omnibox;
using Signum.React.ApiControllers;
using Signum.React.Facades;
using Microsoft.AspNetCore.Builder;

namespace Signum.React.Omnibox;

public static class OmniboxServer
{
    public static Func<Type, bool> IsNavigable;

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
        return OmniboxServer.IsNavigable.GetInvocationListTyped().All(f => f(type));
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

        return Database.FillToString(Lite.Create(type, id));
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
