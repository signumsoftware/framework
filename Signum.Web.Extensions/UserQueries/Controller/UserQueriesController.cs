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
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities.Authorization;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;
using Signum.Web.Operations;
using Signum.Entities.UserAssets;

namespace Signum.Web.UserQueries
{
    public class UserQueriesController : Controller
    {
        public ActionResult View(Lite<UserQueryEntity> lite, FindOptions findOptions, Lite<Entity> currentEntity)
        {
            UserQueryPermission.ViewUserQuery.AssertAuthorized();

            UserQueryEntity uq =  UserQueryLogic.RetrieveUserQuery(lite);

            using (uq.EntityType == null ? null : CurrentEntityConverter.SetCurrentEntity(currentEntity.Retrieve()))
            {
                if (findOptions == null)
                    findOptions = uq.ToFindOptions();
                else
                    findOptions.ApplyUserQuery(uq);
            }

            return Finder.SearchPage(this, findOptions);
        }

        public ActionResult Create(QueryRequest request)
        {
            if (!Finder.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().FormatWith(request.QueryName));

            var userQuery = ToUserQuery(request);

            userQuery.Owner = UserQueryUtils.DefaultOwner();

            return Navigator.NormalPage(this, userQuery);
        }

        public static UserQueryEntity ToUserQuery(QueryRequest request)
        {
            return request.ToUserQuery(
                DynamicQueryManager.Current.QueryDescription(request.QueryName),
                QueryLogic.GetQueryEntity(request.QueryName),
                FindOptions.DefaultPagination,
                withoutFilters: false /*Implement Simple Filter Builder*/);
        }

        [HttpPost]
        public JsonResult GetUserQueryImplementations()
        {
            var userQuery = Lite.Parse<UserQueryEntity>(Request["userQuery"]);

            var entityType = userQuery.InDB(a=>a.EntityType.Entity)?.ToType();

            return new JsonResult { Data = entityType == null ? 
                new JsExtensions.JsTypeInfo[0] : 
                Implementations.By(entityType).ToJsTypeInfos(isSearch: false, prefix: "") };
        }
    }
}
