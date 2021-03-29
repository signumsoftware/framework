using Signum.Engine.Authorization;
using Signum.Engine.Basics;
using Signum.Engine.Chart;
using Signum.Engine.Dashboard;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Engine.Translation;
using Signum.Engine.UserAssets;
using Signum.Engine.UserQueries;
using Signum.Engine.Workflow;
using Signum.Entities;
using Signum.Entities.Authorization;
using Signum.Entities.Basics;
using Signum.Entities.Chart;
using Signum.Entities.Dashboard;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Toolbar;
using Signum.Entities.UserQueries;
using Signum.Entities.Workflow;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json.Serialization;

namespace Signum.Engine.Toolbar
{

    public static class ToolbarLogic
    {
        public static ResetLazy<Dictionary<Lite<ToolbarEntity>, ToolbarEntity>> Toolbars = null!;
        public static ResetLazy<Dictionary<Lite<ToolbarMenuEntity>, ToolbarMenuEntity>> ToolbarMenus = null!;

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

                UserAssetsImporter.Register<ToolbarEntity>("Toolbar", ToolbarOperation.Save);
                UserAssetsImporter.Register<ToolbarMenuEntity>("ToolbarMenu", ToolbarMenuOperation.Save);

                RegisterDelete<UserQueryEntity>(sb);
                RegisterDelete<UserChartEntity>(sb);
                RegisterDelete<QueryEntity>(sb);
                RegisterDelete<DashboardEntity>(sb);
                RegisterDelete<ToolbarMenuEntity>(sb);
                RegisterDelete<WorkflowEntity>(sb);

                RegisterContentConfig<ToolbarMenuEntity>(
                    lite => true,
                    lite => TranslatedInstanceLogic.TranslatedField(ToolbarMenus.Value.GetOrCreate(lite), a => a.Name));

                RegisterContentConfig<UserQueryEntity>(
                    lite => { var uq = UserQueryLogic.UserQueries.Value.GetOrCreate(lite); return InMemoryFilter(uq) && QueryLogic.Queries.QueryAllowed(uq.Query.ToQueryName(), true); },
                    lite => TranslatedInstanceLogic.TranslatedField(UserQueryLogic.UserQueries.Value.GetOrCreate(lite), a => a.DisplayName));

                RegisterContentConfig<UserChartEntity>(
                    lite => { var uc = UserChartLogic.UserCharts.Value.GetOrCreate(lite); return InMemoryFilter(uc) && QueryLogic.Queries.QueryAllowed(uc.Query.ToQueryName(), true); },
                    lite => TranslatedInstanceLogic.TranslatedField(UserChartLogic.UserCharts.Value.GetOrCreate(lite), a => a.DisplayName));

                RegisterContentConfig<QueryEntity>(
                  lite => IsQueryAllowed(lite),
                  lite => QueryUtils.GetNiceName(QueryLogic.QueryNames.GetOrThrow(lite.ToString()!)));

                RegisterContentConfig<DashboardEntity>(
                  lite => InMemoryFilter(DashboardLogic.Dashboards.Value.GetOrCreate(lite)),
                  lite => TranslatedInstanceLogic.TranslatedField(DashboardLogic.Dashboards.Value.GetOrCreate(lite), a => a.DisplayName));

                RegisterContentConfig<PermissionSymbol>(
                    lite => PermissionAuthLogic.IsAuthorized(SymbolLogic< PermissionSymbol>.ToSymbol(lite.ToString()!)),
                    lite => SymbolLogic<PermissionSymbol>.ToSymbol(lite.ToString()!).NiceToString());

                RegisterContentConfig<WorkflowEntity>(
                  lite => { var wf = WorkflowLogic.WorkflowGraphLazy.Value.GetOrCreate(lite); return InMemoryFilter(wf.Workflow) && wf.IsStartCurrentUser(); },
                    lite => TranslatedInstanceLogic.TranslatedField(WorkflowLogic.WorkflowGraphLazy.Value.GetOrCreate(lite).Workflow, a => a.Name));


                


