using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Entities.Reports;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;

namespace Signum.Engine.Reports
{
    public static class UserQueryLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                sb.Include<UserQueryDN>();

                dqm[typeof(UserQueryDN)] = (from uq in Database.Query<UserQueryDN>()
                                            select new
                                            {
                                                Entity = uq.ToLite(),
                                                Query = uq.Query.ToLite(),
                                                uq.Id,
                                                uq.DisplayName,
                                                Filters = uq.Filters.Count,
                                                Columns = uq.Columns.Count,
                                                Orders = uq.Orders.Count,
                                            }).ToDynamic(); 

                sb.Schema.EntityEvents<UserQueryDN>().Retrieved += new RetrievedEventHandler<UserQueryDN>(UserQueryLogic_Retrieved);
            }
        }

        static void UserQueryLogic_Retrieved(UserQueryDN userQuery)
        {
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            foreach (var f in userQuery.Filters)
                f.PostRetrieving(description);

            foreach (var c in userQuery.Columns)
                c.PostRetrieving(description);

            foreach (var o in userQuery.Orders)
                o.PostRetrieving(description);
        }

        public static List<Lite<UserQueryDN>> GetUserQueries(object queryName)
        {
            return (from er in Database.Query<UserQueryDN>()
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(queryName)
                    select er.ToLite()).ToList(); 
        }

        public static void RemoveUserQuery(Lite<UserQueryDN> lite)
        {
            Database.Delete(lite);
        }

        public static void RegisterUserEntityGroup(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserQueryDN uq) => uq.Related, typeof(UserDN));

            EntityGroupLogic.Register<UserQueryDN>(newEntityGroupKey, 
                uq => uq.Related.RefersTo(UserDN.Current), 
                uq => uq.Related != null && uq.Related.Entity is UserDN); 
        }

        public static void RegisterRoleEntityGroup(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserQueryDN uq) => uq.Related, typeof(RoleDN));

            EntityGroupLogic.Register<UserQueryDN>(newEntityGroupKey, 
                uq => AuthLogic.CurrentRoles().Contains(uq.Related.ToLite<RoleDN>()),
                uq => uq.Related != null && uq.Related.Entity is RoleDN);
        }
    }
}
