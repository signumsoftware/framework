using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.MachineLearning;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using Signum.Entities;
using System.Diagnostics;
using Signum.Engine.Files;
using Signum.Engine.Operations;
using Signum.Entities.UserAssets;
using System.IO;
using static Tensorflow.Binding;
using Tensorflow;
using Tensorflow.Keras.Optimizers;
using NumSharp;

namespace Signum.Engine.MachineLearning.TensorFlow
{
    public class TensorFlowNeuralNetworkPredictor : IPredictorAlgorithm
    {
        public static string TempFile = "tempFile.ckpt";

        public Dictionary<PredictorColumnEncodingSymbol, ITensorFlowEncoding> Encodings = new Dictionary<PredictorColumnEncodingSymbol, ITensorFlowEncoding>
        {
            { DefaultColumnEncodings.None, new NoneTFEncoding() },
            { DefaultColumnEncodings.OneHot, new OneHotTFEncoding() },
            { DefaultColumnEncodings.NormalizeZScore, new NormalizeZScoreTFEncoding() },
            { DefaultColumnEncodings.NormalizeMinMax, new NormalizeMinMaxTFEncoding() },
            { DefaultColumnEncodings.NormalizeLog, new NormalizeLogTFEncoding() },
            { DefaultColumnEncodings.SplitWords, new SplitWordsTFEncoding() },
        };

