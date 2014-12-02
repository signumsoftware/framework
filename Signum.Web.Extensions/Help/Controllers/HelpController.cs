using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Mvc.Ajax;
using System.Reflection;
using Signum.Engine.Help;
using Signum.Utilities;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Signum.Web;
using System.Text;
using Signum.Engine;
using Signum.Engine.WikiMarkup;
using Signum.Engine.Basics;
using Signum.Entities.Help;
using Signum.Entities;
using Signum.Engine.Operations;
using Signum.Engine.Maps;
using Signum.Engine.Authorization;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Signum.Engine.DynamicQuery;
using Signum.Services;
using Signum.Web.Help;

namespace Signum.Web.Help
{
    public static class HelpHelpers
    {
        public static MvcHtmlString WikiParse(this HtmlHelper helper, string text, WikiSettings settings)
        {
            return MvcHtmlString.Create(settings.WikiParse(text ?? "").Replace("\n", "<p>")); 
        }
    }

    public class HelpController : Controller
    {
        public ActionResult Index()
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            return View(HelpClient.IndexUrl);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewEntity(string entity)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            Type type = TypeLogic.GetType(entity);

            EntityHelp eh = HelpLogic.GetEntityHelp(type);

            return View(HelpClient.ViewEntityUrl, eh);
        }

        [HttpPost]
        public RedirectResult SaveEntity()
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            var entity = this.ExtractEntity<EntityHelpEntity>();

            var oldProperties = entity.Properties.ToList();

            var ctx = entity.ApplyChanges(this);

            foreach (var query in ctx.Value.Queries)
            {
                query.Columns.RemoveAll(a => !a.Description.HasText());

                if (query.Columns.IsEmpty() && !query.Description.HasText())
                {
                    if (!query.IsNew)
                        query.Delete();
                }
                else
                    query.Execute(QueryHelpOperation.Save);
            }

            foreach (var oper in ctx.Value.Operations)
            {
                if (!oper.Description.HasText())
                {
                    if (!oper.IsNew)
                        oper.Delete();
                }
                else
                    oper.Execute(OperationHelpOperation.Save);
            }

            var currentProperties = entity.Properties.Select(p => p.Property).ToHashSet();

            entity.Properties.AddRange(oldProperties.Where(p => !currentProperties.Contains(p.Property))); //Hidden properties due to permissions
            entity.Properties.RemoveAll(a => !a.Description.HasText());

            if (entity.Properties.IsEmpty() && !entity.Description.HasText())
            {
                if (!entity.IsNew)
                    entity.Delete();
            }
            else
                entity.Execute(EntityHelpOperation.Save);

