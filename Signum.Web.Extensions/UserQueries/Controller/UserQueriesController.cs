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
using Signum.Entities.Basics;
using Signum.Engine.Basics;
using Signum.Entities.Authorization;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Engine.Operations;
using Signum.Engine.Authorization;
using Signum.Web.Operations;
#endregion

namespace Signum.Web.UserQueries
{
    public class UserQueriesController : Controller
    {
        public ActionResult View(Lite<UserQueryDN> lite, FindOptions findOptions, Lite<IdentifiableEntity> currentEntity)
        {   
            UserQueryPermission.ViewUserQuery.Authorize(); 

            UserQueryDN uq = Database.Retrieve<UserQueryDN>(lite);

            if (uq.EntityType != null)
                CurrentEntityConverter.SetFilterValues(uq.Filters, currentEntity.Retrieve());

            if (findOptions == null)
            {
                findOptions = uq.ToFindOptions();
                return Navigator.Find(this, findOptions);
            }
            else
            {
                findOptions.ApplyUserQuery(uq);
                return Navigator.Find(this, findOptions);
            }
        }

        public ActionResult Create(QueryRequest request)
        {
            if (!Navigator.IsFindable(request.QueryName))
                throw new UnauthorizedAccessException(NormalControlMessage.ViewForType0IsNotAllowed.NiceToString().Formato(request.QueryName));

            var userQuery = ToUserQuery(request);

            userQuery.Owner = UserDN.Current.ToLite();

            return Navigator.NormalPage(this, userQuery);
        }

        public static UserQueryDN ToUserQuery(QueryRequest request)
        {
            return request.ToUserQuery(
                DynamicQueryManager.Current.QueryDescription(request.QueryName),
                QueryLogic.GetQuery(request.QueryName),
                FindOptions.DefaultPagination,
                withoutFilters: false /*Implement Simple Filter Builder*/);
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

            var context = userQuery.ApplyChanges(this).ValidateGlobal();

            if (context.HasErrors())
                return context.ToJsonModelState();

            userQuery = context.Value.Execute(UserQueryOperation.Save);

            return this.DefaultExecuteResult(userQuery);
        }
    }
}
