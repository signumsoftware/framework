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
using Signum.Engine.Operations;

namespace Signum.Engine.MachineLearning.CNTK
{
    public class CNTKNeuralNetworkPredictorAlgorithm : IPredictorAlgorithm
    {
        public string ValidateColumnProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEmbedded column, PropertyInfo pi)
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

            return null;
        }

        //Errors with CNTK: https://github.com/Microsoft/CNTK/issues/2614
        public void Train(PredictorTrainingContext ctx)
        {
            var p = ctx.Predictor;

            var nn = (NeuralNetworkSettingsEntity)p.AlgorithmSettings;

            DeviceDescriptor device = GetDevice(nn);
            Variable inputVariable = Variable.InputVariable(new[] { ctx.InputColumns.Count }, DataType.Float, "input");
            Variable outputVariable = Variable.InputVariable(new[] { ctx.OutputColumns.Count }, DataType.Float, "output");
            
            Variable currentVar = inputVariable;
            nn.HiddenLayers.ForEach((layer, i) =>
            {
                currentVar = NetworkBuilder.DenseLayer(currentVar, layer.Size, device, layer.Activation, p.Settings.Seed ?? 0, "hidden" + i);
            });
            Function calculatedOutputs = NetworkBuilder.DenseLayer(currentVar, ctx.OutputColumns.Count, device, nn.OutputActivation, p.Settings.Seed ?? 0, "output");

            Function loss;
            Function evalError; 
            if (nn.PredictionType == PredictionType.Regression)
            {
                loss = CNTKLib.SquaredError(calculatedOutputs, outputVariable);
                evalError = CNTKLib.SquaredError(calculatedOutputs, outputVariable);
            }
            else if (nn.PredictionType == PredictionType.Classification)
            {
                loss = CNTKLib.CrossEntropyWithSoftmax(calculatedOutputs, outputVariable);
                evalError = CNTKLib.ClassificationError(calculatedOutputs, outputVariable);
            }
            else
            {
                throw new InvalidOperationException("Unexpected " + nn.PredictionType);
            }

            // prepare for training
            Learner learner = nn.LearningMomentum == null ?
                (Learner)Learner.SGDLearner(calculatedOutputs.Parameters(),
                new TrainingParameterScheduleDouble(nn.LearningRate, (uint)nn.MinibatchSize)) :

                (Learner)Learner.MomentumSGDLearner(calculatedOutputs.Parameters(),
                new TrainingParameterScheduleDouble(nn.LearningRate, (uint)nn.MinibatchSize),
                new TrainingParameterScheduleDouble(nn.LearningMomentum.Value, (uint)nn.MinibatchSize), false);

            Trainer trainer = Trainer.CreateTrainer(calculatedOutputs, loss, evalError, new List<Learner>() { learner });

            var (training, validation) = ctx.SplitTrainValidation();

            var totalMinibatches = (int)Math.Ceiling(training.Count / (float)nn.MinibatchSize);
            var minibachtSize = nn.MinibatchSize;
            var numMinibatches = nn.NumMinibatches;
            
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

                    var epoch = new EpochProgress
                    {
                        Ellapsed = sw.ElapsedMilliseconds,
                        Epoch = i,
                        TrainingExamples = examples,
                        LossTraining = trainer.PreviousMinibatchLossAverage(),
                        EvaluationTraining = trainer.PreviousMinibatchEvaluationAverage(),
                        LossValidation = null,
                        EvaluationValidation = null,
                    };

                    ctx.Progresses.Add(epoch);

                    var isLast = i == numMinibatches - 1;
                    if (i == numMinibatches - 1 || (i % nn.SaveProgressEvery) == 0)
                    {
                        if((i % nn.SaveValidationProgressEvery) == 0)
                        {
                        }

                        if(examples != trainer.TotalNumberOfSamplesSeen())
                        {

                        }

                        epoch.SaveEntity(ctx.Predictor);
                    }
                }
            }

            p = ctx.Predictor = ctx.Predictor.ToLite().Retrieve();
            
            CTTKMinibatchEvaluator evaluator = new CTTKMinibatchEvaluator(ctx, inputVariable, calculatedOutputs, device);

            if (nn.PredictionType == PredictionType.Classification)
            {  
                p.ClassificationValidation = evaluator.ClasificationMetrics(validation, nameof(p.ClassificationValidation));
                p.ClassificationTraining = evaluator.ClasificationMetrics(training, nameof(p.ClassificationTraining));
            }
            else
            {
                p.RegressionValidation =null;
                p.ClassificationTraining = null;
            }

            var fp = new Entities.Files.FilePathEmbedded(PredictorFileType.PredictorFile, "Model.cntk", new byte[0]);

            p.Files.Add(fp);

            using (OperationLogic.AllowSave<PredictorEntity>())
                p.Save();

            calculatedOutputs.Save(fp.FullPhysicalPath());
        }
        
        private DeviceDescriptor GetDevice(NeuralNetworkSettingsEntity nnSettings)
        {
            return DeviceDescriptor.CPUDevice;
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

        static Value CreateValue(PredictorTrainingContext ctx, List<ResultRow> rows, List<PredictorCodification> columns, DeviceDescriptor device)
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
                            throw new NotImplementedException("Unexpected encoding ");

                    }
                }
            }

            return Value.CreateBatch<float>(new int[] { columns.Count }, values, device);
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

        public PredictDictionary Predict(PredictorPredictContext ctx, PredictDictionary input)
        {
            var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;
            Function calculatedOutputs = (Function)ctx.Model;

            var device = GetDevice(nnSettings);

            Value inputValue = GetValue(ctx, input, device);

            var inputVar = calculatedOutputs.Inputs.SingleEx(i => i.Name == "input");
            var inputDic = new Dictionary<Variable, Value> { { inputVar, inputValue } };
            var outputDic = new Dictionary<Variable, Value> { { calculatedOutputs, null } };
            calculatedOutputs.Evaluate(inputDic, outputDic, device);

            Value output = outputDic[calculatedOutputs];
            float[] values = output.GetDenseData<float>(calculatedOutputs).SingleEx().ToArray();

            var result = GetPredictionDictionary(values, ctx);

            return result;
        }

        private PredictDictionary GetPredictionDictionary(float[] outputValues, PredictorPredictContext ctx)
        {
            return new PredictDictionary
            {
                MainQueryValues = ctx.MainQueryOutputColumn.SelectDictionary(col => col.Token.Token, (col, list) => FloatToValue(col, list, outputValues)),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary
                {
                    SubQuery = sq,
                    SubQueryGroups = ctx.SubQueryOutputColumn.TryGetC(sq)?.Groups.SelectDictionary(grKey => grKey, dic => dic
                    .Where(a => a.Key.Usage == PredictorColumnUsage.Output)
                    .ToDictionary(a => a.Key.Token.Token, a => FloatToValue(a.Key, a.Value, outputValues))
                    )
                })
            };
        }

        private object FloatToValue(PredictorColumnEmbedded key, List<PredictorCodification> cols, float[] outputValues)
        {
            switch (key.Encoding)
            {
                case PredictorColumnEncoding.None:
                    return ReflectionTools.ChangeType(outputValues[cols.SingleEx().Index], key.Token.Token.Type);
                case PredictorColumnEncoding.OneHot:
                    return cols.WithMax(c => outputValues[c.Index]).IsValue;
                case PredictorColumnEncoding.Codified:
                    throw new InvalidOperationException("Codified");
                default:
                    throw new InvalidOperationException("Unexpected encoding");
            }
        }

        private Value GetValue(PredictorPredictContext ctx, PredictDictionary input, DeviceDescriptor device)
        {
            if (input.SubQueries.Values.Any(a => a.SubQueryGroups.Comparer != ObjectArrayComparer.Instance))
                throw new Exception("Unexpected dictionary comparer");

            float[] values = new float[ctx.InputColumns.Count];
            for (int i = 0; i < ctx.InputColumns.Count; i++)
            {
                values[i] = GetFloat(ctx, input, ctx.InputColumns[i]);
            }


            return Value.CreateBatch<float>(new int[] { ctx.InputColumns.Count }, values, device);
        }

        private float GetFloat(PredictorPredictContext ctx, PredictDictionary input, PredictorCodification c)
        {
            object value;
            if (c.SubQuery != null)
            {
                var sq = input.SubQueries.GetOrThrow(c.SubQuery);

                var dic = sq.SubQueryGroups.TryGetC(c.Keys);

                value = dic == null ? null : dic.GetOrThrow(c.PredictorColumn.Token.Token);
            }
            else
            {
                value = input.MainQueryValues.GetOrThrow(c.PredictorColumn.Token.Token);
            }

            if(value == null)
            {
                switch (c.PredictorColumn.NullHandling)
                {
                    case PredictorColumnNullHandling.Error:
                        throw new Exception($"Null found on {c.PredictorColumn.Token} of {c.SubQuery?.ToString() ?? "MainQuery"}");
                    case PredictorColumnNullHandling.Zero:
                        return 0;
                    case PredictorColumnNullHandling.MinValue:
                        return (float)c.Values[0];
                    case PredictorColumnNullHandling.AvgValue:
                        return (float)c.Values[1];
                    case PredictorColumnNullHandling.MaxValue:
                        return (float)c.Values[2];
                    default:
                        throw new NotImplementedException("Unexpected Null handling");
                }
            }

            switch (c.PredictorColumn.Encoding)
            {
                case PredictorColumnEncoding.None:
                    return Convert.ToSingle(value);
                case PredictorColumnEncoding.OneHot:
                    return value.Equals(c.IsValue) ? 1 : 0;
                case PredictorColumnEncoding.Codified:
                    throw new NotImplementedException("Codified is not for Neuronal Networks");
                default:
                    throw new NotImplementedException("Unexpected encoding ");

            }
        }

        public void LoadModel(PredictorPredictContext ctx)
        {
            var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;

            ctx.Model = Function.Load(ctx.Predictor.Files.SingleEx().FullPhysicalPath(), GetDevice(nnSettings));
        }
    }
}
