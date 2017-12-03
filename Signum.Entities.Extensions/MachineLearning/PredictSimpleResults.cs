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

        [NotNullable]
        [NotNullValidator, ImplementedByAll]
        public Lite<Entity> Target { get; set; }

        public PredictionSet Type { get; set; }

        [SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = true, Max = 200)]
        public string PredictedCategory { get; set; }


        public double? PredictedValue { get; set; }
    }

    public enum PredictionSet
    {
        Validation, 
        Training
    }
}
