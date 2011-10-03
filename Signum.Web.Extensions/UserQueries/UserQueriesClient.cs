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
using Signum.Web.Extensions.Properties;
using Signum.Engine.Basics;
using Signum.Entities.UserQueries;
using Signum.Engine.UserQueries;

namespace Signum.Web.UserQueries
{
    public static class UserQueriesClient
    {
        public const string QueryKey = "QueryKey";

        public static string ViewPrefix = "~/UserQueries/Views/{0}.cshtml";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(UserQueriesClient));

                RouteTable.Routes.MapRoute(null, "UQ/{webQueryName}/{lite}",
                    new { controller = "UserQueries", action = "View" });

                Mapping<QueryToken> qtMapping = ctx=>
                {
                    string tokenStr = "";
                    foreach (string key in ctx.Parent.Inputs.Keys.Where(k => k.Contains("ddlTokens")).Order())
                        tokenStr += ctx.Parent.Inputs[key] + ".";
                    while (tokenStr.EndsWith("."))
                        tokenStr = tokenStr.Substring(0, tokenStr.Length - 1);

                    string queryKey = ((MappingContext<UserQueryDN>)ctx.Parent.Parent.Parent.Parent).Inputs[TypeContextUtilities.Compose("Query", "Key")];
                    object queryName = QueryLogic.ToQueryName(queryKey);
                    QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                    return QueryUtils.Parse(tokenStr, qd);
                };

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<UserQueryDN>(EntityType.NotSaving) { PartialViewName = e => ViewPrefix.Formato("UserQuery") },
                    
                    new EmbeddedEntitySettings<QueryFilterDN>()
                    { 
                        PartialViewName = e => ViewPrefix.Formato("QueryFilter"), 
                        MappingDefault = new EntityMapping<QueryFilterDN>(false)
                            .CreateProperty(a=>a.Operation)
                            .CreateProperty(a=>a.ValueString)
                            .SetProperty(a=>a.Token, qtMapping)
                    },

                    new EmbeddedEntitySettings<QueryColumnDN>()
                    { 
                        PartialViewName = e => ViewPrefix.Formato("QueryColumn"), 
                        MappingDefault = new EntityMapping<QueryColumnDN>(false)
                            .CreateProperty(a=>a.DisplayName)
                            .SetProperty(a=>a.Token, qtMapping)
                    },

                    new EmbeddedEntitySettings<QueryOrderDN>()
                    { 
                        PartialViewName = e => ViewPrefix.Formato("QueryOrder"), 
                        MappingDefault = new EntityMapping<QueryOrderDN>(false)
                            .CreateProperty(a=>a.OrderType)
                            .SetProperty(a=>a.Token, qtMapping)
                    },
                });
                

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryDN)))
                    Navigator.Manager.EntitySettings.Add(typeof(QueryDN), new EntitySettings<QueryDN>(EntityType.Default));

                ButtonBarQueryHelper.GetButtonBarForQueryName += new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName);

                ButtonBarEntityHelper.RegisterEntityButtons<UserQueryDN>((ctx, entity) =>
                {
                    var buttons = new List<ToolBarButton>
                    {
                        new ToolBarButton 
                        { 
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "ebUserQuerySave"),
                            Text = Signum.Web.Properties.Resources.Save, 
                            OnClick = JsValidator.EntityIsValid(ctx.Prefix, 
                                Js.Submit(RouteHelper.New().Action<UserQueriesController>(uqc => uqc.Save()))).ToJS()
                        }
                    };

                    if (!entity.IsNew)
                    {
                        buttons.Add(new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(ctx.Prefix, "ebUserQueryDelete"),
                            Text = Resources.Delete,
                            OnClick = Js.Confirm(Resources.AreYouSureOfDeletingQuery0.Formato(entity.DisplayName), 
                                                Js.Submit(RouteHelper.New().Action<UserQueriesController>(uqc => uqc.Delete(entity.ToLite())))).ToJS()
                        });
                    }

                    return buttons.ToArray();
                });
            }
        }
        
        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            if (prefix.HasText())
                return null;

            var items = new List<ToolBarButton>();

            Lite<UserQueryDN> currentUserQuery = null;
            string url = (controllerContext.RouteData.Route as Route).TryCC(r => r.Url);
            if (url.HasText() && url.Contains("UQ"))
                currentUserQuery = new Lite<UserQueryDN>(int.Parse(controllerContext.RouteData.Values["lite"].ToString()));

            foreach (var uq in UserQueryLogic.GetUserQueries(queryName))
            {
                string uqName = uq.InDB().Select(q => q.DisplayName).SingleOrDefaultEx();
                items.Add(new ToolBarButton
                {
                    Text = uqName,
                    AltText = uqName,
                    Href = RouteHelper.New().Action<UserQueriesController>(uqc => uqc.View(uq)),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass + (currentUserQuery.Is(uq) ? " sf-userquery-selected" : "")
                });
            }

            if (items.Count > 0)
                items.Add(new ToolBarSeparator());

            string uqNewText = Resources.UserQueries_CreateNew;
            items.Add(new ToolBarButton
            {
                Id = TypeContextUtilities.Compose(prefix, "qbUserQueryNew"),
                AltText = uqNewText,
                Text = uqNewText,
                OnClick = Js.SubmitOnly(RouteHelper.New().Action("Create", "UserQueries"), new JsFindNavigator(prefix).requestData()).ToJS(),
                DivCssClass = ToolBarButton.DefaultQueryCssClass
            });

            if (currentUserQuery != null)
            {
                string uqEditText = Resources.UserQueries_Edit;
                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbUserQueryEdit"),
                    AltText = uqEditText,
                    Text = uqEditText,
                    Href = Navigator.ViewRoute(currentUserQuery),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });
            }

            string uqUserQueriesText = Resources.UserQueries_UserQueries;
            return new ToolBarButton[]
            {
                new ToolBarMenu
                { 
                    Id = TypeContextUtilities.Compose(prefix, "tmUserQueries"),
                    AltText = uqUserQueriesText,
                    Text = uqUserQueriesText,
                    DivCssClass = ToolBarButton.DefaultQueryCssClass,
                    Items = items
                }
            };
        }

        public static void ApplyUserQuery(this FindOptions findOptions, UserQueryDN userQuery)
        {
            findOptions.FilterOptions.RemoveAll(fo => !fo.Frozen);
            findOptions.FilterOptions.AddRange(userQuery.Filters.Select(qf => new FilterOption
            {
                Token = qf.Token,
                ColumnName = qf.TokenString,
                Operation = qf.Operation,
                Value = qf.Value
            }));

            findOptions.ColumnOptionsMode = userQuery.ColumnsMode;

            findOptions.ColumnOptions.Clear();
            findOptions.ColumnOptions.AddRange(userQuery.Columns.Select(qc => new ColumnOption
            {
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

            findOptions.Top = userQuery.ElementsPerPage;
            findOptions.TopEmpty = userQuery.ElementsPerPage == null;
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
