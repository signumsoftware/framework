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
using Signum.Utilities.Reflection;
using System.Reflection;
using System.Diagnostics;
using Signum.Engine.Files;

namespace Signum.Engine.MachineLearning.CNTK
{
    public class CNTKNeuralNetworkPredictorAlgorithm : PredictorAlgorithm
    {
        public override string ValidateColumnProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEmbedded column, PropertyInfo pi)
        {
            if(pi.Name == nameof(column.Encoding))
            {
                var nn = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings; 
                switch (column.Encoding)
                {
                    case PredictorColumnEncoding.None:
                        if (!ReflectionTools.IsNumber(column.Token.Token.Type))
                            return PredictorMessage._0IsRequiredFor1.NiceToString(PredictorColumnEncoding.OneHot.NiceToString(), column.Token.Token.NiceTypeName);

                        if (column.Usage == PredictorColumnUsage.Output && nn.PredictionType == PredictionType.Classification)
                            return PredictorMessage._0NotSuportedFor1.NiceToString(column.Encoding.NiceToString(), nn.PredictionType.NiceToString());

                        break;
                    case PredictorColumnEncoding.OneHot:
                        if (ReflectionTools.IsDecimalNumber(column.Token.Token.Type))
                            return PredictorMessage._0NotSuportedFor1.NiceToString(column.Encoding.NiceToString(), predictor.Algorithm.NiceToString());

                        if (column.Usage == PredictorColumnUsage.Output && nn.PredictionType == PredictionType.Regression)
                            return PredictorMessage._0NotSuportedFor1.NiceToString(column.Encoding.NiceToString(), nn.PredictionType.NiceToString());
                        break;
                    case PredictorColumnEncoding.Codified:
                        return PredictorMessage._0NotSuportedFor1.NiceToString(column.Encoding.NiceToString(), predictor.Algorithm.NiceToString());
                }
            }

            return base.ValidateColumnProperty(predictor, subQuery, column, pi);
        }

        public override object[] Predict(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input)
        {
            throw new NotImplementedException();
        }

        //Errors with CNTK: https://github.com/Microsoft/CNTK/issues/2614
        public override void Train(PredictorTrainingContext ctx)
        {
            var p = ctx.Predictor;

            var nnSettings = (NeuralNetworkSettingsEntity)p.AlgorithmSettings;

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

            var totalMinibatches = (int)Math.Ceiling(training.Count / (float)nnSettings.MinibatchSize);
            var minibachtSize = nnSettings.MinibatchSize;
            var numMinibatches = nnSettings.NumMinibatches;
            
            int examples = 0;

            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < numMinibatches && !ctx.StopTraining; i++)
            {
                ctx.ReportProgress("Training Minibatches", (i + 1) / (decimal)numMinibatches);
                var trainSlice = Slice(training, (i % totalMinibatches) * minibachtSize, minibachtSize);
                using (Value inputValue = CreateValue(ctx, trainSlice, ctx.InputColumns, device))
                using (Value outputValue = CreateValue(ctx, trainSlice, ctx.OutputColumns, device))
                {
                    trainer.TrainMinibatch(new Dictionary<Variable, Value>()
                    {
                        { inputVariable, inputValue },
                        { outputVariable, outputValue },
                    }, device);

                    examples += trainSlice.Count;

                    var isLast = i == numMinibatches - 1;
                    if (i == numMinibatches - 1 || (i % nnSettings.SaveProgressEvery) == 0)
                    {
                        if((i % nnSettings.SaveValidationProgressEvery) == 0)
                        {

                        }

                        if(examples != trainer.TotalNumberOfSamplesSeen())
                        {

                        }

                        ctx.AddPredictorProgress(i,
                            examples,
                            sw,
                            lossTraining: trainer.PreviousMinibatchLossAverage(),
                            evaluationTraining: trainer.PreviousMinibatchEvaluationAverage(),
                            lossValidation: null,
                            evaluationValidation: null
                        );
                    }
                }
            }
            
            CTTKMinibatchEvaluator evaluator = new CTTKMinibatchEvaluator(ctx, inputVariable, calculatedOutpus, device);

