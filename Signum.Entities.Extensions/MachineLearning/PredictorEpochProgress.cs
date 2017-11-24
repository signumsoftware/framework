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

        [SqlDbType(Scale = 5)]
        public double? LossTraining { get; set; }
        [SqlDbType(Scale = 5)]
        public double? EvaluationTraining { get; set; }
        [SqlDbType(Scale = 5)]
        public double? LossValidation { get; internal set; }
        [SqlDbType(Scale = 5)]
        public double? EvaluationValidation { get; internal set; }
    }
}
