using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Mvc;
using Signum.Utilities;
using System.Web.UI;
using System.Web.Routing;
using Signum.Entities;
using Signum.Entities.Reports;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Reports;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;
using Signum.Engine.Authorization;
using Signum.Entities.Authorization;
using Signum.Web.Operations;
using Signum.Web.Basic;
using Signum.Web.Extensions.UserQueries;

namespace Signum.Web.UserQueries
{
    public static class UserQueriesClient
    {
        public const string QueryKey = "QueryKey";

        public static string ViewPrefix = "~/UserQueries/Views/{0}.cshtml";

        public static Mapping<QueryToken> QueryTokenMapping = ctx =>
        {
            string tokenStr = "";
            foreach (string key in ctx.Parent.Inputs.Keys.Where(k => k.Contains("ddlTokens")).OrderBy())
                tokenStr += ctx.Parent.Inputs[key] + ".";
            while (tokenStr.EndsWith("."))
                tokenStr = tokenStr.Substring(0, tokenStr.Length - 1);

            string queryKey = ctx.Parent.Parent.Parent.Parent.Inputs[TypeContextUtilities.Compose("Query", "Key")];
            object queryName = QueryLogic.ToQueryName(queryKey);
            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
            return QueryUtils.Parse(tokenStr, qd, canAggregate: false);
        };

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                QueryClient.Start();

                Navigator.RegisterArea(typeof(UserQueriesClient));

                UserAssetsClient.Start();
                UserAssetsClient.RegisterExportAssertLink<UserQueryDN>();

                var defaultScripts = Navigator.Manager.DefaultScripts();
                defaultScripts.Add("~/UserQueries/Scripts/SF_UserQuery.js");
                Navigator.Manager.DefaultScripts = () => defaultScripts;

                RouteTable.Routes.MapRoute(null, "UQ/{webQueryName}/{lite}",
                    new { controller = "UserQueries", action = "View" });

                Navigator.AddSettings(new List<EntitySettings>
                {
                    new EntitySettings<UserQueryDN> { PartialViewName = e => ViewPrefix.Formato("UserQuery") },
                    
                    new EmbeddedEntitySettings<QueryFilterDN>
                    { 
                        PartialViewName = e => ViewPrefix.Formato("QueryFilter"), 
                        MappingDefault = new EntityMapping<QueryFilterDN>(false)
                            .CreateProperty(a=>a.Operation)
                            .CreateProperty(a=>a.ValueString)
                            .SetProperty(a=>a.TryToken, QueryTokenMapping)
                    },

                    new EmbeddedEntitySettings<QueryColumnDN>
                    { 
                        PartialViewName = e => ViewPrefix.Formato("QueryColumn"), 
                        MappingDefault = new EntityMapping<QueryColumnDN>(false)
                            .CreateProperty(a=>a.DisplayName)
                            .SetProperty(a=>a.TryToken, QueryTokenMapping)
                    },

                    new EmbeddedEntitySettings<QueryOrderDN>
                    { 
                        PartialViewName = e => ViewPrefix.Formato("QueryOrder"), 
                        MappingDefault = new EntityMapping<QueryOrderDN>(false)
                            .CreateProperty(a=>a.OrderType)
                            .SetProperty(a=>a.TryToken, QueryTokenMapping)
                    },
                });

                ButtonBarQueryHelper.RegisterGlobalButtons(ButtonBarQueryHelper_GetButtonBarForQueryName);

                OperationsClient.AddSettings(new List<OperationSettings>
                {
                    new EntityOperationSettings(UserQueryOperation.Save)
                    {
                        OnClick = ctx => new JsOperationExecutor(ctx.Options("Save", "UserQueries"))
                            .ajax(ctx.Prefix, JsOpSuccess.DefaultDispatcher)
                    },
                    new EntityOperationSettings(UserQueryOperation.Delete)
                    {
                        OnClick = ctx => new JsOperationDelete(ctx.Options("Delete", "UserQueries"))
                            .confirmAndAjax(ctx.Entity)
                    }
                });

