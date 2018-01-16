using Signum.Entities;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.System, EntityData.Transactional)]
    public class PredictSimpleResultEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public Lite<PredictorEntity> Predictor { get; internal set; }

        [ImplementedByAll]
        public Lite<Entity> Target { get; set; }

        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Key0 { get; set; }

        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Key1 { get; set; }

        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Key2 { get; set; }

        public PredictionSet Type { get; set; }

        [SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string OriginalCategory { get; set; }

        [Format("0.0000")]
        public double? OriginalValue { get; set; }

        [SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string PredictedCategory { get; set; }

        [Format("0.0000")]
        public double? PredictedValue { get; set; }
    }

    public enum PredictionSet
    {
        Validation, 
        Training
    }
}
