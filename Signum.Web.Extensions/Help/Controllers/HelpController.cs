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
using Signum.Web.Extensions;
using System.Text;
using Signum.Web.Extensions.Properties;
using Signum.Engine;
using Signum.Engine.WikiMarkup;
using Signum.Engine.Basics;
using Signum.Entities.Help;

namespace Signum.Web.Help
{
    public static class HelpHelpers
    {
        public static MvcHtmlString WikiParse(this HtmlHelper helper, string text, WikiSettings settings)
        {
            return MvcHtmlString.Create(settings.WikiParse(text).Replace("\n", "<p>")); 
        }

        
    }

    public class HelpController : Controller
    {
        public ActionResult Index()
        {
            NamespaceModel model = new NamespaceModel("", HelpLogic.AllTypes());
            ViewData["appendices"] = HelpLogic.GetAppendices();
            return View(HelpClient.IndexUrl, model);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewEntity(string entity)
        {
            Type type = TypeLogic.GetType(entity);
            List<Type> relatedTypes = (from t in HelpLogic.AllTypes()
                                       where t.Namespace == type.Namespace
                                       orderby t.Name
                                       select t).ToList();

            ViewData["nameSpace"] = relatedTypes;

            //Buscamos en qu� fichero se encuentra
            EntityHelp eh = HelpLogic.GetEntityHelp(type);

            return View(HelpClient.ViewEntityUrl, eh);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewNamespace(string @namespace)
        {
            NamespaceHelp model = HelpLogic.GetNamespace(@namespace);

            List<Type> relatedTypes = (from t in HelpLogic.AllTypes()
                                       where t.Namespace == model.Name
                                       orderby t.Name
                                       select t).ToList();

            ViewData["nameSpace"] = relatedTypes;

            return View(HelpClient.ViewNamespaceUrl, model);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewAppendix(string appendix)
        {
            AppendixHelp model = HelpLogic.GetAppendix(appendix);
            return View(HelpClient.ViewAppendixUrl, model);
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Search(string q)
        {
            Stopwatch sp = new Stopwatch();
            sp.Start();
            Regex regex = new Regex(Regex.Escape(q.RemoveDiacritics()), RegexOptions.IgnoreCase);
            List<List<SearchResult>> results = (from eh in HelpLogic.GetEntitiesHelp()
                                               let result = eh.Value.Search(regex)
                                               where result.Any()
                                               select result.ToList()).ToList();

            //We add the appendices
            results.AddRange(from a in HelpLogic.GetAppendices()
                             let result = a.Search(regex)
                             where result.Any()
                             select result.ToList());

            //We add the appendices
            results.AddRange(from a in HelpLogic.GetNamespaces()
                             let result = a.Search(regex)
                             where result.Any()
                             select result.ToList());

            results.Sort(a => a.FirstEx());

            sp.Stop();
            ViewData["time"] = sp.ElapsedMilliseconds;
            ViewData[ViewDataKeys.Title] = q + " - " + HelpMessage.Buscador.NiceToString();
            return View(HelpClient.SearchResults, results);
        }


        [HttpPost]
        public ContentResult SaveEntity(string entity)
        {
            bool entityModified = false;
            EntityHelp eh = HelpLogic.GetEntityHelp(TypeLogic.GetType(entity));
            Dictionary<string, QueryHelp> processedQueryHelp = new Dictionary<string, QueryHelp>();
            foreach (string key in Request.Form.Keys)
            {
                string subKey = (key.StartsWith("p-") || key.StartsWith("o-") || key.StartsWith("q-") || key.StartsWith("c-")) ? key.Substring(2) : key;

                if (key.StartsWith("p-"))
                {
                    subKey = subKey.Replace("_", "/");
                    if (!eh.Properties.ContainsKey(subKey)) throw new ApplicationException(HelpMessage.Property0NotExistsInType1.NiceToString().Formato(subKey, entity));
                    eh.Properties[subKey].UserDescription = Request.Form[key].ToString();
                    entityModified = true;
                }
                else if (key.StartsWith("o-"))
                {
                    subKey = subKey.Replace("_", ".");
                    Enum e = eh.Operations.Keys.Where(k => OperationDN.UniqueKey(k).Equals(subKey)).SingleEx();
                    eh.Operations[e].UserDescription = Request.Form[key].ToString();
                    entityModified = true;
                }
                else if (key.StartsWith("q-"))
                {
                    subKey = subKey.Replace("_", ".");
                    QueryHelp qh = processedQueryHelp.ContainsKey(subKey) ? processedQueryHelp[subKey] : HelpLogic.GetQueryHelp(subKey);
                    qh.UserDescription = Request.Form[key].ToString();
                    processedQueryHelp[subKey] = qh;
                }
                else if (key.StartsWith("c-"))  //query-column
                {
                    subKey = subKey.Replace("_", ".");
                    string query = subKey.Substring(0, subKey.LastIndexOf("."));
                    string column = subKey.Substring(subKey.LastIndexOf(".") + 1);
                    QueryHelp qh = processedQueryHelp.ContainsKey(query) ? processedQueryHelp[query] : HelpLogic.GetQueryHelp(query);
                    qh.Columns.Where(c=>c.Key == column).SingleEx().Value.UserDescription = Request.Form[key].ToString();
                    processedQueryHelp[subKey] = qh;
                }
                else if (key.Equals("description"))
                {
                    eh.Description = Request.Form[key].ToString();
                    entityModified = true;
                }
                else throw new ApplicationException(HelpMessage.Key0NotFound.NiceToString().Formato(Request.Form[key].ToString()));
            }

            //Save it to file
            if (entityModified) eh.Save();
            foreach (var qh in processedQueryHelp.Values)
            {
                qh.Save();
                HelpLogic.ReloadDocumentQuery(qh);
            }

            //Load the file again
            if (entityModified) HelpLogic.ReloadDocumentEntity(eh);

            return Content("");
        }

        [HttpPost]
        public ContentResult SaveNamespace(string @namespace)
        {
            NamespaceHelp nh = HelpLogic.GetNamespace(@namespace);
            foreach (string key in Request.Form.Keys)
            {
                if (key.Equals("description"))
                    nh.Description = Request.Form[key].ToString();
                else throw new ApplicationException(HelpMessage.Key0NotFound.NiceToString().Formato(Request.Form[key].ToString()));
            }
            nh.Save();            
            HelpLogic.ReloadDocumentNamespace(nh);
            return Content("");
        }

        [HttpPost]
        public ContentResult SaveAppendix(string appendix)
        {
            AppendixHelp ah = HelpLogic.GetAppendix(appendix);
            foreach (string key in Request.Form.Keys)
            {
                if (key.Equals("description"))
                    ah.Description = Request.Form[key].ToString();
                else if (key.Equals("title"))
                    ah.Title = Request.Form[key].ToString();
                else throw new ApplicationException(HelpMessage.Key0NotFound.NiceToString().Formato(Request.Form[key].ToString()));
            }
            ah.Save();
            HelpLogic.ReloadDocumentAppendix(ah);
            return Content("");
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult ViewTodo()
        {
            int count = HelpLogic.GetEntitiesHelp().Count();

            List<EntityHelp> ehs = (from eh in HelpLogic.GetEntitiesHelp()
                                    where !eh.Value.Description.HasText()
                                    orderby eh.Value.Type.Name
                                    select eh.Value).ToList();


         /*   Dictionary<EntityHelp, HashSet<WikiParserExtensions.WikiLink>> unavailable = new Dictionary<EntityHelp, HashSet<WikiParserExtensions.WikiLink>>();
            foreach (EntityHelp eh in HelpLogic.GetEntitiesHelp().Select(d=>d.Value))
            {
                HashSet<WikiParserExtensions.WikiLink> wikiLinks = new HashSet<WikiParserExtensions.WikiLink>();
                if (WikiParserExtensions.WikiLinks(eh.Description).Any(wl => wl.Class == "unavailable"))
                    wikiLinks.AddRange(WikiParserExtensions.WikiLinks(eh.Description).Where(wl => wl.Class == "unavailable"));

                if (eh.Properties!=null)
                    foreach (var p in eh.Properties)
                    {
                        if (WikiParserExtensions.WikiLinks(p.Value.Info + "." + p.Value.UserDescription).Any(wl => wl.Class == "unavailable"))
                            wikiLinks.AddRange(WikiParserExtensions.WikiLinks(p.Value.Info + "." + p.Value.UserDescription).Where(wl => wl.Class == "unavailable"));
                    }

                if (eh.Queries != null)
                    foreach (var q in eh.Queries)
                    {
                        if (WikiParserExtensions.WikiLinks(q.Value.Info + "." + q.Value.UserDescription).Any(wl => wl.Class == "unavailable"))
                            wikiLinks.AddRange(WikiParserExtensions.WikiLinks(q.Value.Info + "." + q.Value.UserDescription).Where(wl => wl.Class == "unavailable"));
                    }

                if (eh.Operations != null)
                    foreach (var o in eh.Operations)
                    {
                        if (WikiParserExtensions.WikiLinks(o.Value.Info + "." + o.Value.UserDescription).Any(wl => wl.Class == "unavailable"))
                            wikiLinks.AddRange(WikiParserExtensions.WikiLinks(o.Value.Info + "." + o.Value.UserDescription).Where(wl => wl.Class == "unavailable"));
                    }

                if (!wikiLinks.Empty())
                    unavailable[eh] = wikiLinks;
            }

            ViewData["EntityCount"] = count;
            ViewData["UnavailableLinks"] = unavailable;*/
            return View(HelpClient.TodoUrl, ehs);
        }
    }
}