            return null;
        }

        [HttpPost]
        public RedirectResult TraslateEntity(string from)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            var entity = this.ExtractEntity<EntityHelpEntity>().ApplyChanges(this).Value;

            entity.AssignTranslatedFullEntity(CultureInfoLogic.GetCultureInfoEntity(from)); 

            return null;
        }


        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewNamespace(string @namespace)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            NamespaceHelp model = HelpLogic.GetNamespaceHelp(@namespace);

            return View(HelpClient.ViewNamespaceUrl, model);
        }


        [HttpPost]
        public ContentResult SaveNamespace()
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            var ctx = this.ExtractEntity<NamespaceHelpEntity>().ApplyChanges(this);

            var entity = ctx.Value;

            if (!entity.Title.HasText() && !entity.Description.HasText())
            {
                if (!entity.IsNew)
                    entity.Delete();
            }
            else
                entity.Execute(NamespaceHelpOperation.Save);

            return null;
        }

        [HttpPost]
        public RedirectResult TraslateNamespace(string from)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            var entity = this.ExtractEntity<NamespaceHelpEntity>().ApplyChanges(this).Value;

            entity.AsignTranslatedNamespace(CultureInfoLogic.GetCultureInfoEntity(from));

            return null;
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewAppendix(string appendix)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            AppendixHelp model = HelpLogic.GetAppendixHelp(appendix);

            return View(HelpClient.ViewAppendixUrl, model);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult NewAppendix()
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            var culture = HelpLogic.GetCulture();
            AppendixHelp model = new AppendixHelp(culture, new AppendixHelpEntity { Culture = culture.ToCultureInfoEntity() });

            return View(HelpClient.ViewAppendixUrl, model);
        }

        [HttpPost]
        public ActionResult SaveAppendix()
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            var ctx = this.ExtractEntity<AppendixHelpEntity>().ApplyChanges(this);

            var entity = ctx.Value;

            if (!entity.Title.HasText() && !entity.Description.HasText())
            {
                if (!entity.IsNew)
                    entity.Delete();

                return JsonAction.RedirectAjax(RouteHelper.New().Action((HelpController a) => a.Index()));
            }
            else
            {
                var wasNew = entity.IsNew;

                entity.Execute(AppendixHelpOperation.Save);


                if (wasNew)
                    return JsonAction.RedirectAjax(RouteHelper.New().Action((HelpController a) => a.ViewAppendix(entity.UniqueName)));
                return null;
            }
        }

        [HttpPost]
        public RedirectResult TraslateAppendix(string from)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            var entity = this.ExtractEntity<AppendixHelpEntity>().ApplyChanges(this).Value;

            entity.AsignTranslatedAppendix(CultureInfoLogic.GetCultureInfoEntity(from));

            return null;
        }

        public ActionResult PropertyRoutes()
        {
            string[] routes = JsonConvert.DeserializeObject<string[]>(this.Request["routes"]);

            var parsed = routes.Select(r => PropertyRoute.Parse(r)).Distinct().ToList();

            return this.JsonNet(HelpLogic.GetPropertyRoutesService(parsed).ToDictionary(a=>a.Key.ToString(), a=>a.Value));
        }

        public ActionResult ComplexColumns()
        {
            object queryName = QueryLogic.ToQueryName(this.Request["queryName"]);

            string[] columns = JsonConvert.DeserializeObject<string[]>(this.Request["columns"]);

            var description = DynamicQueryManager.Current.QueryDescription(queryName);

            var parsed = columns.Select(r => QueryUtils.Parse(r, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement)).Distinct().ToList();

            var routes = parsed.Select(a => a.GetPropertyRoute()).NotNull().Distinct().ToList();

            var help = HelpLogic.GetPropertyRoutesService(routes);

            return this.JsonNet(parsed.ToDictionary(a => a.FullKey(), a => { var pr = a.GetPropertyRoute(); return pr == null ? null : help.TryGetC(pr); }));
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Search(string q)
        {
            HelpPermissions.ViewHelp.AssertAuthorized();

            Stopwatch sp = new Stopwatch();
            sp.Start();
            Regex regex = new Regex(Regex.Escape(q.RemoveDiacritics()), RegexOptions.IgnoreCase);
            List<List<SearchResult>> results = new List<List<SearchResult>>();
            results.AddRange(from eh in HelpLogic.GetEntityHelps()
                             select eh.Search(regex).ToList() into l
                             where l.Any()
                             select l);

            //We add the appendices
            results.AddRange(from a in HelpLogic.GetAppendixHelps()
                             let result = a.Search(regex)
                             where result != null
                             select new List<SearchResult> { result });

            //We add the namespaces
            results.AddRange(from a in HelpLogic.GetNamespaceHelps()
                             let result = a.Search(regex)
                             where result != null
                             select new List<SearchResult> { result });

            results = results.OrderBy(a => a.First().IsDescription).ThenBy(a => a.First().MatchType).ThenBy(a => a.First().TypeSearchResult).ToList();

            sp.Stop();
            ViewData["time"] = sp.ElapsedMilliseconds;
            ViewData[ViewDataKeys.Title] = q + " - " + HelpMessage.Buscador.NiceToString();
            return View(HelpClient.SearchResults, results);
        }
    }
}
