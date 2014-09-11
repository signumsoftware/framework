using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities.Omnibox;
using Signum.Windows.Authorization;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using Signum.Services;
using System.Reflection;
using System.Windows.Documents;
using System.Windows;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities.Authorization;

namespace Signum.Windows.Omnibox
{
    public static class OmniboxClient
    {
        public static Dictionary<Type, OmniboxProviderBase> Providers = new Dictionary<Type,OmniboxProviderBase>();
        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                OmniboxParser.Manager = new WindowsOmniboxManager();
            }
        }

        public static void Register<T>(this OmniboxProvider<T> provider) where T : OmniboxResult
        {
            OmniboxParser.Generators.Add(provider.CreateGenerator());
            Providers[typeof(T)] = provider;
        }

        public static void AddMatch(this InlineCollection lines, OmniboxMatch match)
        {
            foreach (var item in match.BoldSpans())
            {
                var run = new Run(item.Item1);

                lines.Add(item.Item2 ? (Inline)new Bold(run) : run);
            }
        }
    }

    public abstract class OmniboxProviderBase
    {
        public abstract void RenderLinesBase(OmniboxResult result, InlineCollection lines);

        public abstract void OnSelectedBase(OmniboxResult result, Window window);

        public abstract string GetNameBase(OmniboxResult result);

        public abstract Run GetIcon();

    }

    public abstract class OmniboxProvider<T> : OmniboxProviderBase where T : OmniboxResult
    {
        public abstract OmniboxResultGenerator<T> CreateGenerator();

        public abstract void RenderLines(T result, InlineCollection lines);

        public abstract void OnSelected(T result, Window window);

        public abstract string GetName(T result);
    
        public override void RenderLinesBase(OmniboxResult result, InlineCollection lines)
        {
            RenderLines((T)result, lines); 
        }

        public override void OnSelectedBase(OmniboxResult result, Window window)
        {   
            OnSelected((T)result, window);
        }

        public override string GetNameBase(OmniboxResult result)
        {
            return GetName((T)result);
        }
    }

    public class WindowsOmniboxManager : OmniboxManager
    {
        public override bool AllowedType(Type type)
        {
            return Navigator.IsNavigable(type, isSearch: true);
        }

        public override bool AllowedPermission(PermissionSymbol permission)
        {
            return permission.IsAuthorized();
        }

        public override bool AllowedQuery(object queryName)
        {
            return Finder.IsFindable(queryName);
        }


        public override Lite<IdentifiableEntity> RetrieveLite(Type type, int id)
        {
            if (!Server.Return((IBaseServer bs) => bs.Exists(type, id)))
                return null;
            return Server.FillToStr(Lite.Create(type, id));
        }

       
        public override QueryDescription GetDescription(object queryName)
        {
            return DynamicQueryServer.GetQueryDescription(queryName);
        }

        public override List<Lite<IdentifiableEntity>> Autocomplete(Implementations implementations, string subString, int count)
        {
            if (string.IsNullOrEmpty(subString))
                return new List<Lite<IdentifiableEntity>>();

            return Server.Return((IBaseServer bs) => bs.FindLiteLike(implementations, subString, 5));
        }

        protected override IEnumerable<object> GetAllQueryNames()
        {
            return QueryClient.queryNames.Values;
        }

        protected override IEnumerable<Type> GetAllTypes()
        {
            return Server.ServerTypes.Keys;
        }
    }
}
