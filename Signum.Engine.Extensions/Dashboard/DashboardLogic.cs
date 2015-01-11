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

namespace Signum.Engine.Dashboard
{
    public static class DashboardLogic
    {
        public static ResetLazy<Dictionary<Lite<DashboardEntity>, DashboardEntity>> Dashboards;
        public static ResetLazy<Dictionary<Type, List<Lite<DashboardEntity>>>> DashboardsByType;

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterPermissions(DashboardPermission.ViewDashboard);

                UserAssetsImporter.UserAssetNames.Add("Dashboard", typeof(DashboardEntity));

                UserAssetsImporter.PartNames.AddRange(new Dictionary<string, Type>
                {
                    {"UserChartPart", typeof(UserChartPartEntity)},
                    {"UserQueryPart", typeof(UserQueryPartEntity)},
                    {"LinkListPart", typeof(LinkListPartEntity)},
                    {"CountSearchControlPart", typeof(CountSearchControlPartEntity)},
                });

                sb.Include<DashboardEntity>();


                dqm.RegisterQuery(typeof(DashboardEntity), () =>
                    from cp in Database.Query<DashboardEntity>()
                    select new
                    {
                        Entity = cp,
                        cp.Id,
                        cp.DisplayName,
                        cp.EntityType,
                        Related = cp.Owner,
                    });

                dqm.RegisterQuery(typeof(LinkListPartEntity), () =>
                    from cp in Database.Query<LinkListPartEntity>()
                    select new
                    {
                        Entity = cp,
                        ToStr = cp.ToString(),
                        Links = cp.Links.Count
                    });

                dqm.RegisterQuery(typeof(CountSearchControlPartEntity), () =>
                    from cp in Database.Query<CountSearchControlPartEntity>()
                    select new
                    {
                        Entity = cp,
                        ToStr = cp.ToString(),
                        Links = cp.UserQueries.Count
                    });

                if (sb.Settings.ImplementedBy((DashboardEntity cp) => cp.Parts.First().Content, typeof(UserQueryPartEntity)))
                {
                    sb.Schema.EntityEvents<UserQueryEntity>().PreUnsafeDelete += query =>
                    {
                        Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((UserQueryPartEntity)mle.Element.Content).UserQuery)).UnsafeDeleteMList();
                        Database.Query<UserQueryPartEntity>().Where(uqp => query.Contains(uqp.UserQuery)).UnsafeDelete();
                    };

                    sb.Schema.Table<UserQueryEntity>().PreDeleteSqlSync += arg =>
                    {
                        var uq = (UserQueryEntity)arg;

                        var parts = Administrator.UnsafeDeletePreCommand(Database.MListQuery((DashboardEntity cp) => cp.Parts)
                            .Where(mle => ((UserQueryPartEntity)mle.Element.Content).UserQuery == uq));

                        var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryPartEntity>()
                          .Where(mle => mle.UserQuery == uq));

