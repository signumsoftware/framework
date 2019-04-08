using Signum.Entities.Processes;
using Signum.Entities.Reflection;
using Signum.Utilities;
using Signum.Utilities.Reflection;
using System;
using System.Linq;
using System.Reflection;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Part, EntityData.Master)]
    public class NeuralNetworkSettingsEntity : Entity, IPredictorAlgorithmSettings
    {
        [StringLengthValidator(Max = 100)]
        public string? Device { get; set; }

        public PredictionType PredictionType { get; set; }

        [PreserveOrder]
        [NoRepeatValidator]
        public MList<NeuralNetworkHidenLayerEmbedded> HiddenLayers { get; set; } = new MList<NeuralNetworkHidenLayerEmbedded>();

        public NeuralNetworkActivation OutputActivation { get; set; }
        public NeuralNetworkInitializer OutputInitializer { get; set; }

        public NeuralNetworkLearner Learner { get; set; }

        public NeuralNetworkEvalFunction LossFunction { get; set; }
        public NeuralNetworkEvalFunction EvalErrorFunction { get; set; }

        [DecimalsValidator(5), NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public double LearningRate { get; set; } = 0.2;

        [DecimalsValidator(5), NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public double? LearningMomentum { get; set; } = null;

        public bool? LearningUnitGain { get; set; }

        [DecimalsValidator(5), NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public double? LearningVarianceMomentum { get; set; } = null;

        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int MinibatchSize { get; set; } = 1000;

        [NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int NumMinibatches { get; set; } = 100;

        [Unit("Minibaches"), NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int BestResultFromLast { get; set; } = 10;

        [Unit("Minibaches"), NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int SaveProgressEvery { get; set; } = 5;

        [Unit("Minibaches"), NumberIsValidator(ComparisonType.GreaterThan, 0)]
        public int SaveValidationProgressEvery { get; set; } = 10;

        protected override string? PropertyValidation(PropertyInfo pi)
        {
            if (pi.Name == nameof(SaveValidationProgressEvery))
            {
                if (SaveValidationProgressEvery % SaveProgressEvery != 0)
                {
                    return PredictorMessage._0ShouldBeDivisibleBy12.NiceToString(pi.NiceName(), ReflectionTools.GetPropertyInfo(() => SaveProgressEvery).NiceName(), SaveProgressEvery);
                }

            }

            if (pi.Name == nameof(OutputActivation))
            {
                if (OutputActivation == NeuralNetworkActivation.ReLU || OutputActivation == NeuralNetworkActivation.Sigmoid)
                {
                    var p = this.GetParentEntity<PredictorEntity>();
                    var errors = p.MainQuery.Columns.Where(a => a.Usage == PredictorColumnUsage.Output && a.Encoding.Is(DefaultColumnEncodings.NormalizeZScore)).Select(a => a.Token).ToList();
                    errors.AddRange(p.SubQueries.SelectMany(sq => sq.Columns).Where(a => a.Usage == PredictorSubQueryColumnUsage.Output && a.Encoding.Is(DefaultColumnEncodings.NormalizeZScore)).Select(a => a.Token).ToList());

                    if (errors.Any())
                        return PredictorMessage._0CanNotBe1Because2Use3.NiceToString(pi.NiceName(), OutputActivation.NiceToString(), errors.CommaAnd(), DefaultColumnEncodings.NormalizeZScore.NiceToString());
                }
            }

            string? Validate(NeuralNetworkEvalFunction function)
            {
                bool lossIsClassification = function == NeuralNetworkEvalFunction.CrossEntropyWithSoftmax || function == NeuralNetworkEvalFunction.ClassificationError;
                bool typeIsClassification = this.PredictionType == PredictionType.Classification || this.PredictionType == PredictionType.MultiClassification;

                if (lossIsClassification != typeIsClassification)
                    return PredictorMessage._0IsNotCompatibleWith12.NiceToString(function.NiceToString(), this.NicePropertyName(a => a.PredictionType), this.PredictionType.NiceToString());

                return null;
            }

            if (pi.Name == nameof(LossFunction))
            {
                return Validate(LossFunction);
            }

            if (pi.Name == nameof(EvalErrorFunction))
            {
                return Validate(EvalErrorFunction);
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


            LossFunction = LossFunction,
            EvalErrorFunction = EvalErrorFunction,
            Learner = Learner,
            LearningRate = LearningRate,
            LearningMomentum = LearningMomentum,
            LearningUnitGain = LearningUnitGain,
            LearningVarianceMomentum = LearningVarianceMomentum,

            MinibatchSize = MinibatchSize,
            NumMinibatches = NumMinibatches,
            BestResultFromLast = BestResultFromLast,
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

    public enum NeuralNetworkEvalFunction
    {
        CrossEntropyWithSoftmax,
        ClassificationError,
        SquaredError,
        MeanAbsoluteError,
        MeanAbsolutePercentageError,
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Transactional)]
    public class AutoconfigureNeuralNetworkEntity : Entity, IProcessDataEntity
    {
        
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
