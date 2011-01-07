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
#endregion

namespace Signum.Web.Controllers
{
    [HandleException, AuthenticationRequired, UserInterface]
    public class SignumController : Controller
    {
        [ValidateInput(false)]  //this is needed since a return content(View...) from an action that doesn't validate will throw here an exception. We suppose that validation has already been performed before getting here
        public ViewResult View(string webTypeName, int? id)
        {
            Type t = Navigator.ResolveType(webTypeName);

            Response.CacheControl = "no-cache";
            Response.AddHeader("Pragma", "no-cache");
            Response.Expires = -1;

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

        public ActionResult Create(string sfRuntimeType, string sfOnOk, string sfOnCancel, string prefix)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);

            return Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.Navigate);
        }

        public PartialViewResult PopupCreate(string sfRuntimeType, string sfOnOk, string sfOnCancel, string prefix, string sfUrl)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);

            object result = Constructor.VisualConstruct(this, type, prefix, VisualConstructStyle.PopupView);
            if (result.GetType() == typeof(PartialViewResult))
                return (PartialViewResult)result;

            if (result.GetType().IsEmbeddedEntity())
                throw new InvalidOperationException("PopupCreate cannot be called for EmbeddedEntity {0}".Formato(result.GetType()));

            if (!typeof(IdentifiableEntity).IsAssignableFrom(result.GetType()))
                throw new InvalidOperationException("Invalid result type for a Constructor");

            IdentifiableEntity entity = (IdentifiableEntity)result;

            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, entity, prefix, sfUrl);
        }

        public PartialViewResult PopupView(string sfRuntimeType, int? sfId, string sfOnOk, string sfOnCancel, string prefix, bool? sfReadOnly, string sfUrl)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            bool isReactive = this.IsReactive();

            IdentifiableEntity entity = null;
            if (isReactive)
            {
                IdentifiableEntity parent = (IdentifiableEntity)this.UntypedExtractEntity().ThrowIfNullC("PopupView: Entity was not possible to extract");
                entity = (IdentifiableEntity)MappingContext.FindSubentity(parent, prefix);
            }
            if (entity == null || entity.GetType() != type || sfId != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve(type, sfId.Value);
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

            if (sfReadOnly.HasValue)
                tc.ReadOnly = true;

            return Navigator.PopupView(this, tc, sfUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult PartialView(string sfRuntimeType, int? sfId, string prefix, bool? sfEmbeddedControl, bool? sfReadOnly, string sfUrl)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            bool isReactive = this.IsReactive();

            IdentifiableEntity entity = null;
            if (isReactive)
            {
                IdentifiableEntity parent = (IdentifiableEntity)this.UntypedExtractEntity()
                    .ThrowIfNullC("PartialView: Entity was not possible to extract");
                entity = (IdentifiableEntity)MappingContext.FindSubentity(parent, prefix);
            }
            if (entity == null || entity.GetType() != type || sfId != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve(type, sfId.Value);
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

            if (sfReadOnly.HasValue)
                tc.ReadOnly = true;

            return Navigator.PartialView(this, tc, sfUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult TrySave()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            if (context.GlobalErrors.Any())
            {
                this.ModelState.FromContext(context);
                return Navigator.ModelState(ModelState);
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
                return Navigator.RedirectUrl(newUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult Validate()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);
            return Navigator.ModelState(ModelState);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TrySavePartial(string prefix)
        {
            MappingContext context = this.UntypedExtractEntity(prefix).UntypedApplyChanges(ControllerContext, prefix, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);

            IdentifiableEntity ident = context.UntypedValue as IdentifiableEntity;
            if (ident != null && !context.GlobalErrors.Any())
                Database.Save(ident);

            string newLink = Navigator.ViewRoute(context.UntypedValue.GetType(), ident.TryCS(e => e.IdOrNull));

            return Navigator.ModelState(new ModelStateData(this.ModelState)
            {
                NewToStr = context.UntypedValue.ToString(),
                NewtoStrLink = newLink
            });
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult ValidatePartial(string prefix)
        {
            MappingContext context = this.UntypedExtractEntity(prefix).UntypedApplyChanges(ControllerContext, prefix, true).UntypedValidateGlobal();

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

            return Navigator.ModelState(new ModelStateData(this.ModelState)
            {
                NewToStr = context.UntypedValue.ToString(),
                NewtoStrLink = newLink
            });
        }

        [AcceptVerbs(HttpVerbs.Get)]
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

            return Json(result, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Find(FindOptions findOptions)
        {
            return Navigator.Find(this, findOptions);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult PartialFind(FindOptions findOptions, string prefix)
        {
            return Navigator.PartialFind(this, findOptions, prefix);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult Search(FindOptions findOptions, int? sfTop, string prefix)
        {
            return Navigator.Search(this, findOptions, sfTop, prefix);
        }

		[AcceptVerbs(HttpVerbs.Post)]
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

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult GetContextualPanel(string lite, string webQueryName, string prefix)
        {
            string[] liteParts = lite.Split(new string[] { ";" }, StringSplitOptions.RemoveEmptyEntries);

            object queryName = Navigator.ResolveQueryName(webQueryName);
            
            string result = ContextualItemsHelper.GetContextualItemListForLite(this.ControllerContext, Lite.Create(Navigator.ResolveType(liteParts[0]), int.Parse(liteParts[1])) , queryName, prefix).ToString("");

            if (string.IsNullOrEmpty(result))
                result = Resources.NoResults;

            return Content(result);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult QuickFilter(string webQueryName, string tokenName, int index, string prefix, string sfValue)
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
                fo.Value = FindOptionsModelBinder.Convert(sfValue, fo.Token.Type);
            }
            catch (Exception) 
            { 
                //Cell Value must be custom and cannot be parsed automatically: Leave value to null
            }

            return Content(SearchControlHelper.NewFilter(CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index).ToHtmlString());
        }

        [AcceptVerbs(HttpVerbs.Post)]
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

        [AcceptVerbs(HttpVerbs.Post)]
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
                    .Attrs(new { type = "button", name = webTypeName, value = t.NiceName() })
                    .ToHtmlSelf());
                sb.Add(new HtmlTag("br").ToHtmlSelf());
            }

            ViewData.Model = new Context(null, prefix);
            ViewData[ViewDataKeys.CustomHtml] = sb.ToHtml().ToString();

            return PartialView(Navigator.Manager.ChooserPopupUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult GetChooser(List<string> buttons, List<string> ids, string prefix, string title)
        {
            if (buttons == null || buttons.Count == 0)
                throw new InvalidOperationException("GetChooser needs a list of options");

            HtmlStringBuilder sb = new HtmlStringBuilder();
            int i = 0;
            foreach (string button in buttons) 
            {
                string id = ids != null ? ids[i] : button.Replace(" ", "");
                sb.Add(new HtmlTag("input", id).Attrs(new { type = "button", name = id, value = button }).ToHtmlSelf());
                sb.Add(new HtmlTag("br").ToHtmlSelf());
                i++;
            }

            ViewData.Model = new Context(null, prefix);
            ViewData[ViewDataKeys.CustomHtml] = sb.ToString();
            if (!string.IsNullOrEmpty(title)) ViewData[ViewDataKeys.PageTitle] = title;
            return PartialView(Navigator.Manager.ChooserPopupUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult ReloadEntity(string prefix, string sfPartialViewName)
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
                return Navigator.NormalControl(this, entity, sfPartialViewName);
        }

        public static HtmlHelper CreateHtmlHelper(Controller c)
        {
            return new HtmlHelper(
                        new ViewContext(
                            c.ControllerContext,
                            new WebFormView(c.ControllerContext.RequestContext.HttpContext.Request.FilePath),
                            c.ViewData,
                            c.TempData,
                            c.Response.Output
                        ),
                        new ViewPage());
        }
    }
}
