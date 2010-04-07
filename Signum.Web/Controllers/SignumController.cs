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
#endregion

namespace Signum.Web.Controllers
{
    [HandleException, AuthenticationRequired]
    public class SignumController : Controller
    {
        static SignumController()
        {
            ModelBinders.Binders[typeof(FindOptions)] = new FindOptionsModelBinder();
        }

        public ViewResult View(string typeUrlName, int? id)
        {
            Type t = Navigator.ResolveTypeFromUrlName(typeUrlName);
            
            if (id.HasValue && id.Value > 0)
                return Navigator.View(this, Database.Retrieve(t, id.Value), true); //Always admin

            IIdentifiable entity = null;
            object result = Constructor.Construct(t);
            if (typeof(IIdentifiable).IsAssignableFrom(result.GetType()))
                entity = (IIdentifiable)result;
            else
                throw new InvalidOperationException("Invalid result type for a Direct Constructor");

            return Navigator.View(this, entity, true); //Always admin
        }

        public ActionResult Create(string sfRuntimeType, string sfOnOk, string sfOnCancel, string prefix)
        { 
            Type type = Navigator.ResolveType(sfRuntimeType);

            IIdentifiable entity = null;
            object result = Constructor.VisualConstruct(type, this);
            if (result.GetType() == typeof(PartialViewResult))
                return (PartialViewResult)result;
            else if (typeof(IIdentifiable).IsAssignableFrom(result.GetType()))
                entity = (IIdentifiable)result;
            else
                throw new InvalidOperationException("Invalid result type for a Constructor");

            return Content(Navigator.ViewRoute(type, null));
        }

        public PartialViewResult PopupCreate(string sfRuntimeType, int? sfId, string sfOnOk, string sfOnCancel, string prefix, bool? sfReadOnly, string sfUrl)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            
            ModifiableEntity entity = null;
            if (sfId.HasValue)
                entity = Database.Retrieve(type, sfId.Value);
            else
            {
                object result = Constructor.VisualConstruct(type, this);
                if (result.GetType() == typeof(PartialViewResult))
                    return (PartialViewResult)result;
                else if (typeof(ModifiableEntity).IsAssignableFrom(result.GetType()))
                    entity = (ModifiableEntity)result;
                else
                    throw new InvalidOperationException("Invalid result type for a Constructor");
            }
            
            if (typeof(EmbeddedEntity).IsAssignableFrom(entity.GetType()))
                throw new InvalidOperationException("PopupCreate cannot be called for Embedded type {0}".Formato(entity.GetType()));

            if (sfReadOnly.HasValue)
                ViewData[ViewDataKeys.StyleContext] = new StyleContext(false) { ReadOnly = true };

            ViewData[ViewDataKeys.WriteSFInfo] = true;
            return Navigator.PopupView(this, entity, prefix, sfUrl);
        }

        public PartialViewResult PopupView(string sfRuntimeType, int? sfId, string sfOnOk, string sfOnCancel, string prefix, bool? sfReadOnly, string sfUrl)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            bool isReactive = this.IsReactive();
            
            ModifiableEntity entity = null;
            if (isReactive)
            {
                entity = this.UntypedExtractEntity().ThrowIfNullC(Resources.PartialViewTypeWasNotPossibleToExtract);
                entity = (ModifiableEntity)MappingContext.GetPropertyValue(entity, prefix);
            }
            if (entity == null || entity.GetType() != type || sfId != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve(type, sfId.Value);
                else
                {
                    object result = Constructor.VisualConstruct(type, this);
                    if (result.GetType() == typeof(PartialViewResult))
                        return (PartialViewResult)result;
                    else if (typeof(ModifiableEntity).IsAssignableFrom(result.GetType()))
                        entity = (ModifiableEntity)result;
                    else
                        throw new InvalidOperationException("Invalid result type for a Constructor");
                }
            }

            if (typeof(EmbeddedEntity).IsAssignableFrom(entity.GetType()))
                throw new InvalidOperationException("PopupView cannot be called for Embedded type {0}".Formato(entity.GetType()));

            if (isReactive)
                this.ViewData[ViewDataKeys.Reactive] = true;

            if (sfReadOnly.HasValue)
                ViewData[ViewDataKeys.StyleContext] = new StyleContext(false) { ReadOnly = true };

