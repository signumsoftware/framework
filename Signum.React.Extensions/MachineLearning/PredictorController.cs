using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.React.Files;
using System.IO;
using Signum.Entities.MachineLearning;
using Signum.Engine.MachineLearning;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities;
using Signum.React.ApiControllers;
using Newtonsoft.Json.Linq;
using Signum.Entities.DynamicQuery;
using Newtonsoft.Json;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;

namespace Signum.React.MachineLearning
{
    public class PredictorController : ApiController
    {
        [Route("api/predictor/csv/{id}"), HttpGet]
        public HttpResponseMessage DownloadCsvById(int id)
        {
            var predictor = Database.Query<PredictorEntity>().SingleEx(p => p.Id == id);
            byte[] content = predictor.GetCsv();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.csv");
        }

        [Route("api/predictor/tsv/{id}"), HttpGet]
        public HttpResponseMessage DownloadTsvById(int id)
        {
            var predictor = Database.Query<PredictorEntity>().SingleEx(p => p.Id == id);
            byte[] content = predictor.GetTsv();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.tsv");
        }

        [Route("api/predictor/tsv/{id}/metadata"), HttpGet]
        public HttpResponseMessage DownloadTsvMetadataById(int id)
        {
            var predictor = Database.Query<PredictorEntity>().SingleEx(p => p.Id == id);
            byte[] content = predictor.GetTsvMetadata();

            return FilesController.GetHttpReponseMessage(new MemoryStream(content), $"{predictor.Name}.metadata.tsv");
        }

        [Route("api/predictor/trainingProgress/{id}"), HttpGet]
        public TrainingProgress GetTrainingState(int id)
        {
            var state = PredictorLogic.Trainings.TryGetC(Lite.Create<PredictorEntity>(id));

            return new TrainingProgress
            {
                Running = state != null,
                State = state?.Context.Predictor.State ?? Database.Query<PredictorEntity>().Where(a => a.Id == id).Select(a => a.State).SingleEx(),
                Message = state?.Context.Message,
                Progress = state?.Context.Progress,
                EpochProgresses = state?.Context.GetProgessArray(),
            };
        }

        [Route("api/predictor/epochProgress/{id}"), HttpGet]
        public List<object[]> GetProgressLosses(int id)
        {
            return Database.Query<PredictorEpochProgressEntity>().Where(a => a.Predictor.Id == id).Select(p => new EpochProgress
            {
                Ellapsed = p.Ellapsed,
                Epoch = p.Epoch,
                TrainingExamples = p.TrainingExamples,
                EvaluationTraining = p.EvaluationTraining,
                EvaluationValidation = p.EvaluationValidation,
                LossTraining = p.LossTraining,
                LossValidation = p.LossValidation,
            })
            .Select(a => a.ToObjectArray())
            .ToList();
        }

        [Route("api/predict/get/{predictorId}"), HttpPost]
        public PredictRequestTS GetPredict(string predictorId, Lite<Entity> entity)
        {
            var p = Lite.ParsePrimaryKey<PredictorEntity>(predictorId);

            PredictorPredictContext pctx = PredictorPredictLogic.GetPredictContext(p);

            PredictDictionary fromEntity = entity != null ? PredictorPredictLogic.FromEntity(p, entity) : null;
            PredictDictionary inputs = fromEntity ?? PredictorPredictLogic.Empty(p);
            PredictDictionary originalOutputs = fromEntity;

            PredictDictionary predictedOutputs = PredictorPredictLogic.PredictBasic(p, inputs);

            PredictRequestTS pmodel = ToPredictModel(pctx, inputs, originalOutputs, predictedOutputs);
            
            return pmodel;
        }

