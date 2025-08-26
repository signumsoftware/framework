using Microsoft.AspNetCore.Builder;
using Signum.API;
using Signum.API.Controllers;
using Signum.API.Json;
using Signum.Authorization;
using Signum.Authorization.Rules;
using Signum.Eval.TypeHelp;
using Signum.Templating;
using System.Text.Json;

namespace Signum.Word;

public static class WordServer
{
    public static void Start(WebServerBuilder wsb)
    {
        if (wsb.AlreadyDefined(MethodBase.GetCurrentMethod()))
            return;

        TemplatingServer.TemplateTokenMessageAllowed += () => TypeAuthLogic.GetAllowed(typeof(WordTemplateEntity)).MaxUI() > TypeAllowedBasic.None;

        TemplatingServer.Start(wsb);

        CustomizeFiltersModel();

        EntityPackTS.AddExtension += ep =>
        {
            if (ep.entity.IsNew || !WordTemplatePermission.GenerateReport.IsAuthorized())
                return;

            try
            {
                var wordTemplates = WordTemplateLogic.TemplatesByEntityType.Value.TryGetC(ep.entity.GetType());
                if (wordTemplates != null)
                {
                    var applicable = wordTemplates.Where(a => a.IsApplicable(ep.entity));
                    if (applicable.HasItems())
                        ep.extension.Add("wordTemplates", applicable.Select(a => a.ToLite()).ToList());
                }

            }
            catch (Exception e)
            {
                e.LogException(); //An error could make the applicaiton unusable
                ep.extension.Add("wordTemplates", "error");
            }
        };

        QueryDescriptionTS.AddExtension += qd =>
        {
            object type = QueryLogic.ToQueryName(qd.queryKey);
            if (Schema.Current.IsAllowed(typeof(WordTemplateEntity), true) == null)
            {
                try
                {
                    var templates = WordTemplateLogic.GetApplicableWordTemplates(type, null, WordTemplateVisibleOn.Query);

                    if (templates.HasItems())
                        qd.Extension.Add("wordTemplates", templates);
                }
                catch (Exception e)
                {
                    e.LogException(); //An error could make the applicaiton unusable
                    qd.Extension.Add("wordTemplates", "error");
                }
            }
        };

        SignumServer.WebEntityJsonConverterFactory.AfterDeserilization.Register((WordTemplateEntity uq) =>
        {
            if (uq.Query != null)
            {
                var qd = QueryLogic.Queries.QueryDescription(uq.Query.ToQueryName());
                uq.ParseData(qd);
            }
        });
    }

    private static void CustomizeFiltersModel()
    {
        var converters = SignumServer.WebEntityJsonConverterFactory.GetPropertyConverters(typeof(QueryModel));
        converters.Remove("queryName");

        converters.Add("queryKey", new PropertyConverter()
        {
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                ((QueryModel)ctx.Entity).QueryName = QueryLogic.ToQueryName(reader.GetString()!);
            },
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var cr = (QueryModel)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                writer.WriteStringValue(QueryLogic.GetQueryEntity(cr.QueryName).Key);
            }
        });

        converters.Add("filters", new PropertyConverter()
        {
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                var list = JsonSerializer.Deserialize<List<FilterTS>>(ref reader,  ctx.JsonSerializerOptions)!;

                var cr = (QueryModel)ctx.Entity;

                var qd = QueryLogic.Queries.QueryDescription(cr.QueryName);

                cr.Filters = list.Select(l => l.ToFilter(qd, canAggregate: true, SignumServer.JsonSerializerOptions, false)).ToList();
            },
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var cr = (QueryModel)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                JsonSerializer.Serialize(writer, cr.Filters.Select(f => FilterTS.FromFilter(f)).ToList(), ctx.JsonSerializerOptions);
            }
        });

        converters.Add("orders", new PropertyConverter()
        {
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                var list = JsonSerializer.Deserialize<List<OrderTS>>(ref reader, ctx.JsonSerializerOptions)!;

                var cr = (QueryModel)ctx.Entity;

                var qd = QueryLogic.Queries.QueryDescription(cr.QueryName);

                cr.Orders = list.Select(l => l.ToOrder(qd, canAggregate: true, canTimeSeries: true)).ToList();
            },
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var cr = (QueryModel)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                JsonSerializer.Serialize(writer, cr.Orders.Select(f => new OrderTS
                {
                    token = f.Token.FullKey(),
                    orderType = f.OrderType
                }), ctx.JsonSerializerOptions);
            }
        });

        converters.Add("pagination", new PropertyConverter()
        {
            AvoidValidate = true,
            CustomReadJsonProperty = (ref Utf8JsonReader reader, ReadJsonPropertyContext ctx) =>
            {
                var pagination = JsonSerializer.Deserialize<PaginationTS>(ref reader, ctx.JsonSerializerOptions)!;
                var cr = (QueryModel)ctx.Entity;
                cr.Pagination = pagination.ToPagination();
            },
            CustomWriteJsonProperty = (Utf8JsonWriter writer, WriteJsonPropertyContext ctx) =>
            {
                var cr = (QueryModel)ctx.Entity;

                writer.WritePropertyName(ctx.LowerCaseName);
                JsonSerializer.Serialize(new PaginationTS(cr.Pagination), ctx.JsonSerializerOptions);
            }
        });
    }
}
