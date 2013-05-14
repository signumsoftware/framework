using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using System.Reflection;
using Signum.Entities.UserQueries;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using Signum.Entities.Reports;
using Signum.Entities.Basics;
using Signum.Engine.Operations;
using Signum.Utilities;

namespace Signum.Engine.UserQueries
{
    public static class UserQueryLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryLogic.Start(sb);

                PermissionAuthLogic.RegisterPermissions(UserQueryPermission.ViewUserQuery); 

                sb.Include<UserQueryDN>();

                dqm.RegisterQuery(typeof(UserQueryDN), () =>
                    from uq in Database.Query<UserQueryDN>()
                    select new
                    {
                        Entity = uq,
                        uq.Query,
                        uq.Id,
                        uq.DisplayName,
                        uq.EntityType,
                        Filters = uq.Filters.Count,
                        Columns = uq.Columns.Count,
                        Orders = uq.Orders.Count,
                    });

                sb.Schema.EntityEvents<UserQueryDN>().Retrieved += UserQueryLogic_Retrieved;

                new BasicExecute<UserQueryDN>(UserQueryOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (uq, _) => { }
                }.Register();

                new BasicDelete<UserQueryDN>(UserQueryOperation.Delete)
                {
                    Lite = true,
                    Delete = (uq, _) => uq.Delete()
                }.Register();
            }
        }

        public static UserQueryDN ParseAndSave(this UserQueryDN userQuery)
        {
            if (!userQuery.IsNew || userQuery.queryName == null)
                throw new InvalidOperationException("userQuery should be new and have queryName");

            userQuery.Query = QueryLogic.RetrieveOrGenerateQuery(userQuery.queryName);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(userQuery.queryName);

            userQuery.ParseData(description);

            return userQuery.Execute(UserQueryOperation.Save);
        }


        static void UserQueryLogic_Retrieved(UserQueryDN userQuery)
        {
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            userQuery.ParseData(description);
        }

        public static List<Lite<UserQueryDN>> GetUserQueries(object queryName)
        {
            return (from er in Database.Query<UserQueryDN>()
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(queryName) && er.EntityType == null
                    select er.ToLite()).ToList();
        }

        public static List<Lite<UserQueryDN>> GetUserQueriesEntity(Type entityType)
        {
            return (from er in Database.Query<UserQueryDN>()
                    where er.EntityType == entityType.ToTypeDN().ToLite()
                    select er.ToLite()).ToList();
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserQueryDN uq) => uq.Related, typeof(UserDN));

            TypeConditionLogic.Register<UserQueryDN>(newEntityGroupKey,
                uq => uq.Related.RefersTo(UserDN.Current));
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserQueryDN uq) => uq.Related, typeof(RoleDN));

            TypeConditionLogic.Register<UserQueryDN>(newEntityGroupKey,
                uq => AuthLogic.CurrentRoles().Contains(uq.Related));
        }

        public static List<Lite<UserQueryDN>> Autocomplete(string subString, int limit)
        {
            return Database.Query<UserQueryDN>().Where(uq => uq.EntityType == null).Autocomplete(subString, limit);
        }
    }
}
