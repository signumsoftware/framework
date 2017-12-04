using Signum.Entities;
using Signum.Entities.MachineLearning;
using Signum.Entities.Processes;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Utilities.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class NeuralNetworkSettingsEntity : Entity, IPredictorAlgorithmSettings
    {
        [SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = true, Max = 100)]
        public string Device { get; set; }

        public PredictionType PredictionType { get; set; }

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<NeuralNetworkHidenLayerEmbedded> HiddenLayers { get; set; } = new MList<NeuralNetworkHidenLayerEmbedded>();

        public NeuralNetworkActivation OutputActivation { get; set; }
        public NeuralNetworkInitializer OutputInitializer { get; set; }

        public NeuralNetworkLearner Learner { get; set; }

        [DecimalsValidator(5)]
        public double LearningRate { get; set; } = 0.2;

        [DecimalsValidator(5)]
        public double? LearningMomentum { get; set; } = null;

        public bool? LearningUnitGain { get; set; }

        [DecimalsValidator(5)]
        public double? LearningVarianceMomentum { get; set; } = null;

        public int MinibatchSize { get; set; } = 1000;
        public int NumMinibatches { get; set; } = 100;

        [Unit("Minibaches")]
        public int SaveProgressEvery { get; set; } = 5;

        [Unit("Minibaches")]
        public int SaveValidationProgressEvery { get; set; } = 10;

        protected override string PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(SaveValidationProgressEvery))
            {
                if (SaveValidationProgressEvery % SaveProgressEvery != 0)
                {
                    return PredictorMessage._0ShouldBeDivisibleBy12.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => SaveProgressEvery).NiceName(), SaveProgressEvery);
                }

            }

            return base.PropertyValidation(pi);
        }

        public IPredictorAlgorithmSettings Clone() => new NeuralNetworkSettingsEntity
        {
            Device = Device,
            PredictionType = PredictionType,
            HiddenLayers = HiddenLayers.Select(hl => hl.Clone()).ToMList(),
            OutputActivation = OutputActivation,
            OutputInitializer = OutputInitializer,
            LearningRate = LearningRate,
            LearningMomentum = LearningMomentum,
            MinibatchSize = MinibatchSize,  
            NumMinibatches = NumMinibatches,
            SaveProgressEvery = SaveProgressEvery,
            SaveValidationProgressEvery = SaveValidationProgressEvery,
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

        internal NeuralNetworkHidenLayerEmbedded Clone() => new NeuralNetworkHidenLayerEmbedded
        {
            Size = Size,
            Activation = Activation,
            Initializer = Initializer
        };
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

    public enum NeuralNetworkLearner
    {
        Adam,
        AdaDelta,
        AdaGrad,
        FSAdaGrad,
        RMSProp,
        MomentumSGD,
        SGD,
    }
    
    [Serializable, EntityKind(EntityKind.Part, EntityData.Transactional)]
    public class AutoconfigureNeuralNetworkEntity : Entity, IProcessDataEntity
    {
        [NotNullable]
        [NotNullValidator]
        public Lite<PredictorEntity> InitialPredictor { get; set; }

        public bool ExploreLearner { get; set; }
        public bool ExploreLearningValues { get; set; }
        public bool ExploreHiddenLayers { get; set; }
        public bool ExploreOutputLayer { get; set; }

        public int MaxLayers { get; set; } = 2;
        public int MinNeuronsPerLayer { get; set; } = 5;
        public int MaxNeuronsPerLayer { get; set; } = 20;

        [Unit("seconds")]
        public long? OneTrainingDuration { get; set; }

        public int Generations { get; set; } = 10;
        public int Population { get; set; } = 10;

        [Format("p")]
        public double SurvivalRate { get; set; } = 0.4;

        [Format("p")]
        public double InitialMutationProbability { get; set; } = 0.1;

        public int? Seed { get; set; }
    }
}
