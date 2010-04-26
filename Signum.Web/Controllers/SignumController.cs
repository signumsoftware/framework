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

            IdentifiableEntity entity = null;
            object result = Constructor.Construct(t);
            if (typeof(IdentifiableEntity).IsAssignableFrom(result.GetType()))
                entity = (IdentifiableEntity)result;
            else
                throw new InvalidOperationException(Resources.InvalidResultTypeForADirectConstructor);

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
                throw new InvalidOperationException(Resources.PopupCreateCannotBeCalledForEmbeddedType0.Formato(result.GetType()));

            if (!typeof(IdentifiableEntity).IsAssignableFrom(result.GetType()))
                throw new InvalidOperationException(Resources.InvalidResultTypeForAConstructor);

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
                IdentifiableEntity parent = (IdentifiableEntity)this.UntypedExtractEntity().ThrowIfNullC(Resources.PartialViewTypeWasNotPossibleToExtract);
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
                        throw new InvalidOperationException(Resources.InvalidResultTypeForAConstructor);
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
                    .ThrowIfNullC(Resources.PartialViewTypeWasNotPossibleToExtract);
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
                        throw new InvalidOperationException(Resources.InvalidResultTypeForAConstructor);
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
                throw new ArgumentNullException(Resources.IdentifiableEntityToSave);
    
            Database.Save(ident);

            ViewData[ViewDataKeys.ChangeTicks] = context.GetTicksDictionary();
            return Navigator.View(this, ident);
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
            MappingContext context = this.UntypedExtractEntity(prefix).UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

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

            return(Content(jSerializer.Serialize(
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
        public ContentResult AddFilter(string sfQueryUrlName, string columnName, int index, string prefix)
        {
            object queryName = Navigator.ResolveQueryFromUrlName(sfQueryUrlName);

            FilterOption fo = new FilterOption(columnName, null);
            if (fo.Token == null)
            {
                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                fo.Token = QueryToken.Parse(qd, fo.ColumnName);
            }
            fo.Operation = QueryUtils.GetFilterOperations(QueryUtils.GetFilterType(fo.Token.Type)).First();

            return Content(SearchControlHelper.NewFilter(CreateHtmlHelper(this), queryName, fo, new Context(null, prefix), index));
        }

        //[AcceptVerbs(HttpVerbs.Post)]
        //public ContentResult QuickFilter(string sfQueryUrlName, bool isLite, string typeUrlName, int? sfId, string sfValue, int sfColIndex, int index, string prefix, string suffix)
        //{
        //    object value = (isLite) ? (object)Lite.Create(Navigator.ResolveTypeFromUrlName(typeUrlName), sfId.Value) : (object)sfValue;
        //    return Content(SearchControlHelper.QuickFilter(this, sfQueryUrlName, sfColIndex, index, value, prefix, suffix));
        //}

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult GetTypeChooser(Implementations sfImplementations, string prefix)
        {
            if (sfImplementations == null || sfImplementations.IsByAll)
                throw new InvalidOperationException(Resources.GetTypeChooserNeedsAnImplementedBy);

            string strButtons = ((ImplementedByAttribute)sfImplementations).ImplementedTypes
                .ToString(type => "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(type.Name, type.NiceName()), "");

            ViewData.Model = new Context(null, prefix);
            ViewData[ViewDataKeys.CustomHtml] = strButtons;

            return PartialView(Navigator.Manager.ChooserPopupUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult GetChooser(List<string> buttons, string prefix)
        {
            if (buttons == null || buttons.Count==0)
                throw new InvalidOperationException(Resources.GetChooserNeedsAListOfOptions);

            string strButtons = buttons
                .ToString(b => "<input type='button' id='{0}' name='{0}' value='{1}' /><br />\n".Formato(b.Replace(" ", ""), b), "");

            ViewData.Model = new Context(null, prefix);
            ViewData[ViewDataKeys.CustomHtml] = strButtons;

            return PartialView(Navigator.Manager.ChooserPopupUrl);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult ReloadEntity(string prefix)
        {
            MappingContext context = this.UntypedExtractEntity(prefix)
                .ThrowIfNullC(Resources.TypeWasNotPossibleToExtract)
                .UntypedApplyChanges(this.ControllerContext, prefix, true);

            IdentifiableEntity entity = (IdentifiableEntity)context.UntypedValue;

            if (this.IsReactive())
            {
                Session[this.TabID()] = entity;
                this.ViewData[ViewDataKeys.Reactive] = true;
                if (prefix.HasText())
                    entity = (IdentifiableEntity)MappingContext.FindSubentity(entity, prefix);
            }

            this.ViewData[ViewDataKeys.ChangeTicks] = context.GetTicksDictionary();
            return Navigator.PartialView(this, entity, prefix);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult GetButtonBar(string sfRuntimeType, int? sfId, string prefix)
        {
            Type type = Navigator.ResolveType(sfRuntimeType);
            IdentifiableEntity entity = null;

            if (Request.Form.AllKeys.Contains(ViewDataKeys.TabId))
            {
                IdentifiableEntity parent = (IdentifiableEntity)Session[Request.Form[ViewDataKeys.TabId]];
                entity = (IdentifiableEntity)MappingContext.FindSubentity(parent, prefix);
            }
            else
            {
                if (sfId.HasValue)
                    entity = Database.Retrieve(Lite.Create(type, sfId.Value));
                else
                    entity = (IdentifiableEntity)Constructor.Construct(type);
            }

            HtmlHelper helper = CreateHtmlHelper(this);
            return Content(ButtonBarEntityHelper.GetForEntity(this.ControllerContext, entity, 
                Navigator.Manager.EntitySettings[type].OnPartialViewName(entity), prefix).ToString(helper));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DoPostBack()
        {
            MappingContext context = this.UntypedExtractEntity().UntypedApplyChanges(ControllerContext, null, true).UntypedValidateGlobal();

            this.ModelState.FromContext(context);

            ViewData[ViewDataKeys.ChangeTicks] = context.GetTicksDictionary();
            return Navigator.View(this, (IdentifiableEntity)context.UntypedValue);
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
