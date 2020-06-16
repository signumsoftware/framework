using CNTK;
using Signum.Entities.MachineLearning;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Engine.MachineLearning.CNTK
{
    public static class NetworkBuilder
    {
        public static Function DenseLayer(Variable input, int outputDim, DeviceDescriptor device, NeuralNetworkActivation activation, NeuralNetworkInitializer initializer, int seed, string name)
        {
            if (input.Shape.Rank != 1)
            {
                int newDim = input.Shape.Dimensions.Aggregate((d1, d2) => d1 * d2);
                input = CNTKLib.Reshape(input, new int[] { newDim });
            }

            Function fullyConnected = FullyConnectedLinearLayer(input, outputDim, initializer, seed,  device);
            fullyConnected.SetName(name);
            switch (activation)
            {
                case NeuralNetworkActivation.None: return fullyConnected;
                case NeuralNetworkActivation.ReLU: return CNTKLib.ReLU(fullyConnected, name + "ReLU");
                case NeuralNetworkActivation.Sigmoid: return CNTKLib.Sigmoid(fullyConnected, name + "Sigmoid");
                case NeuralNetworkActivation.Tanh: return CNTKLib.Tanh(fullyConnected, name + "Tanh");
                default: throw new InvalidOperationException("Unexpected activation " + activation);
            }
        }

        public static Function FullyConnectedLinearLayer(Variable input, int outputDim, NeuralNetworkInitializer initializer, int seed, DeviceDescriptor device)
        {
            System.Diagnostics.Debug.Assert(input.Shape.Rank == 1);
            int inputDim = input.Shape[0];

            var init = GetInitializer(initializer, (uint)seed);
            var W = new Parameter(new int[] { outputDim, inputDim }, DataType.Float, init, device, "W");

            var b = new Parameter(new int[] { outputDim }, DataType.Float, init, device, "b");
            return b + W * input;
        }

        private static CNTKDictionary GetInitializer(NeuralNetworkInitializer initializer, uint seed)
        {
            var scale = CNTKLib.DefaultParamInitScale;
            var rank = CNTKLib.SentinelValueForInferParamInitRank;
            switch (initializer)
            {
                case NeuralNetworkInitializer.Zero: return CNTKLib.ConstantInitializer(0.0f);
                case NeuralNetworkInitializer.GlorotNormal: return CNTKLib.GlorotNormalInitializer(scale, rank, rank, seed);
                case NeuralNetworkInitializer.GlorotUniform: return CNTKLib.GlorotUniformInitializer(scale, rank, rank, seed);
                case NeuralNetworkInitializer.HeNormal:return CNTKLib.HeNormalInitializer(scale, rank, rank, seed);
                case NeuralNetworkInitializer.HeUniform: return CNTKLib.HeUniformInitializer(scale, rank, rank, seed);
                case NeuralNetworkInitializer.Normal:return CNTKLib.NormalInitializer(scale, rank, rank, seed);
                case NeuralNetworkInitializer.TruncateNormal: return CNTKLib.TruncatedNormalInitializer(scale, seed);
                case NeuralNetworkInitializer.Uniform: return CNTKLib.UniformInitializer(scale, seed);
                case NeuralNetworkInitializer.Xavier:return CNTKLib.XavierInitializer(scale, rank, rank, seed);
                default:
                    throw new InvalidOperationException("");
            }
        }

        public static float EvaluateAvg(this Function func, Dictionary<Variable, Value> inputs, DeviceDescriptor device)
        {
            if (func.Output.Shape.TotalSize != 1 && func.Output.Shape.TotalSize != func.Output.Shape[0])
                throw new InvalidOperationException("func should return a vector");

            var value = func.Evaluate(inputs, device);
            var result = value.Average(a => a[0]);
            return result;
        }

        public static IList<IList<float>> Evaluate(this Function func, Dictionary<Variable, Value> inputs, DeviceDescriptor device)
        {
            var outputs = new Dictionary<Variable, Value>()
            {
                { func.Output, null!},
            };

            func.Evaluate(inputs, outputs, device);

            return outputs[func.Output].GetDenseData<float>(func.Output);
        }

        public static TrainingParameterScheduleDouble ToTrainParam(this double value)
        {
            return new TrainingParameterScheduleDouble(value);
        }

        public static Function GetEvalFunction(NeuralNetworkEvalFunction lossFunction, Function calculatedOutputs, Variable outputVariable)
        {
            switch (lossFunction)
            {
                case NeuralNetworkEvalFunction.CrossEntropyWithSoftmax: return CNTKLib.CrossEntropyWithSoftmax(calculatedOutputs, outputVariable);
                case NeuralNetworkEvalFunction.ClassificationError: return CNTKLib.ClassificationError(calculatedOutputs, outputVariable);
                case NeuralNetworkEvalFunction.SquaredError: return CNTKLib.SquaredError(calculatedOutputs, outputVariable);
                case NeuralNetworkEvalFunction.MeanAbsoluteError: return NetworkBuilder.MeanAbsoluteError(calculatedOutputs, outputVariable);
                case NeuralNetworkEvalFunction.MeanAbsolutePercentageError: return NetworkBuilder.MeanAbsolutePercentageError(calculatedOutputs, outputVariable);
                default:
                    throw new InvalidOperationException("Unexpected " + lossFunction);
            }
        }

        internal static Learner GetInitializer(IList<Parameter> parameters, NeuralNetworkSettingsEntity s)
        {
            var vector = new ParameterVector((ICollection)parameters);
            switch (s.Learner)
            {
                case NeuralNetworkLearner.Adam: return CNTKLib.AdamLearner(vector,
                     s.LearningRate.ToTrainParam(),
                     s.LearningMomentum?.ToTrainParam(),
                     s.LearningUnitGain ?? false,
                     s.LearningVarianceMomentum?.ToTrainParam());

                case NeuralNetworkLearner.AdaDelta:
                    return CNTKLib.AdaDeltaLearner(vector,
                        s.LearningRate.ToTrainParam());

                case NeuralNetworkLearner.AdaGrad:
                    return CNTKLib.AdaGradLearner(vector,
                        s.LearningRate.ToTrainParam());

                case NeuralNetworkLearner.FSAdaGrad:
                    return CNTKLib.FSAdaGradLearner(vector,
                        s.LearningRate.ToTrainParam(),
                        s.LearningMomentum?.ToTrainParam(),
                        s.LearningUnitGain ?? false,
                        s.LearningVarianceMomentum?.ToTrainParam());

                case NeuralNetworkLearner.RMSProp:
                    return CNTKLib.FSAdaGradLearner(vector,
                        s.LearningRate.ToTrainParam(),
                        s.LearningMomentum?.ToTrainParam(),
                        s.LearningUnitGain ?? false,
                        s.LearningVarianceMomentum?.ToTrainParam());

                case NeuralNetworkLearner.MomentumSGD:
                    return CNTKLib.MomentumSGDLearner(vector,
                        s.LearningRate.ToTrainParam(),
                        s.LearningMomentum?.ToTrainParam(),
                        s.LearningUnitGain ?? false);

                case NeuralNetworkLearner.SGD:
                    return CNTKLib.SGDLearner(vector,
                        s.LearningRate.ToTrainParam());
                default:
                    throw new InvalidOperationException("Unexpected Learner");
            }
        }

        public static Function MeanAbsoluteError(Variable prediction, Variable targets)
        {
            return CNTKLib.ReduceMean(CNTKLib.Abs(CNTKLib.Minus(targets, prediction)), new Axis(-1));
        }

        public static Function MeanAbsolutePercentageError(Variable prediction, Variable targets)
        {
            var error = CNTKLib.Minus(targets, prediction);
            var percentage = CNTKLib.Abs(CNTKLib.ElementDivide(error, targets));
            return CNTKLib.ReduceMean(percentage, new Axis(-1));
        }
    }
}
