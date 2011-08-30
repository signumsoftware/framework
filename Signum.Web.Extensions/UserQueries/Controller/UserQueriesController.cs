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
using System.IO;
using Signum.Entities.Reports;
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities.Authorization;
using Signum.Entities.UserQueries;
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

        public ActionResult Create(FindOptions findOptions)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            var userQuery = findOptions.ToUserQuery(UserDN.Current.ToLite<IdentifiableEntity>());

            return Navigator.View(this, userQuery);
        }

        public ActionResult Delete(Lite<UserQueryDN> lite)
        {
            var queryName = QueryLogic.ToQueryName(lite.InDB().Select(uq => uq.Query.Key).First());

            Database.Delete<UserQueryDN>(lite);

            return Redirect(Navigator.FindRoute(queryName));
        }

        public RedirectResult Save()
        {
            var context = this.ExtractEntity<UserQueryDN>().ApplyChanges(this.ControllerContext, null, true).ValidateGlobal();

            var userQuery = context.Value.Save();

            return Redirect(Navigator.ViewRoute(userQuery.ToLite()));
        }
    }
}
