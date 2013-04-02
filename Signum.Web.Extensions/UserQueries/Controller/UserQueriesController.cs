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
using System.IO;
using Signum.Entities.Reports;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities.Authorization;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Engine.Operations;
#endregion

namespace Signum.Web.UserQueries
{
    public class UserQueriesController : Controller
    {
        public ActionResult View(Lite<UserQueryDN> lite)
        {
            UserQueryDN uq = Database.Retrieve<UserQueryDN>(lite);

            FindOptions fo = uq.ToFindOptions();
           
            return Navigator.Find(this, fo);
        }

        public ActionResult Create(QueryRequest request)
        {
            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(request.QueryName));

            var userQuery = ToUserQuery(request);

            userQuery.Related = UserDN.Current.ToLite();

            return Navigator.NormalPage(this, userQuery);
        }

        public static UserQueryDN ToUserQuery(QueryRequest request)
        {
            return request.ToUserQuery(
                DynamicQueryManager.Current.QueryDescription(request.QueryName),
                QueryLogic.RetrieveOrGenerateQuery(request.QueryName),
                FindOptions.DefaultElementsPerPage,
                preserveFilters: false /*Implement Simple Filter Builder*/);
        }

        [HttpPost]
        public ActionResult Save()
        {
            UserQueryDN userQuery = null;
            
            try
            {
                userQuery = this.ExtractEntity<UserQueryDN>();
            }
            catch(Exception){}

            var context = userQuery.ApplyChanges(this.ControllerContext, null, true).ValidateGlobal();

            if (context.GlobalErrors.Any())
            {
                ModelState.FromContext(context);
                return JsonAction.ModelState(ModelState);
            }

            userQuery = context.Value.Execute(UserQueryOperation.Save);
            return JsonAction.Redirect(Navigator.NavigateRoute(userQuery.ToLite()));
        }

        [HttpPost]
        public ActionResult Delete(string prefix)
        {
            var userQuery = this.ExtractLite<UserQueryDN>(prefix);
            
            var queryName = QueryLogic.ToQueryName(userQuery.InDB().Select(uq => uq.Query.Key).SingleEx());

            userQuery.Delete();

            return JsonAction.Redirect(Navigator.FindRoute(queryName));
        }
    }
}
