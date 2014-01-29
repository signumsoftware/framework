#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Web;
using System.Web.Mvc;
using Signum.Utilities;
using Signum.Engine;
using Signum.Entities;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Text;
using System.IO;
using Signum.Engine.Basics;
#endregion

namespace Signum.Web.Controllers
{
    public class SignumController : Controller
    {
        [ValidateInput(false)]  //this is needed since a return content(View...) from an action that doesn't validate will throw here an exception. We suppose that validation has already been performed before getting here
        public ViewResult View(string webTypeName, int? id)
        {
            Type t = Navigator.ResolveType(webTypeName);

            if (id.HasValue)
                return Navigator.NormalPage(this, Database.Retrieve(t, id.Value)); 

            IdentifiableEntity entity = null;
            object result = Constructor.Construct(t);
            if (typeof(IdentifiableEntity).IsAssignableFrom(result.GetType()))
                entity = (IdentifiableEntity)result;
            else
                throw new InvalidOperationException("Invalid result type for a Constructor");
             
            return Navigator.NormalPage(this, entity); 
        }

        public ActionResult Create(string entityType, string prefix)
        {
            Type type = Navigator.ResolveType(entityType);

            return Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.Navigate, null);
        }

        public PartialViewResult PopupNavigate(string entityType, int? id, string prefix, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity = null;
            if (id.HasValue)
                entity = Database.Retrieve(type, id.Value);
            else
            {
                object result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PopupNavigate, partialViewName);
                if (result.GetType() == typeof(PartialViewResult))
                    return (PartialViewResult)result;

                if (result.GetType().IsEmbeddedEntity())
                    throw new InvalidOperationException("PopupNavigate cannot be called for EmbeddedEntity {0}".Formato(result.GetType()));

                if (!typeof(IdentifiableEntity).IsAssignableFrom(result.GetType()))
                    throw new InvalidOperationException("Invalid result type for a Constructor");

                entity = (IdentifiableEntity)result;
            }

