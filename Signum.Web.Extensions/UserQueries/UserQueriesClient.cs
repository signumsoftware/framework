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
        public const string QueryKey = "QueryKey";

        public static void Start()
        {
            if (Navigator.Manager.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                Navigator.RegisterArea(typeof(UserQueriesClient));

                RouteTable.Routes.MapRoute(null, "UQ/{webQueryName}/{id}",
                    new { controller = "Queries", action = "ViewUserQuery" });

                string viewPrefix = "~/UserQueries/Views/{0}.cshtml";
                Navigator.AddSettings(new List<EntitySettings>{
                    new EntitySettings<UserQueryDN>(EntityType.NotSaving) { PartialViewName = e => viewPrefix.Formato("UserQuery") },
                    
                    new EmbeddedEntitySettings<QueryFilterDN>()
                    { 
                        PartialViewName = e => viewPrefix.Formato("QueryFilter"), 
                        MappingDefault = new EntityMapping<QueryFilterDN>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryDN>)ctx.Parent.Parent.Parent).Inputs[TypeContextUtilities.Compose("Query", "Key")];
                                object queryName = QueryLogic.ToQueryName(queryKey);
                                
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

                    new EmbeddedEntitySettings<QueryColumnDN>()
                    { 
                        PartialViewName = e => viewPrefix.Formato("QueryColumn"), 
                        MappingDefault = new EntityMapping<QueryColumnDN>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryDN>)ctx.Parent.Parent.Parent).Inputs[TypeContextUtilities.Compose("Query", "Key")];
                                object queryName = QueryLogic.ToQueryName(queryKey);
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

                    new EmbeddedEntitySettings<QueryOrderDN>()
                    { 
                        PartialViewName = e => viewPrefix.Formato("QueryOrder"), 
                        MappingDefault = new EntityMapping<QueryOrderDN>(true)
                        {
                            GetValue = ctx => 
                            {
                                string tokenStr = ExtractQueryTokenString(ctx);
            
                                string queryKey = ((MappingContext<UserQueryDN>)ctx.Parent.Parent.Parent).Inputs[TypeContextUtilities.Compose("Query", "Key")];
                                object queryName = QueryLogic.ToQueryName(queryKey);
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

                ButtonBarQueryHelper.GetButtonBarForQueryName += new GetToolBarButtonQueryDelegate(ButtonBarQueryHelper_GetButtonBarForQueryName);

                ButtonBarEntityHelper.RegisterEntityButtons<UserQueryDN>((controllerContext, entity, partialViewName, prefix) =>
                {
                    return new ToolBarButton[]
                    {
                        new ToolBarButton 
                        { 
                            Id = TypeContextUtilities.Compose(prefix, "ebUserQuerySave"),
                            Text = Signum.Web.Properties.Resources.Save, 
                            OnClick = JsValidator.EntityIsValid(prefix, Js.Submit(RouteHelper.New().Action("SaveUserQuery", "Queries"))).ToJS()
                        },
                        new ToolBarButton
                        {
                            Id = TypeContextUtilities.Compose(prefix, "ebUserQueryDelete"),
                            Text = Resources.Delete,
                            Enabled = !entity.IsNew,
                            OnClick = Js.Confirm(Resources.AreYouSureOfDeletingQuery0.Formato(entity.DisplayName), 
                                                Js.SubmitOnly(RouteHelper.New().Action("DeleteUserQuery", "Queries"), "{{id:{0}}}".Formato(entity.IdOrNull.TryToString()))).ToJS(),
                            
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
                string uqName = uq.InDB().Select(q => q.DisplayName).SingleOrDefault();
                items.Add(new ToolBarButton
                {
                    Text = uqName,
                    AltText = uqName,
                    OnClick = Js.Submit(RouteHelper.New().Action("ViewUserQuery", "Queries", new { id = uq.Id })).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass + (idCurrentUserQuery == uq.Id ? " SelectedUserQuery" : "")
                });
            }

            if (items.Count > 0)
                items.Add(new ToolBarSeparator());

            string uqNewText = Signum.Web.Properties.Resources.New;
            items.Add(new ToolBarButton
            {
                Id = TypeContextUtilities.Compose(prefix, "qbUserQueryNew"),
                AltText = uqNewText,
                Text = uqNewText,
                OnClick = Js.SubmitOnly(RouteHelper.New().Action("CreateUserQuery", "Queries"), JsFindNavigator.JsRequestData(new JsFindOptions { Prefix = prefix })).ToJS(),
                DivCssClass = ToolBarButton.DefaultQueryCssClass
            });


            if (idCurrentUserQuery > 0)
            {
                items.Add(new ToolBarButton
                {
                    Id = TypeContextUtilities.Compose(prefix, "qbUserQueryEdit"),
                    AltText = "Edit",
                    Text = "Edit",
                    OnClick = Js.SubmitOnly(RouteHelper.New().Action("EditUserQuery", "Queries"), "{{id:{0}}}".Formato(idCurrentUserQuery)).ToJS(),
                    DivCssClass = ToolBarButton.DefaultQueryCssClass
                });
            }

            return new ToolBarButton[]
            {
                new ToolBarMenu
                { 
                    Id = TypeContextUtilities.Compose(prefix, "tmUserQueries"),
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
            object queryName = QueryLogic.ToQueryName(userQuery.Query.Key);

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
