using Signum.Entities.UserAssets;
using Signum.React.Json;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Web;
using System.Web.Http;
using Newtonsoft.Json;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;
using Signum.React.UserAssets;
using Signum.Entities.Chart;
using Signum.Entities;
using Signum.React.ApiControllers;
using Signum.Entities.DynamicQuery;

namespace Signum.React.Chart
{
    public static class ChartServer
    {
        public static void Start(HttpConfiguration config)
        {
            UserAssetServer.Start(config);

            SignumControllerFactory.RegisterArea(MethodInfo.GetCurrentMethod());

            CustomizeChartRequest();

            PropertyConverter.GetPropertyConverters(typeof(ChartScriptParameterEntity)).Add("enumValues", new PropertyConverter()
            {
                CustomWriteJsonProperty = ctx =>
                {
                    var csp = (ChartScriptParameterEntity)ctx.Entity;

                    if (csp.Type == ChartParameterType.Enum)
                    {
                        ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                        ctx.JsonSerializer.Serialize(ctx.JsonWriter, csp.GetEnumValues().Select(a => new { name = a.Name, typeFilter = a.TypeFilter }).ToList());
                    }
                },
                AvoidValidate = true,
                CustomReadJsonProperty = ctx => 
                {
                    var list = ctx.JsonSerializer.Deserialize(ctx.JsonReader);
                    //Discard
                }
            });

            EntityJsonConverter.AfterDeserilization.Register((ChartRequest cr) =>
            {
                var qd = DynamicQueryManager.Current.QueryDescription(cr.QueryName);

                if (cr.Columns != null)
                    foreach (var c in cr.Columns)
                        c.ParseData(cr, qd, SubTokensOptions.CanElement | (c.IsGroupKey == false ? SubTokensOptions.CanAggregate : 0));
            });

            EntityJsonConverter.AfterDeserilization.Register((UserChartEntity ue) =>
            {
                var qd = DynamicQueryManager.Current.QueryDescription(ue.Query.ToQueryName());
                ue.ParseData(qd);
            });
        }

        private static void CustomizeChartRequest()
        {
            var converters = PropertyConverter.GetPropertyConverters(typeof(ChartRequest));
            converters.Remove("queryName");
            
            converters.Add("queryKey", new PropertyConverter()
            {
                AvoidValidate = true,
                CustomReadJsonProperty = ctx =>
                {
                    ((ChartRequest)ctx.Entity).QueryName = QueryLogic.ToQueryName((string)ctx.JsonReader.Value);
                },
                CustomWriteJsonProperty = ctx =>
                {
                    var cr = (ChartRequest)ctx.Entity;

                    ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                    ctx.JsonWriter.WriteValue(QueryLogic.GetQueryEntity(cr.QueryName).Key);
                }
            });
            
            converters.Add("filters", new PropertyConverter()
            {
                AvoidValidate = true,
                CustomReadJsonProperty = ctx =>
                {
                    var list = (List<FilterTS>)ctx.JsonSerializer.Deserialize(ctx.JsonReader, typeof(List<FilterTS>));

                    var cr = (ChartRequest)ctx.Entity;

                    var qd = DynamicQueryManager.Current.QueryDescription(cr.QueryName);

                    cr.Filters = list.Select(l => l.ToFilter(qd, canAggregate: true)).ToList();
                },
                CustomWriteJsonProperty = ctx =>
                {
                    var cr = (ChartRequest)ctx.Entity;

                    ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                    ctx.JsonSerializer.Serialize(ctx.JsonWriter, cr.Filters.Select(f => new FilterTS
                    {
                        token = f.Token.FullKey(),
                        operation = f.Operation,
                        value = f.Value
                    }).ToList());
                }
            });
            
            converters.Add("orders", new PropertyConverter()
            {
                AvoidValidate = true,
                CustomReadJsonProperty = ctx =>
                {
                    var list = (List<OrderTS>)ctx.JsonSerializer.Deserialize(ctx.JsonReader, typeof(List<OrderTS>));

                    var cr = (ChartRequest)ctx.Entity;

                    var qd = DynamicQueryManager.Current.QueryDescription(cr.QueryName);

                    cr.Orders = list.Select(l => l.ToOrder(qd, canAggregate: true)).ToList();
                },
                CustomWriteJsonProperty = ctx =>
                {
                    var cr = (ChartRequest)ctx.Entity;

                    ctx.JsonWriter.WritePropertyName(ctx.LowerCaseName);
                    ctx.JsonSerializer.Serialize(ctx.JsonWriter, cr.Orders.Select(f => new OrderTS
                    {
                        token = f.Token.FullKey(),
                        orderType = f.OrderType
                    }));
                }
            });
        }
    }
}