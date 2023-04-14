using System.Diagnostics;
using System.IO;
using static Tensorflow.Binding;
using Tensorflow;
using Tensorflow.NumPy;
using Signum.Files;
using Signum.UserAssets.Queries;

namespace Signum.MachineLearning.TensorFlow;

public class TensorFlowNeuralNetworkPredictor : IPredictorAlgorithm
{
    public static Func<PredictorEntity, int, string> TrainingModelDirectory = (PredictorEntity p, int miniBatchIndex) => $"TensorFlowModels/{p.Id}/Training/{miniBatchIndex}";
    public static Func<PredictorEntity, string> PredictorDirectory = (PredictorEntity p) => $"TensorFlowModels/{p.Id}";
    public string ModelFileName = "Model";

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

        tf.compat.v1.disable_eager_execution();
        var p = ctx.Predictor;

        var nn = (NeuralNetworkSettingsEntity)p.AlgorithmSettings;

        Tensor inputPlaceholder = tf.placeholder(tf.float32, new[] { -1, ctx.InputCodifications.Count }, "inputPlaceholder");
        Tensor outputPlaceholder = tf.placeholder(tf.float32, new[] { -1, ctx.OutputCodifications.Count }, "outputPlaceholder");

        Tensor currentTensor = inputPlaceholder;
        nn.HiddenLayers.ForEach((layer, i) =>
        {
            currentTensor = NetworkBuilder.DenseLayer(currentTensor, layer.Size, layer.Activation, layer.Initializer, p.Settings.Seed ?? 0, "hidden" + i);
        });
        Tensor output = NetworkBuilder.DenseLayer(currentTensor, ctx.OutputCodifications.Count, nn.OutputActivation, nn.OutputInitializer, p.Settings.Seed ?? 0, "output");
        Tensor calculatedOutput = tf.identity(output, "calculatedOutput");
        
        Tensor loss = NetworkBuilder.GetEvalFunction(nn.LossFunction, outputPlaceholder, calculatedOutput);
        Tensor accuracy = NetworkBuilder.GetEvalFunction(nn.EvalErrorFunction, outputPlaceholder, calculatedOutput);

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

        ctx.ReportProgress($"Deleting Files");
        var dir = PredictorDirectory(ctx.Predictor);

        if (Directory.Exists(dir))
            Directory.Delete(dir, true);

        Directory.CreateDirectory(dir);
        
        ctx.ReportProgress($"Starting training...");

        var saver = tf.train.Saver();

