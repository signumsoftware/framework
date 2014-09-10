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
using Signum.Engine.UserAssets;
using Signum.Engine.ViewLog;
using Signum.Engine.Exceptions;

namespace Signum.Engine.Dashboard
{
    public static class DashboardLogic
    {
        public static ResetLazy<Dictionary<Lite<DashboardDN>, DashboardDN>> Dashboards;
        public static ResetLazy<Dictionary<Type, List<Lite<DashboardDN>>>> DashboardsByType;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
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
                        cp.Id,
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
                        Database.MListQuery((DashboardDN cp) => cp.Parts).Where(mle => query.Contains(((UserQueryPartDN)mle.Element.Content).UserQuery)).UnsafeDeleteMList();
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
                        Database.MListQuery((DashboardDN cp) => cp.Parts).Where(mle => query.Contains(((UserChartPartDN)mle.Element.Content).UserChart)).UnsafeDeleteMList();
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


                Dashboards = sb.GlobalLazy(() => Database.Query<DashboardDN>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(DashboardDN)));

                DashboardsByType = sb.GlobalLazy(() => Dashboards.Value.Values.Where(a => a.EntityType != null).GroupToDictionary(a => TypeLogic.IdToType.GetOrThrow(a.EntityType.Id), a => a.ToLite()),
                    new InvalidateWith(typeof(DashboardDN)));
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
                    Delete = (cp, _) =>
                    {
                        var parts = cp.Parts.Select(a => a.Content).ToList();
                        cp.Delete();
                        Database.DeleteList(parts);
                    }
                }.Register();

                new ConstructFrom<DashboardDN>(DashboardOperation.Clone)
                {
                    Construct = (cp, _) => cp.Clone()
                }.Register();
            }
        }

        public static DashboardDN GetHomePageDashboard()
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardDN>(userInterface: true);

            var result =  Dashboards.Value.Values
                .Where(d => d.EntityType == null && d.DashboardPriority.HasValue && isAllowed(d))
                .OrderByDescending(a => a.DashboardPriority)
                .FirstOrDefault();

            if (result == null)
                return null;

            using (ViewLogLogic.LogView(result.ToLite(), "GetHomePageDashboard"))
                return result;
        }

        public static DashboardDN GetEmbeddedDashboard(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardDN>(userInterface: true);

            var result = DashboardsByType.Value.TryGetC(entityType).EmptyIfNull().Select(Dashboards.Value.GetOrThrow)
                .Where(d => d.EmbeddedInEntity.Value != DashboardEmbedededInEntity.None && isAllowed(d))
                .OrderByDescending(a => a.DashboardPriority).FirstOrDefault();

            if (result == null)
                return null;

            using (ViewLogLogic.LogView(result.ToLite(), "GetEmbeddedDashboard"))
                return result;
        }

        public static List<Lite<DashboardDN>> GetDashboards()
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardDN>(userInterface: true);
            return Dashboards.Value.Where(d => d.Value.EntityType == null && isAllowed(d.Value))
                .Select(d => d.Key).ToList();
        }

        public static List<Lite<DashboardDN>> GetDashboardsEntity(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardDN>(userInterface: true);
            return DashboardsByType.Value.TryGetC(entityType).EmptyIfNull()
                .Where(e => isAllowed(Dashboards.Value.GetOrThrow(e))).ToList();
        }

        public static List<Lite<DashboardDN>> Autocomplete(string subString, int limit)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardDN>(userInterface: true);
            return Dashboards.Value.Where(a => a.Value.EntityType == null && isAllowed(a.Value))
                .Select(a => a.Key).Autocomplete(subString, limit).ToList();
        }

        public static DashboardDN RetrieveDashboard(this Lite<DashboardDN> dashboard)
        {
            using (ViewLogLogic.LogView(dashboard, "Dashboard"))
            {
                var result = Dashboards.Value.GetOrThrow(dashboard);

                var isAllowed = Schema.Current.GetInMemoryFilter<DashboardDN>(userInterface: true);
                if (!isAllowed(result))
                    throw new EntityNotFoundException(dashboard.EntityType, dashboard.Id);

                return result;
            }
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardDN uq) => uq.Owner, typeof(UserDN));

            TypeConditionLogic.RegisterCompile<DashboardDN>(typeCondition,
                uq => uq.Owner.RefersTo(UserDN.Current));

            RegisterPartsTypeCondition(typeCondition);
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardDN uq) => uq.Owner, typeof(RoleDN));

            TypeConditionLogic.RegisterCompile<DashboardDN>(typeCondition,
                uq => AuthLogic.CurrentRoles().Contains(uq.Owner));

            RegisterPartsTypeCondition(typeCondition);
        }

        public static void RegisterPartsTypeCondition(TypeConditionSymbol typeCondition)
        {
            TypeConditionLogic.Register<CountSearchControlPartDN>(typeCondition,
                 cscp => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(cscp)));

            TypeConditionLogic.Register<LinkListPartDN>(typeCondition,
                 llp => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(llp)));

            TypeConditionLogic.Register<UserChartPartDN>(typeCondition,
                 ucp => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(ucp)));

            TypeConditionLogic.Register<UserQueryPartDN>(typeCondition,
                uqp => Database.Query<DashboardDN>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(uqp)));
        }
    }
}
