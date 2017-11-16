using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class NeuralNetworkSettingsEntity : Entity, IPredictorAlgorithmSettings
    {
        public PredictionType PredictionType { get; set; }
        public int MinibatchSize { get; set; } = 1000;
        public int NumMinibatches { get; set; } = 100;

        public bool? SparseMatrix { get; set; }
        [Unit("Minibaches")]
        public int SaveProgressEvery { get; set; } = 5;

        [Unit("Minibaches")]
        public int SaveValidationProgressEvery { get; set; } = 10;

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if(pi.Name == nameof(SaveValidationProgressEvery))
            {
                if(SaveValidationProgressEvery % SaveProgressEvery != 0)
                {
                    return PredictorMessage._0ShouldBeDivisibleBy12.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => SaveProgressEvery).NiceName());
                }

            }

            return base.PropertyValidation(pi);
        }

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
