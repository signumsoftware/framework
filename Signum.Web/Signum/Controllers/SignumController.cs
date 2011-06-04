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
using Signum.Web.Properties;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Reflection;
using Signum.Entities.DynamicQuery;
using System.Reflection;
using System.Collections;
using System.Collections.Specialized;
using System.Web.Script.Serialization;
using System.Text;
using System.IO;
#endregion

namespace Signum.Web.Controllers
{
    public class SignumController : Controller
    {
        [ValidateInput(false)]  //this is needed since a return content(View...) from an action that doesn't validate will throw here an exception. We suppose that validation has already been performed before getting here
        public ViewResult View(string webTypeName, int? id)
        {
            Type t = Navigator.ResolveType(webTypeName);

            if (id.HasValue && id.Value > 0)
                return Navigator.View(this, Database.Retrieve(t, id.Value), true); //Always admin

            IdentifiableEntity entity = null;
            object result = Constructor.Construct(t);
            if (typeof(IdentifiableEntity).IsAssignableFrom(result.GetType()))
                entity = (IdentifiableEntity)result;
            else
                throw new InvalidOperationException("Invalid result type for a Constructor");
             
            return Navigator.View(this, entity, true); //Always admin
        }

        public ActionResult Create(string runtimeType, string prefix)
        {
            Type type = Navigator.ResolveType(runtimeType);

            return Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.Navigate);
        }

        public PartialViewResult PopupCreate(string runtimeType, string prefix, string url)
        {
            Type type = Navigator.ResolveType(runtimeType);

            object result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PopupView);
            if (result.GetType() == typeof(PartialViewResult))
                return (PartialViewResult)result;

            if (result.GetType().IsEmbeddedEntity())
                throw new InvalidOperationException("PopupCreate cannot be called for EmbeddedEntity {0}".Formato(result.GetType()));

            if (!typeof(IdentifiableEntity).IsAssignableFrom(result.GetType()))
                throw new InvalidOperationException("Invalid result type for a Constructor");

