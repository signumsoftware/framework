using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Signum.Entities.MachineLearning;
using Signum.Engine.MachineLearning;
using Signum.Engine;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.Basics;
using Signum.React.Filters;

namespace Signum.React.MachineLearning
{
    [ValidateModelFilter]
    public class PredictorController : ControllerBase
    {
        [HttpGet("api/predictor/availableDevices/{algorithmKey}")]
        public string[] AvailableDevices(string algorithmKey)
        {
            var key = SymbolLogic<PredictorAlgorithmSymbol>.ToSymbol(algorithmKey);

            var alg = PredictorLogic.Algorithms.GetOrThrow(key);
            return alg.GetAvailableDevices();
        }

        [HttpGet("api/predictor/trainingProgress/{id}")]
        public TrainingProgress GetTrainingState(int id)
        {
            var ptc = PredictorLogic.GetTrainingContext(Lite.Create<PredictorEntity>(id));

            return new TrainingProgress
            {
                Running = ptc != null,
                State = ptc?.Predictor.State ?? Database.Query<PredictorEntity>().Where(a => a.Id == id).Select(a => a.State).SingleEx(),
                Message = ptc?.Message,
                Progress = ptc?.Progress,
                EpochProgresses = ptc?.GetProgessArray(),
            };
        }

        [HttpGet("api/predictor/epochProgress/{id}")]
        public List<object?[]> GetProgressLosses(int id)
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

        [HttpPost("api/predict/get/{predictorId}")]
        public PredictRequestTS GetPredict(string predictorId, [Required, FromBody]Dictionary<string, object?> mainKeys)
        {
            var p = Lite.ParsePrimaryKey<PredictorEntity>(predictorId);

            PredictorPredictContext pctx = PredictorPredictLogic.GetPredictContext(p);

            PredictDictionary? fromEntity = mainKeys == null ? null : pctx.GetInputsFromParentKeys(pctx.ParseMainKeys(mainKeys));
            PredictDictionary inputs = fromEntity ?? pctx.GetInputsEmpty();
            PredictDictionary? originalOutputs = fromEntity;

            PredictDictionary predictedOutputs = inputs.PredictBasic();

            PredictRequestTS pmodel = pctx.CreatePredictModel(inputs, originalOutputs, predictedOutputs);

            return pmodel;
        }

        [HttpPost("api/predict/update")]
        public PredictRequestTS UpdatePredict([Required, FromBody]PredictRequestTS request)
        {
            PredictorPredictContext pctx = PredictorPredictLogic.GetPredictContext(request.predictor);

            PredictDictionary inputs = pctx.GetInputsFromRequest(request);

            if (request.alternativesCount != null)
                inputs.Options = new PredictionOptions { AlternativeCount = request.alternativesCount };

            PredictDictionary predictedOutputs =  inputs.PredictBasic() ;

            request.SetOutput(predictedOutputs);

            return request;
        }

        [HttpGet("api/predict/publications/{queryKey}")]
        public List<PredictorPublicationSymbol> GetPublications(string queryKey)
        {
            object queryName = QueryLogic.ToQueryName(queryKey);

            return PredictorLogic.Publications.Where(a => object.Equals(a.Value.QueryName, queryName)).Select(a => a.Key).ToList();
        }
    }
}
