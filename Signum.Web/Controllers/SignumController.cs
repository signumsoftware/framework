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

namespace Signum.Web.Controllers
{
    [HandleError]
    public class SignumController : Controller
    {
        public ViewResult View(string typeFriendlyName, int id)
        {
            Type t = Navigator.ResolveTypeFromUrlName(typeFriendlyName);
            
            return Navigator.View(this, Database.Retrieve(t,id));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult PopupView(string sfStaticType, int? sfId, string sfOnOk, string sfOnCancel, string prefix)
        {
            Type type = Navigator.ResolveType(sfStaticType);
             
            ModifiableEntity entity = null;
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

            return Navigator.PopupView(this, entity, prefix);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult PartialView(string sfStaticType, int? sfId, string prefix, bool? sfEmbedControl)
        {
            Type type = Navigator.ResolveType(sfStaticType);
            
            ModifiableEntity entity = null;
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

            if (sfEmbedControl != null && sfEmbedControl.Value)
                this.ViewData[ViewDataKeys.EmbeddedControl] = true;

            return Navigator.PartialView(this, entity, prefix);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult ReloadEntity(string prefix)
        {
            ModifiableEntity entity = Navigator.ExtractEntity(this, Request.Form, prefix)
                .ThrowIfNullC("PartialView: Type was not possible to extract");

            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(this, prefix, "", ref entity);

            this.ViewData[ViewDataKeys.LoadAll] = true; //Prevents losing unsaved changes of the UI when reloading control
            return Navigator.PartialView(this, entity, prefix);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TrySave(string prefixToIgnore)
        {   
            Modifiable entity = Navigator.ExtractEntity(this, Request.Form);

            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(this, prefixToIgnore, ref entity);

            this.ModelState.FromDictionary(errors, Request.Form);

            if (entity is IdentifiableEntity && (errors == null || errors.Count == 0))
                Database.Save((IdentifiableEntity)entity);

            return Content(this.ModelState.ToJsonData());
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult Validate(string prefixToIgnore)
        {
            Modifiable entity = Navigator.ExtractEntity(this, Request.Form);

            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(this, prefixToIgnore, ref entity);

            this.ModelState.FromDictionary(errors, Request.Form);

            return Content(this.ModelState.ToJsonData());
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TrySavePartial(string prefix, string prefixToIgnore, string sfStaticType, int? sfId, bool? save)
        {
            Type type = Navigator.ResolveType(sfStaticType);

            Modifiable entity = null;
            if (sfId.HasValue)
                entity = Database.Retrieve(type, sfId.Value);
            else
                entity = (ModifiableEntity)Navigator.CreateInstance(this, type);

            var sortedList = Navigator.ToSortedList(Request.Form, prefix, prefixToIgnore);
            
            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(this, sortedList, ref entity, prefix);

            this.ModelState.FromDictionary(errors, Request.Form);

            if (entity is IdentifiableEntity && (errors == null || errors.Count == 0) && save.HasValue && save.Value)
                Database.Save((IdentifiableEntity)entity);

            return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + ",\"" + TypeContext.Separator + EntityBaseKeys.ToStr + "\":" + entity.ToString().Quote() + "}");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TrySavePartialStrict(string prefix, string prefixToIgnore, string sfStaticType, int? sfId, bool? save)
        {
            Type type = Navigator.ResolveType(sfStaticType);

            Modifiable entity = null;
            if (sfId.HasValue)
                entity = Database.Retrieve(type, sfId.Value);
            else
                entity = (ModifiableEntity)Navigator.CreateInstance(type);

            var sortedList = Navigator.ToSortedList(Request.Form, prefix, prefixToIgnore);

            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(this, sortedList, ref entity, prefix);

            this.ModelState.FromDictionary(errors, Request.Form);

            if (entity is IdentifiableEntity && (errors == null || errors.Count == 0) && save.HasValue && save.Value)
                Database.Save((IdentifiableEntity)entity);

            return Content("{\"ModelState\":" + this.ModelState.ToJsonData() + ",\"" + TypeContext.Separator + EntityBaseKeys.ToStr + "\":" + entity.ToString().Quote() + "}");
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult Autocomplete(string typeName, string implementations, string input, int limit)
        {
            Type type = Navigator.ResolveType(typeName);

            Type[] implementationTypes = null;
            if (!string.IsNullOrEmpty(implementations))
            {
                string[] implementationsArray = implementations.Split(',');
                implementationTypes = new Type[implementationsArray.Length];
                for (int i=0; i<implementationsArray.Length;i++)
                {
                    implementationTypes[i] = Navigator.ResolveType(implementationsArray[i]);
                }
            }
            var result = AutoCompleteUtils.FindLazyLike(type, implementationTypes, input, limit)
                .ToDictionary(l => l.Id.ToString() + "_" + l.RuntimeType.Name, l => l.ToStr);

            return Content(result.ToJSonObject(idAndType => idAndType.Quote(), str => str.Quote()));
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Find(string queryFriendlyName, bool? allowMultiple)
        {
            object queryName = Navigator.ResolveQueryFromUrlName(queryFriendlyName);

            FindOptions findOptions = new FindOptions(queryName);
            
            if (allowMultiple.HasValue)
                findOptions.AllowMultiple = allowMultiple.Value;

            return Navigator.Find(this, findOptions);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult PartialFind(string queryFriendlyName, bool? allowMultiple, string prefix, string prefixEnd)
        {
            object queryName = Navigator.ResolveQueryFromUrlName(queryFriendlyName);

            FindOptions findOptions = new FindOptions(queryName);

            findOptions.AllowMultiple = allowMultiple;

            return Navigator.PartialFind(this, findOptions, prefix, prefixEnd);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult Search(string sfQueryNameToStr, string sfFilters, int? sfTop, bool? sfAllowMultiple, string sfPrefix)
        {
            object queryName = Navigator.ResolveQueryFromToStr(sfQueryNameToStr);

            List<Filter> filters = Navigator.ExtractFilters(Request.Form, queryName, sfPrefix);

            return Navigator.Search(this, queryName, filters, sfTop, sfAllowMultiple, sfPrefix);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult AddFilter(string filterType, string columnName, string displayName, int index, string entityTypeName, string prefix)
        {
            return Content(SearchControlHelper.NewFilter(this, filterType, columnName, displayName, index, entityTypeName, prefix));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DoPostBack(string prefixToIgnore)
        {
            Modifiable entity = Navigator.ExtractEntity(this, Request.Form);

            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(this, prefixToIgnore, ref entity);

            this.ModelState.FromDictionary(errors, Request.Form);
            
            return Navigator.View(this, entity);
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