            if (nnSettings.PredictionType == PredictionType.Classification)
            {  
                ctx.Predictor.ClassificationValidation = evaluator.ClasificationMetrics(validation, nameof(ctx.Predictor.ClassificationValidation));
                ctx.Predictor.ClassificationTraining = evaluator.ClasificationMetrics(training, nameof(ctx.Predictor.ClassificationTraining));
            }
            else
            {
                ctx.Predictor.RegressionValidation =null;
                ctx.Predictor.ClassificationTraining = null;
            }

            var fp = new Entities.Files.FilePathEmbedded(PredictorFileType.PredictorFile, "Model.cntk", new byte[0]);

            p.Files.Add(fp);

            p.Save();

            calculatedOutpus.Save(fp.FullPhysicalPath());
        }

        private static bool HasOneHot(PredictorEntity p)
        {
            return p.MainQuery.Columns.Any(a => a.Encoding == PredictorColumnEncoding.OneHot) ||
                p.SubQueries.Any(mc => mc.Aggregates.Any(a => a.Encoding == PredictorColumnEncoding.OneHot));
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
                    if (c.SubQuery == null)
                        value = mainRow[c.PredictorColumnIndex.Value];
                    else
                    {
                        var dic = ctx.SubQueries[c.SubQuery].GroupedValues;
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

        class CTTKMinibatchEvaluator
        {
            public PredictorTrainingContext ctx;
            public Variable inputVariable;
            public Function calculatedOutpus;
            public DeviceDescriptor device;

            public CTTKMinibatchEvaluator(PredictorTrainingContext ctx, Variable inputVariable, Function calculatedOutpus, DeviceDescriptor device)
            {
                this.ctx = ctx;
                this.inputVariable = inputVariable;
                this.calculatedOutpus = calculatedOutpus;
                this.device = device;
            }

            List<T> EvaluateByMiniBatch<T>(List<ResultRow> rows, string name, Func<(List<ResultRow> slice, Value outputValue, Value expectedValue), T> selector)
            {
                List<T> results = new List<T>();

                var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;
                var batches = (int)Math.Ceiling(rows.Count / (float)nnSettings.MinibatchSize);

                for (int i = 0; i < batches; i++)
                {
                    ctx.ReportProgress("Evaluating " + name, (i + 1) / (decimal)batches);
                    var slice = Slice(rows, i * nnSettings.MinibatchSize, nnSettings.MinibatchSize);

                    using (Value inputValue = CreateValue(ctx, slice, ctx.InputColumns, device))
                    {
                        var inputs = new Dictionary<Variable, Value> { { inputVariable, inputValue } };
                        var outputs = new Dictionary<Variable, Value> { { calculatedOutpus.Output, null } };
                        calculatedOutpus.Evaluate(inputs, outputs, device);

                        using (Value outputValue = outputs[calculatedOutpus.Output])
                        {
                            using (Value expectedValue = CreateValue(ctx, slice, ctx.OutputColumns, device))
                            {
                                results.Add(selector((slice: slice, outputValue: outputValue, expectedValue: expectedValue)));
                            }
                        }
                    }
                }

                return results;
            }

            public PredictorClassificationMetricsEmbedded ClasificationMetrics(List<ResultRow> rows, string name)
            {
                var missCount = EvaluateByMiniBatch(rows, name, tuple =>
                {
                    IList<IList<float>> actualLabelSoftMax = tuple.outputValue.GetDenseData<float>(calculatedOutpus.Output);
                    List<int> actualLabels = actualLabelSoftMax.Select((IList<float> l) => l.IndexOf(l.Max())).ToList();
                    
                    IList<IList<float>> expectedOneHot = tuple.expectedValue.GetDenseData<float>(calculatedOutpus.Output);
                    List<int> expectedLabels = expectedOneHot.Select(l => l.IndexOf(1.0F)).ToList();
                    int misMatches = actualLabels.Zip(expectedLabels, (a, b) => a.Equals(b) ? 0 : 1).Sum();

                    return misMatches;
                }).Sum();

                return new PredictorClassificationMetricsEmbedded
                {
                    MissCount = missCount,
                    TotalCount = rows.Count
                };
            }

        }
    }

    
}
