﻿
namespace Signum.MachineLearning;

[EntityKind(EntityKind.System, EntityData.Transactional)]
public class PredictorEpochProgressEntity : Entity
{
    
    public Lite<PredictorEntity> Predictor { get; set; }

    public DateTime CreationDate { get; private set; } = Clock.Now;
    [Unit("ms")]
    public long Ellapsed { get; internal set; }

    public int TrainingExamples { get; set; }

    public int Epoch { get; set; }

    [Format("0.0000")]
    public double? LossTraining { get; set; }
    [Format("0.0000")]
    public double? AccuracyTraining { get; set; }
    [Format("0.0000")]
    public double? LossValidation { get; internal set; }
    [Format("0.0000")]
    public double? AccuracyValidation { get; internal set; }
}
