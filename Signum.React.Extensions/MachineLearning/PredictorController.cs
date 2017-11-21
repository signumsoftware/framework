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
                State = state?.Context.Predictor.State ?? Database.Query<PredictorEntity>().Where(a => a.Id == id).Select(a => a.State).SingleEx(),
                Message = state?.Context.Message,
                Progress = state?.Context.Progress,
                EpochProgresses = state?.Context.Progresses.Select(a=>a.ToObjectArray()).ToList(),
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

        [Route("api/predictor/predict/{predictorId}"), HttpGet]
        public PredictModelTS GetPredictModel(PrimaryKey predictorId, Lite<Entity> entity)
        {
            var p = Lite.Create<PredictorEntity>(predictorId);

            var pctx = PredictorPredictLogic.GetPredictContext(p);

            var pmodel = GetPredictModel(pctx, entity != null);

            if(entity != null)
            {
                var dic = PredictorPredictLogic.FromEntity(p, entity);
                FillModel(pctx, dic);
            }
            
            return pmodel;
        }

        private void FillModel(PredictorPredictContext pctx, PredictorDictionary dic)
        {

        }

        private PredictModelTS GetPredictModel(PredictorPredictContext pctx)
        {
            return new PredictModelTS
            {
                Columns = pctx.su
            }
        }
    }
}

public class PredictModelTS
{
    public List<PredictColumnTS> Columns { get; set; }
    public List<PredictSubQueryTableTS> SubQuery { get; set; }
}

public class PredictColumnTS
{
    public QueryTokenTS Token { get; private set; }
    public PredictorColumnUsage Usage { get; private set; }
    public object value { get; private set; }
    public object originalValue { get; private set; }
}

public class PredictSubQueryTableTS
{
    public string Name { get; set; }
    public List<PredictSubQueryHeaderTS> Headers { get; set; }
    public List<List<object>> Rows { get; set; }
}

public class PredictSubQueryHeaderTS
{
    public QueryTokenTS Token { get; private set; }
    public PredictorHeaderType HeaderType { get; private set; }
}

public enum PredictorHeaderType
{
    Key,
    Input,
    Output,
    OutputRef
}