        [Route("api/predict/update"), HttpPost]
        public PredictRequestTS UpdatePredict(PredictRequestTS request)
        {   
            PredictorPredictContext pctx = PredictorPredictLogic.GetPredictContext(request.predictor);

            ParseValues(request, pctx);

            PredictDictionary inputs = GetInputs(request, pctx);

            PredictDictionary predictedOutputs = inputs != null ? PredictorPredictLogic.PredictBasic(request.predictor, inputs) : null;
            
            SetOutput(request, pctx, predictedOutputs);

            return request;
        }

        PredictDictionary GetInputs(PredictRequestTS request, PredictorPredictContext pctx)
        {
            return new PredictDictionary(pctx.Predictor)
            {
                MainQueryValues = pctx.Predictor.MainQuery.Columns
                .Select((col, i) => new { col, request.columns[i].value })
                .Where(a => a.col.Usage == PredictorColumnUsage.Input)
                .Select(a => KVP.Create(a.col, a.value))
                .ToDictionaryEx(),
                
                SubQueries = pctx.Predictor.SubQueries.Select(sq =>
                {
                    var sqt = request.subQueries.Single(a => a.subQuery.RefersTo(sq));
                    SplitColumns(sq, out var splitKeys, out var values);

                    return new PredictSubQueryDictionary(sq)
                    {
                        SubQueryGroups = sqt.rows.Select(array => KVP.Create(array.Slice(0, splitKeys.Count),
                            values.Select((a, i) => KVP.Create(a, array[splitKeys.Count + i])).ToDictionary()
                        )).ToDictionary()
                    };
                }).ToDictionaryEx(a => a.SubQuery)
            };
        }

        private static void SplitColumns(PredictorSubQueryEntity sq, out List<PredictorSubQueryColumnEmbedded> splitKeys, out List<PredictorSubQueryColumnEmbedded> values)
        {
            var columns = sq.Columns.ToList();
            var parentKey = columns.Extract(a => a.Usage == PredictorSubQueryColumnUsage.ParentKey);
            splitKeys = columns.Extract(a => a.Usage == PredictorSubQueryColumnUsage.SplitBy).ToList();
            values = columns.Extract(a => a.Usage == PredictorSubQueryColumnUsage.Input).ToList();
        }

        void SetOutput(PredictRequestTS request, PredictorPredictContext pctx, PredictDictionary predicted)
        {
            var predictedMainCols = predicted.MainQueryValues.SelectDictionary(qt => qt.Token.Token.FullKey(), v => v);

            foreach (var c in request.columns.Where(a=>a.usage == PredictorColumnUsage.Output))
            {
                var pValue = predictedMainCols.GetOrThrow(c.token.fullKey);
                if (request.hasOriginal)
                    ((PredictOutputTuple)c.value).predicted = pValue;
                else
                    c.value = pValue;
            }

            foreach (var sq in request.subQueries)
            {
                PredictSubQueryDictionary psq = predicted.SubQueries.Values.Single(a => sq.subQuery.RefersTo(a.SubQuery));
                
                SplitColumns(psq.SubQuery, out var splitKeys, out var values);
                
                Dictionary<string, PredictorSubQueryColumnEmbedded> fullKeyToToken = psq.SubQuery.Columns.ToDictionary(a => a.Token.Token.FullKey());

                foreach (var r in sq.rows)
                {
                    var key = r.Slice(0, splitKeys.Count);
                    var dic = psq.SubQueryGroups.TryGetC(key);

                    for (int i = 0; i < values.Count; i++)
                    {
                        var c = sq.columnHeaders[splitKeys.Count + i];

                        if(c.headerType == PredictorHeaderType.Output)
                        {
                            ref var box = ref r[splitKeys.Count + i];

                            var token = fullKeyToToken.GetOrThrow(c.token.fullKey);

                            var pValue = dic?.GetOrThrow(token);
                            if (request.hasOriginal)
                                ((PredictOutputTuple)box).predicted = pValue;
                            else
                                box = pValue;

                        }
                    }
                }
            }
        }

