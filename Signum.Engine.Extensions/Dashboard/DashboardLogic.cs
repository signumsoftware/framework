using Signum.Engine.Authorization;
using Signum.Engine.Translation;
using Signum.Engine.UserAssets;
using Signum.Engine.ViewLog;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.UserQueries;

namespace Signum.Engine.Dashboard
{
    public static class DashboardLogic
    {
        public static ResetLazy<Dictionary<Lite<DashboardEntity>, DashboardEntity>> Dashboards = null!;
        public static ResetLazy<Dictionary<Type, List<Lite<DashboardEntity>>>> DashboardsByType = null!;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                PermissionAuthLogic.RegisterPermissions(DashboardPermission.ViewDashboard);

                UserAssetsImporter.Register<DashboardEntity>("Dashboard", DashboardOperation.Save);

                UserAssetsImporter.PartNames.AddRange(new Dictionary<string, Type>
                {
                    {"UserChartPart", typeof(UserChartPartEntity)},
                    {"CombinedUserChartPart", typeof(CombinedUserChartPartEntity)},
                    {"UserQueryPart", typeof(UserQueryPartEntity)},
                    {"LinkListPart", typeof(LinkListPartEntity)},
                    {"ValueUserQueryListPart", typeof(ValueUserQueryListPartEntity)},
                    {"UserTreePart", typeof(UserTreePartEntity)},
                });

                sb.Include<DashboardEntity>()
                    .WithQuery(() => cp => new
                    {
                        Entity = cp,
                        cp.Id,
                        cp.DisplayName,
                        cp.EntityType,
                        cp.Owner,
                        cp.DashboardPriority,
                    });

                if (sb.Settings.ImplementedBy((DashboardEntity cp) => cp.Parts.First().Content, typeof(UserQueryPartEntity)))
                {
                    sb.Schema.EntityEvents<UserQueryEntity>().PreUnsafeDelete += query =>
                    {
                        Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((UserQueryPartEntity)mle.Element.Content).UserQuery)).UnsafeDeleteMList();
                        Database.Query<UserQueryPartEntity>().Where(uqp => query.Contains(uqp.UserQuery)).UnsafeDelete();
                        return null;
                    };

