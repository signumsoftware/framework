using Newtonsoft.Json;
using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Chart;
using Signum.Engine.Dashboard;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.UserAssets;
using Signum.Engine.UserQueries;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.Toolbar;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Signum.Engine.Toolbar
{

    public static class ToolbarLogic
    {
        public static ResetLazy<Dictionary<Lite<ToolbarEntity>, ToolbarEntity>> Toolbars;
        public static ResetLazy<Dictionary<Lite<ToolbarMenuEntity>, ToolbarMenuEntity>> ToolbarMenus;

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<ToolbarEntity>()
                    .WithSave(ToolbarOperation.Save)
                    .WithDelete(ToolbarOperation.Delete)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Owner,
                        e.Priority
                    });


                sb.Include<ToolbarMenuEntity>()
                    .WithSave(ToolbarMenuOperation.Save)
                    .WithDelete(ToolbarMenuOperation.Delete)
                    .WithQuery(() => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name
                    });

                UserAssetsImporter.RegisterName<ToolbarEntity>("Toolbar");
                UserAssetsImporter.RegisterName<ToolbarMenuEntity>("ToolbarMenu");

                RegisterDelete<UserQueryEntity>(sb);
                RegisterDelete<UserChartEntity>(sb);
                RegisterDelete<QueryEntity>(sb);
                RegisterDelete<DashboardEntity>(sb);
                RegisterDelete<ToolbarMenuEntity>(sb);

                Toolbars = sb.GlobalLazy(() => Database.Query<ToolbarEntity>().ToDictionary(a => a.ToLite()),
                   new InvalidateWith(typeof(ToolbarEntity)));

                ToolbarMenus = sb.GlobalLazy(() => Database.Query<ToolbarMenuEntity>().ToDictionary(a => a.ToLite()),
                   new InvalidateWith(typeof(ToolbarMenuEntity)));
            }
        }

        public static void RegisterUserTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Owner, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<ToolbarEntity>(typeCondition,
                t => t.Owner.Is(UserEntity.Current));

            sb.Schema.Settings.AssertImplementedBy((ToolbarMenuEntity t) => t.Owner, typeof(UserEntity));

            TypeConditionLogic.RegisterCompile<ToolbarMenuEntity>(typeCondition,
                t => t.Owner.Is(UserEntity.Current));
        }

        public static void RegisterRoleTypeCondition(SchemaBuilder sb, TypeConditionSymbol typeCondition)
        {
            sb.Schema.Settings.AssertImplementedBy((ToolbarEntity t) => t.Owner, typeof(RoleEntity));

            TypeConditionLogic.RegisterCompile<ToolbarEntity>(typeCondition,
                t => AuthLogic.CurrentRoles().Contains(t.Owner));

            sb.Schema.Settings.AssertImplementedBy((ToolbarMenuEntity t) => t.Owner, typeof(RoleEntity));

            TypeConditionLogic.RegisterCompile<ToolbarMenuEntity>(typeCondition,
                t => AuthLogic.CurrentRoles().Contains(t.Owner));
        }

        private static void RegisterDelete<T>(SchemaBuilder sb) where T : Entity
        {
            if (sb.Settings.ImplementedBy((ToolbarEntity tb) => tb.Elements.First().Content, typeof(T)))
            {
                sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
                {
                    Database.MListQuery((ToolbarEntity tb) => tb.Elements).Where(mle => query.Contains((T)mle.Element.Content.Entity)).UnsafeDeleteMList();
                    return null;
                };

                sb.Schema.Table<T>().PreDeleteSqlSync += arg =>
                {
                    var entity = (T)arg;

                    var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarEntity tb) => tb.Elements, Database.MListQuery((ToolbarEntity tb) => tb.Elements)
                        .Where(mle => mle.Element.Content.Entity == entity));

                    return parts;
                };
            }

            if (sb.Settings.ImplementedBy((ToolbarMenuEntity tb) => tb.Elements.First().Content, typeof(T)))
            {
                sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
                {
                    Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements).Where(mle => query.Contains((T)mle.Element.Content.Entity)).UnsafeDeleteMList();
                    return null;
                };

                sb.Schema.Table<T>().PreDeleteSqlSync += arg =>
                {
                    var entity = (T)arg;

                    var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarMenuEntity tb) => tb.Elements, Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements)
                        .Where(mle => mle.Element.Content.Entity == entity));

                    return parts;
                };
            }
        }

        public static ToolbarEntity GetCurrent(ToolbarLocation location)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<ToolbarEntity>(userInterface: true);

            var result = Toolbars.Value.Values
                .Where(t => isAllowed(t) && t.Location == location)
                .OrderByDescending(a => a.Priority)
                .FirstOrDefault();

            return result;
        }

        public static ToolbarResponse GetCurrentToolbarResponse(ToolbarLocation location)
        {
            var curr = GetCurrent(location);

            if (curr == null)
                return null;

            var responses = ToResponseList(curr.Elements);

            if (responses.Count == 0)
                return null;

            return new ToolbarResponse
            {
                type = ToolbarElementType.Header,
                content = curr.ToLite(),
                label = curr.Name,
                elements = responses,
            };
        }

        private static List<ToolbarResponse> ToResponseList(MList<ToolbarElementEmbedded> elements)
        {
            var result = elements.Select(a => ToResponse(a)).NotNull().ToList();


            retry:
                var extraDividers = result.Where((a, i) => a.type == ToolbarElementType.Divider && (i == 0 || result[i - 1].type == ToolbarElementType.Divider || i == result.Count)).ToList();
                result.RemoveAll(extraDividers.Contains);
                var extraHeaders = result.Where((a, i) => IsPureHeader(a) && (i == result.Count || IsPureHeader(result[i + 1]) || result[i + 1].type == ToolbarElementType.Divider)).ToList();
                result.RemoveAll(extraHeaders.Contains);

            if (extraDividers.Any() || extraHeaders.Any())
                goto retry;

            return result;
        }

        private static bool IsPureHeader(ToolbarResponse tr)
        {
            return tr.type == ToolbarElementType.Header && tr.content == null && string.IsNullOrEmpty(tr.url);
        }

        private static ToolbarResponse ToResponse(ToolbarElementEmbedded element)
        {
            if(element.Content != null && !(element.Content is Lite<ToolbarMenuEntity>))
            {
                if (!IsAuthorized(element.Content))
                    return null;
            }

            var result = new ToolbarResponse
            {
                type = element.Type,
                content = element.Content,
                url = element.Url,
                label = element.Label,
                iconName = element.IconName,
                iconColor = element.IconColor,
                autoRefreshPeriod = element.AutoRefreshPeriod,
                openInPopup = element.OpenInPopup,
            };

            if (element.Content is Lite<ToolbarMenuEntity>)
            {
                var tme = ToolbarMenus.Value.GetOrThrow((Lite<ToolbarMenuEntity>)element.Content);
                result.elements = ToResponseList(tme.Elements);
                if (result.elements.Count == 0)
                    return null;
            }
            
            return result;
        }

        static Dictionary<Type, Func<Lite<Entity>, bool>> IsAuthorizedDictionary = new Dictionary<Type, Func<Lite<Entity>, bool>>
        {
            { typeof(QueryEntity), a => IsQueryAllowed((Lite<QueryEntity>)a) },
            { typeof(PermissionSymbol), a => PermissionAuthLogic.IsAuthorized((PermissionSymbol)a.Retrieve()) },
            { typeof(UserQueryEntity), a => InMemoryFilter(UserQueryLogic.UserQueries.Value.GetOrCreate((Lite<UserQueryEntity>)a)) },
            { typeof(UserChartEntity), a => InMemoryFilter(UserChartLogic.UserCharts.Value.GetOrCreate((Lite<UserChartEntity>)a)) },
            { typeof(DashboardEntity), a => InMemoryFilter(DashboardLogic.Dashboards.Value.GetOrCreate((Lite<DashboardEntity>)a)) },
        };

        static bool IsQueryAllowed(Lite<QueryEntity> query)
        {
            try
            {
                return QueryLogic.Queries.QueryAllowed(QueryLogic.QueryNames.GetOrThrow(query.ToString()), true);
            }
            catch (Exception e) when (StartParameters.IgnoredDatabaseMismatches != null)
            {
                //Could happen when not 100% synchronized
                StartParameters.IgnoredDatabaseMismatches.Add(e);

                return false;
            }
        }
        
        static bool InMemoryFilter<T>(T entity) where T : Entity
        {
            if (Schema.Current.IsAllowed(typeof(T), inUserInterface: true) != null)
                return false;

            var isAllowed = Schema.Current.GetInMemoryFilter<T>(userInterface: true);
            return isAllowed(entity);
        }

        public static bool IsAuthorized(Lite<Entity> lite)
        {
            return IsAuthorizedDictionary.GetOrThrow(lite.EntityType)(lite);
        }
    }

    public class ToolbarResponse
    {
        public ToolbarElementType type;
        public string label;
        public Lite<Entity> content;
        public string url;
        public List<ToolbarResponse> elements;
        public string iconName;
        public string iconColor;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public int? autoRefreshPeriod;
        [JsonProperty(DefaultValueHandling = DefaultValueHandling.Ignore )]
        public bool openInPopup;
    }
}
