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
using Signum.Engine.Files;
using System.IO;
using Accord.IO;
using Accord.MachineLearning;

namespace Signum.Engine.MachineLearning.AccordNet
{
    public class DiscreteNaiveBayesPredictorAlgorithm : ClasificationPredictorAlgorithm
    {
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

        public override void Train(PredictorTrainingContext ctx)
        {
            var bayes = new NaiveBayes(
               classes: ctx.OutputColumns.SingleEx().ValuesToIndex.Count(),
               symbols: ctx.InputColumns.Select(col => col.ValuesToIndex.Count).ToArray()
            );

            int[][] inputs = ctx.Input.Select(a => a.Cast<int>().ToArray()).ToArray();
            int[] outputs = ctx.Output.Select(a => (int)a.SingleEx()).ToArray();

            var trainedClassifier = new NaiveBayesLearning
            {
                Empirical = true,
                Model = bayes
            }.Learn(inputs, outputs);

            var predictor = ctx.Predictor;
            predictor.Files.ForEach(f => f.DeleteFileOnCommit());
            predictor.Files.Clear();
            predictor.Files.Add(new Entities.Files.FilePathEmbedded(PredictorFileType.PredictorFile,
                fileName: $"{typeof(NaiveBayes).FullName}.bin",
                fileData: AccordExtensions.SerializeToBytes(trainedClassifier)));
            predictor.Save();
        }

        public override EvaluateResult Evaluate(PredictorTrainingContext ctx)
        {
            var bayes = new NaiveBayes(
                classes: ctx.OutputColumns.SingleEx().ValuesToIndex.Count(),
                symbols: ctx.InputColumns.Select(col => col.ValuesToIndex.Count).ToArray()
             );

            int[][] inputs = ctx.Input.Select(a => a.Cast<int>().ToArray()).ToArray();
            int[] outputs = ctx.Output.Select(a => (int)a.SingleEx()).ToArray();
            
            var crossValidation = new CrossValidation<NaiveBayes, int[], int>
            {
                K = ctx.Predictor.Settings.CrossValidationFolds,
                
                Learner = (s) => new NaiveBayesLearning
                {
                    Empirical = true,
                    Model = bayes,
                },
                
                Loss = (expected, actual, p) => new ZeroOneLoss(expected).Loss(actual),

                DefaultValue = 1,
            };

            var result = crossValidation.Learn(inputs, outputs);

            return new EvaluateResult
            {
                Training = ToStats(result.Training),
                Validation = ToStats(result.Validation),
            };
        }

        private EvaluateStats ToStats(CrossValidationStatistics training)
        {
            return new EvaluateStats
            {
                Mean = training.Mean,
                Variance = training.Variance,
                StandartDeviation = training.StandardDeviation,
            };
        }


        public override Dictionary<object, double> PredictProbabilities(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input)
        {
            var pair = LoadModel(predictor, columns);

            var probabilities = pair.NaiveBayes.Probabilities(input.Cast<int>().ToArray());

            return probabilities.Select((p, i) => KVP.Create(pair.ResultColumn.Values[i], p)).ToDictionary();

        }

        public override object PredictDecide(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input)
        {
            var pair = LoadModel(predictor, columns);

            var index = pair.NaiveBayes.Decide(input.Cast<int>().ToArray());

            return pair.ResultColumn.Values[index];
        }


        private static ModelPair LoadModel(PredictorEntity predictor, PredictorResultColumn[] columns)
        {
            if (predictor.Model != null)
                return (ModelPair)predictor.Model;
            
            var file = predictor.Files.Single(a => a.FileName == $"{ typeof(NaiveBayes).FullName}.bin");

            var naiveBayes = file.OpenRead().Using(ms => Serializer.Load<NaiveBayes>(ms));

            var resultColumn = columns.Single(c => c.PredictorColumn.Usage == PredictorColumnUsage.Output);

            predictor.Model = new ModelPair
            {
                NaiveBayes = naiveBayes,
                ResultColumn = resultColumn,
            };

            return (ModelPair)predictor.Model;
        }

        class ModelPair
        {
            public NaiveBayes NaiveBayes;
            public PredictorResultColumn ResultColumn;
        }
    }

    public static class AccordExtensions
    {
        public static byte[] SerializeToBytes<T>(T obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                Serializer.Save<T>(obj, ms);
                return ms.ToArray();
            }
        }
    }
}
