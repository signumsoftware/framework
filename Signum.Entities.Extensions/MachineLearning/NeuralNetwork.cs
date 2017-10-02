using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class NeuronalNetworkSettingsEntity : Entity, IPredictorAlgorithmSettings
    {
        public double LearningRate { get; set; }

        public ActivationFunction ActivationFunction { get; set; }

        public Regularization Regularization { get; set; }

        public double RegularizationRate { get; set; }

        public double TrainingRatio { get; set; }

        public double BackSize { get; set; }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        public string NeuronalNetworkDescription { get; set; }
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
