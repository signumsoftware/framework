using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine.Maps;
using Signum.Engine.DynamicQuery;
using System.Reflection;
using Signum.Entities.Dashboard;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Utilities;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.UserQueries;
using Signum.Engine.Operations;
using Signum.Entities.UserQueries;
using Signum.Entities.Chart;
using Signum.Entities.Basics;

namespace Signum.Engine.Dashboard
{
    public static class DashboardLogic
    {
        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                UserQueryLogic.Start(sb, dqm);

                PermissionAuthLogic.RegisterPermissions(DashboardPermission.ViewDashboard);

                UserAssetsImporter.UserAssetNames.Add("Dashboard", typeof(DashboardDN));

                UserAssetsImporter.PartNames.AddRange(new Dictionary<string, Type>
                {
                    {"UserChartPart", typeof(UserChartPartDN)},
                    {"UserQueryPart", typeof(UserQueryPartDN)},
                    {"LinkListPart", typeof(LinkListPartDN)},
                    {"CountSearchControlPart", typeof(CountSearchControlPartDN)},
                });

                sb.Include<DashboardDN>();


                dqm.RegisterQuery(typeof(DashboardDN), () =>
                    from cp in Database.Query<DashboardDN>()
                    select new
                    {
                        Entity = cp,
                        cp.DisplayName,
                        cp.EntityType,
                        Related = cp.Owner,
                    });

                dqm.RegisterQuery(typeof(LinkListPartDN), () =>
                    from cp in Database.Query<LinkListPartDN>()
                    select new
                    {
                        Entity = cp,
                        ToStr = cp.ToString(),
                        Links = cp.Links.Count
                    });

                dqm.RegisterQuery(typeof(CountSearchControlPartDN), () =>
                    from cp in Database.Query<CountSearchControlPartDN>()
                    select new
                    {
                        Entity = cp,
                        ToStr = cp.ToString(),
                        Links = cp.UserQueries.Count
                    });

                if (sb.Settings.ImplementedBy((DashboardDN cp) => cp.Parts.First().Content, typeof(UserQueryPartDN)))
                {
                    sb.Schema.EntityEvents<UserQueryDN>().PreUnsafeDelete += query =>
                    {
                        Database.MListQuery((DashboardDN cp) => cp.Parts).Where(mle => query.Contains(((UserQueryPartDN)mle.Element.Content).UserQuery)).UnsafeDelete();
                        Database.Query<UserQueryPartDN>().Where(uqp => query.Contains(uqp.UserQuery)).UnsafeDelete();
                    };

                    sb.Schema.Table<UserQueryDN>().PreDeleteSqlSync += arg =>
                    {
                        var uq = (UserQueryDN)arg;

                        var parts = Administrator.UnsafeDeletePreCommand(Database.MListQuery((DashboardDN cp) => cp.Parts)
                            .Where(mle => ((UserQueryPartDN)mle.Element.Content).UserQuery == uq));

                        var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryPartDN>()
                          .Where(mle => mle.UserQuery == uq));

                        return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
                    };
                }

                if (sb.Settings.ImplementedBy((DashboardDN cp) => cp.Parts.First().Content, typeof(UserChartPartDN)))
                {
                    sb.Schema.EntityEvents<UserChartDN>().PreUnsafeDelete += query =>
                    {
                        Database.MListQuery((DashboardDN cp) => cp.Parts).Where(mle => query.Contains(((UserChartPartDN)mle.Element.Content).UserChart)).UnsafeDelete();
                        Database.Query<UserChartPartDN>().Where(uqp => query.Contains(uqp.UserChart)).UnsafeDelete();
                    };

                    sb.Schema.Table<UserChartDN>().PreDeleteSqlSync += arg =>
                    {
                        var uc = (UserChartDN)arg;

                        var parts = Administrator.UnsafeDeletePreCommand(Database.MListQuery((DashboardDN cp) => cp.Parts)
                            .Where(mle => ((UserChartPartDN)mle.Element.Content).UserChart == uc));

                        var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserChartPartDN>()
                            .Where(mle => mle.UserChart == uc));

                        return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
                    };
                }

                DashboardGraph.Register();
            }
        }

        class DashboardGraph : Graph<DashboardDN>
        {
            public static void Register()
            {
                new Construct(DashboardOperation.Create)
                {
                    Construct = (_) => new DashboardDN { Owner = UserQueryUtils.DefaultRelated() }
                }.Register();

                new Execute(DashboardOperation.Save)
                {
                    AllowsNew = true,
                    Lite = false,
                    Execute = (cp, _) => { }
                }.Register();

                new Delete(DashboardOperation.Delete)
                {
                    Lite = false,
                    Delete = (cp, _) =>
                    {
                        var parts = cp.Parts.Select(a => a.Content).ToList();
                        cp.Delete();
                        Database.DeleteList(parts);
                    }
                }.Register();

                new ConstructFrom<DashboardDN>(DashboardOperation.Clone)
                {
                    Lite = true,
                    AllowsNew = false,
                    Construct = (cp, _) => cp.Clone()
                }.Register();
            }
        }

        public static DashboardDN GetHomePageDashboard()
        {
            var cps = Database.Query<DashboardDN>()
                .Where(a => a.HomePagePriority.HasValue)
                .OrderByDescending(a => a.HomePagePriority)
                .Select(a => a.ToLite())
                .FirstOrDefault();

            if (cps == null)
                return null;

            return cps.Retrieve(); //I assume this simplifies the cross applys.
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardDN uq) => uq.Owner, typeof(UserDN));

            TypeConditionLogic.Register<DashboardDN>(typeCondition,
                uq => uq.Owner.RefersTo(UserDN.Current));

            TypeConditionLogic.Register<CountSearchControlPartDN>(typeCondition,
                 cscp => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(cscp)));

            TypeConditionLogic.Register<LinkListPartDN>(typeCondition,
                 llp => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(llp)));

            TypeConditionLogic.Register<UserChartPartDN>(typeCondition,
                 llp => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(llp)));
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardDN uq) => uq.Owner, typeof(RoleDN));

            TypeConditionLogic.Register<DashboardDN>(typeCondition,
                uq => AuthLogic.CurrentRoles().Contains(uq.Owner));

            TypeConditionLogic.Register<CountSearchControlPartDN>(typeCondition,
                 uq => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(uq)));

            TypeConditionLogic.Register<LinkListPartDN>(typeCondition,
                 uq => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(uq)));

            TypeConditionLogic.Register<UserChartPartDN>(typeCondition,
                 uq => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(uq)));
        }

        public static List<Lite<DashboardDN>> GetDashboardEntity(Type entityType)
        {
            return (from er in Database.Query<DashboardDN>()
                    where er.EntityType == entityType.ToTypeDN().ToLite()
                    select er.ToLite()).ToList();
        }

        public static List<Lite<DashboardDN>> Autocomplete(string subString, int limit)
        {
            return Database.Query<DashboardDN>().Where(cp => cp.EntityType == null).Select(o => o.ToLite()).Autocomplete(subString, limit);
        }
    }
}
