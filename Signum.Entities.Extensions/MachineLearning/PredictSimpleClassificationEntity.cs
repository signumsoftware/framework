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
    public class PredictSimpleClassificationEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public Lite<Entity> Target { get; set; }

        public PredictionSet Type { get; set; }

        [NotNullable, SqlDbType(Size = 200)]
        [StringLengthValidator(AllowNulls = false, Max = 200)]
        public string PredictedValue { get; set; }
    }

    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class PredictSimpleRegressionEntity : Entity
    {
        [NotNullable]
        [NotNullValidator]
        public Lite<Entity> Target { get; set; }

        public PredictionSet Type { get; set; }

        public decimal? PredictedValue { get; set; }
    }

    public enum PredictionSet
    {
        Evaluation, 
        Training
    }
}