        public void InitialSetup()
        {
         
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

        //Errors with CNTK: https://github.com/Microsoft/CNTK/issues/2614
        public void Train(PredictorTrainingContext ctx)
        {
            InitialSetup();
            var p = ctx.Predictor;

            var nn = (NeuralNetworkSettingsEntity)p.AlgorithmSettings;

            Tensor inputVariable = tf.placeholder(tf.float32, new[] { ctx.InputCodifications.Count }, "input");
            Tensor outputVariable = tf.placeholder(tf.float32, new[] { ctx.OutputCodifications.Count }, "output");

            Tensor currentVar = inputVariable;
            nn.HiddenLayers.ForEach((layer, i) =>
            {
                currentVar = NetworkBuilder.DenseLayer(currentVar, layer.Size, layer.Activation, layer.Initializer, p.Settings.Seed ?? 0, "hidden" + i);
            });
            Tensor calculatedOutputs = NetworkBuilder.DenseLayer(currentVar, ctx.OutputCodifications.Count, nn.OutputActivation, nn.OutputInitializer, p.Settings.Seed ?? 0, "output");

            Tensor loss = NetworkBuilder.GetEvalFunction(nn.LossFunction, outputVariable, calculatedOutputs);
            Tensor accuracy = NetworkBuilder.GetEvalFunction(nn.EvalErrorFunction, outputVariable, calculatedOutputs);

            // prepare for training
            Optimizer optimizer = NetworkBuilder.GetOptimizer(nn);

            Operation trainOperation = optimizer.minimize(loss);

            Random rand = p.Settings.Seed == null ?
                new Random() :
                new Random(p.Settings.Seed.Value);

            var (training, validation) = ctx.SplitTrainValidation(rand);

            var minibachtSize = nn.MinibatchSize;
            var numMinibatches = nn.NumMinibatches;


            Stopwatch sw = Stopwatch.StartNew();
            List<FinalCandidate> candidate = new List<FinalCandidate>();

            var config = new ConfigProto
            {
                IntraOpParallelismThreads = 1,
                InterOpParallelismThreads = 1,
                LogDevicePlacement = true
            };

            var saver = tf.train.Saver();

            using (var sess = tf.Session(config))
            {
                sess.run(tf.global_variables_initializer());

                for (int i = 0; i < numMinibatches; i++)
                {
                    using (HeavyProfiler.Log("MiniBatch", () => i.ToString()))
                    {
                        ctx.ReportProgress("Training Minibatches", (i + 1) / (decimal)numMinibatches);

                        var trainMinibatch = 0.To(minibachtSize).Select(_ => rand.NextElement(training)).ToList();

                        var inputValue = CreateNDArray(ctx, trainMinibatch, ctx.InputCodifications.Count, ctx.InputCodificationsByColumn);
                        var outputValue = CreateNDArray(ctx, trainMinibatch, ctx.OutputCodifications.Count, ctx.OutputCodificationsByColumn);

                        using (HeavyProfiler.Log("TrainMinibatch", () => i.ToString())) 
                        {
                            sess.run(optimizer,
                                (inputVariable, inputValue),
                                (outputVariable, outputValue));
                        }

                        if (ctx.StopTraining)
                            p = ctx.Predictor = ctx.Predictor.ToLite().RetrieveAndRemember();

                        var isLast = numMinibatches - nn.BestResultFromLast <= i;
                        if (isLast || (i % nn.SaveProgressEvery) == 0 || ctx.StopTraining)
                        {
                            float loss_val = 100.0f;
                            float accuracy_val = 0f;

                            using (HeavyProfiler.Log("EvalTraining", () => i.ToString()))
                            {
                                (loss_val, accuracy_val) = sess.run((loss, accuracy), 
                                    (inputVariable, inputValue),
                                    (outputVariable, outputValue));
                            }

                            var ep = new EpochProgress
                            {
                                Ellapsed = sw.ElapsedMilliseconds,
                                Epoch = i,
                                TrainingExamples = i * minibachtSize,
                                LossTraining = loss_val,
                                AccuracyTraining = accuracy_val,
                                LossValidation = null,
                                AccuracyValidation = null,
                            };

                            ctx.Progresses.Enqueue(ep);

                            if (isLast || (i % nn.SaveValidationProgressEvery) == 0 || ctx.StopTraining)
                            {
                                using (HeavyProfiler.LogNoStackTrace("EvalValidation"))
                                {
                                    var validateMinibatch = 0.To(minibachtSize).Select(_ => rand.NextElement(validation)).ToList();

                                    var inputValValue = CreateNDArray(ctx, validateMinibatch, ctx.InputCodifications.Count, ctx.InputCodificationsByColumn);
                                    var outputValValue = CreateNDArray(ctx, validateMinibatch, ctx.OutputCodifications.Count, ctx.OutputCodificationsByColumn);

                                    (loss_val, accuracy_val) = sess.run((loss, accuracy),
                                    (inputVariable, inputValue),
                                    (outputVariable, outputValue));


                                    ep.LossValidation = loss_val;
                                    ep.AccuracyValidation = accuracy_val;
                                }
                            }

                            var progress = ep.SaveEntity(ctx.Predictor);

                            if (isLast || ctx.StopTraining)
                            {
                                var save = saver.save(sess, TempFile);

                                using (HeavyProfiler.LogNoStackTrace("FinalCandidate"))
                                {
                                    candidate.Add(new FinalCandidate
                                    {
                                        Model = null!,

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

        NDArray CreateNDArray(PredictorTrainingContext ctx, List<ResultRow> rows, int codificationCount, Dictionary<PredictorColumnBase, List<PredictorCodification>> codificationByColumn)
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
                            ITensorFlowEncoding encoding = Encodings.GetOrThrow(col.Encoding);

                            encoding.EncodeValue(value ?? TensorFlowDefault.GetDefaultValue(kvp.Value.FirstEx()), col, kvp.Value, inputValues, offset);
                        }
                    }
                }

                using (HeavyProfiler.LogNoStackTrace("CreateBatch"))
                    return np.array(inputValues);
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
                lock (lockKey)
                {
                    var graph = new Graph().as_default();
                    using (var sess = tf.Session(graph))
                    {


                    }

                    Function calculatedOutputs = (Function)ctx.Model!;
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

        private NDArray GetValueForPredict(PredictorPredictContext ctx, List<PredictDictionary> inputs)
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
                            enc.EncodeValue(value ?? TensorFlowDefault.GetDefaultValue(kvp.Value.FirstEx()), col, kvp.Value, inputValues, offset);
                        }
                    }
                }

                using (HeavyProfiler.LogNoStackTrace("CreateBatch"))
                    return np.array(inputValues);
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
