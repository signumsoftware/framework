using System;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PredictorCodificationEntity : Entity
    {
        
        public Lite<PredictorEntity> Predictor { get; set; }

        public PredictorColumnUsage Usage { get; set; }

        public int Index { get; set; }

        public int? SubQueryIndex{ get; set; }

        public int OriginalColumnIndex { get; set; }

        //For flatting collections
        [SqlDbType(Size = 100)]
        public string? SplitKey0 { get; set; }

        [SqlDbType(Size = 100)]
        public string? SplitKey1 { get; set; }

        [SqlDbType(Size = 100)]
        public string? SplitKey2 { get; set; }


        //For 1-hot encoding
        [SqlDbType(Size = 100)]
        public string? IsValue { get; set; }

        public float? Average { get; set; }
        public float? StdDev { get; set; }

        public float? Min { get; set; }
        public float? Max { get; set; }
    }
}