                    sb.Schema.Table<UserQueryEntity>().PreDeleteSqlSync += arg =>
                    {
                        var uq = (UserQueryEntity)arg;

                        var parts = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts)
                            .Where(mle => ((UserQueryPartEntity)mle.Element.Content).UserQuery.Is(uq)));

                        var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<UserQueryPartEntity>()
                          .Where(mle => mle.UserQuery.Is(uq)));

                        return SqlPreCommand.Combine(Spacing.Simple, parts, parts2);
                    };
                }

                if (sb.Settings.ImplementedBy((DashboardEntity cp) => cp.Parts.First().Content, typeof(UserChartPartEntity)))
                {
                    sb.Schema.EntityEvents<UserChartEntity>().PreUnsafeDelete += query =>
                    {
                        Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => query.Contains(((UserChartPartEntity)mle.Element.Content).UserChart)).UnsafeDeleteMList();
                        Database.Query<UserChartPartEntity>().Where(uqp => query.Contains(uqp.UserChart)).UnsafeDelete();

                        Database.MListQuery((DashboardEntity cp) => cp.Parts).Where(mle => ((CombinedUserChartPartEntity)mle.Element.Content).UserCharts.Any(uc => query.Contains(uc))).UnsafeDeleteMList();
                        Database.Query<CombinedUserChartPartEntity>().Where(cuqp =>  cuqp.UserCharts.Any(uc => query.Contains(uc))).UnsafeDelete();

                        return null;
                    };

                    sb.Schema.Table<UserChartEntity>().PreDeleteSqlSync += arg =>
                    {
                        var uc = (UserChartEntity)arg;

                        var mlistElems = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts)
                            .Where(mle => ((UserChartPartEntity)mle.Element.Content).UserChart.Is(uc)));

                        var parts = Administrator.UnsafeDeletePreCommand(Database.Query<UserChartPartEntity>()
                           .Where(mle => mle.UserChart.Is(uc)));

                        var mlistElems2 = Administrator.UnsafeDeletePreCommandMList((DashboardEntity cp) => cp.Parts, Database.MListQuery((DashboardEntity cp) => cp.Parts)
                            .Where(mle => ((CombinedUserChartPartEntity)mle.Element.Content).UserCharts.Contains(uc)));

                        var parts2 = Administrator.UnsafeDeletePreCommand(Database.Query<CombinedUserChartPartEntity>()
                            .Where(mle => mle.UserCharts.Contains(uc)));

                        return SqlPreCommand.Combine(Spacing.Simple, mlistElems, parts, mlistElems2, parts2);
                    };
                }

                DashboardGraph.Register();


                Dashboards = sb.GlobalLazy(() => Database.Query<DashboardEntity>().ToDictionary(a => a.ToLite()),
                    new InvalidateWith(typeof(DashboardEntity)));

                DashboardsByType = sb.GlobalLazy(() => Dashboards.Value.Values.Where(a => a.EntityType != null)
                .SelectCatch(d => KeyValuePair.Create(TypeLogic.IdToType.GetOrThrow(d.EntityType!.Id), d.ToLite()))
                .GroupToDictionary(),
                    new InvalidateWith(typeof(DashboardEntity)));
            }
        }

        class DashboardGraph : Graph<DashboardEntity>
        {
            public static void Register()
            {

                new Execute(DashboardOperation.Save)
                {
                    CanBeNew = true,
                    CanBeModified = true,
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

        public static DashboardEntity? GetHomePageDashboard()
        {
            var result = GetDashboard(null);

            if (result == null)
                return null;

            using (ViewLogLogic.LogView(result.ToLite(), "GetHomePageDashboard"))
                return result;
        }

        public static Func<string?, DashboardEntity?> GetDashboard = GetDashboardDefault;

        static DashboardEntity? GetDashboardDefault(string? key)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);

            var result = Dashboards.Value.Values
                .Where(d =>
                    (!key.HasText() || d.Key == key)
                    && d.EntityType == null && d.DashboardPriority.HasValue && isAllowed(d))
                .OrderByDescending(a => a.DashboardPriority)
                .FirstOrDefault();

            return result;
        }

        public static void RegisterTranslatableRoutes()
        {
            TranslatedInstanceLogic.AddRoute((DashboardEntity d) => d.DisplayName);
            TranslatedInstanceLogic.AddRoute((DashboardEntity d) => d.Parts[0].Title);
        }

        public static List<DashboardEntity> GetEmbeddedDashboards(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);

            var result = DashboardsByType.Value.TryGetC(entityType).EmptyIfNull().Select(lite => Dashboards.Value.GetOrThrow(lite))
                .Where(d => d.EmbeddedInEntity != DashboardEmbedededInEntity.None && isAllowed(d))
                .OrderByDescending(a => a.DashboardPriority)
                .ToList();

            if (!result.Any())
                return result;

            foreach (var item in result)
            {
                using (ViewLogLogic.LogView(item.ToLite(), "GetEmbeddedDashboards"))
                {
                } 
            }
            return result;
        }

        public static List<Lite<DashboardEntity>> GetDashboards()
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);
            return Dashboards.Value.Values
                .Where(d => d.EntityType == null && isAllowed(d))
                .Select(d => d.ToLite(TranslatedInstanceLogic.TranslatedField(d, d => d.DisplayName)))
                .ToList();
        }

        public static List<Lite<DashboardEntity>> GetDashboardsEntity(Type entityType)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);
            return DashboardsByType.Value.TryGetC(entityType)
                .EmptyIfNull()
                .Select(lite => Dashboards.Value.GetOrThrow(lite))
                .Where(d => isAllowed(d))
                .Select(d => d.ToLite(TranslatedInstanceLogic.TranslatedField(d, d => d.DisplayName)))
                .ToList();
        }

        public static List<Lite<DashboardEntity>> Autocomplete(string subString, int limit)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);
            return Dashboards.Value.Values
                .Where(d => d.EntityType == null && isAllowed(d))
                .Select(d => d.ToLite(TranslatedInstanceLogic.TranslatedField(d, d => d.DisplayName)))
                .Autocomplete(subString, limit).ToList();
        }

        public static DashboardEntity RetrieveDashboard(this Lite<DashboardEntity> dashboard)
        {
            using (ViewLogLogic.LogView(dashboard, "Dashboard"))
            {
                var result = Dashboards.Value.GetOrThrow(dashboard);

                var isAllowed = Schema.Current.GetInMemoryFilter<DashboardEntity>(userInterface: false);
                if (!isAllowed(result))
                    throw new EntityNotFoundException(dashboard.EntityType, dashboard.Id);

                return result;
            }
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardEntity uq) => uq.Owner, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<DashboardEntity>(typeCondition,
                uq => uq.Owner.Is(UserEntity.Current));

            RegisterPartsTypeCondition(typeCondition);
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((DashboardEntity uq) => uq.Owner, typeof(RoleEntity));

            TypeConditionLogic.RegisterCompile<DashboardEntity>(typeCondition,
                uq => AuthLogic.CurrentRoles().Contains(uq.Owner) || uq.Owner == null);

            RegisterPartsTypeCondition(typeCondition);
        }

        public static void RegisterPartsTypeCondition(TypeConditionSymbol typeCondition)
        {
            TypeConditionLogic.Register<ValueUserQueryListPartEntity>(typeCondition,
                 cscp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(cscp)));

            TypeConditionLogic.Register<LinkListPartEntity>(typeCondition,
                 llp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(llp)));

            TypeConditionLogic.Register<UserChartPartEntity>(typeCondition,
                 ucp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(ucp)));

            TypeConditionLogic.Register<CombinedUserChartPartEntity>(typeCondition,
           ucp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(ucp)));

            TypeConditionLogic.Register<UserQueryPartEntity>(typeCondition,
                uqp => Database.Query<DashboardEntity>().WhereCondition(typeCondition).Any(d => d.ContainsContent(uqp)));
        }
    }
}
