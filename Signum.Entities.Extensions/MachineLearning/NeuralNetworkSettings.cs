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
        public PredictionType PredictionType { get; set; }
        public int MinibatchSize { get; set; } = 100;
        public bool? SparseMatrix { get; set; }

        public IPredictorAlgorithmSettings Clone() => new NeuralNetworkSettingsEntity
        {
            MinibatchSize = MinibatchSize,

        };
    }

    public enum PredictionType
    {
        Regression,
        Classification,
    }
}