                LinksClient.RegisterEntityLinks<IdentifiableEntity>((entity, ctrl) =>
                {
                    if (!UserQueryPermission.ViewUserQuery.IsAuthorized())
                        return null;

                    return UserQueryLogic.GetUserQueriesEntity(entity.EntityType)
                        .Select(cp => new UserQueryQuickLink(cp, entity)).ToArray();
                });
            }
        }

        class UserQueryQuickLink : QuickLink
        {
            Lite<UserQueryDN> userQuery;
            Lite<IdentifiableEntity> entity;

            public UserQueryQuickLink(Lite<UserQueryDN> userQuery, Lite<IdentifiableEntity> entity)
            {
                this.Text = userQuery.ToString();
                this.userQuery = userQuery;
                this.entity = entity;
                this.IsVisible = true;
            }

            public override MvcHtmlString Execute()
            {
                return new HtmlTag("a").Attr("href", RouteHelper.New().Action((UserQueriesController c) => c.View(userQuery, null, entity))).SetInnerText(Text);
            }
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(QueryButtonContext ctx)
        {
            if (ctx.Prefix.HasText())
                return null;

            if (!Navigator.IsNavigable(typeof(UserQueryDN), isSearchEntity: true))
                return null;

            var items = new List<ToolBarButton>();

            Lite<UserQueryDN> currentUserQuery = null;
            string url = (ctx.ControllerContext.RouteData.Route as Route).TryCC(r => r.Url);
            if (url.HasText() && url.Contains("UQ"))
                currentUserQuery = Lite.Create<UserQueryDN>(int.Parse(ctx.ControllerContext.RouteData.Values["lite"].ToString()));

            foreach (var uq in UserQueryLogic.GetUserQueries(ctx.QueryName))
            {
                items.Add(new ToolBarButton
                {
                    Text = uq.ToString(),
                    AltText = uq.ToString(),
                    Href = RouteHelper.New().Action<UserQueriesController>(uqc => uqc.View(uq, null, null)), 
                    DivCssClass = ToolBarButton.DefaultQueryCssClass + " sf-userquery" + (currentUserQuery.Is(uq) ? " sf-userquery-selected" : "")
                });
            }

            if (items.Count > 0)
                items.Add(new ToolBarSeparator());

            if (Navigator.IsCreable(typeof(UserQueryDN), isSearchEntity:true))
            {
                string uqNewText = UserQueryMessage.UserQueries_CreateNew.NiceToString();
                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(ctx.Prefix, "qbUserQueryNew"),
                    AltText = uqNewText,
                    Text = uqNewText,
                    OnClick = Js.SubmitOnly(RouteHelper.New().Action("Create", "UserQueries"), JsFindNavigator.GetFor(ctx.Prefix).requestData()).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });
            }

            if (currentUserQuery != null && currentUserQuery.IsAllowedFor(TypeAllowedBasic.Modify, inUserInterface: true))
            {
                string uqEditText = UserQueryMessage.UserQueries_Edit.NiceToString();
                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(ctx.Prefix, "qbUserQueryEdit"),
                    AltText = uqEditText,
                    Text = uqEditText,
                    Href = Navigator.NavigateRoute(currentUserQuery),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });
            }

            string uqUserQueriesText = UserQueryMessage.UserQueries_UserQueries.NiceToString();
            return new ToolBarButton[]
            {
                new ToolBarMenu
                { 
                    Id = TypeContextUtilities.Compose(ctx.Prefix, "tmUserQueries"),
                    AltText = uqUserQueriesText,
                    Text = uqUserQueriesText,
                    DivCssClass = ToolBarButton.DefaultQueryCssClass,
                    Items = items
                }
            };
        }

        public static void ApplyUserQuery(this FindOptions findOptions, UserQueryDN userQuery)
        {
            if (!userQuery.WithoutFilters)
            {
                findOptions.FilterOptions.RemoveAll(fo => !fo.Frozen);

                findOptions.FilterOptions.AddRange(userQuery.Filters.Select(qf => new FilterOption
                {
                    Token = qf.Token,
                    ColumnName = qf.TokenString,
                    Operation = qf.Operation,
                    Value = qf.Value
                }));
            }

            findOptions.ColumnOptionsMode = userQuery.ColumnsMode;

            findOptions.ColumnOptions.Clear();
            findOptions.ColumnOptions.AddRange(userQuery.Columns.Select(qc => new ColumnOption
            {
                Token = qc.Token,
                ColumnName = qc.TokenString,                
                DisplayName = qc.DisplayName,
            }));

            findOptions.OrderOptions.Clear();
            findOptions.OrderOptions.AddRange(userQuery.Orders.Select(qo => new OrderOption
            {
                Token = qo.Token,
                ColumnName = qo.TokenString,
                OrderType = qo.OrderType
            }));

            findOptions.ElementsPerPage = userQuery.ElementsPerPage;
        }

        public static FindOptions ToFindOptions(this UserQueryDN userQuery)
        {
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);

            var result = new FindOptions(queryName);
            result.ApplyUserQuery(userQuery);
            return result;
        }
    }
}
