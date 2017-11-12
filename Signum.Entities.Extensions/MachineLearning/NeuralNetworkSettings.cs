using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class NeuralNetworkSettingsEntity : Entity, IPredictorAlgorithmSettings
    {
        public int MinibatchSize { get; set; } = 100;

        public IPredictorAlgorithmSettings Clone() => new NeuralNetworkSettingsEntity
        {
            MinibatchSize = MinibatchSize,

        };
    }

    public enum ActivationFunction
    {
        ReLU,
        Tanh,
        Sigmoid,
        Linear,
    }

    public enum Regularization
    {
        None,
        L1,
        L2,
    }
}
