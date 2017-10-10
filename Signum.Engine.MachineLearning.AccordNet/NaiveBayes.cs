using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.MachineLearning;
using Accord.MachineLearning.Performance;
using Accord.MachineLearning.Bayes;
using Accord.Math.Optimization.Losses;
using System.Reflection;
using Signum.Utilities;

namespace Signum.Engine.MachineLearning.AccordNet
{
    public class DiscreteNaiveBayesPredictorAlgorithm : PredictorAlgorithm
    {
        public override void Initialize(PredictorEntity predictor)
        {
            predictor.AlgorithmSettings = new NaiveBayesSettingsEntity();
        }

        public override string ValidatePredictor(PredictorEntity predictor)
        {
            var outputCount = predictor.GetAllPredictorColumnEmbeddeds().Count(a => a.Usage == PredictorColumnUsage.Output);
            if (outputCount != 1)
                return "NaiveBayes requires exactly one output";

            var errors = predictor.GetAllPredictorColumnEmbeddeds().Where(a => a.Encoding != PredictorColumnEncoding.Codified);
            if (errors.Any())
                return "NaiveBayes requires Codified encoding in all columns (" + errors.ToString(a => a.Token.TryToken?.ToString(), ", ") + ")";
     

            return base.ValidatePredictor(predictor);
        }
    

        public override void Train(PredictorEntity predictor, PredictorResultColumn[] columns, object[][] input, object[][] output)
        {
            var bayes = new NaiveBayes(
               classes: columns.Single(c => c.PredictorColumn.Usage == PredictorColumnUsage.Output).Values.Count(),
               symbols: columns.Where(c => c.PredictorColumn.Usage == PredictorColumnUsage.Input).Select(col => col.Values.Count).ToArray()
            );

            int[][] inputs = input.Select(a => a.Cast<int>().ToArray()).ToArray();
            int[] outputs = output.Select(a => (int)a.SingleEx()).ToArray();

            var trainedClassifier = new NaiveBayesLearning
            {
                Empirical = true,
                Model = bayes
            }.Learn(, output.Select(a => a.SingleEx()).ToArray());
        }
        
        public override object[][] Predict(PredictorEntity predictor, PredictorResultColumn[] column, object[][] input)
        {
            throw new NotImplementedException();
        }
    }
}
