using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using Signum.Entities.Chart;
using Signum.Engine.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Engine.Authorization;
using Signum.Engine.Operations;
using Signum.Utilities;
using Signum.Engine.UserQueries;

namespace Signum.Engine.Chart
{
    public static class UserChartLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                if (sb.Schema.Tables.ContainsKey(typeof(UserChartDN)))
                    throw new InvalidOperationException("UserChart has already been registered");

                sb.Settings.OverrideAttributes((UserChartDN uc) => uc.Columns.First().TokenString, new Attribute[0]);

                UserAssetsImporter.UserAssetNames.Add("UserChart", typeof(UserChartDN));

                sb.Include<UserChartDN>();

                dqm.RegisterQuery(typeof(UserChartDN), () =>
                    from uq in Database.Query<UserChartDN>()
                    select new
                    {
                        Entity = uq,
                        uq.Query,
                        uq.EntityType,
                        uq.Id,
                        uq.DisplayName,
                        uq.ChartScript,
                        uq.GroupResults,
                    });

                sb.Schema.EntityEvents<UserChartDN>().Retrieved += ChartLogic_Retrieved;

                new Graph<UserChartDN>.Execute(UserChartOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (uc, _) => { }
                }.Register();

                new Graph<UserChartDN>.Delete(UserChartOperation.Delete)
                {
                    Lite = true,
                    Delete = (uc, _) => { uc.Delete(); }
                }.Register();
            }
        }

        public static UserChartDN ParseData(this UserChartDN userChart)
        {
            if (!userChart.IsNew || userChart.queryName == null)
                throw new InvalidOperationException("userChart should be new and have queryName");

            userChart.Query = QueryLogic.GetQuery(userChart.queryName);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(userChart.queryName);

            userChart.ParseData(description);

            return userChart;
        }

        static void ChartLogic_Retrieved(UserChartDN userQuery)
        {
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);

            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            foreach (var item in userQuery.Columns)
            {
                item.parentChart = userQuery;
            }

            userQuery.ParseData(description);
        }

        public static List<Lite<UserChartDN>> GetUserCharts(object queryName)
        {
            return (from er in Database.Query<UserChartDN>()
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(queryName) && er.EntityType == null
                    select er.ToLite()).ToList();
        }

        public static List<Lite<UserChartDN>> Autocomplete(string content, int limit)
        {
            return (from er in Database.Query<UserChartDN>()
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(content) && er.EntityType == null
                    select er.ToLite()).ToList();
        }


        public static List<Lite<UserChartDN>> GetUserChartsEntity(Type entityType)
        {
            return (from er in Database.Query<UserChartDN>()
                    where er.EntityType == entityType.ToTypeDN().ToLite()
                    select er.ToLite()).ToList();
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartDN uq) => uq.Related, typeof(UserDN));

            TypeConditionLogic.Register<UserChartDN>(newEntityGroupKey, uq => uq.Related.RefersTo(UserDN.Current));
        }


        public static void RegisterRoleTypeCondition(SchemaBuilder sb, Enum newEntityGroupKey)
        {
            sb.Schema.Settings.AssertImplementedBy((UserChartDN uq) => uq.Related, typeof(RoleDN));

            TypeConditionLogic.Register<UserChartDN>(newEntityGroupKey, uq => AuthLogic.CurrentRoles().Contains(uq.Related));
        }
    }
}
