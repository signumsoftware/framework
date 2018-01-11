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
using Signum.Entities.UserAssets;

namespace Signum.Engine.MachineLearning.CNTK
{
    public class CNTKNeuralNetworkPredictorAlgorithm : IPredictorAlgorithm
    {
        public void InitialSetup()
        {
            /// This is a workaround to load unmanaged CNTK dlls from the applications \bin directory.
            var dir = AppDomain.CurrentDomain.BaseDirectory + "/bin";
            var oldPath = Environment.GetEnvironmentVariable("Path");
            Environment.SetEnvironmentVariable("Path", dir + ";" + oldPath, EnvironmentVariableTarget.Process);
        }

        public string ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEncoding encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            var nn = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            switch (encoding)
            {
                case PredictorColumnEncoding.None:
                    if (!ReflectionTools.IsNumber(token.Token.Type) && token.Token.Type.UnNullify() != typeof(bool))
                        return PredictorMessage._0IsRequiredFor1.NiceToString(PredictorColumnEncoding.OneHot.NiceToString(), token.Token.NiceTypeName);

                    if (usage == PredictorColumnUsage.Output && (nn.PredictionType == PredictionType.Classification || nn.PredictionType == PredictionType.MultiClassification))
                        return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), nn.PredictionType.NiceToString());

