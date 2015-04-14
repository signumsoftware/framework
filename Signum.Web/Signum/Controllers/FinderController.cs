using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;

namespace Signum.Web.Controllers
{
    public class FinderController : Controller
    {
        [HttpPost, ActionSplitter("types")]
        public JsonNetResult Autocomplete(string types, string q, int l)
        {
            Type[] typeArray = EntityBase.ParseTypes(types);
            if (typeArray == null)
                throw new ArgumentException("ImplementedByAll not allowed in Autocomplete");

            List<Lite<Entity>> lites = AutocompleteUtils.FindLiteLike(Implementations.By(typeArray), q, l);

            var result = lites.Select(o => new AutocompleteResult(o)).ToList();

            return this.JsonNet(result);
        }

        [HttpPost]
        public JsonNetResult TypeAutocomplete(string types, string q, int l)
        {
            var result = TypeClient.ViewableServerTypes()
                .Where(t => t.CleanName.Contains(q, StringComparison.InvariantCultureIgnoreCase)).
                Take(l)
                .Select(o => new AutocompleteResult
                {
                    id = o.Id.ToString(),
                    text = o.ToString(),
                    type = Navigator.ResolveWebTypeName(o.GetType())
                }).ToList();

            return this.JsonNet(result);
        }

        [ActionSplitter("webQueryName")]
        public ContentResult Count(FindOptions findOptions)
        {
            int count = Finder.QueryCount(new CountOptions(findOptions.QueryName)
            {
                FilterOptions = findOptions.FilterOptions
            });

            return this.Content(count.ToString());
        }

        [ActionSplitter("webQueryName")]
        public ActionResult Find(FindOptions findOptions)
        {
            return Finder.SearchPage(this, findOptions);
        }

        [HttpPost, ActionSplitter("webQueryName")]
        public PartialViewResult PartialFind(FindOptions findOptions, string prefix, bool isExplore)
        {
            return Finder.SearchPopup(this, findOptions, isExplore ? FindMode.Explore : FindMode.Find, prefix);
        }

        [HttpPost, ActionSplitter("webQueryName")]
        public PartialViewResult Search(QueryRequest queryRequest, bool allowSelection, bool navigate, bool showFooter, string prefix)
        {
            return Finder.SearchResults(this, queryRequest, allowSelection, navigate, showFooter, prefix);
        }

        [HttpPost]
        public ContentResult AddColumn(string webQueryName, string tokenName)
        {
            object queryName = Finder.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            QueryToken token = QueryUtils.Parse(tokenName, qd, SubTokensOptions.CanElement);
            return Content(SearchControlHelper.Header(new Column(token, token.NiceName()), null).ToString());
        }

        [HttpPost, ActionSplitter("webQueryName")]
        public ContentResult SelectedItemsContextMenu(string webQueryName, string implementationsKey, string prefix)
        {
            var lites = this.ParseLiteKeys<Entity>();
            if (lites.IsEmpty())
                return Content("");

            object queryName = Finder.ResolveQueryName(webQueryName);
            Implementations implementations = implementationsKey == "[All]" ? Implementations.ByAll :
                Implementations.By(implementationsKey.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(t => Navigator.ResolveType(t)).ToArray());

            var items = ContextualItemsHelper.GetContextualItemListForLites(new SelectedItemsMenuContext
            {
                Url = RouteHelper.New(),
                ControllerContext = this.ControllerContext,
                Lites = lites,
                QueryName = queryName,
                Implementations = implementations,
                Prefix = prefix,
            });

            if (items.IsNullOrEmpty())
                return Content("");

            var sb = new HtmlStringBuilder(items.Select(mi => mi.ToHtml()));
            sb.Add(new MvcHtmlString(@"<script>$(function(){ $('[data-toggle=""tooltip""]').tooltip({}); });</script>"));
            return Content(sb.ToHtml().ToString());
        }

      

        [HttpPost]
        public ContentResult AddFilter(string webQueryName, string tokenName, int index, string prefix, string value)
        {
            object queryName = Finder.ResolveQueryName(webQueryName);

            FilterOption fo = new FilterOption(tokenName, null);
            
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            fo.Token = QueryUtils.Parse(tokenName, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement);
           
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).FirstEx();

            try
            {
                fo.Value = FindOptionsModelBinder.Convert(value, fo.Token.Type);
            }
            catch (Exception)
            {
                //Cell Value must be custom and cannot be parsed automatically: Leave value to null
            }

            return Content(FilterBuilderHelper.NewFilter(CreateHtmlHelper(this), fo, new Context(null, prefix), index).ToHtmlString());
        }

        [HttpPost]
        public ContentResult NewSubTokensCombo(string webQueryName, string tokenName, string prefix, int options)
        {
            object queryName = Finder.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            var token = QueryUtils.Parse(tokenName, qd, (SubTokensOptions)options);

            var combo = CreateHtmlHelper(this).QueryTokenBuilderOptions(token, new Context(null, prefix), SearchControlHelper.GetQueryTokenBuilderSettings(qd, (SubTokensOptions)options));

            return Content(combo.ToHtmlString());
        }

        public static HtmlHelper CreateHtmlHelper(Controller c)
        {
            var viewContext = new ViewContext(c.ControllerContext, new FakeView(), c.ViewData, c.TempData, TextWriter.Null);
            return new HtmlHelper(viewContext, new ViewPage());
        }

        class FakeView : System.Web.Mvc.IView
        {
            public void Render(ViewContext viewContext, TextWriter writer)
            {
                throw new InvalidOperationException();
            }
        }
    }

    public class AutocompleteResult
    {
        public AutocompleteResult()
        {
        }

        public AutocompleteResult(Lite<Entity> lite)
        {
            id = lite.Id.ToString();
            text = lite.ToString();
            type = Navigator.ResolveWebTypeName(lite.EntityType);
            link = Navigator.NavigateRoute(lite);
        }

        public string id;
        public string text;
        public string type;
        public string link;
    }
}