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
using Signum.Entities.DynamicQuery;
using System.Reflection;

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
        public MList<PredictorColumnEmbedded> Columns { get; set; } = new MList<PredictorColumnEmbedded>();

        internal void ParseData(QueryDescription qd)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(this, qd, SubTokensOptions.CanAnyAll);

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(this, qd, SubTokensOptions.CanElement);
        }
    }

    [Serializable]
    public class PredictorColumnEmbedded : EmbeddedEntity
    {
        public PredictorColumnType Type { get; set; }

        public PredictorColumnUsage Usage { get; set; }

        public QueryTokenEmbedded Token { get; set; }

        public PredictorMultiColumnEntity MultiColumn { get; set; }

        public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
        {
            Token.ParseData(context, description, options);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            return base.PropertyValidation(pi) ?? StateValidator.Validate(this, pi);
        }

        static readonly StateValidator<PredictorColumnEmbedded, PredictorColumnType> StateValidator = new StateValidator<PredictorColumnEmbedded, PredictorColumnType>
            (p => p.Type, p => p.Token, p => p.MultiColumn)
        {
            { PredictorColumnType.SimpleColumn, true, false },
            { PredictorColumnType.MultiColumn, false, true},
        };
    }

    [Serializable, EntityKind(EntityKind.Part, EntityData.Transactional)]
    public class PredictorMultiColumnEntity : Entity
    {
        [NotNullable, NotNullValidator]
        public QueryEntity Query { get; set; }

        [NotNullable, PreserveOrder]
        public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<QueryTokenEmbedded> GroupKeys { get; set; } = new MList<QueryTokenEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<QueryTokenEmbedded> Aggregates { get; set; } = new MList<QueryTokenEmbedded>();

        public void ParseData(QueryDescription description)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(this, description, SubTokensOptions.CanAnyAll);

            if (GroupKeys != null)
                foreach (var k in GroupKeys)
                    k.ParseData(this, description, SubTokensOptions.CanElement);

            if (Aggregates != null)
                foreach (var a in Aggregates)
                    a.ParseData(this, description, SubTokensOptions.CanElement);
        }
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

    public enum PredictorColumnType
    {
        SimpleColumn,
        MultiColumn,
    }

    public enum PredictorColumnUsage
    {
        Input,
        Output
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