        private PredictRequestTS ToPredictModel(PredictorPredictContext pctx, PredictDictionary inputs, PredictDictionary originalOutputs, PredictDictionary predictedOutputs)
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
                        rows = pctx.SubQueryOutputColumn[sq].Groups.Select(kvp => CreateRow(splitKeys, values, kvp.Key, inputsSQ, originalOutputsSQ, predictedOutputsSQ)).ToList()
                    };
                }).ToList()
            };
        }

        private object[] CreateRow(List<PredictorSubQueryColumnEmbedded> groupKeys, List<PredictorSubQueryColumnEmbedded> values, object[] key, PredictSubQueryDictionary inputs, PredictSubQueryDictionary originalOutputs, PredictSubQueryDictionary predictedOutputs)
        {
            var row = new object[groupKeys.Count + values.Count];

            var inputsGR = inputs?.SubQueryGroups.TryGetC(key);
            var originalOutputsGR = originalOutputs?.SubQueryGroups.TryGetC(key);
            var predictedOutputsGR = predictedOutputs?.SubQueryGroups.GetOrThrow(key);

            for (int i = 0; i < groupKeys.Count - 1; i++)
            {
                row[i] = key[i];
            }

            for (int i = 0; i < values.Count; i++)
            {
                var v = values[i];
                row[i + key.Length] = v.Usage == PredictorSubQueryColumnUsage.Input ? inputsGR?.GetOrThrow(v) :
                    originalOutputs == null ? predictedOutputsGR.GetOrThrow(v) :
                    new PredictOutputTuple
                    {
                        predicted = predictedOutputsGR.GetOrThrow(v),
                        original = originalOutputsGR?.GetOrThrow(v),
                    };
            }

            return row;
        }

        public void ParseValues(PredictRequestTS predict, PredictorPredictContext ctx)
        {
            var serializer = JsonSerializer.Create(GlobalConfiguration.Configuration.Formatters.JsonFormatter.SerializerSettings);

            for (int i = 0; i < ctx.Predictor.MainQuery.Columns.Count; i++)
            {
                predict.columns[i].value = FixValue(predict.columns[i].value, ctx.Predictor.MainQuery.Columns[i].Token.Token, serializer);
            }

            foreach (var tuple in ctx.SubQueryOutputColumn.Values.ZipStrict(predict.subQueries, (sqCtx, table) => (sqCtx, table)))
            {
                var sq = tuple.sqCtx.SubQuery;

                SplitColumns(sq, out var splitKeys, out var values);

                foreach (var r in tuple.table.rows)
                {   
                    for (int i = 0; i < splitKeys.Count - 1; i++)
                    {
                        r[i] = FixValue(r[i], splitKeys[i].Token.Token, serializer);
                    }

                    for (int i = 0; i < values.Count - 1; i++)
                    {
                        r[i + splitKeys.Count] = FixValue(r[i + splitKeys.Count], sq.Columns[i].Token.Token, serializer);
                    }
                }
            }
        }

        private object FixValue(object value, QueryToken token, JsonSerializer serializer)
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

            return jt.ToObject(token.Type, serializer);
        }
    }
}

public class PredictRequestTS
{
    public bool hasOriginal { get; set; }
    public Lite<PredictorEntity> predictor { get; set; }
    public List<PredictColumnTS> columns { get; set; }
    public List<PredictSubQueryTableTS> subQueries { get; set; }
}

public class PredictColumnTS
{
    public QueryTokenTS token { get; set; }
    public PredictorColumnUsage usage { get; set; }
    public object value { get; set; }
}

public class PredictSubQueryTableTS
{
    public Lite<PredictorSubQueryEntity> subQuery { get; set; }
    //Key* (Input|Output)*
    public List<PredictSubQueryHeaderTS> columnHeaders { get; set; }
    public List<object[]> rows { get; set; }
}

public class PredictOutputTuple
{
    public object predicted;
    public object original;
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