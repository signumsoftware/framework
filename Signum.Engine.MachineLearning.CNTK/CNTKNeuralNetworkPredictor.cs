using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.MachineLearning;
using CNTK;

namespace Signum.Engine.MachineLearning.CNTK
{
    public class CNTKNeuralNetworkPredictorAlgorithm : PredictorAlgorithm
    {
        private Value labels;

        public override object[] Predict(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input)
        {
            throw new NotImplementedException();
        }

        public override void Train(PredictorTrainingContext ctx)
        {
            var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;

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

            var batches = (int)Math.Ceiling(ctx.TrainigRows.Rows.Length / (float)nnSettings.MinibatchSize);

            for (int i = 0; i < batches; i++)
            {
                ctx.ReportProgress("Training Minibatches", i / (decimal)batches);
                trainer.TrainMinibatch(new Dictionary<Variable, Value>()
                {
                    { inputVariable, CreateValue(Slice(ctx.TrainigRows.Input, i * nnSettings.MinibatchSize, nnSettings.MinibatchSize), ctx.InputColumns, device) },
                    { outputVariable, CreateValue(Slice(ctx.TrainigRows.Output, i * nnSettings.MinibatchSize, nnSettings.MinibatchSize), ctx.OutputColumns, device) },
                }, device);
            }
            
            ctx.Predictor.TestStats = CalculateStats(ctx, ctx.TestRows, inputVariable, calculatedOutpus, device);
            ctx.Predictor.TrainingStats = CalculateStats(ctx, ctx.TrainigRows, inputVariable, calculatedOutpus, device);
        }

        private static PredictorStatsEmbedded CalculateStats(PredictorTrainingContext ctx, RowSelection rowSelection, Variable inputVariable, Function calculatedOutpus, DeviceDescriptor device)
        {
            var inputs = new Dictionary<Variable, Value> { { inputVariable, CreateValue(rowSelection.Input, ctx.InputColumns, device) } };
            var outputs = new Dictionary<Variable, Value> { { calculatedOutpus.Output, null } };
            calculatedOutpus.Evaluate(inputs, outputs, device);

            Value outputValue = outputs[calculatedOutpus.Output];
            IList<IList<float>> actualLabelSoftMax = outputValue.GetDenseData<float>(calculatedOutpus.Output);
            List<int> actualLabels = actualLabelSoftMax.Select((IList<float> l) => l.IndexOf(l.Max())).ToList();

            Value expectedValue = CreateValue(rowSelection.Output, ctx.OutputColumns, device);
            IList<IList<float>> expectedOneHot = expectedValue.GetDenseData<float>(calculatedOutpus.Output);
            List<int> expectedLabels = expectedOneHot.Select(l => l.IndexOf(1.0F)).ToList();
            int misMatches = actualLabels.Zip(expectedLabels, (a, b) => a.Equals(b) ? 0 : 1).Sum();

            return new PredictorStatsEmbedded
            {
                TotalCount = rowSelection.Rows.Length,
                ErrorCount = misMatches,
            };
        }

        static object[][] Slice(object[][] rows, int startIndex, int length)
        {
            var max = Math.Min(startIndex + length, rows.Length);
            var fixedLength = max - startIndex;

            object[][] result = new object[fixedLength][];
            for (int i = 0; i < result.Length; i++)
                result[i] = rows[startIndex + i];

            return result;
        }

        static Value CreateValue(object[][] rows, List<PredictorResultColumn> columns, DeviceDescriptor device)
        {
            float[] values = new float[rows.Length * columns.Count];
            for (int i = 0; i < rows.Length; i++)
            {
                var row = rows[i];
                for (int j = 0; j < row.Length; j++)
                {
                    values[i * columns.Count + j] = Convert.ToSingle(row[j]);
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
