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

namespace Signum.Engine.Reports
{
    public static class UserQueryLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                LiteFilterValueConverter.TryParseLite = TypeLogic.TryParseLite;

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
                                            }).ToDynamic().Column(a => a.Query, c => c.Visible = false); 

                sb.Schema.EntityEvents<UserQueryDN>().Retrieved += new EntityEventHandler<UserQueryDN>(UserQueryLogic_Retrieved);
            }
        }

        static void UserQueryLogic_Retrieved(UserQueryDN userQuery, bool isRoot)
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
                    where er.Query.Key == QueryUtils.GetQueryName(queryName)
                    select er.ToLite()).ToList(); 
        }

        public static void RemoveUserQuery(Lite<UserQueryDN> lite)
        {
            Database.Delete(lite);
        }
    }
}
