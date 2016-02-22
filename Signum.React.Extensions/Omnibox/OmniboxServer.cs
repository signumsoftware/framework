using Signum.Engine;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Omnibox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;

namespace Signum.React.Omnibox
{
    public static class OmniboxServer
    {
        public static void Start(HttpConfiguration config, params IOmniboxResultGenerator[] generators)
        {
            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

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
            return Navigator.IsNavigable(type, null, isSearch: true);
        }

        public override bool AllowedPermission(PermissionSymbol permission)
        {
            return permission.IsAuthorized();
        }

        public override bool AllowedQuery(object queryName)
        {
            return Finder.IsFindable(queryName);
        }

        public override Lite<Entity> RetrieveLite(Type type, PrimaryKey id)
        {
            if (!Database.Exists(type, id))
                return null;
            return Database.FillToString(Lite.Create(type, id));
        }

        public override QueryDescription GetDescription(object queryName)
        {
            return DynamicQueryManager.Current.QueryDescription(queryName);
        }

        public override List<Lite<Entity>> Autocomplete(Implementations implementations, string subString, int count)
        {
            if (string.IsNullOrEmpty(subString))
                return new List<Lite<Entity>>();

            return AutocompleteUtils.FindLiteLike(implementations, subString, 5);
        }

        protected override IEnumerable<object> GetAllQueryNames()
        {
            return DynamicQueryManager.Current.GetQueryNames();
        }

        protected override IEnumerable<Type> GetAllTypes()
        {
            return Schema.Current.Tables.Keys;
        }
    }
}