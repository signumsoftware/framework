using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.MachineLearning;
using CNTK;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using System.Diagnostics;
using Signum.Engine.Files;
using Signum.Engine.Operations;
using Signum.Entities.UserAssets;
using System.IO;


namespace Signum.Engine.MachineLearning.CNTK
{
    public class CNTKNeuralNetworkPredictorAlgorithm : IPredictorAlgorithm
    {
        public Dictionary<PredictorColumnEncodingSymbol, ICNTKEncoding> Encodings = new Dictionary<PredictorColumnEncodingSymbol, ICNTKEncoding>
        {
            { DefaultColumnEncodings.None, new NoneCNTKEncoding() },
            { DefaultColumnEncodings.OneHot, new OneHotCNTKEncoding() },
            { DefaultColumnEncodings.NormalizeZScore, new NormalizeZScoreCNTKEncoding() },
            { DefaultColumnEncodings.NormalizeMinMax, new NormalizeMinMaxCNTKEncoding() },
            { DefaultColumnEncodings.NormalizeLog, new NormalizeLogCNTKEncoding() },
            { DefaultColumnEncodings.SplitWords, new SplitWordsCNTKEncoding() },
        };

        public void InitialSetup()
        {
            if (!Environment.Is64BitProcess)
                throw new InvalidOperationException("CNTK only works with starting projects compiled for x64");


            /// This is a workaround to load unmanaged CNTK dlls from the applications \bin directory.
            var dir = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.GetFiles(dir, "Cntk.Core.*.dll").Any())
            {
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "bin");
                if (!Directory.Exists(dir) || !Directory.GetFiles(dir, "Cntk.Core.*.dll").Any())
                    throw new InvalidOperationException($@"No CNTK dll found in {AppDomain.CurrentDomain.BaseDirectory} or {Path.Combine(AppDomain.CurrentDomain.BaseDirectory!, "bin")}");
            }

