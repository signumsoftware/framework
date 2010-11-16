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
        public ViewResult View(string typeUrlName, int? id)
        {
            Type t = Navigator.ResolveTypeFromUrlName(typeUrlName);

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

            if (typeof(EmbeddedEntity).IsAssignableFrom(result.GetType()))
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

        JavaScriptSerializer jSerializer = new JavaScriptSerializer();

        [AcceptVerbs(HttpVerbs.Get)]
        public ContentResult Autocomplete(string typeName, Implementations implementations, string q, int l)
        {
            Type type = Navigator.NamesToTypes[typeName];

            return (Content(jSerializer.Serialize(
                AutoCompleteUtils.FindLiteLike(type, implementations, q, l)
                .Select(o => new { id = o.Id, text = o.ToStr, type = o.RuntimeType.Name }).ToList())));
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
        public ContentResult AddFilter(string sfQueryUrlName, string tokenName, int index, string prefix)
        {
            object queryName = Navigator.ResolveQueryFromUrlName(sfQueryUrlName);

            FilterOption fo = new FilterOption(tokenName, null);
            if (fo.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                fo.Token = QueryUtils.Parse(tokenName, qd);
            }
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).First();

            return Content(SearchControlHelper.NewFilter(CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult GetContextualPanel(string liteUrl, string sfQueryUrlName, string prefix)
        {
            string[] urlParts = liteUrl.Split(new string[] { "/" }, StringSplitOptions.RemoveEmptyEntries);

            Type t = Navigator.ResolveTypeFromUrlName(urlParts[1]);
            int id = int.Parse(urlParts[2]);
            Lite lite = Lite.Create(t, id);

            object queryName = Navigator.ResolveQueryFromUrlName(sfQueryUrlName);
            
            string result = ContextualItemsHelper.GetContextualItemListForLite(this.ControllerContext, lite, queryName, prefix).ToString("");

            if (string.IsNullOrEmpty(result))
                result = Resources.NoResults;

            return Content(result);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult QuickFilter(string sfQueryUrlName, string tokenName, int index, string prefix, bool isLite, string typeUrlName, int? sfId, string sfValue)
        {
            object queryName = Navigator.ResolveQueryFromUrlName(sfQueryUrlName);

            FilterOption fo = new FilterOption(tokenName, null);
            if (fo.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                fo.Token = QueryUtils.Parse(tokenName, qd); 
            }
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).First();
            
            if (isLite)
                sfValue = sfId.Value + ";" + Navigator.TypesToNames[Navigator.ResolveTypeFromUrlName(typeUrlName)];
            
            try
            {
                fo.Value = FindOptionsModelBinder.Convert(sfValue, fo.Token.Type);
            }
            catch (Exception) 
            { 
                //Cell Value must be custom and cannot be parsed automatically: Leave value to null
            }

            return Content(SearchControlHelper.NewFilter(CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult NewSubTokensCombo(string sfQueryUrlName, string tokenName, string prefix, int index)
        { 
            object queryName = Navigator.ResolveQueryFromUrlName(sfQueryUrlName);
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
            return Content(SearchControlHelper.TokensCombo(CreateHtmlHelper(this), Navigator.Manager.QuerySettings[queryName].UrlName, items, new Context(null, prefix), index + 1, true));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult GetTypeChooser(Implementations sfImplementations, string prefix)
        {
            if (sfImplementations == null || sfImplementations.IsByAll)
                throw new InvalidOperationException("GetTypeChooser needs an ImplementedBy");

            string strButtons = ((ImplementedByAttribute)sfImplementations).ImplementedTypes
                .ToString(type => {
                    TagBuilder builder = new TagBuilder("input");
                    builder.GenerateId(type.Name);
                    builder.MergeAttribute("type", "button");
                    builder.MergeAttribute("name", type.Name);
                    builder.MergeAttribute("value", type.NiceName());
                    
                    return builder.ToString(TagRenderMode.SelfClosing);
                }, "");

            ViewData.Model = new Context(null, prefix);
            ViewData[ViewDataKeys.CustomHtml] = strButtons;

            return PartialView(Navigator.Manager.ChooserPopupUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult GetChooser(List<string> buttons, List<string> ids, string prefix, string title)
        {
            if (buttons == null || buttons.Count == 0)
                throw new InvalidOperationException("GetChooser needs a list of options");

            StringBuilder sb = new StringBuilder();
            int i = 0;
            foreach (string button in buttons) {
                TagBuilder builder = new TagBuilder("input");
                string id = ids != null ? ids[i] : button.Replace(" ", "");
                builder.GenerateId(id);
                builder.MergeAttribute("type", "button");
                builder.MergeAttribute("name", id);
                builder.MergeAttribute("value", button);

                sb.Append(builder.ToString(TagRenderMode.SelfClosing));
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

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DoPostBack()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);

            ViewData[ViewDataKeys.ChangeTicks] = context.GetTicksDictionary();
            return Navigator.View(this, (IdentifiableEntity)context.UntypedValue);
        }

        public ActionResult Error()
        {
            Exception ex = HttpContext.Session[HandleExceptionAttribute.ErrorSessionKey] as Exception;
            HttpContext.Application.Remove(Request.UserHostAddress);
            ViewData.Model = ex;
            return View(Navigator.Manager.ErrorPageUrl);
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

        public static string GetMimeType(string extension)
        {
            if (extension.HasText() && !extension.StartsWith("."))
                extension = ".{0}".Formato(extension);

            string mimeType = String.Empty;

            // Attempt to get the mime-type from the registry.
            Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(extension);

            if (regKey != null)
            {
                string type = (string)regKey.GetValue("Content Type");

                if (type != null)
                    mimeType = type;
            }

            if (!mimeType.HasText())
            {
                switch (extension)
                {
                    case "txt":
                        mimeType = "application/plain-text";
                        break;
                    case ".doc":
                        mimeType = "application/msword";
                        break;
                    case ".dot":
                        mimeType = "application/msword";
                        break;
                    case ".docx":
                        mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                        break;
                    case ".dotx":
                        mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.template";
                        break;
                    case ".docm":
                        mimeType = "application/vnd.ms-word.document.macroEnabled.12";
                        break;
                    case ".dotm":
                        mimeType = "application/vnd.ms-word.template.macroEnabled.12";
                        break;
                    case ".xls":
                        mimeType = "application/vnd.ms-excel";
                        break;
                    case ".xlt":
                        mimeType = "application/vnd.ms-excel";
                        break;
                    case ".xla":
                        mimeType = "application/vnd.ms-excel";
                        break;
                    case ".xlsx":
                        mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                        break;
                    case ".xltx":
                        mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.template";
                        break;
                    case ".xlsm":
                        mimeType = "application/vnd.ms-excel.sheet.macroEnabled.12";
                        break;
                    case ".xltm":
                        mimeType = "application/vnd.ms-excel.template.macroEnabled.12";
                        break;
                    case ".xlam":
                        mimeType = "application/vnd.ms-excel.addin.macroEnabled.12";
                        break;
                    case ".xlsb":
                        mimeType = "application/vnd.ms-excel.sheet.binary.macroEnabled.12";
                        break;
                    case ".ppt":
                        mimeType = "application/vnd.ms-powerpoint";
                        break;
                    case ".pot":
                        mimeType = "application/vnd.ms-powerpoint";
                        break;
                    case ".pps":
                        mimeType = "application/vnd.ms-powerpoint";
                        break;
                    case ".ppa":
                        mimeType = "application/vnd.ms-powerpoint";
                        break;
                    case ".pptx":
                        mimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                        break;
                    case ".potx":
                        mimeType = "application/vnd.openxmlformats-officedocument.presentationml.template";
                        break;
                    case ".ppsx":
                        mimeType = "application/vnd.openxmlformats-officedocument.presentationml.slideshow";
                        break;
                    case ".ppam":
                        mimeType = "application/vnd.ms-powerpoint.addin.macroEnabled.12";
                        break;
                    case ".pptm":
                        mimeType = "application/vnd.ms-powerpoint.presentation.macroEnabled.12";
                        break;
                    case ".potm":
                        mimeType = "application/vnd.ms-powerpoint.presentation.macroEnabled.12";
                        break;
                    case ".ppsm":
                        mimeType = "application/vnd.ms-powerpoint.slideshow.macroEnabled.12";
                        break;
                    default:
                        mimeType = "application/" + extension.Right(extension.Length -1);
                        break;
                }
            }

            return mimeType;
        }
    }
}
