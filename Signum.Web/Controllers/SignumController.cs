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
                return Navigator.View(this, Database.Retrieve(t, id.Value));

            IIdentifiable entity = null;
            object result = Navigator.CreateInstance(this, t);
            if (typeof(IIdentifiable).IsAssignableFrom(result.GetType()))
                entity = (IIdentifiable)result;
            else
                throw new ApplicationException("Invalid result type for a Direct Constructor");

            return Navigator.View(this, entity);
        }

        public ActionResult Create(string sfRuntimeType, string sfOnOk, string sfOnCancel, string prefix)
        { 
            Type type = Navigator.ResolveType(sfRuntimeType);

            IIdentifiable entity = null;
            object result = Navigator.CreateInstance(this, type);
            if (result.GetType() == typeof(PartialViewResult))
                return (PartialViewResult)result;
            else if (typeof(IIdentifiable).IsAssignableFrom(result.GetType()))
                entity = (IIdentifiable)result;
            else
                throw new ApplicationException("Invalid result type for a Constructor");

            return Content(Navigator.ViewRoute(type, null));
        }

        public PartialViewResult PopupView(string sfRuntimeType, int? sfId, string sfOnOk, string sfOnCancel, string prefix, string sfUrl)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            bool isReactive = Navigator.ExtractIsReactive(Request.Form);
            
            ModifiableEntity entity = null;
            if (isReactive)
            {
                //NameValueCollection nvc = new NameValueCollection();
                entity = Navigator.ExtractEntity(this, Request.Form)
                    .ThrowIfNullC("PartialView: Type was not possible to extract");
                entity = (ModifiableEntity)Modification.GetPropertyValue(entity, prefix);
            }
            if (entity == null || entity.GetType() != type || sfId != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve(type, sfId.Value);
                else
                {
                    object result = Navigator.CreateInstance(this, type);
                    if (result.GetType() == typeof(PartialViewResult))
                        return (PartialViewResult)result;
                    else if (typeof(ModifiableEntity).IsAssignableFrom(result.GetType()))
                        entity = (ModifiableEntity)result;
                    else
                        throw new ApplicationException("Invalid result type for a Constructor");
                }
            }

            if (isReactive)
                this.ViewData[ViewDataKeys.Reactive] = true;

            if (typeof(EmbeddedEntity).IsAssignableFrom(entity.GetType()))
            {
                this.ViewData[ViewDataKeys.WriteSFInfo] = true;

                Type ts = typeof(TypeSubContext<>).MakeGenericType(new Type[] { entity.GetType() });
                TypeContext tc = (TypeContext)Activator.CreateInstance(ts, new object[] { entity, Modification.GetTCforEmbeddedEntity(this, Request.Form, entity, ref prefix), new PropertyInfo[] { } });

                return Navigator.PopupView(this, tc, "", sfUrl); //No prefix as its info is in the TypeContext
            }

            return Navigator.PopupView(this, entity, prefix, sfUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult PartialView(string sfRuntimeType, int? sfId, string prefix, bool? sfEmbeddedControl, string sfUrl)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            bool isReactive = Navigator.ExtractIsReactive(Request.Form);

            ModifiableEntity entity = null;
            if (isReactive)
            {
                entity = Navigator.ExtractEntity(this, Request.Form, "")
                    .ThrowIfNullC("PartialView: Type was not possible to extract");
                entity = (ModifiableEntity)Modification.GetPropertyValue(entity, prefix);
            }
            if (entity == null || entity.GetType() != type || sfId != (entity as IIdentifiable).TryCS(e => e.IdOrNull))
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve(type, sfId.Value);
                else
                {
                    object result = Navigator.CreateInstance(this, type);
                    if (result.GetType() == typeof(PartialViewResult))
                        return (PartialViewResult)result;
                    else if (typeof(ModifiableEntity).IsAssignableFrom(result.GetType()))
                        entity = (ModifiableEntity)result;
                    else
                        throw new ApplicationException("Invalid result type for a Constructor");
                }
            }

            if (isReactive)
                this.ViewData[ViewDataKeys.Reactive] = true;

            if (typeof(EmbeddedEntity).IsAssignableFrom(entity.GetType()))
            {
                this.ViewData[ViewDataKeys.WriteSFInfo] = true;

                //Type ts = typeof(TypeSubContext<>).MakeGenericType(new Type[] { entity.GetType() });
                //TypeContext tc = (TypeContext)Activator.CreateInstance(ts, new object[] { entity, Modification.GetTCforEmbeddedEntity(Request.Form, entity, ref prefix), new PropertyInfo[]{} });
                string prefixClon = prefix;
                TypeContext tc = Modification.GetTCforEmbeddedEntity(this, Request.Form, entity, ref prefixClon);
                return Navigator.PartialView(this, tc, "", sfUrl); //No prefix as its info is in the TypeContext
            }

            return Navigator.PartialView(this, entity, prefix, sfUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult ReloadEntity(string prefix)
        {
            ModifiableEntity entity = Navigator.ExtractEntity(this, Request.Form, prefix)
                .ThrowIfNullC("Type was not possible to extract");

            Modification modification = Navigator.GenerateModification(this, entity, prefix);
            ModificationState modState = Navigator.ApplyChanges(this, modification, ref entity);
            
            if (Navigator.ExtractIsReactive(Request.Form))
            {
                string tabID = Navigator.ExtractTabID(Request.Form);
                Session[tabID] = entity;
                this.ViewData[ViewDataKeys.Reactive] = true;
                if (prefix.HasText())
                    entity = (ModifiableEntity)Modification.GetPropertyValue(entity, prefix);
            }
            
            this.ViewData[ViewDataKeys.LoadAll] = true; //Prevents losing unsaved changes of the UI when reloading control
            return Navigator.PartialView(this, entity, prefix, ModificationState.ToDictionary(modState.Actions));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult TrySave(string prefixToIgnore)
        {   
            Modifiable entity = Navigator.ExtractEntity(this, Request.Form);

            ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref entity, "", prefixToIgnore);

            if (changesLog.Errors != null && changesLog.Errors.Count > 0)
            {
                this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
                return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
            }

            if (entity is IdentifiableEntity)
                Database.Save((IdentifiableEntity)entity);

            if (Navigator.ExtractIsReactive(Request.Form))
            {
                string tabID = Navigator.ExtractTabID(Request.Form);
                Session[tabID] = entity;
            }

            return Navigator.View(this, entity); //changesLog.ChangeTicks);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult Validate(string prefixToIgnore)
        {
            Modifiable entity = Navigator.ExtractEntity(this, Request.Form);

            ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref entity, "", prefixToIgnore);

            this.ModelState.FromDictionary(changesLog.Errors, Request.Form);

            return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + "}");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TrySavePartial(string prefix, string prefixToIgnore)
        {
            Modifiable parentEntity = Navigator.ExtractEntity(this, Request.Form, prefix);
            
            ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref parentEntity, prefix, prefixToIgnore);

            this.ModelState.FromDictionary(changesLog.Errors, Request.Form);

            if (parentEntity is IdentifiableEntity && (changesLog.Errors == null || changesLog.Errors.Count == 0))
                Database.Save((IdentifiableEntity)parentEntity);

            Modifiable entity = Navigator.ExtractIsReactive(Request.Form) ? Modification.GetPropertyValue(parentEntity, prefix) : parentEntity;

            string newLink = Navigator.ViewRoute(entity.GetType(), (entity as IIdentifiable).TryCS(e => e.IdOrNull));

            return Content("{{\"ModelState\":{0}, \"{1}\":\"{2}\", \"{3}\":\"{4}\"}}".Formato(
                this.ModelState.ToJsonData(),
                TypeContext.Separator + EntityBaseKeys.ToStr,
                entity.ToString(),
                TypeContext.Separator + EntityBaseKeys.ToStrLink,
                newLink
                ));
            //return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + ",\"" + TypeContext.Separator + EntityBaseKeys.ToStr + "\":" + newToStr.Quote() + "}");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult ValidatePartial(string prefix, string prefixToIgnore)
        {
            Modifiable parentEntity = Navigator.ExtractEntity(this, Request.Form, prefix);

            ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref parentEntity, prefix, prefixToIgnore);

            /*if (Request.Form.AllKeys.Contains(ViewDataKeys.Reactive)) //Apply changes and update entity in session
            {
                //SetPropertyValue(parentEntity, prefix, entity);
                Session[ViewDataKeys.TabEntity] = parentEntity;
            }*/

            this.ModelState.FromDictionary(changesLog.Errors, Request.Form);

            Modifiable entity = Navigator.ExtractIsReactive(Request.Form) ? Modification.GetPropertyValue(parentEntity, prefix) : parentEntity;

            string newLink = "";
            if (entity == null)
            {
                EntityInfo ei = EntityInfo.FromFormValue(Request.Form[TypeContext.Compose(prefix, EntityBaseKeys.Info)]);
                newLink = Navigator.ViewRoute(ei.StaticType, (entity as IIdentifiable).TryCS(e => e.IdOrNull));
            }
            else
                newLink = Navigator.ViewRoute(entity.GetType(), (entity as IIdentifiable).TryCS(e => e.IdOrNull));
            
            return Content("{{\"ModelState\":{0}, \"{1}\":\"{2}\", \"{3}\":\"{4}\"}}".Formato(
                this.ModelState.ToJsonData(),
                TypeContext.Separator + EntityBaseKeys.ToStr,
                entity.TryToString(""),
                TypeContext.Separator + EntityBaseKeys.ToStrLink,
                newLink
                ));
            //return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + ",\"" + TypeContext.Separator + EntityBaseKeys.ToStr + "\":" + newToStr.Quote() + "}");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult SavePartial(string prefix, string prefixToIgnore)
        {
            Modifiable entity = Navigator.ExtractEntity(this, Request.Form, prefix);

            ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref entity, prefix, prefixToIgnore);

            if (changesLog.Errors != null && changesLog.Errors.Count > 0)
                throw new ApplicationException(Resources.ItsNotPossibleToSaveAnEntityOfType0WithErrors.Formato(entity.GetType().Name));

            if (entity is IdentifiableEntity)
                Database.Save((IdentifiableEntity)entity);

            if (prefix.HasText())
                return Navigator.PopupView(this, entity, prefix);
            else //NormalWindow
                return Navigator.View(this, entity);
        }

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ContentResult TrySavePartialStrict(string prefix, string prefixToIgnore)
        //{
        //    Modifiable parentEntity = Navigator.ExtractEntity(this, Request.Form, prefix);

        //    ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref parentEntity, prefix, prefixToIgnore);

        //    this.ModelState.FromDictionary(changesLog.Errors, Request.Form);

        //    if (entity is IdentifiableEntity && (changesLog.Errors == null || changesLog.Errors.Count == 0) && save.HasValue && save.Value)
        //        Database.Save((IdentifiableEntity)entity);

        //    string newLink = Navigator.ViewRoute(entity.GetType(), (entity as IIdentifiable).TryCS(e => e.IdOrNull));

        //    return Content("{{\"ModelState\":{0}, \"{1}\":\"{2}\", \"{3}\":\"{4}\"}}".Formato(
        //        this.ModelState.ToJsonData(),
        //        TypeContext.Separator + EntityBaseKeys.ToStr,
        //        entity.ToString(),
        //        TypeContext.Separator + EntityBaseKeys.ToStrLink,
        //        newLink
        //        ));
        //    //return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + ",\"" + TypeContext.Separator + EntityBaseKeys.ToStr + "\":" + entity.ToString().Quote() + "}");
        //}

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult Autocomplete(string typeName, string implementations, string input, int limit)
        {
            Type type = Navigator.NameToType[typeName];

            Type[] implementationTypes = null;
            if (!string.IsNullOrEmpty(implementations))
            {
                string[] implementationsArray = implementations.Split(',');
                implementationTypes = new Type[implementationsArray.Length];
                for (int i=0; i<implementationsArray.Length;i++)
                {
                    Type t = Navigator.NameToType.TryGetC(implementationsArray[i]) ?? null;
                    if (t != null)
                        implementationTypes[i] = t;
                }
            }
            var result = AutoCompleteUtils.FindLiteLike(type, implementationTypes, input, limit)
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
        public PartialViewResult Search(FindOptions findOptions, int? sfTop, string prefix)
        {
            return Navigator.Search(this, findOptions, sfTop, prefix);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult AddFilter(string filterType, string columnName, string displayName, int index, string prefix)
        {
            return Content(SearchControlHelper.NewFilter(this, filterType, columnName, displayName, index, prefix, null, null));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult QuickFilter(string sfQueryUrlName, bool isLite, string typeUrlName, int? sfId, string sfValue, int sfColIndex, int index, string prefix)
        {
            object value = (isLite) ? (object)Lite.Create(Navigator.ResolveTypeFromUrlName(typeUrlName), sfId.Value) : (object)sfValue;
            return Content(SearchControlHelper.QuickFilter(this, sfQueryUrlName, sfColIndex, index, value, prefix));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult GetChooser(string sfImplementations, string prefix)
        {
            string strButtons = "";
            foreach (string typeUrlName in sfImplementations.Split(new[] { ";" }, StringSplitOptions.RemoveEmptyEntries))
            {
                Type type = Navigator.Manager.URLNamesToTypes[typeUrlName];
                strButtons += "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(type.Name, type.NiceName());
            }
            ViewData[ViewDataKeys.CustomHtml] = strButtons;
            ViewData[ViewDataKeys.PopupPrefix] = prefix;
            return PartialView(Navigator.Manager.ChooserPopupUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DoPostBack(string prefixToIgnore)
        {
            Modifiable entity = Navigator.ExtractEntity(this, Request.Form);

            ChangesLog changesLog = Navigator.ApplyChangesAndValidate(this, ref entity, "", prefixToIgnore);

            this.ModelState.FromDictionary(changesLog.Errors, Request.Form);
            
            return Navigator.View(this, entity, changesLog.ChangeTicks);
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