            TypeContext tc = TypeContextUtilities.UntypedNew(entity, prefix);
            return this.PopupOpen(new PopupNavigateOptions(tc) { PartialViewName = partialViewName });
        }

        public PartialViewResult PopupView(string entityType, int? id, string prefix, bool? readOnly, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);
            
            IdentifiableEntity entity = null;
            if (id.HasValue)
                entity = Database.Retrieve(type, id.Value);
            else
            {
                ActionResult result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PopupView, partialViewName);
                if (result is PartialViewResult)
                    return (PartialViewResult)result;
                else
                    throw new InvalidOperationException("Invalid result type for a Constructor");
            }
            
            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);
            return this.PopupOpen(new PopupViewOptions(tc) { PartialViewName = partialViewName, ReadOnly = readOnly.HasValue });
        }

        [HttpPost]
        public PartialViewResult PartialView(string entityType, int? id, string prefix, bool? readOnly, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);
            
            IdentifiableEntity entity = null;
            if (id.HasValue)
                entity = Database.Retrieve(type, id.Value);
            else
            {
                object result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PartialView, partialViewName);
                if (result is PartialViewResult)
                    return (PartialViewResult)result;
                else
                    throw new InvalidOperationException("Invalid result type for a Constructor");
            }
            
            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);

            if (readOnly == true)
                tc.ReadOnly = true;

            return Navigator.PartialView(this, tc, partialViewName);
        }

        [HttpPost]
        public PartialViewResult NormalControl(string entityType, int id, bool? readOnly, string partialViewName)
        {
            Type type = Navigator.ResolveType(entityType);

            IdentifiableEntity entity =  Database.Retrieve(type, id);

            return Navigator.NormalControl(this, new NavigateOptions(entity) { ReadOnly = readOnly, PartialViewName = partialViewName });
        }

        [HttpPost]
        public JsonResult Validate()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);
            return JsonAction.ModelState(ModelState);
        }

        [HttpPost]
        public JsonResult ValidatePartial(string prefix, string rootType = null, string propertyRoute = null)
        {
            ModifiableEntity mod = this.UntypedExtractEntity(prefix);

            PropertyRoute route = (rootType.HasText() || propertyRoute.HasText()) ? PropertyRoute.Parse(TypeLogic.GetType(rootType), propertyRoute) : PropertyRoute.Root(mod.GetType());
          
            MappingContext context = mod.UntypedApplyChanges(ControllerContext, prefix, admin: true, route: route).UntypedValidateGlobal();
            
            this.ModelState.FromContext(context);
            
            IIdentifiable ident = context.UntypedValue as IIdentifiable;
            string newLink = ident != null && ident.IdOrNull != null ? Navigator.NavigateRoute(ident) : null;
            string newToStr = context.UntypedValue.ToString();
            
            return JsonAction.ModelState(ModelState, newToStr, newLink);
        }

        [HttpPost]
        public JsonResult Autocomplete(string types, string q, int l)
        {
            Type[] typeArray = StaticInfo.ParseTypes(types);
            if (typeArray == StaticInfo.ImplementedByAll)
                throw new ArgumentException("ImplementedBy not allowed in Autocomplete");

            List<Lite<IdentifiableEntity>> lites = AutocompleteUtils.FindLiteLike(Implementations.By(typeArray), q, l);

            var result = lites.Select(o => new AutoCompleteResult(o)).ToList();

            return Json(result);
        }

        [HttpPost]
        public JsonResult TypeAutocomplete(string types, string q, int l)
        {
            var result = TypeClient.ViewableServerTypes()
                .Where(t => t.CleanName.Contains(q, StringComparison.InvariantCultureIgnoreCase)).
                Take(l)
                .Select(o => new AutoCompleteResult
                {
                    id = o.Id,
                    text = o.ToString(),
                    type = Navigator.ResolveWebTypeName(o.GetType())
                }).ToList();

            return Json(result);
        }

        public ActionResult Find(FindOptions findOptions)
        {
            return Navigator.Find(this, findOptions);
        }

        [HttpPost]
        public PartialViewResult PartialFind(FindOptions findOptions, string prefix)
        {
            return Navigator.PartialFind(this, findOptions, prefix);
        }

        [HttpPost]
        public PartialViewResult Search(QueryRequest queryRequest, bool allowMultiple, bool navigate, FilterMode filterMode, string prefix)
        {
            return Navigator.Search(this, queryRequest, allowMultiple, navigate, filterMode, prefix);
        }

        [HttpPost]
        public ContentResult AddFilter(string webQueryName, string tokenName, int index, string prefix)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);

            FilterOption fo = new FilterOption(tokenName, null);
            if (fo.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                fo.Token = QueryUtils.Parse(tokenName, qd, canAggregate: false);
            }
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).FirstEx();

            return Content(SearchControlHelper.NewFilter(CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index).ToHtmlString());
        }

        [HttpPost]
        public ContentResult GetColumnName(string webQueryName, string tokenName)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            QueryToken token = QueryUtils.Parse(tokenName, qd, canAggregate: false);
            return Content(token.NiceName());
        }

        [HttpPost]
        public ContentResult SelectedItemsContextMenu(string liteKeys, string webQueryName, string implementationsKey, string prefix)
        {
            var noResults = new HtmlTag("li").Class("sf-search-ctxitem sf-search-ctxitem-no-results")
                .InnerHtml(new HtmlTag("span").InnerHtml(SearchMessage.NoResults.NiceToString().EncodeHtml()).ToHtml())
                .ToHtml().ToString();

            if (string.IsNullOrEmpty(liteKeys))
                return Content(noResults);

            var lites = Navigator.ParseLiteKeys<IdentifiableEntity>(liteKeys);
            object queryName = Navigator.ResolveQueryName(webQueryName);
            Implementations implementations = implementationsKey == "[All]" ? Implementations.ByAll :
                Implementations.By(implementationsKey.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).Select(t => Navigator.ResolveType(t)).ToArray());
            
            string result = ContextualItemsHelper.GetContextualItemListForLites(new SelectedItemsMenuContext
            {
                ControllerContext = this.ControllerContext,
                Lites = lites,
                QueryName = queryName,
                Implementations = implementations,
                Prefix = prefix,
            }).ToString("");

            if (string.IsNullOrEmpty(result))
                return Content(noResults); 
            else 
                return Content(result);
        }

        [HttpPost]
        public ContentResult QuickFilter(string webQueryName, string tokenName, int index, string prefix, string value)
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

            return Content(SearchControlHelper.NewFilter(CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index).ToHtmlString());
        }

        [HttpPost]
        public ContentResult NewSubTokensCombo(string webQueryName, string tokenName, string prefix, int index)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            var token = QueryUtils.Parse(tokenName, qd, canAggregate: false);

            var combo = CreateHtmlHelper(this).QueryTokenCombo(token, null, new Context(null, prefix), index + 1, qd, canAggregate: false);

            var content = combo.ToHtmlString();

            if (content.HasText())
                return Content(content);
            else
                return Content("<span>no-results</span>");
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

    public class AutoCompleteResult
    {
        public AutoCompleteResult()
        {
        }

        public AutoCompleteResult(Lite<IdentifiableEntity> lite)
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
