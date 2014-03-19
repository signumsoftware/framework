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
        [HttpPost]
        public JsonNetResult Autocomplete(string types, string q, int l)
        {
            Type[] typeArray = EntityBase.ParseTypes(types);
            if (typeArray == EntityBase.ImplementedByAll)
                throw new ArgumentException("ImplementedBy not allowed in Autocomplete");

            List<Lite<IdentifiableEntity>> lites = AutocompleteUtils.FindLiteLike(Implementations.By(typeArray), q, l);

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
                    id = o.Id,
                    text = o.ToString(),
                    type = Navigator.ResolveWebTypeName(o.GetType())
                }).ToList();

            return this.JsonNet(result);
        }

        public ActionResult Find(FindOptions findOptions)
        {
            return Navigator.Find(this, findOptions);
        }

        [HttpPost]
        public PartialViewResult PartialFind(FindOptions findOptions, string prefix, bool isExplore)
        {
            return Navigator.PartialFind(this, findOptions, isExplore ? FindMode.Explore : FindMode.Find, prefix);
        }

        [HttpPost]
        public PartialViewResult Search(QueryRequest queryRequest, bool allowSelection, bool navigate, FilterMode filterMode, string prefix)
        {
            return Navigator.Search(this, queryRequest, allowSelection, navigate, filterMode, prefix);
        }

        [HttpPost]
        public ContentResult AddColumn(string webQueryName, string tokenName)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            QueryToken token = QueryUtils.Parse(tokenName, qd, canAggregate: false);
            return Content(SearchControlHelper.Header(new Column(token, token.NiceName()), null).ToString());
        }

        [HttpPost]
        public ContentResult SelectedItemsContextMenu(string webQueryName, string implementationsKey, string prefix)
        {
            var lites = this.ParseLiteKeys<IdentifiableEntity>();
            if (lites.IsEmpty())
                return Content("");

            object queryName = Navigator.ResolveQueryName(webQueryName);
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

            var helper = CreateHtmlHelper(this);
            return Content(new HtmlStringBuilder(items.Select(mi => mi.ToHtml(helper))).ToHtml().ToString());
        }

      

        [HttpPost]
        public ContentResult AddFilter(string webQueryName, string tokenName, int index, string prefix, string value)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);

            FilterOption fo = new FilterOption(tokenName, null);
            if (fo.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                fo.Token = QueryUtils.Parse(tokenName, qd, canAggregate: false);
            }
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).FirstEx();

            try
            {
                fo.Value = FindOptionsModelBinder.Convert(value, fo.Token.Type);
            }
            catch (Exception)
            {
                //Cell Value must be custom and cannot be parsed automatically: Leave value to null
            }

            return Content(FilterBuilderHelper.NewFilter(CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index).ToHtmlString());
        }

        [HttpPost]
        public ContentResult NewSubTokensCombo(string webQueryName, string tokenName, string prefix)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            var token = QueryUtils.Parse(tokenName, qd, canAggregate: false);

            var combo = CreateHtmlHelper(this).QueryTokenBuilderOptions(token, new Context(null, prefix), SearchControlHelper.GetQueryTokenBuilderSettings(qd));

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

        public AutocompleteResult(Lite<IdentifiableEntity> lite)
        {
            id = lite.Id;
            text = lite.ToString();
            type = Navigator.ResolveWebTypeName(lite.EntityType);
            link = Navigator.NavigateRoute(lite);
        }

        public int id;
        public string text;
        public string type;
        public string link;
    }
}