            IdentifiableEntity entity = (IdentifiableEntity)result;

            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, entity, prefix, url);
        }

        public PartialViewResult PopupView(string runtimeType, int? id, string prefix, bool? readOnly, string url)
        {
            Type type = Navigator.ResolveType(runtimeType);
            bool isReactive = this.IsReactive();

            IdentifiableEntity entity = null;
            if (isReactive)
            {
                IdentifiableEntity parent = (IdentifiableEntity)this.UntypedExtractEntity().ThrowIfNullC("PopupView: Entity was not possible to extract");
                entity = (IdentifiableEntity)MappingContext.FindSubentity(parent, prefix);
            }
            if (entity == null || entity.GetType() != type || id != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (id.HasValue)
                    entity = Database.Retrieve(type, id.Value);
                else
                {
                    ActionResult result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PopupView);
                    if (result is PartialViewResult)
                        return (PartialViewResult)result;
                    else
                        throw new InvalidOperationException("Invalid result type for a Constructor");
                }
            }

            if (isReactive)
                this.ViewData[ViewDataKeys.Reactive] = true;

            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);

            if (readOnly.HasValue)
                tc.ReadOnly = true;

            return Navigator.PopupView(this, tc, url);
        }

        [HttpPost]
        public PartialViewResult PartialView(string runtimeType, int? id, string prefix, bool? readOnly, string url)
        {
            Type type = Navigator.ResolveType(runtimeType);
            bool isReactive = this.IsReactive();

            IdentifiableEntity entity = null;
            if (isReactive)
            {
                IdentifiableEntity parent = (IdentifiableEntity)this.UntypedExtractEntity()
                    .ThrowIfNullC("PartialView: Entity was not possible to extract");
                entity = (IdentifiableEntity)MappingContext.FindSubentity(parent, prefix);
            }
            if (entity == null || entity.GetType() != type || id != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (id.HasValue)
                    entity = Database.Retrieve(type, id.Value);
                else
                {
                    object result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PartialView);
                    if (result is PartialViewResult)
                        return (PartialViewResult)result;
                    else
                        throw new InvalidOperationException("Invalid result type for a Constructor");
                }
            }

            if (isReactive)
                this.ViewData[ViewDataKeys.Reactive] = true;

            TypeContext tc = TypeContextUtilities.UntypedNew((IdentifiableEntity)entity, prefix);

            if (readOnly.HasValue)
                tc.ReadOnly = true;

            return Navigator.PartialView(this, tc, url);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult TrySave()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            if (context.GlobalErrors.Any())
            {
                this.ModelState.FromContext(context);
                return JsonAction.ModelState(ModelState);
            }

            IdentifiableEntity ident = context.UntypedValue as IdentifiableEntity;
            if (ident == null)
                throw new ArgumentNullException("No IdentifiableEntity to save");

            Database.Save(ident);

            ViewData[ViewDataKeys.ChangeTicks] = context.GetTicksDictionary();

            string newUrl = Navigator.ViewRoute(ident.GetType(), ident.Id);
            if (HttpContext.Request.UrlReferrer.AbsolutePath.Contains(newUrl))
                return Navigator.View(this, ident, true);
            else
                return JsonAction.Redirect(newUrl);
        }

        [HttpPost]
        public JsonResult Validate()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);
            return JsonAction.ModelState(ModelState);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public JsonResult TrySavePartial(string prefix)
        {
            MappingContext context = this.UntypedExtractEntity(prefix).UntypedApplyChanges(ControllerContext, prefix, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);

            IdentifiableEntity ident = context.UntypedValue as IdentifiableEntity;
            if (ident != null && !context.GlobalErrors.Any())
                Database.Save(ident);

            string newLink = Navigator.ViewRoute(context.UntypedValue.GetType(), ident.TryCS(e => e.IdOrNull));

            return JsonAction.ModelState(ModelState, context.UntypedValue.ToString(), newLink);
        }

        [HttpPost, ValidateInput(false)]
        public JsonResult ValidatePartial(string prefix)
        {
            ModifiableEntity mod = this.UntypedExtractEntity(prefix);
            MappingContext context = null;
            if (mod as EmbeddedEntity != null && !(mod is ModelEntity))
            {
                mod = this.UntypedExtractEntity(); //apply changes to the parent entity
                context = mod.UntypedApplyChanges(ControllerContext, "", true).UntypedValidateGlobal();
            }
            else
            {
                context = mod.UntypedApplyChanges(ControllerContext, prefix, true).UntypedValidateGlobal();
            }

            this.ModelState.FromContext(context);

            string newLink = "";
            IIdentifiable ident = context.UntypedValue as IIdentifiable;
            if (context.UntypedValue == null)
            {
                RuntimeInfo ei = RuntimeInfo.FromFormValue(Request.Form[TypeContextUtilities.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);
                newLink = Navigator.ViewRoute(ei.RuntimeType, ident.TryCS(e => e.IdOrNull));
            }
            else
                newLink = Navigator.ViewRoute(context.UntypedValue.GetType(), ident.TryCS(e => e.IdOrNull));

            return JsonAction.ModelState(ModelState, context.UntypedValue.ToString(), newLink);
        }

        [HttpPost]
        public JsonResult Autocomplete(string types, string q, int l)
        {
            Type[] typeArray = StaticInfo.ParseTypes(types);
            if (typeArray == StaticInfo.ImplementedByAll)
                throw new ArgumentException("ImplementedBy not allowed in Autocomplete");

            List<Lite> lites  = AutoCompleteUtils.FindLiteLike(typeof(IdentifiableEntity), typeArray, q, l);

            var result = lites.Select(o => new
            {
                id = o.Id,
                text = o.ToStr,
                type = Navigator.ResolveWebTypeName(o.RuntimeType)
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
        public PartialViewResult Search(FindOptions findOptions, int? top, string prefix)
        {
            return Navigator.Search(this, findOptions, top, prefix);
        }

        [HttpPost]
        public ContentResult AddFilter(string webQueryName, string tokenName, int index, string prefix)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);

            FilterOption fo = new FilterOption(tokenName, null);
            if (fo.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                fo.Token = QueryUtils.Parse(tokenName, qd);
            }
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).First();

            return Content(SearchControlHelper.NewFilter(CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index).ToHtmlString());
        }

        [HttpPost]
        public ContentResult GetColumnName(string webQueryName, string tokenName)
        {
            object queryName = Navigator.ResolveQueryName(webQueryName);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            QueryToken token = QueryUtils.Parse(tokenName, qd);
            return Content(token.NiceName());
        }

        [HttpPost]
        public ContentResult GetContextualPanel(string lite, string webQueryName, string prefix)
        {
            string[] liteParts = lite.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

            object queryName = Navigator.ResolveQueryName(webQueryName);
            
            string result = ContextualItemsHelper.GetContextualItemListForLite(this.ControllerContext, Lite.Create(Navigator.ResolveType(liteParts[0]), int.Parse(liteParts[1])) , queryName, prefix).ToString("");

            if (string.IsNullOrEmpty(result))
                result = Resources.NoResults;

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
                fo.Token = QueryUtils.Parse(tokenName, qd); 
            }
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).First();
            
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
            QueryToken[] subtokens = QueryUtils.Parse(tokenName, t => QueryUtils.SubTokens(t, qd.Columns)).SubTokens();
            if (subtokens == null)
                return Content("");

            var items = subtokens.Select(t => new SelectListItem
            {
                Text = t.ToString(),
                Value = t.Key,
                Selected = false
            }).ToList();
            items.Insert(0, new SelectListItem { Text = "-", Selected = true, Value = "" });

            return Content(SearchControlHelper.TokensCombo(CreateHtmlHelper(this), queryName, items, new Context(null, prefix), index + 1, true).ToHtmlString());
        }

        [HttpPost]
        public PartialViewResult GetTypeChooser(string types, string prefix)
        {
            Type[] typeArray = StaticInfo.ParseTypes(types);

            if (typeArray == StaticInfo.ImplementedByAll)
                throw new ArgumentException("ImplementedByAll is not allowed in GetTypeChooser");

            if (typeArray.Length == 1)
                throw new ArgumentException("GetTypeChooser must recieve at least 2 types to chose from");

            HtmlStringBuilder sb = new HtmlStringBuilder();
            foreach (Type t in typeArray)
            {
                string webTypeName = Navigator.ResolveWebTypeName(t);

                sb.Add(new HtmlTag("input", webTypeName)
                    .Attrs(new { type = "button", name = webTypeName, value = t.NiceName(), @class = "sf-chooser-button" })
                    .ToHtmlSelf());
                sb.Add(new HtmlTag("br").ToHtmlSelf());
            }

            ViewData.Model = new Context(null, prefix);
            ViewData[ViewDataKeys.CustomHtml] = sb.ToHtml();

            return PartialView(Navigator.Manager.ChooserPopupView);
        }

        [HttpPost]
        public PartialViewResult GetChooser(List<string> buttons, List<string> ids, string prefix, string title)
        {
            if (buttons == null || buttons.Count == 0)
                throw new InvalidOperationException("GetChooser needs a list of options");

            HtmlStringBuilder sb = new HtmlStringBuilder();
            int i = 0;
            foreach (string button in buttons) 
            {
                string id = ids != null ? ids[i] : button.Replace(" ", "");
                sb.Add(new HtmlTag("input", id)
                    .Attrs(new { type = "button", name = id, value = button, @class="sf-chooser-button" })
                    .ToHtmlSelf());
                sb.Add(new HtmlTag("br").ToHtmlSelf());
                i++;
            }

            ViewData.Model = new Context(null, prefix);
            ViewData[ViewDataKeys.CustomHtml] = sb.ToHtml();
            if (title.HasText())
                ViewData[ViewDataKeys.Title] = title;

            return PartialView(Navigator.Manager.ChooserPopupView);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public PartialViewResult ReloadEntity(string prefix, string partialViewName)
        {
            bool isReactive = this.IsReactive();

            var ctx = this.UntypedExtractEntity(prefix)
                .UntypedApplyChanges(this.ControllerContext, isReactive ? "" : prefix, true);

            IdentifiableEntity entity = (IdentifiableEntity)ctx.UntypedValue;

            var ticksDic = ctx.GetTicksDictionary();

            if (isReactive && prefix.HasText() && !prefix.StartsWith("New"))
            {
                ModifiableEntity subentity = (ModifiableEntity)MappingContext.FindSubentity(entity, prefix);

                //If subentity == null, it's a new entity => create it and apply changes partially
                if (subentity == null)
                {
                    string runtimeInfoKey = TypeContextUtilities.Compose(prefix ?? "", EntityBaseKeys.RuntimeInfo);
                    if (Request.Form.AllKeys.Contains(runtimeInfoKey))
                    { // If there's runtimeInfo in the form => use it to create subentity
                        RuntimeInfo runtimeInfo = RuntimeInfo.FromFormValue(Request.Form[runtimeInfoKey]);
                        if (runtimeInfo.IdOrNull != null)
                            subentity = Database.Retrieve(runtimeInfo.RuntimeType, runtimeInfo.IdOrNull.Value);
                        else
                            subentity = (ModifiableEntity)Constructor.Construct(runtimeInfo.RuntimeType);
                    }
                    else
                    { // Try to create subentity from the string route => will fail if there are interfaces
                        Type type = MappingContext.FindSubentityType(entity, prefix);
                        subentity = (ModifiableEntity)Constructor.Construct(type);
                    }

                    var subctx = subentity.UntypedApplyChanges(this.ControllerContext, prefix, true);

                    ticksDic.AddRange(subctx.GetTicksDictionary());

                    subentity = (ModifiableEntity)subctx.UntypedValue;
                }

                entity = (IdentifiableEntity)subentity;
            }

            this.ViewData[ViewDataKeys.ChangeTicks] = ticksDic;
            
            if (isReactive)
            {
                if (!prefix.HasText())
                    Session[this.TabID()] = entity;
                this.ViewData[ViewDataKeys.Reactive] = true;
            }

            if (prefix.HasText())
                return Navigator.PartialView(this, entity, prefix);
            else
                return Navigator.NormalControl(this, entity, partialViewName);
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
}
