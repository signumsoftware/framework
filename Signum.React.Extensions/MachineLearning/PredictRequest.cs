using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Signum.Engine.Basics;
using Signum.Engine.MachineLearning;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.React.ApiControllers;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using Signum.React.Facades;

namespace Signum.React.MachineLearning
{
    public static class PredictRequestExtensions
    {
        public static Dictionary<QueryToken, object?> ParseMainKeys(this PredictorPredictContext pctx, Dictionary<string, object?> mainKeys)
        {
            Dictionary<QueryToken, object?> filters = new Dictionary<QueryToken, object?>();

            var serializer = JsonSerializer.Create(SignumServer.JsonSerializerSettings);
            var qd = QueryLogic.Queries.QueryDescription(pctx.Predictor.MainQuery.Query.ToQueryName());
            foreach (var kvp in mainKeys)
            {
                var qt = QueryUtils.Parse(kvp.Key, qd, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate);

                var obj = kvp.Value is JToken jt ? jt.ToObject(qt.Type, serializer) : ReflectionTools.ChangeType(kvp.Value, qt.Type);

                filters.Add(qt, obj);
            }

            return filters;
        }

        public static PredictDictionary GetInputsFromRequest(this PredictorPredictContext pctx, PredictRequestTS request)
        {
            ParseValues(request, pctx);

            return new PredictDictionary(pctx.Predictor, null, null)
            {
                MainQueryValues = pctx.Predictor.MainQuery.Columns
                .Select((col, i) => new { col, request.columns[i].value })
                .Where(a => a.col!.Usage == PredictorColumnUsage.Input)
                .Select(a => KeyValuePair.Create(a.col!, a.value))
                .ToDictionaryEx(),

                SubQueries = pctx.Predictor.SubQueries.Select(sq =>
                {
                    var sqt = request.subQueries.Single(a => a.subQuery.Is(sq));
                    SplitColumns(sq, out var splitKeys, out var values);

                    return new PredictSubQueryDictionary(sq)
                    {
                        SubQueryGroups = sqt.rows.Select(array => KeyValuePair.Create(
                            array.Slice(0, splitKeys.Count),
                            values.Select((a, i) => KeyValuePair.Create(a, array[splitKeys.Count + i])).ToDictionary()
                        )).ToDictionary(ObjectArrayComparer.Instance)
                    };
                }).ToDictionaryEx(a => a.SubQuery)
            };
        }

        private static void SplitColumns(PredictorSubQueryEntity sq, out List<PredictorSubQueryColumnEmbedded> splitKeys, out List<PredictorSubQueryColumnEmbedded> values)
        {
            var columns = sq.Columns.ToList();
            var parentKey = columns.Extract(a => a.Usage == PredictorSubQueryColumnUsage.ParentKey);
            splitKeys = columns.Extract(a => a.Usage == PredictorSubQueryColumnUsage.SplitBy).ToList();
            values = columns.ToList();
        }

        public static void SetOutput(this PredictRequestTS request, PredictDictionary predicted)
        {
            var predictedMainCols = predicted.MainQueryValues.SelectDictionary(qt => qt.Token.Token.FullKey(), v => v);

            foreach (var c in request.columns.Where(a => a.usage == PredictorColumnUsage.Output))
            {
                var pValue = predictedMainCols.GetOrThrow(c.token.fullKey);
                if (request.hasOriginal)
                    ((PredictOutputTuple)c.value!).predicted = pValue;
                else
                    c.value = pValue;
            }

            foreach (var sq in request.subQueries)
            {
                PredictSubQueryDictionary psq = predicted.SubQueries.Values.Single(a => sq.subQuery.Is(a.SubQuery));

                if (psq.SubQueryGroups.Comparer != ObjectArrayComparer.Instance)
                    throw new InvalidOperationException("Unexpected comparer");

                SplitColumns(psq.SubQuery, out var splitKeys, out var values);

                Dictionary<string, PredictorSubQueryColumnEmbedded> fullKeyToToken = psq.SubQuery.Columns.ToDictionary(a => a.Token.Token.FullKey());

                foreach (var r in sq.rows)
                {
                    var key = r.Slice(0, splitKeys.Count);
                    var dic = psq.SubQueryGroups.TryGetC(key);

                    for (int i = 0; i < values.Count; i++)
                    {
                        var c = sq.columnHeaders[splitKeys.Count + i];

                        if (c.headerType == PredictorHeaderType.Output)
                        {
                            ref var box = ref r[splitKeys.Count + i];

                            var token = fullKeyToToken.GetOrThrow(c.token.fullKey);

                            var pValue = dic?.GetOrThrow(token);
                            if (request.hasOriginal)
                                ((PredictOutputTuple)box!).predicted = pValue;
                            else
                                box = pValue;

                        }
                    }
                }
            }
        }

