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
using System.IO;

namespace Signum.Engine.MachineLearning.CNTK
{
    public class CNTKNeuralNetworkPredictorAlgorithm : IPredictorAlgorithm
    {
        public void InitialSetup()
        {
            /// This is a workaround to load unmanaged CNTK dlls from the applications \bin directory.
            var dir = AppDomain.CurrentDomain.BaseDirectory;

            if (!Directory.GetFiles(dir, "Cntk.Core.*.dll").Any())
            {
                dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin");
                if (!Directory.Exists(dir) || !Directory.GetFiles(dir, "Cntk.Core.*.dll").Any())
                    throw new InvalidOperationException($@"No CNTK dll found in {AppDomain.CurrentDomain.BaseDirectory} or {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "bin")}");
            }

            var oldPath = Environment.GetEnvironmentVariable("Path");
            if (!oldPath.Contains(dir + ";"))
                Environment.SetEnvironmentVariable("Path", dir + ";" + oldPath, EnvironmentVariableTarget.Process);
        }

        public string ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEncoding encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            return CNTKEncoding.ValidateEncodingProperty(predictor, subQuery, encoding, usage, token);
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
            List<FinalCandidate> candidate = new List<FinalCandidate>();
            for (int i = 0; i < numMinibatches; i++)
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

                ctx.Progresses.Add(ep);
                
                if (ctx.StopTraining)
                    p = ctx.Predictor = ctx.Predictor.ToLite().Retrieve();

                var isLast = numMinibatches - nn.BestResultFromLast <= i;
                if (isLast || (i % nn.SaveProgressEvery) == 0 || ctx.StopTraining)
                {
                    if (isLast || (i % nn.SaveValidationProgressEvery) == 0 || ctx.StopTraining)
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

                    var progress = ep.SaveEntity(ctx.Predictor);

                    if (isLast || ctx.StopTraining)
                    {
                        candidate.Add(new FinalCandidate
                        {
                            Model = calculatedOutputs.Save(),

                            ResultTraining = new PredictorMetricsEmbedded { Evaluation = progress.EvaluationTraining, Loss = progress.LossTraining },
                            ResultValidation = new PredictorMetricsEmbedded { Evaluation = progress.EvaluationValidation, Loss = progress.LossValidation },
                        });
                    }
                }

                if (ctx.StopTraining)
                    break;
            }

            var best = candidate.WithMin(a => a.ResultValidation.Loss.Value);

            p.ResultTraining = best.ResultTraining;
            p.ResultValidation = best.ResultValidation;
            
            var fp = new Entities.Files.FilePathEmbedded(PredictorFileType.PredictorFile, "Model.cntk", best.Model);

            p.Files.Add(fp);

            using (OperationLogic.AllowSave<PredictorEntity>())
                p.Save();
        }
   
        public class FinalCandidate
        {
            public byte[] Model;
            public PredictorMetricsEmbedded ResultTraining;
            public PredictorMetricsEmbedded ResultValidation;
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

                    values[i * codifications.Count + j] = CNTKEncoding.GetFloat(value, c);
                }
            }

            return Value.CreateBatch<float>(new int[] { codifications.Count }, values, device);
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
                MainQueryValues = ctx.MainQueryOutputColumn.SelectDictionary(col => col, (col, list) => CNTKEncoding.FloatToValue(col.Encoding, col.Token.Token, list, outputValues)),
                SubQueries = ctx.Predictor.SubQueries.ToDictionary(sq => sq, sq => new PredictSubQueryDictionary(sq)
                {
                    SubQueryGroups = ctx.SubQueryOutputColumn.TryGetC(sq)?.Groups.ToDictionary(
                        kvp => kvp.Key, 
                        kvp => kvp.Value
                        .Where(a => a.Key.Usage ==  PredictorSubQueryColumnUsage.Output)
                        .ToDictionary(a => a.Key, a => CNTKEncoding.FloatToValue(a.Key.Encoding.Value, a.Key.Token.Token, a.Value, outputValues)), 
                        ObjectArrayComparer.Instance
                    ) ?? new Dictionary<object[], Dictionary<PredictorSubQueryColumnEmbedded, object>>(ObjectArrayComparer.Instance),
                })
            };
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

                values[i] = CNTKEncoding.GetFloat(value, c);
            }


            return Value.CreateBatch<float>(new int[] { ctx.InputColumns.Count }, values, device);
        }

        public void LoadModel(PredictorPredictContext ctx)
        {
            this.InitialSetup();

            var nnSettings = (NeuralNetworkSettingsEntity)ctx.Predictor.AlgorithmSettings;

            ctx.Model = Function.Load(ctx.Predictor.Files.SingleEx().GetByteArray(), GetDevice(nnSettings));
        }
    }
}