                //    { typeof(QueryEntity), a => IsQueryAllowed((Lite<QueryEntity>)a) },
                //{ typeof(PermissionSymbol), a => PermissionAuthLogic.IsAuthorized((PermissionSymbol)a.RetrieveAndRemember()) },
                //{ typeof(UserQueryEntity), a => ,
                //{ typeof(UserChartEntity), a => { var uc = UserChartLogic.UserCharts.Value.GetOrCreate((Lite<UserChartEntity>)a); return InMemoryFilter(uc) && QueryLogic.Queries.QueryAllowed(uc.Query.ToQueryName(), true); } },
                //{ typeof(DashboardEntity), a => InMemoryFilter(DashboardLogic.Dashboards.Value.GetOrCreate((Lite<DashboardEntity>)a)) },
                //{ typeof(WorkflowEntity), a => { var wf = WorkflowLogic.WorkflowGraphLazy.Value.GetOrCreate((Lite<WorkflowEntity>)a); return InMemoryFilter(wf.Workflow) && wf.IsStartCurrentUser(); } },


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
                t => AuthLogic.CurrentRoles().Contains(t.Owner) || t.Owner == null);

            sb.Schema.Settings.AssertImplementedBy((ToolbarMenuEntity t) => t.Owner, typeof(RoleEntity));

