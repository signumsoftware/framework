using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class PredictorEntity : Entity
    {
        [Ignore]
        internal object queryName;

        [NotNullValidator]
        public QueryEntity Query { get; set; }

        [NotNullable, SqlDbType(Size = 100), UniqueIndex]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable, PreserveOrder]
        public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<PredictorInputEntity> Inputs { get; set; } = new MList<PredictorInputEntity>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<PredictorOutputEntity> Output { get; set; } = new MList<PredictorOutputEntity>();

 
    }

    [Serializable]
    public class PredictorInputEntity : EmbeddedEntity
    {
        public QueryTokenEmbedded Token { get; set; }
    }

    [Serializable]
    public class PredictorOutputEntity : EmbeddedEntity
    {
        public QueryTokenEmbedded Token { get; set; }
    }

    [Serializable]
    public class NeuronalNetworkSettingsEntity : EmbeddedEntity
    {
        public double LearningRate { get; set; }

        public ActivationFunction ActivationFunction { get; set; }

        public Regularization Regularization { get; set; }

        public double RegularizationRate { get; set; }

        public double TrainingRatio { get; set; }

        public double BackSize { get; set; }

        [NotNullable, SqlDbType(Size = int.MaxValue)]
        public string NeuronalNetworkDescription { get; set; }
    }

    [AutoInit]
    public static class PredictorOperation
    {
        public static readonly ExecuteSymbol<PredictorEntity> Save;
    }

    public class NeuronalNetworkDescription
    {
        public double LearningRate;
        public ActivationFunction ActivationFuntion;
    }

    public enum ActivationFunction
    {
        ReLU,
        Tanh,
        Sigmoid,
        Linear,
    }

    public enum Regularization
    {
        None, 
        L1,
        L2,
    }
}
