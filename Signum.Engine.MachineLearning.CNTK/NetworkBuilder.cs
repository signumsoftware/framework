using CNTK;
using Signum.Entities.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.MachineLearning.CNTK
{
    public class NetworkBuilder
    {
        public static Function DenseLayer(Variable input, int outputDim, DeviceDescriptor device, NeuralNetworkActivation activation, int seed, string name)
        {
            if (input.Shape.Rank != 1)
            {
                int newDim = input.Shape.Dimensions.Aggregate((d1, d2) => d1 * d2);
                input = CNTKLib.Reshape(input, new int[] { newDim });
            }

            Function fullyConnected = FullyConnectedLinearLayer(input, outputDim, device);
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

        public static Function FullyConnectedLinearLayer(Variable input, int outputDim,  DeviceDescriptor device)
        {
            System.Diagnostics.Debug.Assert(input.Shape.Rank == 1);
            int inputDim = input.Shape[0];

            var W = new Parameter(new int[] { outputDim, inputDim }, DataType.Float,
                CNTKLib.GlorotUniformInitializer(
                    scale: CNTKLib.DefaultParamInitScale,
                    outputRank: CNTKLib.SentinelValueForInferParamInitRank,
                    filterRank: CNTKLib.SentinelValueForInferParamInitRank, 
                    seed: 1),
                device, "W");

            var b = new Parameter(new int[] { outputDim }, 0.0f, device, "b");
            return b + CNTKLib.Times(W, input);
        }
    }
}
