using Signum.Entities;
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
        
        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<NeuralNetworkHidenLayerEmbedded> HiddenLayers { get; set; } = new MList<NeuralNetworkHidenLayerEmbedded>();

        public NeuralNetworkActivation OutputActivation { get; set; }
        public NeuralNetworkInitializer OutputInitializer { get; set; }

        public double LearningRate { get; set; } = 0.2;
        public double? LearningMomentum { get; set; } = null;

        public int MinibatchSize { get; set; } = 1000;
        public int NumMinibatches { get; set; } = 100;

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
                    return PredictorMessage._0ShouldBeDivisibleBy12.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => SaveProgressEvery).NiceName(), SaveProgressEvery);
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
        MultiRegression,
        Classification,
        MultiClassification,
    }

    [Serializable]
    public class NeuralNetworkHidenLayerEmbedded : EmbeddedEntity
    {
        [Unit("Neurons")]
        public int Size { get; set; }

        public NeuralNetworkActivation Activation { get; set; }

        public NeuralNetworkInitializer Initializer { get; set; }
    }

    public enum NeuralNetworkActivation
    {
        None, 
        ReLU,
        Sigmoid,
        Tanh
    }

    public enum NeuralNetworkInitializer
    {
        Zero,
        GlorotNormal,
        GlorotUniform,
        HeNormal,
        HeUniform,
        Normal,
        TruncateNormal,
        Uniform,
        Xavier,
    }
}