            return Navigator.PopupView(this, entity, prefix, sfUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult PartialView(string sfRuntimeType, int? sfId, string prefix, bool? sfEmbeddedControl, bool? sfReadOnly, string sfUrl)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            bool isReactive = this.IsReactive();

            ModifiableEntity entity = null;
            if (isReactive)
            {
                entity = this.UntypedExtractEntity()
                    .ThrowIfNullC(Resources.PartialViewTypeWasNotPossibleToExtract);
                entity = (ModifiableEntity)MappingContext.GetPropertyValue(entity, prefix);
            }
            if (entity == null || entity.GetType() != type || sfId != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve(type, sfId.Value);
                else
                {
                    object result = Constructor.VisualConstruct(type, this);
                    if (result.GetType() == typeof(PartialViewResult))
                        return (PartialViewResult)result;
                    else if (typeof(ModifiableEntity).IsAssignableFrom(result.GetType()))
                        entity = (ModifiableEntity)result;
                    else
                        throw new InvalidOperationException("Invalid result type for a Constructor");
                }
            }

            if (isReactive)
                this.ViewData[ViewDataKeys.Reactive] = true;

            if (typeof(EmbeddedEntity).IsAssignableFrom(entity.GetType()))
                throw new InvalidOperationException("PopupView cannot be called for Embedded type {0}".Formato(entity.GetType()));

            if (sfReadOnly.HasValue)
                ViewData[ViewDataKeys.StyleContext] = new StyleContext(false) { ReadOnly = true };

            return Navigator.PartialView(this, entity, prefix, sfUrl);
        }

