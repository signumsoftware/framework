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

namespace Signum.Web.Queries
{
    public static class UserQueriesClient
    {
        public static Func<object, int, string> UserQueryFindRoute = (queryName, userQueryId) => 
            "UQ/{0}/{1}".Formato(Navigator.Manager.QuerySettings[queryName].UrlName, userQueryId);

        public static string ViewPrefix = "queries/Views/";
        public const string QueryKey = "QueryKey";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                AssemblyResourceManager.RegisterAreaResources(
                    new AssemblyResourceStore(typeof(UserQueriesClient), "~/queries/", "Signum.Web.Extensions.Queries."));

                RouteTable.Routes.InsertRouteAt0("queries/{resourcesFolder}/{*resourceName}",
                    new { controller = "Resources", action = "Index", area = "queries" },
                    new { resourcesFolder = new InArray(new string[] { "Scripts", "Content", "Images" }) });

                RouteTable.Routes.InsertRouteAt0("UQ/{queryUrlName}/{id}",
                    new { controller = "Queries", action = "ViewUserQuery" });

                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<UserQueryDN>(EntityType.NotSaving) { PartialViewName = e => ViewPrefix + "UserQuery" },
                    
                    new EntitySettings<QueryFilterDN>(EntityType.Default)
                    { 
                        PartialViewName = e => ViewPrefix + "QueryFilterIU", 
                        MappingDefault = new EntityMapping<QueryFilterDN>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryDN>)ctx.Parent.Parent.Parent).Inputs[TypeContextUtilities.Compose("Query", "Key")];
                                object queryName = Navigator.Manager.QuerySettings.Keys.First(key => QueryUtils.GetQueryName(key) == queryKey);
                                
                                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                                var result = new QueryFilterDN
                                    {
                                        Token = QueryUtils.Parse(tokenStr, qd), 
                                        Operation = EnumExtensions.ToEnum<FilterOperation>(ctx.Inputs["Operation"]),
                                        ValueString = ctx.Inputs["ValueString"]
                                    };
                                    
                                ctx.Value = result;
                                return result;
                            }
                        }
                    },

                    new EntitySettings<QueryColumnDN>(EntityType.Default)
                    { 
                        PartialViewName = e => ViewPrefix + "QueryColumn", 
                        MappingDefault = new EntityMapping<QueryColumnDN>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryDN>)ctx.Parent.Parent.Parent).Inputs[TypeContextUtilities.Compose("Query", "Key")];
                                object queryName = Navigator.Manager.QuerySettings.Keys.First(key => QueryUtils.GetQueryName(key) == queryKey);
                                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);

                                var result = new QueryColumnDN 
                                    { 
                                        Token = QueryUtils.Parse(tokenStr, qd),
                                        DisplayName = ctx.Inputs["DisplayName"]
                                    }; 
                                    
                                ctx.Value = result;
                                return result;
                            }
                        }
                    },

                    new EntitySettings<QueryOrderDN>(EntityType.Default)
                    { 
                        PartialViewName = e => ViewPrefix + "QueryOrder", 
                        MappingDefault = new EntityMapping<QueryOrderDN>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryDN>)ctx.Parent.Parent.Parent).Inputs[TypeContextUtilities.Compose("Query", "Key")];
                                object queryName = Navigator.Manager.QuerySettings.Keys.First(key => QueryUtils.GetQueryName(key) == queryKey);
                                QueryDescription qd = DynamicQueryManager.Current.QueryDescription(queryName);
                                QueryToken token = QueryUtils.Parse(tokenStr, qd);

                                var result = new QueryOrderDN 
                                    { 
                                        Token = token,
                                        OrderType = (OrderType)Enum.Parse(typeof(OrderType), ctx.Inputs["OrderType"]),
                                    };
                                    
                                ctx.Value = result;
                                return result;
                            }
                        }
                    },
                });

                if (!Navigator.Manager.EntitySettings.ContainsKey(typeof(QueryDN)))
                    Navigator.Manager.EntitySettings.Add(typeof(QueryDN), new EntitySettings<QueryDN>(EntityType.Default));

                Navigator.RegisterTypeName<QueryTokenDN>();

                ButtonBarQueryHelper.GetButtonBarForQueryName += new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName);

                ButtonBarEntityHelper.RegisterEntityButtons<UserQueryDN>((controllerContext, entity, partialViewName, prefix) =>
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
                                                Js.SubmitOnly("Queries/DeleteUserQuery", "{{id:{0}}}".Formato(entity.IdOrNull.TryToString()))).ToJS(),
                            Enabled = !entity.IsNew
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
                    Text = uq.InDB().Select(q => q.DisplayName).SingleOrDefault(),
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
                    Id = TypeContextUtilities.Compose(prefix, "uqmenu"),
                    AltText = "User Queries", 
                    Text = "User Queries",
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

            findOptions.Top = userQuery.MaxItems;
        }

        public static FindOptions ToFindOptions(this UserQueryDN userQuery)
        {
            object queryName = Navigator.ResolveQueryFromKey(userQuery.Query.Key); //.ToStr); ;

            var result = new FindOptions(queryName);
            result.ApplyUserQuery(userQuery);
            return result;
        }

        public static UserQueryDN ToUserQuery(this FindOptions findOptions, Lite<IdentifiableEntity> related)
        {

            QueryDescription qd = DynamicQueryManager.Current.QueryDescription(findOptions.QueryName);
            var tuple = QueryColumnDN.SmartColumns(findOptions.ColumnOptions.Select(co => co.ToColumn(qd)).ToList(), qd.Columns);

            return new UserQueryDN
            {
                Related = related,
                Query = QueryLogic.RetrieveOrGenerateQuery(findOptions.QueryName),
                Filters = findOptions.FilterOptions.Where(fo => !fo.Frozen).Select(fo => new QueryFilterDN { Token = fo.Token, Operation = fo.Operation, Value = fo.Value, ValueString = FilterValueConverter.ToString(fo.Value, fo.Token.Type) }).ToMList(),
                ColumnsMode = tuple.Item1,
                Columns = tuple.Item2,
                Orders = findOptions.OrderOptions.Select(oo => new QueryOrderDN { Token = oo.Token, OrderType = oo.OrderType }).ToMList(),
                MaxItems = findOptions.Top
            };
        }
    }
}