        public static PredictRequestTS CreatePredictModel(this PredictorPredictContext pctx, 
            PredictDictionary? inputs, 
            PredictDictionary? originalOutputs, 
            PredictDictionary predictedOutputs)
        {
            return new PredictRequestTS
            {
                predictor = pctx.Predictor.ToLite(),

                hasOriginal = originalOutputs != null,

                columns = pctx.Predictor.MainQuery.Columns.Select(c => new PredictColumnTS
                {
                    token = new QueryTokenTS(c.Token.Token, true),
                    usage = c.Usage,
                    value = c.Usage == PredictorColumnUsage.Input ? inputs?.MainQueryValues.GetOrThrow(c) :
                    originalOutputs == null ? predictedOutputs?.MainQueryValues.GetOrThrow(c) :
                    new PredictOutputTuple
                    {
                        original = originalOutputs?.MainQueryValues.GetOrThrow(c),
                        predicted = predictedOutputs?.MainQueryValues.GetOrThrow(c),
                    }
                }).ToList(),

                subQueries = pctx.Predictor.SubQueries.Select(sq =>
                {
                    var inputsSQ = inputs?.SubQueries.GetOrThrow(sq);
                    var originalOutputsSQ = originalOutputs?.SubQueries.GetOrThrow(sq);
                    var predictedOutputsSQ = predictedOutputs?.SubQueries.GetOrThrow(sq);

                    SplitColumns(sq, out var splitKeys, out var values);

                    var columnHeaders =
                        splitKeys.Select(gk => new PredictSubQueryHeaderTS { token = new QueryTokenTS(gk.Token.Token, true), headerType = PredictorHeaderType.Key }).Concat(
                        values.Select(agg => new PredictSubQueryHeaderTS { token = new QueryTokenTS(agg.Token.Token, true), headerType = agg.Usage == PredictorSubQueryColumnUsage.Input ? PredictorHeaderType.Input : PredictorHeaderType.Output }))
                        .ToList();

                    return new PredictSubQueryTableTS
                    {
                        subQuery = sq.ToLite(),
                        columnHeaders = columnHeaders,
                        rows = pctx.SubQueryOutputCodifications[sq].Groups
                        .Select(kvp => CreateRow(splitKeys, values, kvp.Key, inputsSQ, originalOutputsSQ, predictedOutputsSQ))
                        .ToList()
                    };
                }).ToList()
            };
        }

        static object?[] CreateRow(List<PredictorSubQueryColumnEmbedded> groupKeys, List<PredictorSubQueryColumnEmbedded> values, object?[] key, PredictSubQueryDictionary? inputs, PredictSubQueryDictionary? originalOutputs, PredictSubQueryDictionary? predictedOutputs)
        {
            var row = new object?[groupKeys.Count + values.Count];

            var inputsGR = inputs?.SubQueryGroups.TryGetC(key);
            var originalOutputsGR = originalOutputs?.SubQueryGroups.TryGetC(key);
            var predictedOutputsGR = predictedOutputs?.SubQueryGroups.GetOrThrow(key);

