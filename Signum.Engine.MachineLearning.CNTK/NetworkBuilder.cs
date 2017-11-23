using CNTK;
using Signum.Entities.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            
            var W = new Parameter(new int[] { outputDim, inputDim }, DataType.Float, GetInitializer(initializer, (uint)seed), device, "W");

            var b = new Parameter(new int[] { outputDim }, 0.0f, device, "b");
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

            var outputs = new Dictionary<Variable, Value>()
            {
                { func.Output, null},
            };

            func.Evaluate(inputs, outputs, device);

            var value = outputs[func.Output].GetDenseData<float>(func.Output).Average(a => a[0]);
            return value;
        }
    }
}
