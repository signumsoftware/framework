using Signum.Entities;
using Signum.Entities.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Signum.Engine.MachineLearning
{
    public abstract class PredictorAlgorithm
    {
        public abstract void Initialize(PredictorEntity predictor);
        public virtual string ValidatePredictor(PredictorEntity predictor) => null;
        public abstract void Train(PredictorEntity predictor, PredictorResultColumn[] column, object[][] input, object[][] output);
        public abstract void Evaluate(PredictorEntity predictor, PredictorResultColumn[] column, object[][] input, object[][] output);
        public abstract object[][] Predict(PredictorEntity predictor, PredictorResultColumn[] column, object[][] input);
    }

    public static class PredictorAlgorithmValidation
    {
        public static IEnumerable<PredictorColumnEmbedded> GetAllPredictorColumnEmbeddeds(this PredictorEntity predictor)
        {
            return predictor.SimpleColumns.Concat(predictor.MultiColumns.SelectMany(a => a.Aggregates));
        }
    }
}
