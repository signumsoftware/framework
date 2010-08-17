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
using Signum.Web.Queries.Models;
using Signum.Engine.Basics;
#endregion

namespace Signum.Web.Queries
{
    [HandleException, AuthenticationRequired]
    public class QueriesController : Controller
    {
        public ActionResult ViewUserQuery(int id)
        {
            UserQueryDN uq = Database.Retrieve<UserQueryDN>(id);

            object queryName = Navigator.Manager.QuerySettings.Keys.First(k => QueryUtils.GetQueryName(k) == uq.Query.Key);
                // Navigator.ResolveQueryFromToStr(uq.Query.Key);

            FindOptions fo = new FindOptions(queryName)
            { 
                FilterOptions = uq.Filters.Select(qf => new FilterOption { ColumnName = qf.TokenString, Token = qf.Token, Operation = qf.Operation, Value = qf.Value }).ToList(),
                UserColumnOptions = uq.Columns.Select((qc, index) => new UserColumnOption { DisplayName = qc.DisplayName, UserColumn = new UserColumn(index, qc.Token) }).ToList(),
                OrderOptions = uq.Orders.Select(qo => new OrderOption { Token = qo.Token, ColumnName = qo.TokenString, Type = qo.OrderType }).ToList(),
                Top = uq.MaxItems
            };

            return Navigator.Find(this, fo);
        }

        public ActionResult CreateUserQuery(FindOptions findOptions)
        {
            if (!Navigator.IsFindable(findOptions.QueryName))
                throw new UnauthorizedAccessException(Resources.ViewForType0IsNotAllowed.Formato(findOptions.QueryName));

            var userQuery = new UserQueryDN
            {
                Query = QueryLogic.RetrieveOrGenerateQuery(findOptions.QueryName),
                Filters = findOptions.FilterOptions.Select(fo => new QueryFilterDN { Token = fo.Token, Operation = fo.Operation, Value = fo.Value, ValueString = FilterValueConverter.ToString(fo.Value, fo.Token.Type) }).ToMList(),
                Columns = findOptions.UserColumnOptions.Select(uco => new QueryColumnDN { Token = uco.UserColumn.Token, DisplayName = uco.DisplayName }).ToMList(),
                Orders = findOptions.OrderOptions.Select((oo, index) => new QueryOrderDN { Token = oo.Token, OrderType = oo.Type, Index = index }).ToMList(),
                MaxItems = findOptions.Top
            };

            return Navigator.View(this, new UserQueryModel(userQuery, findOptions.QueryName));
        }

        public ActionResult EditUserQuery(int id)
        {
            UserQueryDN uq = Database.Retrieve<UserQueryDN>(id);

            object queryName = Navigator.Manager.QuerySettings.First(kvp => QueryUtils.GetQueryName(kvp.Key) == uq.Query.Key).Key;

            return Navigator.View(this, new UserQueryModel(uq, queryName));
        }

        public ActionResult DeleteUserQuery(int id)
        {
            UserQueryDN uq = Database.Retrieve<UserQueryDN>(id);

            Database.Delete<UserQueryDN>(id);

            return Redirect(HttpContextUtils.FullyQualifiedApplicationPath + Navigator.FindRoute(Navigator.ResolveQueryFromToStr(uq.Query.Key)));
        }

        public ActionResult SaveUserQuery()
        {
            var context = this.ExtractEntity<UserQueryModel>().ApplyChanges(this.ControllerContext, null, true).ValidateGlobal();

            var userQuery = context.Value.ToUserQueryDN().Save();

            object queryName = Navigator.Manager.QuerySettings.First(kvp => QueryUtils.GetQueryName(kvp.Key) == userQuery.Query.Key).Key;

            return Navigator.View(this, new UserQueryModel(userQuery, queryName));
        }

        public ActionResult NewQueryFilter(string prefix, string queryKey)
        {
            var query = Database.Query<QueryDN>().FirstOrDefault(q => q.Key == queryKey) ?? new QueryDN { Key = queryKey };

            var filterModel = new QueryFilterModel(new QueryFilterDN(), Navigator.Manager.QuerySettings.First(kvp => QueryUtils.GetQueryName(kvp.Key) == queryKey).Key.ToString());

            return Navigator.PartialView(this, filterModel, prefix);
        }

        public ActionResult NewQueryColumn(string prefix, string queryKey)
        {
            var query = Database.Query<QueryDN>().FirstOrDefault(q => q.Key == queryKey) ?? new QueryDN { Key = queryKey };

            var columnModel = new QueryColumnModel(new QueryColumnDN(), Navigator.Manager.QuerySettings.First(kvp => QueryUtils.GetQueryName(kvp.Key) == queryKey).Key.ToString());

            return Navigator.PartialView(this, columnModel, prefix);
        }

        public ActionResult NewQueryOrder(string prefix, string queryKey)
        {
            var query = Database.Query<QueryDN>().FirstOrDefault(q => q.Key == queryKey) ?? new QueryDN { Key = queryKey };

            var orderModel = new QueryOrderModel(new QueryOrderDN(), Navigator.Manager.QuerySettings.First(kvp => QueryUtils.GetQueryName(kvp.Key) == queryKey).Key.ToString());

            return Navigator.PartialView(this, orderModel, prefix);
        }
    }
}
