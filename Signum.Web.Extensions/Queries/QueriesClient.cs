#region usings
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Web.Mvc;
using Signum.Utilities;
using System.Web.UI;
using System.Web.Routing;
using Signum.Entities.Reports;
using Signum.Entities.Basics;
using Signum.Web.Queries.Models;
using Signum.Entities.DynamicQuery;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Reports;
using Signum.Engine;
using Signum.Web.Extensions.Properties;
#endregion

namespace Signum.Web.Queries
{
    public class UserQueriesClient
    {
        public static Func<object, int, string> UserQueryFindRoute = (queryName, userQueryId) => 
            "UQ/{0}/{1}".Formato(Navigator.Manager.QuerySettings[queryName].UrlName, userQueryId);

        public static string ViewPrefix = "queries/Views/";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(UserQueriesClient), "/queries/", "Signum.Web.Extensions.Queries."));

                RouteTable.Routes.InsertRouteAt0("queries/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "queries" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                RouteTable.Routes.InsertRouteAt0("UQ/{queryUrlName}/{id}",
                    new { controller = "Queries", action = "ViewUserQuery" });

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<UserQueryModel>(EntityType.NotSaving)
                    { 
                        PartialViewName = e => ViewPrefix + "UserQuery"
                    },
                    
                    new EntitySettings<QueryFilterModel>(EntityType.Default)
                    { 
                        PartialViewName = e => ViewPrefix + "QueryFilter", 
                        MappingDefault = new EntityMapping<QueryFilterModel>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryModel>)ctx.Parent.Parent.Parent).Value.Query.Key;
                                object queryName = Navigator.Manager.QuerySettings.Keys.First(key => QueryUtils.GetQueryName(key) == queryKey);
                                
                                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                                var result = new QueryFilterModel(
                                    new QueryFilterDN 
                                    { 
                                        Token = QueryUtils.ParseFilter(tokenStr, qd), 
                                        Operation = EnumExtensions.ToEnum<FilterOperation>(ctx.Inputs["Operation"]),
                                        ValueString = ctx.Inputs["ValueString"]
                                    }, 
                                    Navigator.Manager.QuerySettings[queryName].UrlName);
                                    
                                ctx.Value = result;
                                return result;
                            }
                        }
                    },

                    new EntitySettings<QueryColumnModel>(EntityType.Default)
                    { 
                        PartialViewName = e => ViewPrefix + "QueryColumn", 
                        MappingDefault = new EntityMapping<QueryColumnModel>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryModel>)ctx.Parent.Parent.Parent).Value.Query.Key;
                                object queryName = Navigator.Manager.QuerySettings.Keys.First(key => QueryUtils.GetQueryName(key) == queryKey);
                                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                                var result = new QueryColumnModel(
                                    new QueryColumnDN 
                                    { 
                                        Token = QueryUtils.ParseColumn(tokenStr, qd),
                                        DisplayName = ctx.Inputs["DisplayName"]
                                    }, 
                                    Navigator.Manager.QuerySettings[queryName].UrlName);
                                    
                                ctx.Value = result;
                                return result;
                            }
                        }
                    },

                    new EntitySettings<QueryOrderModel>(EntityType.Default)
                    { 
                        PartialViewName = e => ViewPrefix + "QueryOrder", 
                        MappingDefault = new EntityMapping<QueryOrderModel>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryModel>)ctx.Parent.Parent.Parent).Value.Query.Key;
                                object queryName = Navigator.Manager.QuerySettings.Keys.First(key => QueryUtils.GetQueryName(key) == queryKey);
                                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                                QueryToken token = QueryUtils.ParseOrder(tokenStr, qd);

                                var result = new QueryOrderModel(
                                    new QueryOrderDN 
                                    { 
                                        Token = token,
                                        OrderType = (OrderType)Enum.Parse(typeof(OrderType), ctx.Inputs["OrderType"]),
                                    }, 
                                    Navigator.Manager.QuerySettings[queryName].UrlName);
                                    
                                ctx.Value = result;
                                return result;
                            }
                        }
                    },

                    new EntitySettings<QueryTokenModel>(EntityType.Default) { PartialViewName = e => ViewPrefix + "QueryToken" },
                    new EntitySettings<UserQueryDN>(EntityType.NotSaving)
                });

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryDN)))
                    Navigator.Manager.EntitySettings.Add(typeof(QueryDN), new EntitySettings<QueryDN>(EntityType.Default));

                
                Navigator.RegisterTypeName<QueryFilterDN>();
                Navigator.RegisterTypeName<QueryColumnDN>();
                Navigator.RegisterTypeName<QueryTokenDN>();

                ButtonBarQueryHelper.GetButtonBarForQueryName += new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName);

                ButtonBarEntityHelper.RegisterEntityButtons<UserQueryModel>((controllerContext, entity, partialViewName, prefix) =>
                {
                    return new ToolBarButton[]
                    {
                        new ToolBarButton 
                        { 
                            Text = Signum.Web.Properties.Resources.Save, 
                            OnClick = JsValidator.EntityIsValid(prefix, Js.Submit("Queries/SaveUserQuery")).ToJS()
                        },
                        new ToolBarButton
                        {
                            Text = Resources.Delete,
                            OnClick = Js.Confirm(Resources.AreYouSureOfDeletingQuery0.Formato(entity.DisplayName), 
                                                Js.SubmitOnly("Queries/DeleteUserQuery", "{{id:{0}}}".Formato(entity.IdUserQuery))).ToJS(),
                            Enabled = entity.IdUserQuery != null
                        }
                    };
                });
            }
        }

        private static string ExtractQueryTokenString(MappingContext ctx)
        {
            string tokenStr = "";
            foreach (string key in ctx.Inputs.Keys.Where(k => k.Contains("ddlTokens")).Order())
                tokenStr += ctx.Inputs[key] + ".";
            while (tokenStr.EndsWith("."))
                tokenStr = tokenStr.Substring(0, tokenStr.Length - 1);
            return tokenStr;
        }

        static ToolBarButton[] ButtonBarQueryHelper_GetButtonBarForQueryName(ControllerContext controllerContext, object queryName, Type entityType, string prefix)
        {
            if (prefix.HasText())
                return null;

            var items = new List<ToolBarButton>();

            int idCurrentUserQuery = 0;
            string url = (controllerContext.RouteData.Route as Route).TryCC(r => r.Url);
            if (url.HasText() && url.Contains("UQ"))
                idCurrentUserQuery = int.Parse(controllerContext.RouteData.Values["id"].ToString());

            foreach (var uq in UserQueryLogic.GetUserQueries(queryName))
            {
                items.Add(new ToolBarButton
                {
                    Text = uq.Retrieve().DisplayName,
                    OnClick = Js.Submit(UserQueryFindRoute(queryName, uq.Id)).ToJS(),
                    DivCssClass = idCurrentUserQuery == uq.Id ? "SelectedUserQuery" : ""
                });
            }

            if (items.Count > 0)
                items.Add(new ToolBarSeparator());

            items.Add(new ToolBarButton
            {
                AltText = Signum.Web.Properties.Resources.New,
                Text = Signum.Web.Properties.Resources.New,
                ImgSrc = "signum/images/lineButtons.gif",
                OnClick = Js.SubmitOnly("Queries/CreateUserQuery", JsFindNavigator.JsRequestData(new JsFindOptions{Prefix = prefix})).ToJS()
            });

            if (idCurrentUserQuery > 0)
            {
                items.Add(new ToolBarButton
                {
                    AltText = "Edit",
                    Text = "Edit",
                    OnClick = Js.SubmitOnly("Queries/EditUserQuery", "{{id:{0}}}".Formato(idCurrentUserQuery)).ToJS()
                });
            }

            return new ToolBarButton[]
            {
                new ToolBarMenu
                { 
                    AltText = "User Queries", 
                    Text = "User Queries",
                    DivCssClass = ToolBarButton.DefaultQueryCssClass,
                    Items = items
                }
            };
        }
    }
}