            for (int i = 0; i < groupKeys.Count; i++)
            {
                row[i] = key[i];
            }

            for (int i = 0; i < values.Count; i++)
            {
                var v = values[i];
                row[i + key.Length] = v.Usage == PredictorSubQueryColumnUsage.Input ? inputsGR?.GetOrThrow(v) :
                    originalOutputs == null ? predictedOutputsGR!.GetOrThrow(v) :
                    new PredictOutputTuple
                    {
                        predicted = predictedOutputsGR!.GetOrThrow(v),
                        original = originalOutputsGR?.GetOrThrow(v),
                    };
            }

            return row;
        }

        public static void ParseValues(this PredictRequestTS predict, PredictorPredictContext ctx)
        {
            var serializer = JsonSerializer.Create(SignumServer.JsonSerializerSettings);

            for (int i = 0; i < ctx.Predictor.MainQuery.Columns.Count; i++)
            {
                predict.columns[i].value = FixValue(predict.columns[i].value, ctx.Predictor.MainQuery.Columns[i].Token.Token, serializer);
            }

            foreach (var tuple in ctx.SubQueryOutputCodifications.Values.ZipStrict(predict.subQueries, (sqCtx, table) => (sqCtx, table)))
            {
                var sq = tuple.sqCtx.SubQuery;

                SplitColumns(sq, out var splitKeys, out var values);

                foreach (var row in tuple.table.rows)
                {
                    for (int i = 0; i < splitKeys.Count; i++)
                    {
                        row[i] = FixValue(row[i], splitKeys[i].Token.Token, serializer);
                    }

                    for (int i = 0; i < values.Count; i++)
                    {
                        var colIndex = i + splitKeys.Count;
                        row[colIndex] = FixValue(row[colIndex], values[i].Token.Token, serializer);
                    }
                }
            }
        }

        static object? FixValue(object? value, QueryToken token, JsonSerializer serializer)
        {
            if (!(value is JToken jt))
                return ReflectionTools.ChangeType(value, token.Type);

            if (jt is JObject jo &&
                jo.Property(nameof(PredictOutputTuple.original)) != null &&
                jo.Property(nameof(PredictOutputTuple.predicted)) != null)
            {
                return new PredictOutputTuple
                {
                    original = FixValue(jo[nameof(PredictOutputTuple.original)], token, serializer),
                    predicted = FixValue(jo[nameof(PredictOutputTuple.predicted)], token, serializer),
                };
            }

            if(jt is JArray ja)
            {
                var list = ja.ToObject<List<AlternativePrediction>>();
                var result = list.Select(val => ReflectionTools.ChangeType(val, token.Type));
                return result;
            }

            return jt.ToObject(token.Type, serializer);
        }
    }



    public class PredictRequestTS
    {
        public bool hasOriginal { get; set; }
        public int? alternativesCount { get; set; }
        public Lite<PredictorEntity> predictor { get; set; }
        public List<PredictColumnTS> columns { get; set; }
        public List<PredictSubQueryTableTS> subQueries { get; set; }
    }

    public class PredictColumnTS
    {
        public QueryTokenTS token { get; set; }
        public PredictorColumnUsage usage { get; set; }
        public object? value { get; set; }
    }

    public class PredictSubQueryTableTS
    {
        public Lite<PredictorSubQueryEntity> subQuery { get; set; }
        //Key* (Input|Output)*
        public List<PredictSubQueryHeaderTS> columnHeaders { get; set; }
        public List<object?[]> rows { get; set; }
    }

    public class PredictOutputTuple
    {
        public object? predicted;
        public object? original;
    }

    public class PredictSubQueryHeaderTS
    {
        public QueryTokenTS token { get; set; }
        public PredictorHeaderType headerType { get; set; }
    }

    public enum PredictorHeaderType
    {
        Key,
        Input,
        Output,
    }
}