                    break;
                case PredictorColumnEncoding.OneHot:
                    if (ReflectionTools.IsDecimalNumber(token.Token.Type))
                        return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), predictor.Algorithm.NiceToString());

                    if (usage == PredictorColumnUsage.Output && (nn.PredictionType == PredictionType.Regression || nn.PredictionType == PredictionType.MultiRegression))
                        return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), nn.PredictionType.NiceToString());
                    break;
                case PredictorColumnEncoding.Codified:
                    return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), predictor.Algorithm.NiceToString());
            }

            return null;
        }
        
        public string[] GetAvailableDevices()
        {
            return DeviceDescriptor.AllDevices().Select(a => a.AsString()).ToArray();
        }

        private DeviceDescriptor GetDevice(NeuralNetworkSettingsEntity nnSettings)
        {
            if (!nnSettings.Device.HasText())
                return DeviceDescriptor.UseDefaultDevice();

            var dev = DeviceDescriptor.AllDevices().FirstOrDefault(a => a.AsString() == nnSettings.Device);
            if(dev == null)
                return DeviceDescriptor.UseDefaultDevice();

            return dev;
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
                currentVar = NetworkBuilder.DenseLayer(currentVar, layer.Size, device, layer.Activation, layer.Initializer, p.Settings.Seed ?? 0, "hidden" + i);
            });
            Function calculatedOutputs = NetworkBuilder.DenseLayer(currentVar, ctx.OutputColumns.Count, device, nn.OutputActivation, nn.OutputInitializer, p.Settings.Seed ?? 0, "output");

            Function loss = NetworkBuilder.GetEvalFunction(nn.LossFunction, calculatedOutputs, outputVariable);
            Function evalError = NetworkBuilder.GetEvalFunction(nn.EvalErrorFunction, calculatedOutputs, outputVariable);
           
            // prepare for training
            Learner learner = NetworkBuilder.GetInitializer(calculatedOutputs.Parameters(), nn);

            Trainer trainer = Trainer.CreateTrainer(calculatedOutputs, loss, evalError, new List<Learner>() { learner });

            Random rand = p.Settings.Seed == null ?
                new Random() :
                new Random(p.Settings.Seed.Value);

            var (training, validation) = ctx.SplitTrainValidation(rand);

            var minibachtSize = nn.MinibatchSize;
            var numMinibatches = nn.NumMinibatches;            

            Stopwatch sw = Stopwatch.StartNew();
            for (int i = 0; i < numMinibatches && !ctx.StopTraining; i++)
            {
                ctx.ReportProgress("Training Minibatches", (i + 1) / (decimal)numMinibatches);
                {
                    var trainMinibatch = 0.To(minibachtSize).Select(_ => rand.NextElement(training)).ToList();
                    using (Value inputValue = CreateValue(ctx, trainMinibatch, ctx.InputColumns, device))
                    using (Value outputValue = CreateValue(ctx, trainMinibatch, ctx.OutputColumns, device))
                    {
                        trainer.TrainMinibatch(new Dictionary<Variable, Value>()
                        {
                            { inputVariable, inputValue },
                            { outputVariable, outputValue },
                        }, device);
                    }
                }
                var ep = new EpochProgress
                {
                    Ellapsed = sw.ElapsedMilliseconds,
                    Epoch = i,
                    TrainingExamples = (int)trainer.TotalNumberOfSamplesSeen(),
                    LossTraining = trainer.PreviousMinibatchLossAverage(),
                    EvaluationTraining = trainer.PreviousMinibatchEvaluationAverage(),
                    LossValidation = null,
                    EvaluationValidation = null,
                };

                ctx.Progresses.Add(ep);

                var isLast = i == numMinibatches - 1;
                if (isLast || (i % nn.SaveProgressEvery) == 0)
                {
                    if(isLast || (i % nn.SaveValidationProgressEvery) == 0)
                    {
                        var validateMinibatch = 0.To(minibachtSize).Select(_ => rand.NextElement(validation)).ToList();
                        using (Value inputValValue = CreateValue(ctx, validateMinibatch, ctx.InputColumns, device))
                        using (Value outputValValue = CreateValue(ctx, validateMinibatch, ctx.OutputColumns, device))
                        {
                            var inputs = new Dictionary<Variable, Value>()
                            {
                                { inputVariable, inputValValue },
                                { outputVariable, outputValValue },
                            };
                            
                            ep.LossValidation = loss.EvaluateAvg(inputs, device);
                            ep.EvaluationValidation = evalError.EvaluateAvg(inputs, device);
                        }
                    }
                    
                    ep.SaveEntity(ctx.Predictor);
                }
                
            }

            p = ctx.Predictor = ctx.Predictor.ToLite().Retrieve();
            
            CTTKMinibatchEvaluator evaluator = new CTTKMinibatchEvaluator(ctx, inputVariable, calculatedOutputs, device);


            var last  = p.EpochProgresses().OrderByDescending(a => a.Id).FirstEx();

            p.ResultTraining = new PredictorMetricsEmbedded { Evaluation = last.EvaluationTraining, Loss = last.LossTraining };
            p.ResultValidation = new PredictorMetricsEmbedded { Evaluation = last.EvaluationValidation, Loss = last.LossValidation };

            if (nn.PredictionType == PredictionType.Classification || nn.PredictionType == PredictionType.MultiClassification)
            {  
                p.ClassificationValidation = evaluator.ClasificationMetrics(validation, nameof(p.ClassificationValidation));
                p.ClassificationTraining = evaluator.ClasificationMetrics(training, nameof(p.ClassificationTraining));
            }
            else
            {
                p.RegressionValidation = evaluator.RegressionMetrics(validation, nameof(p.RegressionValidation), nn.PredictionType == PredictionType.MultiRegression);
                p.RegressionTraining = evaluator.RegressionMetrics(training, nameof(p.RegressionValidation), nn.PredictionType == PredictionType.MultiRegression);
            }

            var fp = new Entities.Files.FilePathEmbedded(PredictorFileType.PredictorFile, "Model.cntk", new byte[0]);

            p.Files.Add(fp);

            using (OperationLogic.AllowSave<PredictorEntity>())
                p.Save();

            calculatedOutputs.Save(fp.FullPhysicalPath());
        }

 

        static Value CreateValue(PredictorTrainingContext ctx, List<ResultRow> rows, List<PredictorCodification> codifications, DeviceDescriptor device)
        {
            float[] values = new float[rows.Count * codifications.Count];
            for (int i = 0; i < rows.Count; i++)
            {
                var mainRow = rows[i];
                var mainKey = ctx.MainQuery.GetParentKey(mainRow);

                for (int j = 0; j < codifications.Count; j++)
                {
                    PredictorCodification c = codifications[j];
                    object value;
                    if (c.SubQuery == null)
                        value = mainRow[c.PredictorColumnIndex];
                    else
                    {
                        var sq = ctx.SubQueries.GetOrThrow(c.SubQuery);
                        var rowValues = sq.GroupedValues.TryGetC(mainKey)?.TryGetC(c.Keys);
                        value = rowValues == null ? null : rowValues[sq.ColumnIndexToValueIndex[c.PredictorColumnIndex]];
                    }

                    values[i * codifications.Count + j] = GetFloat(value, c);
                }
            }

            return Value.CreateBatch<float>(new int[] { codifications.Count }, values, device);
        }

        public static float minLog = -5;

        private static float GetFloat(object value, PredictorCodification c)
        {
            var valueDefault = value ?? GetDefaultValue(c);

            //TODO: Codification
            switch (c.Encoding)
            {
                case PredictorColumnEncoding.None: return Convert.ToSingle(valueDefault);
                case PredictorColumnEncoding.OneHot: return Object.Equals(valueDefault, c.IsValue) ? 1 : 0;
                case PredictorColumnEncoding.Codified: throw new NotImplementedException("Codified is not usable for Neural Networks");
                case PredictorColumnEncoding.NormalizeZScore: return (Convert.ToSingle(valueDefault) - c.Average.Value) / c.StdDev.Value;
                case PredictorColumnEncoding.NormalizeMinMax: return (Convert.ToSingle(valueDefault) - c.Min.Value) / (c.Max.Value - c.Min.Value);
                case PredictorColumnEncoding.NormalizeLog:
                    {
                        var val = Convert.ToDouble(valueDefault);
                        return (val <= 0 ? minLog : Math.Max(minLog, (float)Math.Log(val)));
                    }
                default: throw new NotImplementedException("Unexpected encoding " + c.Encoding);
            }
        }

        private static object GetDefaultValue(PredictorCodification c)
        {
            switch (c.NullHandling)
            {
                case PredictorColumnNullHandling.Zero: return 0;
                case PredictorColumnNullHandling.Error: throw new Exception($"Null found on {c.Token} of {c.SubQuery?.ToString() ?? "MainQuery"}");
                case PredictorColumnNullHandling.Average: return c.Average;
                case PredictorColumnNullHandling.Min: return c.Min;
                case PredictorColumnNullHandling.Max: return c.Max;
                default: throw new NotImplementedException("Unexpected NullHanndling " + c.NullHandling);
            }
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

            static List<T> Slice<T>(List<T> rows, int startIndex, int length)
            {
                var max = Math.Min(startIndex + length, rows.Count);
                var fixedLength = max - startIndex;

                List<T> result = new List<T>();
                for (int i = 0; i < fixedLength; i++)
                    result.Add(rows[startIndex + i]);

                return result;
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

            internal PredictorRegressionMetricsEmbedded RegressionMetrics(List<ResultRow> rows, string name, bool multiRegression)
            {
                List<(float predicted, float expected)> pairs = new List<(float predicted, float expected)>();

                EvaluateByMiniBatch(rows, name, tuple =>
                {
                    IList<IList<float>> predictedOutput = tuple.outputValue.GetDenseData<float>(calculatedOutpus.Output);
                    IList<IList<float>> expectedOutput = tuple.expectedValue.GetDenseData<float>(calculatedOutpus.Output);
                    var jCount = predictedOutput.Count == expectedOutput.Count ? predictedOutput.Count : throw new InvalidOperationException("Count missmatch");
                    for (int j = 0; j < jCount; j++)
                    {
                        var po = predictedOutput[j];
                        var eo = expectedOutput[j];

                        foreach (var c in this.ctx.OutputColumns)
                        {
                            switch (c.Encoding)
                            {
                                case PredictorColumnEncoding.None:
                                    break;
                                case PredictorColumnEncoding.NormalizeZScore:
                                case PredictorColumnEncoding.NormalizeMinMax:
                                case PredictorColumnEncoding.NormalizeLog:
                                    po[c.Index] = c.Denormalize(po[c.Index]);
                                    eo[c.Index] = c.Denormalize(eo[c.Index]);
                                    break;
                                default:
                                    throw new NotImplementedException(c.Encoding + " not supported");
                            }
                        }

                        var iCount = po.Count == eo.Count ? po.Count : throw new InvalidOperationException("Count missmatch");

                        if (multiRegression)
                        {
                            pairs.Add((predicted: po.Sum(), expected: eo.Sum()));
                        }
                        else {
                            for (int i = 0; i < iCount; i++)
                            {
                                pairs.Add((predicted: po[i], expected: eo[i]));
                            }
                        }
                    }

                    return 0;
                });

                var mse = pairs.Average(p => Error(p) * Error(p));

                var result = new PredictorRegressionMetricsEmbedded
                {
                    MeanError = pairs.Average(p => Error(p)).CleanDouble(),
                    MeanAbsoluteError = pairs.Average(p => Math.Abs(Error(p))).CleanDouble(),
                    MeanSquaredError = mse.CleanDouble(),
                    RootMeanSquareError = Math.Sqrt(mse).CleanDouble(),
                    MeanPercentageError = pairs.Average(p => SafeDiv(Error(p), p.expected)).CleanDouble(),
                    MeanAbsolutePercentageError = pairs.Average(p => Math.Abs(SafeDiv(Error(p), p.expected))).CleanDouble(),
                };

                return result;
            }

            private double SafeDiv(double dividend, double divisor)
            {
                if (divisor == 0)
                    return Math.Abs(dividend - divisor) < 0.0001 ? 0 : 1;

                return dividend / divisor;
            }

            private double Error((float predicted, float expected) p)
            {
                return p.predicted - p.expected;
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
            return new PredictDictionary(ctx.Predictor)
            {
                MainQueryValues = ctx.MainQueryOutputColumn.SelectDictionary(col => col, (col, list) => FloatToValue(col.Encoding, col.Token.Token, list, outputValues)),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = ctx.SubQueryOutputColumn.TryGetC(sq)?.Groups.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => kvp.Value
                        .Where(a => a.Key.Usage ==  PredictorSubQueryColumnUsage.Output)
                        .ToDictionary(a => a.Key, a => FloatToValue(a.Key.Encoding.Value, a.Key.Token.Token, a.Value, outputValues)), 
                        ObjectArrayComparer.Instance
                    ) ?? new Dictionary<object[], Dictionary<PredictorSubQueryColumnEmbedded, object>>(ObjectArrayComparer.Instance),
                })
            };
        }

        private object FloatToValue(PredictorColumnEncoding encoding, QueryToken token, List<PredictorCodification> cols, float[] outputValues)
        {
            switch (encoding)
            {
                case PredictorColumnEncoding.None:
                    {
                        var c = cols.SingleEx();
                        return ReflectionTools.ChangeType(outputValues[c.Index], token.Type);
                    }
                case PredictorColumnEncoding.OneHot:return cols.WithMax(c => outputValues[c.Index]).IsValue;
                case PredictorColumnEncoding.NormalizeZScore:
                case PredictorColumnEncoding.NormalizeMinMax:
                case PredictorColumnEncoding.NormalizeLog:
                    {
                        var c = cols.SingleEx();
                        var value = outputValues[c.Index];
                        var newValue = c.Denormalize(value);
                        return ReflectionTools.ChangeType(newValue, token.Type);
                    }
                case PredictorColumnEncoding.Codified: throw new InvalidOperationException("Codified");
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
                var c = ctx.InputColumns[i];
                object value;
                if (c.SubQuery != null)
                {
                    var sq = input.SubQueries.GetOrThrow(c.SubQuery);

                    var dic = sq.SubQueryGroups.TryGetC(c.Keys);

                    value = dic == null ? null : dic.GetOrThrow(c.PredictorSubQueryColumn);
                }
                else
                {
                    value = input.MainQueryValues.GetOrThrow(c.PredictorColumn);
                }

                values[i] = GetFloat(value, c);
            }


            return Value.CreateBatch<float>(new int[] { ctx.InputColumns.Count }, values, device);
        }

        public void LoadModel(PredictorPredictContext ctx)
        {
            var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;

            ctx.Model = Function.Load(ctx.Predictor.Files.SingleEx().FullPhysicalPath(), GetDevice(nnSettings));
        }
    }
}
