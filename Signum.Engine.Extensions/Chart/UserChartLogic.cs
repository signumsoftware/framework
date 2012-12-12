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

namespace Signum.Engine.Chart
{
    public static class UserChartLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Settings.OverrideAttributes((UserChartDN uc) => uc.Columns.First().TokenString, new Attribute[0]);

                sb.Include<UserChartDN>();

                dqm[typeof(UserChartDN)] = (from uq in Database.Query<UserChartDN>()
                                            select new
                                            {
                                                Entity = uq,
                                                uq.Query,
                                                uq.Id,
                                                uq.DisplayName,
                                                uq.ChartScript,
                                                uq.GroupResults,
                                            }).ToDynamic();

                sb.Schema.EntityEvents<UserChartDN>().Retrieved += ChartLogic_Retrieved;

                new BasicExecute<UserChartDN>(UserChartOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (uc, _) => { }
                }.Register();

                new BasicDelete<UserChartDN>(UserChartOperation.Delete)
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

            userChart.Query = QueryLogic.RetrieveOrGenerateQuery(userChart.queryName);

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
                    where er.Query.Key == QueryUtils.GetQueryUniqueKey(queryName)
                    select er.ToLite()).ToList();
        }

        public static void RemoveUserChart(Lite<UserChartDN> lite)
        {
            Database.Delete(lite);
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