       [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult TrySave()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal(); 

            if (context.Errors.Count > 0)
            {
                this.ModelState.FromContext(context);
                return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
            }

            IdentifiableEntity ident = context.UntypedValue as IdentifiableEntity;
            if (ident != null)
                Database.Save(ident);

            if (this.IsReactive())
                Session[this.TabID()] = context.UntypedValue;
            
            return Navigator.View(this, context.UntypedValue); //changesLog.ChangeTicks);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult Validate()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);

            return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TrySavePartial(string prefix)
        {
            MappingContext context = this.UntypedExtractEntity(prefix).UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);

            IdentifiableEntity ident = context.UntypedValue as IdentifiableEntity;
            if (ident != null && context.Errors.Count == 0)
                Database.Save(ident);

            //Modifiable entity = (Navigator.ExtractIsReactive(Request.Form) && prefix.HasText() && !prefix.Contains("_New")) ? 
            //    Modification.GetPropertyValue(parentEntity, prefix) : 
            //    parentEntity;

            string newLink = Navigator.ViewRoute(context.UntypedValue.GetType(), ident.TryCS(e => e.IdOrNull));

            return Content("{{\"ModelState\":{0}, \"{1}\":\"{2}\", \"{3}\":\"{4}\"}}".Formato(
                this.ModelState.ToJsonData(),
                TypeContext.Separator + EntityBaseKeys.ToStr,
                context.UntypedValue.ToString(),
                TypeContext.Separator + EntityBaseKeys.ToStrLink,
                newLink
                ));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult ValidatePartial(string prefix)
        {
            MappingContext context = this.UntypedExtractEntity(prefix).UntypedApplyChanges(ControllerContext, prefix, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);

            //Modifiable entity = (Navigator.ExtractIsReactive(Request.Form) && prefix.HasText() && !prefix.Contains("_New")) ? 
            //    Modification.GetPropertyValue(parentEntity, prefix) : 
            //    parentEntity;

            string newLink = "";
            IIdentifiable ident = context.UntypedValue as IIdentifiable;
            if (context.UntypedValue == null)
            {
                RuntimeInfo ei = RuntimeInfo.FromFormValue(Request.Form[TypeContext.Compose(prefix, EntityBaseKeys.RuntimeInfo)]);
                newLink = Navigator.ViewRoute(ei.RuntimeType, ident.TryCS(e => e.IdOrNull));
            }
            else
                newLink = Navigator.ViewRoute(context.UntypedValue.GetType(), ident.TryCS(e => e.IdOrNull));
            
            return Content("{{\"ModelState\":{0}, \"{1}\":\"{2}\", \"{3}\":\"{4}\"}}".Formato(
                this.ModelState.ToJsonData(),
                TypeContext.Separator + EntityBaseKeys.ToStr,
                context.UntypedValue.TryToString(),
                TypeContext.Separator + EntityBaseKeys.ToStrLink,
                newLink
                ));
        }

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ActionResult SavePartial(string prefix, string prefixToIgnore)
        //{
        //    Modifiable entity = this.ExtractEntity(prefix);

        //    ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref entity, prefix, prefixToIgnore);

        //    if (changesLog.Errors != null && changesLog.Errors.Count > 0)
        //        throw new ApplicationException(Resources.ItsNotPossibleToSaveAnEntityOfType0WithErrors.Formato(entity.GetType().Name));

        //    if (entity is IdentifiableEntity)
        //        Database.Save((IdentifiableEntity)entity);

        //    if (prefix.HasText())
        //        return Navigator.PopupView(this, entity, prefix);
        //    else //NormalWindow
        //        return Navigator.View(this, entity);
        //}

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult Autocomplete(string typeName, Implementations implementations, string input, int limit)
        {
            Type type = Navigator.NamesToTypes[typeName];

            var result = AutoCompleteUtils.FindLiteLike(type, implementations, input, limit)
                .ToDictionary(l => l.Id.ToString() + "_" + l.RuntimeType.Name, l => l.ToStr);

            return Content(result.ToJSonObject(idAndType => idAndType.Quote(), str => str.Quote()));
        }

        public ActionResult Find(FindOptions findOptions)
        {
            return Navigator.Find(this, findOptions);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult PartialFind(FindOptions findOptions, string prefix, string sfSuffix)
        {
            return Navigator.PartialFind(this, findOptions, prefix, sfSuffix);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult Search(FindOptions findOptions, int? sfTop, string prefix, string sfSuffix)
        {
            return Navigator.Search(this, findOptions, sfTop, prefix, sfSuffix);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult AddFilter(string filterType, string columnName, string displayName, int index, string prefix, string suffix)
        {
            return Content(SearchControlHelper.NewFilter(this, filterType, columnName, displayName, index, prefix, suffix, null, null));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult QuickFilter(string sfQueryUrlName, bool isLite, string typeUrlName, int? sfId, string sfValue, int sfColIndex, int index, string prefix, string suffix)
        {
            object value = (isLite) ? (object)Lite.Create(Navigator.ResolveTypeFromUrlName(typeUrlName), sfId.Value) : (object)sfValue;
            return Content(SearchControlHelper.QuickFilter(this, sfQueryUrlName, sfColIndex, index, value, prefix, suffix));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult GetTypeChooser(Implementations sfImplementations, string prefix)
        {
            if (sfImplementations == null || sfImplementations.IsByAll)
                throw new InvalidOperationException("GetTypeChooser needs an ImplementedBy");

            string strButtons = ((ImplementedByAttribute)sfImplementations).ImplementedTypes
                .ToString(type => "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(type.Name, type.NiceName()), "");

            ViewData[ViewDataKeys.CustomHtml] = strButtons;
            ViewData[ViewDataKeys.PopupPrefix] = prefix;
            return PartialView(Navigator.Manager.ChooserPopupUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult GetChooser(List<string> buttons, string prefix)
        {
            if (buttons == null || buttons.Count==0)
                throw new InvalidOperationException(Resources.GetChooserNeedsAListOfOptions);

            string strButtons = buttons
                .ToString(b => "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(b.Replace(" ", ""), b), "");

            ViewData[ViewDataKeys.CustomHtml] = strButtons;
            ViewData[ViewDataKeys.PopupPrefix] = prefix;
            return PartialView(Navigator.Manager.ChooserPopupUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult ReloadEntity(string prefix)
        {
            MappingContext context = this.UntypedExtractEntity(prefix)
                .ThrowIfNullC("Type was not possible to extract")
                .UntypedApplyChanges(this.ControllerContext, prefix, true);

            ModifiableEntity entity = (ModifiableEntity)context.UntypedValue;

            if (this.IsReactive())
            {
                Session[this.TabID()] = entity;
                this.ViewData[ViewDataKeys.Reactive] = true;
                if (prefix.HasText())
                    entity = (ModifiableEntity)MappingContext.GetPropertyValue(entity, prefix);
            }

            this.ViewData[ViewDataKeys.LoadAll] = true; //Prevents losing unsaved changes of the UI when reloading control
            return Navigator.PartialView(this, entity, prefix, context.GetTicksDictionary());
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult GetButtonBar(string sfRuntimeType, int? sfId, string prefix)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            IdentifiableEntity entity = null;

            if (Request.Form.AllKeys.Contains(ViewDataKeys.TabId))
            {
                entity = (IdentifiableEntity)Session[Request.Form[ViewDataKeys.TabId]];
                entity = (IdentifiableEntity)MappingContext.GetPropertyValue(entity, prefix);
            }
            else
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve(Lite.Create(type, sfId.Value));
                else
                    entity = (IdentifiableEntity)Constructor.Construct(type);
            }

            HtmlHelper helper = CreateHtmlHelper(this);
            return Content(ButtonBarEntityHelper.GetForEntity(this.ControllerContext, entity, Navigator.Manager.EntitySettings[type].OnPartialViewName(entity)).ToString(helper, prefix));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DoPostBack()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);
            
            return Navigator.View(this, context.UntypedValue, context.GetTicksDictionary());
        }

        public static HtmlHelper CreateHtmlHelper(Controller c)
        {
            return new HtmlHelper(
                        new ViewContext(
                            c.ControllerContext,
                            new WebFormView(c.ControllerContext.RequestContext.HttpContext.Request.FilePath),
                            c.ViewData,
                            c.TempData),
                        new ViewPage());
        }
    }
}
