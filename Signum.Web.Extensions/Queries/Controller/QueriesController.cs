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
#endregion

namespace Signum.Web.Queries
{
    [HandleException, AuthenticationRequired]
    public class QueriesController : Controller
    {
        public ActionResult ViewUserQuery(int id)
        {
            UserQueryDN uq = Database.Retrieve<UserQueryDN>(id);

            FindOptions fo = uq.ToFindOptions();
           
            return Navigator.Find(this, fo);
        }

        public ActionResult CreateUserQuery(FindOptions findOptions)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            var userQuery = findOptions.ToUserQuery(UserDN.Current.ToLite<IdentifiableEntity>());

            ViewData[ViewDataKeys.QueryName] = findOptions.QueryName;

            return Navigator.View(this, userQuery);
        }

        public ActionResult EditUserQuery(int id)
        {
            UserQueryDN uq = Database.Retrieve<UserQueryDN>(id);

            ViewData[ViewDataKeys.QueryName] = Navigator.Manager.QuerySettings.First(kvp => QueryUtils.GetQueryName(kvp.Key) == uq.Query.Key).Key;

            return Navigator.View(this, uq);
        }

        public ActionResult DeleteUserQuery(int id)
        {
            UserQueryDN uq = Database.Retrieve<UserQueryDN>(id);

            Database.Delete<UserQueryDN>(id);

            return Redirect(Common.FullyQualifiedApplicationPath + Navigator.FindRoute(Navigator.ResolveQueryFromKey(uq.Query.Key)));
        }

        public ActionResult SaveUserQuery()
        {
            var context = this.ExtractEntity<UserQueryDN>().ApplyChanges(this.ControllerContext, null, true).ValidateGlobal();

            var userQuery = context.Value.Save();

            ViewData[ViewDataKeys.QueryName] = Navigator.Manager.QuerySettings.First(kvp => QueryUtils.GetQueryName(kvp.Key) == userQuery.Query.Key).Key;

            return Navigator.View(this, userQuery);
        }
    }
}
