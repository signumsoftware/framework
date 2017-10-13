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
        public virtual string ValidatePredictor(PredictorEntity predictor) => null;
        public abstract void Train(PredictorEntity predictor, PredictorResultColumn[] columns, object[][] input, object[][] output);
        public abstract EvaluateResult Evaluate(PredictorEntity predictor, PredictorResultColumn[] columns, object[][] input, object[][] output);
        public abstract object[] Predict(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input);
    }

    public abstract class ClasificationPredictorAlgorithm : PredictorAlgorithm
    {
        public override object[] Predict(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input)
        {
            var singleOutput = PredictDecide(predictor, columns, input);
            return new[] { singleOutput };
        }

        public abstract object PredictDecide(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input);
        public abstract Dictionary<object, double> PredictProbabilities(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input);
    }

    public class EvaluateResult
    {
        public EvaluateStats Training;
        public EvaluateStats Validation;
    }

    public class EvaluateStats
    {
        public double Mean;
        public double Variance;
        public double StandartDeviation; 
    }

    public static class PredictorAlgorithmValidation
    {
        public static IEnumerable<PredictorColumnEmbedded> GetAllPredictorColumnEmbeddeds(this PredictorEntity predictor)
        {
            return predictor.SimpleColumns.Concat(predictor.MultiColumns.SelectMany(a => a.Aggregates));
        }
    }
}
