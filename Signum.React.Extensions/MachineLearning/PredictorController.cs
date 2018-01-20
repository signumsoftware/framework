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
using Signum.Engine.DynamicQuery;
using Signum.Engine.Basics;

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


        [Route("api/predictor/availableDevices/{algorithmKey}"), HttpGet]
        public string[] AvailableDevices(string algorithmKey)
        {
            var key = SymbolLogic<PredictorAlgorithmSymbol>.ToSymbol(algorithmKey);

            var alg = PredictorLogic.Algorithms.GetOrThrow(key);
            return alg.GetAvailableDevices();
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
        public PredictRequestTS GetPredict(string predictorId, Dictionary<string, object> mainKeys)
        {
            var p = Lite.ParsePrimaryKey<PredictorEntity>(predictorId);

            PredictorPredictContext pctx = PredictorPredictLogic.GetPredictContext(p);

            PredictDictionary fromEntity = mainKeys == null ? null : pctx.GetInputsFromParentKeys(pctx.ParseMainKeys(mainKeys));
            PredictDictionary inputs = fromEntity ?? pctx.GetInputsEmpty();
            PredictDictionary originalOutputs = fromEntity;

            PredictDictionary predictedOutputs = inputs.PredictBasic();

            PredictRequestTS pmodel = pctx.CreatePredictModel(inputs, originalOutputs, predictedOutputs);

            return pmodel;
        }

        [Route("api/predict/update"), HttpPost]
        public PredictRequestTS UpdatePredict(PredictRequestTS request)
        {
            PredictorPredictContext pctx = PredictorPredictLogic.GetPredictContext(request.predictor);

            PredictDictionary inputs = pctx.GetInputsFromRequest(request);

            PredictDictionary predictedOutputs = inputs != null ? inputs.PredictBasic() : null;

            request.SetOutput(pctx, predictedOutputs);

            return request;
        }

        [Route("api/predict/publications/{queryKey}"), HttpGet]
        public List<PredictorPublicationSymbol> GetPublications(string queryKey)
        {
            object queryName = QueryLogic.ToQueryName(queryKey);

            return PredictorLogic.Publications.Where(a => object.Equals(a.Value.QueryName, queryName)).Select(a => a.Key).ToList();
        }
    }
}
