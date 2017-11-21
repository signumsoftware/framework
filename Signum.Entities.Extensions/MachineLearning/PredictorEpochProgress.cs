using System;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PredictorEpochProgressEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public Lite<PredictorEntity> Predictor { get; set; }

        public DateTime CreationDate { get; private set; } = TimeZoneManager.Now;
        [Unit("ms")]
        public long Ellapsed { get; internal set; }

        public int TrainingExamples { get; set; }

        public int Epoch { get; set; }

        public double LossTraining { get; set; }
        public double EvaluationTraining { get; set; }
        public double? LossValidation { get; internal set; }
        public double? EvaluationValidation { get; internal set; }
    }
}