        using (var sess = tf.Session(config))
        {
            sess.run(tf.global_variables_initializer());

            for (int i = 0; i < numMinibatches; i++)
            {
                using (HeavyProfiler.Log("MiniBatch", () => i.ToString()))
                {
                    var trainMinibatch = 0.To(minibachtSize).Select(_ => rand.NextElement(training)).ToList();

                    var inputValue = CreateNDArray(ctx, trainMinibatch, ctx.InputCodifications.Count, ctx.InputCodificationsByColumn);
                    var outputValue = CreateNDArray(ctx, trainMinibatch, ctx.OutputCodifications.Count, ctx.OutputCodificationsByColumn);

                    using (HeavyProfiler.Log("TrainMinibatch", () => i.ToString())) 
                    {
                        sess.run(trainOperation,
                            (inputPlaceholder, inputValue),
                            (outputPlaceholder, outputValue));
                    }

                    if (ctx.StopTraining)
                        p = ctx.Predictor = ctx.Predictor.ToLite().RetrieveAndRemember();

                    var isLast = numMinibatches - nn.BestResultFromLast <= i;
                    if (isLast || (i % nn.SaveProgressEvery) == 0 || ctx.StopTraining)
                    {
                        float loss_val;
                        float accuracy_val;

                        using (HeavyProfiler.Log("EvalTraining", () => i.ToString()))
                        {
                            (loss_val, accuracy_val) = sess.run((loss, accuracy), 
                                (inputPlaceholder, inputValue),
                                (outputPlaceholder, outputValue));
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

                        ctx.ReportProgress($"Training Minibatches Loss:{loss_val} / Accuracy:{accuracy_val}", (i + 1) / (decimal)numMinibatches);

                        ctx.Progresses.Enqueue(ep);

                        if (isLast || (i % nn.SaveValidationProgressEvery) == 0 || ctx.StopTraining)
                        {
                            using (HeavyProfiler.LogNoStackTrace("EvalValidation"))
                            {
                                var validateMinibatch = 0.To(minibachtSize).Select(_ => rand.NextElement(validation)).ToList();

                                var inputValValue = CreateNDArray(ctx, validateMinibatch, ctx.InputCodifications.Count, ctx.InputCodificationsByColumn);
                                var outputValValue = CreateNDArray(ctx, validateMinibatch, ctx.OutputCodifications.Count, ctx.OutputCodificationsByColumn);

                                (loss_val, accuracy_val) = sess.run((loss, accuracy),
                                (inputPlaceholder, inputValValue),
                                (outputPlaceholder, outputValValue));


                                ep.LossValidation = loss_val;
                                ep.AccuracyValidation = accuracy_val;
                            }
                        }

                        var progress = ep.SaveEntity(ctx.Predictor);

                        if (isLast || ctx.StopTraining)
                        {
                            Directory.CreateDirectory(TrainingModelDirectory(ctx.Predictor, i));
                            var save = saver.save(sess, Path.Combine(TrainingModelDirectory(ctx.Predictor, i), ModelFileName));

                            using (HeavyProfiler.LogNoStackTrace("FinalCandidate"))
                            {
                                candidate.Add(new FinalCandidate
                                {
                                    ModelIndex = i,
                                    ResultTraining = new PredictorMetricsEmbedded { Accuracy = progress.AccuracyTraining, Loss = progress.LossTraining },
                                    ResultValidation = new PredictorMetricsEmbedded { Accuracy = progress.AccuracyValidation, Loss = progress.LossValidation },
                                });
                            }
                        }
                    }

                    if (ctx.StopTraining)
                        break;
                }
            }
        }

        var best = candidate.MinBy(a => a.ResultValidation.Loss!.Value)!;

        p.ResultTraining = best.ResultTraining;
        p.ResultValidation = best.ResultValidation;

        var files = Directory.GetFiles(TrainingModelDirectory(ctx.Predictor, best.ModelIndex));

        p.Files.AddRange(files.Select(p => new FilePathEmbedded(PredictorFileType.PredictorFile, p)));

        using (OperationLogic.AllowSave<PredictorEntity>())
            p.Save();
    }

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    public class FinalCandidate
    {
        public int ModelIndex;
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
                return np.array(inputValues).reshape((rows.Count, codificationCount));
        }
    }

    public PredictDictionary Predict(PredictorPredictContext ctx, PredictDictionary input)
    {
        using (HeavyProfiler.LogNoStackTrace("Predict"))
        {
            lock (lockKey)
            {
                var model = (TensorFlowModel)ctx.Model!;

                model.Session.as_default();
                model.Graph.as_default();

                NDArray inputValue = GetValueForPredict(ctx, input);
                NDArray outputValuesND = model.Session.run(model.CalculatedOutput, (model.InputPlaceholder, inputValue));
                float[] outputValues = outputValuesND.ToArray<float>();
                PredictDictionary dic = GetPredictionDictionary(outputValues, ctx, input.Options!);
                return dic;
            }
        }
    }

    public List<PredictDictionary> PredictMultiple(PredictorPredictContext ctx, List<PredictDictionary> inputs)
    {
        using (HeavyProfiler.LogNoStackTrace("PredictMultiple"))
        {
            lock (lockKey)
            {
                var model = (TensorFlowModel)ctx.Model!;
                tf.compat.v1.disable_eager_execution();
                model.Session.as_default();
                model.Graph.as_default();

                var result = new List<PredictDictionary>();
                foreach (var input in inputs)
                {
                    NDArray inputValue = GetValueForPredict(ctx, input);
                    NDArray outputValuesND = model.Session.run(model.CalculatedOutput, (model.InputPlaceholder, inputValue));
                    float[] outputValues = outputValuesND.ToArray<float>();
                    PredictDictionary dic = GetPredictionDictionary(outputValues, ctx, input.Options!);
                    result.Add(dic);
                }

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

    private NDArray GetValueForPredict(PredictorPredictContext ctx, PredictDictionary input)
    {
        using (HeavyProfiler.Log("GetValueForPredict", () => $"Inputs Codifications {ctx.InputCodifications.Count}"))
        {
            if (input.SubQueries.Values.Any(a => a.SubQueryGroups.Comparer != ObjectArrayComparer.Instance))
                throw new Exception("Unexpected dictionary comparer");

            float[] inputValues = new float[ctx.InputCodifications.Count];
            var groups = ctx.InputCodificationsByColumn;

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
                    enc.EncodeValue(value ?? TensorFlowDefault.GetDefaultValue(kvp.Value.FirstEx()), col, kvp.Value, inputValues, 0);
                }
            }

            using (HeavyProfiler.LogNoStackTrace("CreateBatch"))
                return np.array(inputValues).reshape((-1, inputValues.Length));
        }
    }

    static object lockKey = new object();

    public void LoadModel(PredictorPredictContext ctx)
    {
        lock (lockKey)
        {
            this.InitialSetup();

            var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;

            var dir = Path.Combine(PredictorDirectory(ctx.Predictor));

            if (Directory.Exists(dir))
                Directory.Delete(dir, true);

            Directory.CreateDirectory(dir);

            foreach (var item in ctx.Predictor.Files)
            {
                using (var fileStream = File.Create(Path.Combine(dir, item.FileName)))
                {
                    using (var readStream = item.OpenRead())
                    {
                        readStream.CopyTo(fileStream);
                    }
                }
            }

            var graph = tf.Graph();
            var sess = tf.Session(graph);
            {
                var saver = tf.train.import_meta_graph(Path.Combine(dir, $"{ModelFileName}.meta"));
                saver.restore(sess, Path.Combine(dir, ModelFileName));

                var array = graph.get_operations().Select(a=>a.name).ToArray();

                var inputPlaceholder = graph.get_operation_by_name("inputPlaceholder");
                var calculatedOutput = graph.get_operation_by_name("calculatedOutput");

                ctx.Model = new TensorFlowModel
                {
                    InputPlaceholder = inputPlaceholder,
                    CalculatedOutput = calculatedOutput,
                    Graph = graph,
                    Session = sess,
                };
            }

            //ctx.Model = Function.Load(ctx.Predictor.Files.SingleEx().GetByteArray());
        }
    }
}

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
class TensorFlowModel
{
    public Tensor InputPlaceholder { get; internal set; }
    public Tensor CalculatedOutput { get; internal set; }
    public Graph Graph { get; internal set; }
    public Session Session { get; internal set; }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
