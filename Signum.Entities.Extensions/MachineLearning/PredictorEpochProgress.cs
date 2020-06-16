using Signum.Utilities;
using System;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PredictorEpochProgressEntity : Entity
    {
        
        public Lite<PredictorEntity> Predictor { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;
        [Unit("ms")]
        public long Ellapsed { get; internal set; }

        public int TrainingExamples { get; set; }

        public int Epoch { get; set; }

        [Format("0.0000")]
        public double? LossTraining { get; set; }
        [Format("0.0000")]
        public double? EvaluationTraining { get; set; }
        [Format("0.0000")]
        public double? LossValidation { get; internal set; }
        [Format("0.0000")]
        public double? EvaluationValidation { get; internal set; }
    }
}