                        return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
                    };
                }

                if (sb.Settings.ImplementedBy((DashboardEntity cp) => cp.Parts.First().Content, typeof(UserChartPartEntity)))
                {
                    sb.Schema.EntityEvents<UserChartEntity>().PreUnsafeDelete += query =>
                    {
                        Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((UserChartPartEntity)mle.Element.Content).UserChart)).UnsafeDeleteMList();
                        Database.Query<UserChartPartEntity>().Where(uqp => query.Contains(uqp.UserChart)).UnsafeDelete();
                    };

                    sb.Schema.Table<UserChartEntity>().PreDeleteSqlSync += arg =>
                    {
                        var uc = (UserChartEntity)arg;

                        var parts = Administrator.UnsafeDeletePreCommand(Database.MListQuery((DashboardEntity cp) => cp.Parts)
                            .Where(mle => ((UserChartPartEntity)mle.Element.Content).UserChart == uc));

                        var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserChartPartEntity>()
                            .Where(mle => mle.UserChart == uc));

                        return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
                    };
                }

                DashboardGraph.Register();


                Dashboards = sb.GlobalLazy(() => Database.Query<DashboardEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(DashboardEntity)));

                DashboardsByType = sb.GlobalLazy(() => Dashboards.Value.Values.Where(a => a.EntityType != null).GroupToDictionary(a => TypeLogic.IdToType.GetOrThrow(a.EntityType.Id), a => a.ToLite()),
                    new InvalidateWith(typeof(DashboardEntity)));
            }
        }

        class DashboardGraph : Graph<DashboardEntity>
        {
            public static void Register()
            {
                new Construct(DashboardOperation.Create)
                {
                    Construct = (_) => new DashboardEntity { Owner = UserQueryUtils.DefaultRelated() }
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

                new ConstructFrom<DashboardEntity>(DashboardOperation.Clone)
                {
                    Construct = (cp, _) => cp.Clone()
                }.Register();
            }
        }

        public static DashboardEntity GetHomePageDashboard()
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: true);

            var result =  Dashboards.Value.Values
                .Where(d => d.EntityType == null && d.DashboardPriority.HasValue && isAllowed(d))
                .OrderByDescending(a => a.DashboardPriority)
                .FirstOrDefault();

            if (result == null)
                return null;

            using (ViewLogLogic.LogView(result.ToLite(), "GetHomePageDashboard"))
                return result;
        }

        public static DashboardEntity GetEmbeddedDashboard(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: true);

            var result = DashboardsByType.Value.TryGetC(entityType).EmptyIfNull().Select(Dashboards.Value.GetOrThrow)
                .Where(d => d.EmbeddedInEntity.Value != DashboardEmbedededInEntity.None && isAllowed(d))
                .OrderByDescending(a => a.DashboardPriority).FirstOrDefault();

            if (result == null)
                return null;

            using (ViewLogLogic.LogView(result.ToLite(), "GetEmbeddedDashboard"))
                return result;
        }

        public static List<Lite<DashboardEntity>> GetDashboards()
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: true);
            return Dashboards.Value.Where(d => d.Value.EntityType == null && isAllowed(d.Value))
                .Select(d => d.Key).ToList();
        }

        public static List<Lite<DashboardEntity>> GetDashboardsEntity(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: true);
            return DashboardsByType.Value.TryGetC(entityType).EmptyIfNull()
                .Where(e => isAllowed(Dashboards.Value.GetOrThrow(e))).ToList();
        }

        public static List<Lite<DashboardEntity>> Autocomplete(string subString, int limit)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: true);
            return Dashboards.Value.Where(a => a.Value.EntityType == null && isAllowed(a.Value))
                .Select(a => a.Key).Autocomplete(subString, limit).ToList();
        }

        public static DashboardEntity RetrieveDashboard(this Lite<DashboardEntity> dashboard)
        {
            using (ViewLogLogic.LogView(dashboard, "Dashboard"))
            {
                var result = Dashboards.Value.GetOrThrow(dashboard);

                var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: true);
                if (!isAllowed(result))
                    throw new EntityNotFoundException(dashboard.EntityType, dashboard.Id);

                return result;
            }
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardEntity uq) => uq.Owner, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<DashboardEntity>(typeCondition,
                uq => uq.Owner.RefersTo(UserEntity.Current));

            RegisterPartsTypeCondition(typeCondition);
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardEntity uq) => uq.Owner, typeof(RoleEntity));

            TypeConditionLogic.RegisterCompile<DashboardEntity>(typeCondition,
                uq => AuthLogic.CurrentRoles().Contains(uq.Owner));

            RegisterPartsTypeCondition(typeCondition);
        }

        public static void RegisterPartsTypeCondition(TypeConditionSymbol typeCondition)
        {
            TypeConditionLogic.Register<CountSearchControlPartEntity>(typeCondition,
                 cscp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(cscp)));

            TypeConditionLogic.Register<LinkListPartEntity>(typeCondition,
                 llp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(llp)));

            TypeConditionLogic.Register<UserChartPartEntity>(typeCondition,
                 ucp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(ucp)));

            TypeConditionLogic.Register<UserQueryPartEntity>(typeCondition,
                uqp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(cp => cp.ContainsContent(uqp)));
        }
    }
}
