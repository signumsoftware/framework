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
        public PartialViewResult PartialView(string sfStaticType, int? sfId, string prefix)
        {
            Type type = Navigator.ResolveType(sfStaticType);

            ModifiableEntity entity = null;
            if (sfId.HasValue)
                entity = Database.Retrieve(type, sfId.Value);
            else
                entity = Navigator.CreateInstance(type);

            return Navigator.PartialView(this, entity, prefix);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TrySave(string prefixToIgnore)
        {   
            Modifiable entity = Navigator.ExtractEntity(Request.Form);

            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(Request.Form, prefixToIgnore, ref entity);

            this.ModelState.FromDictionary(errors, Request.Form);

            return Content(this.ModelState.ToJsonData());
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult TrySavePartial(string prefix, string prefixToIgnore, string sfStaticType, int? sfId)
        {
            Type type = Navigator.ResolveType(sfStaticType);

            Modifiable entity = null;
            if (sfId.HasValue)
                entity = Database.Retrieve(type, sfId.Value);
            else
                entity = Navigator.CreateInstance(type);

            var sortedList = Navigator.ToSortedList(Request.Form, prefixToIgnore);
            
            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(sortedList, ref entity, prefix);

            this.ModelState.FromDictionary(errors, Request.Form);

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
            var result = DynamicQueryUtils.FindLazyLike(type, implementationTypes, input, limit)
                .ToDictionary(l => l.Id.ToString() + "_" + l.RuntimeType.Name, l => l.ToStr);

            return Content(result.ToJSonObject(idAndType => idAndType.Quote(), str => str.Quote()));
        }

        [AcceptVerbs(HttpVerbs.Get)]
        public ActionResult Find(string queryFriendlyName)
        {
            object queryName = Navigator.ResolveQueryFromUrlName(queryFriendlyName);

            return Navigator.Find(this, queryName);
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public PartialViewResult Search(string sfQueryNameToStr, string sfFilters, int? sfResultsLimit)
        {
            object queryName = Navigator.ResolveQueryFromToStr(sfQueryNameToStr);

            List<Filter> filters = Navigator.ExtractFilters(Request.Form, queryName);

            return Navigator.Search(this, queryName, filters, sfResultsLimit);
        }
        
        [AcceptVerbs(HttpVerbs.Post)]
        public ContentResult AddFilter(string filterType, string columnName, string displayName, int index)
        {
            return Content(SearchControlHelper.NewFilter(this, filterType, columnName, displayName, index));
        }

        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult DoPostBack(string prefixToIgnore)
        {
            Modifiable entity = Navigator.ExtractEntity(Request.Form);

            Dictionary<string, List<string>> errors = Navigator.ApplyChangesAndValidate(Request.Form, prefixToIgnore, ref entity);

            this.ModelState.FromDictionary(errors, Request.Form);
            
            return Navigator.View(this, entity);
        }

        
    }
}
