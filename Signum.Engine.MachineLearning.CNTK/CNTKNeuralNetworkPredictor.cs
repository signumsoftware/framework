using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.MachineLearning;
using CNTK;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;

namespace Signum.Engine.MachineLearning.CNTK
{
    public class CNTKNeuralNetworkPredictorAlgorithm : PredictorAlgorithm
    {
        private Value labels;

        public override object[] Predict(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input)
        {
            throw new NotImplementedException();
        }

        //Errors with CNTK: https://github.com/Microsoft/CNTK/issues/2614
        public override void Train(PredictorTrainingContext ctx)
        {
            var p = ctx.Predictor;

            var nnSettings = (NeuralNetworkSettingsEntity)p.AlgorithmSettings;
            var sparse = nnSettings.SparseMatrix ?? HasOneHot(p);

            DeviceDescriptor device = DeviceDescriptor.CPUDevice;
            Variable inputVariable = Variable.InputVariable(new[] { ctx.InputColumns.Count }, DataType.Float, "input");
            Variable outputVariable = Variable.InputVariable(new[] { ctx.OutputColumns.Count }, DataType.Float, "output");
            Function calculatedOutpus = CreateLinearModel(inputVariable, ctx.OutputColumns.Count, device);

            Function loss = CNTKLib.CrossEntropyWithSoftmax(calculatedOutpus, outputVariable);
            Function evalError = CNTKLib.ClassificationError(calculatedOutpus, outputVariable);

            // prepare for training
            TrainingParameterScheduleDouble learningRatePerSample = new TrainingParameterScheduleDouble(0.02, 1);
            IList<Learner> parameterLearners = new List<Learner>() { Learner.SGDLearner(calculatedOutpus.Parameters(), learningRatePerSample) };
            var trainer = Trainer.CreateTrainer(calculatedOutpus, loss, evalError, parameterLearners);

            var (training, validation) = ctx.SplitTrainValidation();

            var batches = (int)Math.Ceiling(training.Count / (float)nnSettings.MinibatchSize);
            
            for (int i = 0; i < batches; i++)
            {
                ctx.ReportProgress("Training Minibatches", (i + 1) / (decimal)batches);
                var trainSlice = Slice(training, i * nnSettings.MinibatchSize, nnSettings.MinibatchSize);
                using (Value inputValue = CreateValue(ctx, trainSlice, ctx.InputColumns, device))
                using (Value outputValue = CreateValue(ctx, trainSlice, ctx.OutputColumns, device))
                {
                    trainer.TrainMinibatch(new Dictionary<Variable, Value>()
                    {
                        { inputVariable, inputValue },
                        { outputVariable, outputValue },
                    }, device);

                    if (i == batches - 1 || (i % nnSettings.SaveProgressEvery) == 0)
                        new PredictorProgressEntity
                        {
                            Predictor = p.ToLite(),
                            MiniBatchIndex = i,
                            LossTraining = trainer.PreviousMinibatchLossAverage(),
                        }.Save();
                }
            }


            ctx.ReportProgress("Evaluating");
            ctx.Predictor.ClassificationValidation = nnSettings.PredictionType == PredictionType.Classification ? null :
                ClasificationMetrics(ctx, validation, inputVariable, calculatedOutpus, device);

            ctx.Predictor.ClassificationTraining = nnSettings.PredictionType == PredictionType.Classification ? null :
                ClasificationMetrics(ctx, training, inputVariable, calculatedOutpus, device);

            ctx.Predictor.RegressionValidation = null;
            ctx.Predictor.ClassificationTraining = null;
        }
 
        private static bool HasOneHot(PredictorEntity p)
        {
            return p.MainQuery.Columns.Any(a => a.Encoding == PredictorColumnEncoding.OneHot) ||
                p.SubQueries.Any(mc => mc.Aggregates.Any(a => a.Encoding == PredictorColumnEncoding.OneHot));
        }

        private static PredictorClassificationMetricsEmbedded ClasificationMetrics(PredictorTrainingContext ctx, List<ResultRow> rows, Variable inputVariable, Function calculatedOutpus, DeviceDescriptor device)
        {
            using (Value inputValue = CreateValue(ctx, rows, ctx.InputColumns, device))
            {
                var inputs = new Dictionary<Variable, Value> { { inputVariable, inputValue } };
                var outputs = new Dictionary<Variable, Value> { { calculatedOutpus.Output, null } };
                calculatedOutpus.Evaluate(inputs, outputs, device);

                using (Value outputValue = outputs[calculatedOutpus.Output])
                {
                    IList<IList<float>> actualLabelSoftMax = outputValue.GetDenseData<float>(calculatedOutpus.Output);
                    List<int> actualLabels = actualLabelSoftMax.Select((IList<float> l) => l.IndexOf(l.Max())).ToList();

                    using (Value expectedValue = CreateValue(ctx, rows, ctx.OutputColumns, device))
                    {
                        IList<IList<float>> expectedOneHot = expectedValue.GetDenseData<float>(calculatedOutpus.Output);
                        List<int> expectedLabels = expectedOneHot.Select(l => l.IndexOf(1.0F)).ToList();
                        int misMatches = actualLabels.Zip(expectedLabels, (a, b) => a.Equals(b) ? 0 : 1).Sum();

                        return new PredictorClassificationMetricsEmbedded
                        {
                            TotalCount = rows.Count,
                            MissCount = misMatches,
                        };
                    }
                }
            }
        }

        static List<T> Slice<T>(List<T> rows, int startIndex, int length)
        {
            var max = Math.Min(startIndex + length, rows.Count);
            var fixedLength = max - startIndex;

            List<T> result = new List<T>();
            for (int i = 0; i < fixedLength; i++)
                result.Add(rows[startIndex + i]);

            return result;
        }

        static Value CreateValue(PredictorTrainingContext ctx, List<ResultRow> rows, List<PredictorResultColumn> columns, DeviceDescriptor device)
        {
            float[] values = new float[rows.Capacity * columns.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                var mainRow = rows[i];
                for (int j = 0; j < columns.Count; j++)
                {
                    var c = columns[j];
                    object value;
                    if (c.MultiColumn == null)
                        value = mainRow[c.PredictorColumnIndex.Value];
                    else
                    {
                        var dic = ctx.SubQueries[c.MultiColumn].GroupedValues;
                        var aggregateValues = dic.TryGetC(mainRow.Entity)?.TryGetC(c.Keys);
                        value = aggregateValues == null ? null : aggregateValues[c.PredictorColumnIndex.Value];
                    }

                    ref float box = ref values[i * columns.Count + j];

                    //TODO: Codification
                    switch (c.PredictorColumn.Encoding)
                    {
                        case PredictorColumnEncoding.None:
                            box = Convert.ToSingle(value);
                            break;
                        case PredictorColumnEncoding.OneHot:
                            box = Object.Equals(value, c.IsValue) ? 1 : 0;
                            break;
                        case PredictorColumnEncoding.Codified:
                            throw new NotImplementedException("Codified is not for Neuronal Networks");
                        default:
                            break;
                    }
                }
            }

            return Value.CreateBatch<float>(new int[] { columns.Count }, values, device);
        }

        static Function CreateLinearModel(Variable input, int outputDim, DeviceDescriptor device)
        {
            int inputDim = input.Shape[0];
            var weightParam = new Parameter(new int[] { outputDim, inputDim }, DataType.Float, 1, device, "W");
            var biasParam = new Parameter(new int[] { outputDim }, DataType.Float, 0, device, "b");

            return CNTKLib.Times(weightParam, input) + biasParam;
        }
    }
}