            var oldPath = Environment.GetEnvironmentVariable("Path")!;
            if (!oldPath.Contains(dir + ";"))
                Environment.SetEnvironmentVariable("Path", dir + ";" + oldPath, EnvironmentVariableTarget.Process);
        }

        public string? ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity? subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            return Encodings.GetOrThrow(encoding).ValidateEncodingProperty(predictor, subQuery, encoding, usage, token);
        }

        public List<PredictorCodification> GenerateCodifications(PredictorColumnEncodingSymbol encoding, ResultColumn resultColumn, PredictorColumnBase column)
        {
            return Encodings.GetOrThrow(encoding).GenerateCodifications(resultColumn, column);
        }

        public IEnumerable<PredictorColumnEncodingSymbol> GetRegisteredEncodingSymbols()
        {
            return Encodings.Keys;
        }

        public string[] GetAvailableDevices()
        {
            InitialSetup();
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
            InitialSetup();
            var p = ctx.Predictor;

            var nn = (NeuralNetworkSettingsEntity)p.AlgorithmSettings;

            DeviceDescriptor device = GetDevice(nn);
            Variable inputVariable = Variable.InputVariable(new[] { ctx.InputCodifications.Count }, DataType.Float, "input");
            Variable outputVariable = Variable.InputVariable(new[] { ctx.OutputCodifications.Count }, DataType.Float, "output");

            Variable currentVar = inputVariable;
            nn.HiddenLayers.ForEach((layer, i) =>
            {
                currentVar = NetworkBuilder.DenseLayer(currentVar, layer.Size, device, layer.Activation, layer.Initializer, p.Settings.Seed ?? 0, "hidden" + i);
            });
            Function calculatedOutputs = NetworkBuilder.DenseLayer(currentVar, ctx.OutputCodifications.Count, device, nn.OutputActivation, nn.OutputInitializer, p.Settings.Seed ?? 0, "output");

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
            List<FinalCandidate> candidate = new List<FinalCandidate>();
            for (int i = 0; i < numMinibatches; i++)
            {
                using (HeavyProfiler.Log("MiniBatch", () => i.ToString()))
                {
                    ctx.ReportProgress("Training Minibatches", (i + 1) / (decimal)numMinibatches);

                    {
                        var trainMinibatch = 0.To(minibachtSize).Select(_ => rand.NextElement(training)).ToList();
                        using (Value inputValue = CreateValue(ctx, trainMinibatch, ctx.InputCodifications.Count, ctx.InputCodificationsByColumn, device))
                        using (Value outputValue = CreateValue(ctx, trainMinibatch, ctx.OutputCodifications.Count, ctx.OutputCodificationsByColumn, device))
                        {
                            using (HeavyProfiler.Log("TrainMinibatch", () => i.ToString()))
                                trainer.TrainMinibatch(new Dictionary<Variable, Value>()
                                {
                                    { inputVariable, inputValue },
                                    { outputVariable, outputValue },
                                }, false, device);
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

                    ctx.Progresses.Enqueue(ep);

                    if (ctx.StopTraining)
                        p = ctx.Predictor = ctx.Predictor.ToLite().RetrieveAndRemember();

                    var isLast = numMinibatches - nn.BestResultFromLast <= i;
                    if (isLast || (i % nn.SaveProgressEvery) == 0 || ctx.StopTraining)
                    {
                        if (isLast || (i % nn.SaveValidationProgressEvery) == 0 || ctx.StopTraining)
                        {
                            using (HeavyProfiler.LogNoStackTrace("Validation"))
                            {
                                var validateMinibatch = 0.To(minibachtSize).Select(_ => rand.NextElement(validation)).ToList();

                                using (Value inputValValue = CreateValue(ctx, validateMinibatch, ctx.InputCodifications.Count, ctx.InputCodificationsByColumn, device))
                                using (Value outputValValue = CreateValue(ctx, validateMinibatch, ctx.OutputCodifications.Count, ctx.OutputCodificationsByColumn, device))
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
                        }

                        var progress = ep.SaveEntity(ctx.Predictor);

                        if (isLast || ctx.StopTraining)
                        {
                            using (HeavyProfiler.LogNoStackTrace("FinalCandidate"))
                            {
                                candidate.Add(new FinalCandidate
                                {
                                    Model = calculatedOutputs.Save(),

                                    ResultTraining = new PredictorMetricsEmbedded { Evaluation = progress.EvaluationTraining, Loss = progress.LossTraining },
                                    ResultValidation = new PredictorMetricsEmbedded { Evaluation = progress.EvaluationValidation, Loss = progress.LossValidation },
                                });
                            }
                        }
                    }

                    if (ctx.StopTraining)
                        break;
                }
            }

            var best = candidate.WithMin(a => a.ResultValidation.Loss!.Value);

            p.ResultTraining = best.ResultTraining;
            p.ResultValidation = best.ResultValidation;

            var fp = new Entities.Files.FilePathEmbedded(PredictorFileType.PredictorFile, "Model.cntk", best.Model);

            p.Files.Add(fp);

            using (OperationLogic.AllowSave<PredictorEntity>())
                p.Save();
        }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
        public class FinalCandidate
        {
            public byte[] Model;
            public PredictorMetricsEmbedded ResultTraining;
            public PredictorMetricsEmbedded ResultValidation;
        }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

        Value CreateValue(PredictorTrainingContext ctx, List<ResultRow> rows, int codificationCount, Dictionary<PredictorColumnBase, List<PredictorCodification>> codificationByColumn, DeviceDescriptor device)
        {
            using (HeavyProfiler.Log("CreateValue", () => $"Rows {rows.Count} Codifications {codificationCount}"))
            {
                float[] inputValues = new float[rows.Count * codificationCount];
                for (int i = 0; i < rows.Count; i++)
                {
                    ResultRow mainRow = rows[i];
                    var mainKey = ctx.MainQuery.GetParentKey(mainRow);

                    int offset = i * codificationCount;

                    foreach (var kvp in codificationByColumn)
                    {
                        PredictorColumnBase col = kvp.Key;
                        object? value;
                        if (col is PredictorColumnMain pcm)
                        {
                            value = mainRow[pcm.PredictorColumnIndex];
                        }
                        else if (col is PredictorColumnSubQuery pcsq)
                        {
                            SubQuery sq = ctx.SubQueries.GetOrThrow(pcsq.SubQuery);
                            object?[]? rowValues = sq.GroupedValues.TryGetC(mainKey)?.TryGetC(pcsq.Keys);
                            value = rowValues == null ? null : rowValues[sq.ColumnIndexToValueIndex[pcsq.PredictorColumnIndex]];
                        }
                        else
                        {
                            throw new UnexpectedValueException(col);
                        }

                        using (HeavyProfiler.LogNoStackTrace("EncodeValue"))
                        {
                            ICNTKEncoding encoding = Encodings.GetOrThrow(col.Encoding);

                            encoding.EncodeValue(value ?? CNTKDefault.GetDefaultValue(kvp.Value.FirstOrDefault()), col, kvp.Value, inputValues, offset);
                        }
                    }
                }

                using (HeavyProfiler.LogNoStackTrace("CreateBatch"))
                    return Value.CreateBatch<float>(new int[] { codificationCount }, inputValues, device);
            }
        }

        public PredictDictionary Predict(PredictorPredictContext ctx, PredictDictionary input)
        {
            return PredictMultiple(ctx, new List<PredictDictionary> { input }).SingleEx();
        }

        public List<PredictDictionary> PredictMultiple(PredictorPredictContext ctx, List<PredictDictionary> inputs)
        {
            using (HeavyProfiler.LogNoStackTrace("PredictMultiple"))
            {
                var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;
                lock (lockKey) //https://docs.microsoft.com/en-us/cognitive-toolkit/cntk-library-evaluation-on-windows#evaluation-of-multiple-requests-in-parallel
                {
                    Function calculatedOutputs = (Function)ctx.Model!;
                    var device = GetDevice(nnSettings);
                    Value inputValue = GetValueForPredict(ctx, inputs, device);

                    var inputVar = calculatedOutputs.Inputs.SingleEx(i => i.Name == "input");
                    var inputDic = new Dictionary<Variable, Value> { { inputVar, inputValue } };
                    var outputDic = new Dictionary<Variable, Value> { { calculatedOutputs, null! } };

                    calculatedOutputs.Evaluate(inputDic, outputDic, device);

                    Value output = outputDic[calculatedOutputs];
                    IList<IList<float>> values = output.GetDenseData<float>(calculatedOutputs);
                    var result = values.Select((val, i) => GetPredictionDictionary(val.ToArray(), ctx, inputs[i].Options!)).ToList();
                    return result;
                }
            }
        }

        private PredictDictionary GetPredictionDictionary(float[] outputValues, PredictorPredictContext ctx, PredictionOptions options)
        {
            using (HeavyProfiler.LogNoStackTrace("GetPredictionDictionary"))
            {
                return new PredictDictionary(ctx.Predictor, options, null)
                {
                    MainQueryValues = ctx.MainOutputCodifications.SelectDictionary(col => col,
                    (col, list) => Encodings.GetOrThrow(col.Encoding).DecodeValue(list.First().Column, list, outputValues, options)),

                    SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                    {
                        SubQueryGroups = ctx.SubQueryOutputCodifications.TryGetC(sq)?.Groups.ToDictionary(
                            kvp => kvp.Key,
                            kvp => kvp.Value
                            .Where(a => a.Key.Usage == PredictorSubQueryColumnUsage.Output)
                            .ToDictionary(a => a.Key, a => Encodings.GetOrThrow(a.Key.Encoding).DecodeValue(a.Value.FirstEx().Column, a.Value, outputValues, options)),
                            ObjectArrayComparer.Instance
                        ) ?? new Dictionary<object?[], Dictionary<PredictorSubQueryColumnEmbedded, object?>>(ObjectArrayComparer.Instance),

                    })
                };
            }
        }

        private Value GetValueForPredict(PredictorPredictContext ctx, List<PredictDictionary> inputs, DeviceDescriptor device)
        {
            using (HeavyProfiler.Log("GetValueForPredict", () => $"Inputs {inputs.Count} Codifications {ctx.InputCodifications.Count}"))
            {
                if (inputs.First().SubQueries.Values.Any(a => a.SubQueryGroups.Comparer != ObjectArrayComparer.Instance))
                    throw new Exception("Unexpected dictionary comparer");

                float[] inputValues = new float[inputs.Count * ctx.InputCodifications.Count];
                var groups = ctx.InputCodificationsByColumn;
                for (int i = 0; i < inputs.Count; i++)
                {
                    PredictDictionary input = inputs[i];
                    int offset = i * ctx.InputCodifications.Count;

                    foreach (var kvp in groups)
                    {
                        PredictorColumnBase col = kvp.Key;
                        object? value;
                        if (col is PredictorColumnMain pcm)
                        {
                            value = input.MainQueryValues.GetOrThrow(pcm.PredictorColumn);
                        }
                        else if (col is PredictorColumnSubQuery pcsq)
                        {
                            var sq = input.SubQueries.GetOrThrow(pcsq.SubQuery);

                            var dic = sq.SubQueryGroups.TryGetC(pcsq.Keys);

                            value = dic == null ? null : dic.GetOrThrow(pcsq.PredictorSubQueryColumn);
                        }
                        else
                        {
                            throw new UnexpectedValueException(col);
                        }

                        using (HeavyProfiler.LogNoStackTrace("EncodeValue"))
                        {
                            var enc = Encodings.GetOrThrow(col.Encoding);
                            enc.EncodeValue(value ?? CNTKDefault.GetDefaultValue(kvp.Value.FirstOrDefault()), col, kvp.Value, inputValues, offset);
                        }
                    }
                }

                using (HeavyProfiler.LogNoStackTrace("CreateBatch"))
                    return Value.CreateBatch<float>(new int[] { ctx.InputCodifications.Count }, inputValues, device);
            }
        }

        static object lockKey = new object();

        public void LoadModel(PredictorPredictContext ctx)
        {
            lock (lockKey)
            {
                this.InitialSetup();

                var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;

                ctx.Model = Function.Load(ctx.Predictor.Files.SingleEx().GetByteArray(), GetDevice(nnSettings));
            }
        }
    }
}