            TypeConditionLogic.RegisterCompile<ToolbarMenuEntity>(typeCondition,
                t => AuthLogic.CurrentRoles().Contains(t.Owner) || t.Owner == null);
        }

        public static void RegisterDelete<T>(SchemaBuilder sb) where T : Entity
        {
            if (sb.Settings.ImplementedBy((ToolbarEntity tb) => tb.Elements.First().Content, typeof(T)))
            {
                sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
                {
                    Database.MListQuery((ToolbarEntity tb) => tb.Elements).Where(mle => query.Contains((T)mle.Element.Content!.Entity)).UnsafeDeleteMList();
                    return null;
                };

                sb.Schema.Table<T>().PreDeleteSqlSync += arg =>
                {
                    var entity = (T)arg;

                    var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarEntity tb) => tb.Elements, Database.MListQuery((ToolbarEntity tb) => tb.Elements)
                        .Where(mle => mle.Element.Content!.Entity == entity));

                    return parts;
                };
            }

            if (sb.Settings.ImplementedBy((ToolbarMenuEntity tb) => tb.Elements.First().Content, typeof(T)))
            {
                sb.Schema.EntityEvents<T>().PreUnsafeDelete += query =>
                {
                    Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements).Where(mle => query.Contains((T)mle.Element.Content!.Entity)).UnsafeDeleteMList();
                    return null;
                };

                sb.Schema.Table<T>().PreDeleteSqlSync += arg =>
                {
                    var entity = (T)arg;

                    var parts = Administrator.UnsafeDeletePreCommandMList((ToolbarMenuEntity tb) => tb.Elements, Database.MListQuery((ToolbarMenuEntity tb) => tb.Elements)
                        .Where(mle => mle.Element.Content!.Entity == entity));

                    return parts;
                };
            }
        }

        public static void RegisterTranslatableRoutes()
        {
            TranslatedInstanceLogic.AddRoute((ToolbarEntity tb) => tb.Name);
            TranslatedInstanceLogic.AddRoute((ToolbarEntity tb) => tb.Elements[0].Label);
            TranslatedInstanceLogic.AddRoute((ToolbarMenuEntity tm) => tm.Name);
            TranslatedInstanceLogic.AddRoute((ToolbarMenuEntity tb) => tb.Elements[0].Label);
        }

        public static ToolbarEntity? GetCurrent(ToolbarLocation location)
        {
            var isAllowed = Schema.Current.GetInMemoryFilter<ToolbarEntity>(userInterface: false);

            var result = Toolbars.Value.Values
                .Where(t => isAllowed(t) && t.Location == location)
                .OrderByDescending(a => a.Priority)
                .FirstOrDefault();

            return result;
        }

        public static ToolbarResponse? GetCurrentToolbarResponse(ToolbarLocation location)
        {
            var curr = GetCurrent(location);

            if (curr == null)
                return null;

            var responses = ToResponseList(TranslatedInstanceLogic.TranslatedMList(curr, c => c.Elements).ToList());

            if (responses.Count == 0)
                return null;

            return new ToolbarResponse
            {
                type = ToolbarElementType.Header,
                content = curr.ToLite(),
                label = TranslatedInstanceLogic.TranslatedField(curr, a => a.Name),
                elements = responses,
            };
        }

        private static List<ToolbarResponse> ToResponseList(List<TranslatableElement<ToolbarElementEmbedded>> elements)
        {
            var result = elements.Select(a => ToResponse(a)).NotNull().ToList();

            retry:
            var extraDividers = result.Where((a, i) => a.type == ToolbarElementType.Divider && (
                i == 0 ||
                result[i - 1].type == ToolbarElementType.Divider ||
                i == result.Count
            )).ToList();
            result.RemoveAll(extraDividers.Contains);
            var extraHeaders = result.Where((a, i) => IsPureHeader(a) && (
                i == result.Count - 1 ||
                IsPureHeader(result[i + 1]) ||
                result[i + 1].type == ToolbarElementType.Divider ||
                result[i + 1].type == ToolbarElementType.Header && result[i + 1].content is Lite<ToolbarMenuEntity>
            )).ToList();
            result.RemoveAll(extraHeaders.Contains);

            if (extraDividers.Any() || extraHeaders.Any())
                goto retry;

            return result;
        }

        private static bool IsPureHeader(ToolbarResponse tr)
        {
            return tr.type == ToolbarElementType.Header && tr.content == null && string.IsNullOrEmpty(tr.url);
        }

        private static ToolbarResponse? ToResponse(TranslatableElement<ToolbarElementEmbedded> transElement)
        {
            var element = transElement.Value;

            var config = element.Content == null ? null : ContentCondigDictionary.GetOrThrow(element.Content.EntityType);

            if(config != null)
            {
                if (!config.IsAuhorized(element.Content!))
                    return null;
            }

            var result = new ToolbarResponse
            {
                type = element.Type,
                content = element.Content,
                url = element.Url,
                label = transElement.TranslatedElement(a => a.Label!).DefaultText(null) ?? config?.DefaultLabel(element.Content!),
                iconName = element.IconName,
                iconColor = element.IconColor,
                autoRefreshPeriod = element.AutoRefreshPeriod,
                openInPopup = element.OpenInPopup,
            };

            if (element.Content is Lite<ToolbarMenuEntity>)
            {
                var tme = ToolbarMenus.Value.GetOrThrow((Lite<ToolbarMenuEntity>)element.Content);
                result.elements = ToResponseList(TranslatedInstanceLogic.TranslatedMList(tme, t => t.Elements).ToList());
                if (result.elements.Count == 0)
                    return null;
            }

            return result;
        }

        public static void RegisterContentConfig<T>(Func<Lite<T>, bool> isAuthorized, Func<Lite<T>, string> defaultLabel) 
            where T : Entity
        {
            ContentCondigDictionary.Add(typeof(T), new ContentConfig<T>(isAuthorized, defaultLabel));
        }

        static Dictionary<Type, IContentConfig> ContentCondigDictionary = new Dictionary<Type, IContentConfig>();

        public interface IContentConfig
        {
            bool IsAuhorized(Lite<Entity> lite);
            string DefaultLabel(Lite<Entity> lite);
        }

        public class ContentConfig<T> : IContentConfig where T : Entity
        {
            public Func<Lite<T>, bool> IsAuthorized;
            public Func<Lite<T>, string> DefaultLabel;

            public ContentConfig(Func<Lite<T>, bool> isAuthorized, Func<Lite<T>, string> defaultLabel)
            {
                IsAuthorized = isAuthorized;
                DefaultLabel = defaultLabel;
            }

            bool IContentConfig.IsAuhorized(Lite<Entity> lite) => IsAuthorized((Lite<T>)lite);
            string IContentConfig.DefaultLabel(Lite<Entity> lite) => DefaultLabel((Lite<T>)lite);
        }

        static bool IsQueryAllowed(Lite<QueryEntity> query)
        {
            try
            {
                return QueryLogic.Queries.QueryAllowed(QueryLogic.QueryNames.GetOrThrow(query.ToString()!), true);
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
            if (Schema.Current.IsAllowed(typeof(T), inUserInterface: false) != null)
                return false;

            var isAllowed = Schema.Current.GetInMemoryFilter<T>(userInterface: false);
            return isAllowed(entity);
        }

    }

    public class ToolbarResponse
    {
        public ToolbarElementType type;
        public string? label;
        public Lite<Entity>? content;
        public string? url;
        public List<ToolbarResponse>? elements;
        public string? iconName;
        public string? iconColor;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
        public int? autoRefreshPeriod;

        [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingDefault)]
        public bool openInPopup;

        public override string ToString() => $"{type} {label} {content} {url}";
    }
}
