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
using Signum.Entities.Files;
using Signum.Entities.Authorization;
using System.Xml.Linq;

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class PredictorEntity : Entity
    {
        public PredictorEntity()
        {
            RebindEvents();
        }

        [SqlDbType(Size = 100),]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable]
        [NotNullValidator]
        public PredictorSettingsEmbedded Settings { get; set; }

        [NotNullable, NotNullValidator]
        public PredictorAlgorithmSymbol Algorithm { get; set; }
        
        public Lite<ExceptionEntity> TrainingException { get; set; }

        [ImplementedBy(typeof(UserEntity))]
        public Lite<IUserEntity> User { get; set; }

        [ImplementedBy(typeof(NeuralNetworkSettingsEntity))]
        public IPredictorAlgorithmSettings AlgorithmSettings { get; set; }

        public PredictorState State { get; set; }

        [NotNullable]
        [NotNullValidator, InTypeScript(Undefined = false, Null = false), NotifyChildProperty]
        public PredictorMainQueryEmbedded MainQuery { get; set; }

        [Ignore, NotifyChildProperty, NotifyCollectionChanged] //virtual Mlist
        public MList<PredictorSubQueryEntity> SubQueries { get; set; } = new MList<PredictorSubQueryEntity>();
        
        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<FilePathEmbedded> Files { get; set; } = new MList<FilePathEmbedded>();

        public PredictorClassificationMetricsEmbedded ClassificationTraining { get; set; }
        public PredictorClassificationMetricsEmbedded ClassificationValidation { get; set; }
        public PredictorRegressionMetricsEmbedded RegressionTraining { get; set; }
        public PredictorRegressionMetricsEmbedded RegressionValidation { get; set; }
    }

    [Serializable]
    public class PredictorMainQueryEmbedded : EmbeddedEntity
    {
        public PredictorMainQueryEmbedded()
        {
            RebindEvents();
        }

        [Ignore]
        internal object queryName;

        [NotNullValidator]
        public QueryEntity Query { get; set; }

        [NotNullable, PreserveOrder]
        public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator, NotifyChildProperty, NotifyCollectionChanged]
        public MList<PredictorColumnEmbedded> Columns { get; set; } = new MList<PredictorColumnEmbedded>();

        internal void ParseData(QueryDescription qd)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(this, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement);

            if (Columns != null)
                foreach (var c in Columns)
                    c.ParseData(this, qd, SubTokensOptions.CanElement);
        }

        internal PredictorMainQueryEmbedded Clone() => new PredictorMainQueryEmbedded
        {
            Query = Query,
            Filters = Filters.Select(f => f.Clone()).ToMList(),
            Columns = Columns.Select(a => a.Clone()).ToMList(),
        };
    }

    [Serializable]
    public class PredictorClassificationMetricsEmbedded : EmbeddedEntity
    {
        public int TotalCount { get; set; }
        public int MissCount { get; set; }
        [Format("p")]
        public decimal? MissRate { get; set; }

        protected override void PreSaving(ref bool graphModified)
        {
            MissRate = TotalCount == 0 ? (decimal?)null : Math.Round(MissCount / (decimal)TotalCount, 2);

            base.PreSaving(ref graphModified);
        }
    }

    [Serializable]
    public class PredictorRegressionMetricsEmbedded : EmbeddedEntity
    {
        public double? Signed { get; set; }

        [Unit("±")]
        public double? Absolute { get; set; }

        [Unit("±")]
        public double? Deviation { get; set; }

        [Format("p")]
        public double? PercentageSigned { get; set; }

        [Format("p"), Unit("±")]
        public double? PercentageAbsolute { get; set; }

        [Format("p"), Unit("±")]
        public double? PercentageDeviation { get; set; }

    }

    [Serializable]
    public class PredictorSettingsEmbedded : EmbeddedEntity
    {
        [Format("p")]
        public double TestPercentage { get; set; } = 0.2;

        public int? Seed { get; set; }

        internal PredictorSettingsEmbedded Clone()
        {
            throw new NotImplementedException();
        }
    }

    [AutoInit]
    public static class PredictorFileType
    {
        public static readonly FileTypeSymbol PredictorFile;
    }


    public interface IPredictorAlgorithmSettings : IEntity
    {
        IPredictorAlgorithmSettings Clone();
    }

    [AutoInit]
    public static class PredictorOperation
    {
        public static readonly ExecuteSymbol<PredictorEntity> Save;
        public static readonly ExecuteSymbol<PredictorEntity> Train;
        public static readonly ExecuteSymbol<PredictorEntity> CancelTraining;
        public static readonly ExecuteSymbol<PredictorEntity> StopTraining;
        public static readonly ExecuteSymbol<PredictorEntity> Untrain;
        public static readonly DeleteSymbol<PredictorEntity> Delete;
        public static readonly ConstructSymbol<PredictorEntity>.From<PredictorEntity> Clone;
    }

    [Serializable]
    public class PredictorGroupKeyEmbedded : EmbeddedEntity
    {
        [NotNullable]
        [NotNullValidator]
        public QueryTokenEmbedded Token { get; set; }
        
        public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
        {
            if (Token != null)
                Token.ParseData(context, description, options);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            return base.PropertyValidation(pi);
        }
        
        internal PredictorGroupKeyEmbedded Clone() => new PredictorGroupKeyEmbedded
        {
            Token = Token.Clone(),
        };
    }

    [Serializable]
    public class PredictorColumnEmbedded : EmbeddedEntity
    {
        public PredictorColumnUsage Usage { get; set; }

        [NotNullable]
        [NotNullValidator]
        public QueryTokenEmbedded Token { get; set; }

        public PredictorColumnEncoding Encoding { get; set; }

        public PredictorColumnNullHandling NullHandling { get; set; }

        public void ParseData(ModifiableEntity context, QueryDescription description, SubTokensOptions options)
        {
            if (Token != null)
                Token.ParseData(context, description, options);
        }

        protected override string PropertyValidation(PropertyInfo pi)
        {
            return base.PropertyValidation(pi);
        }
        
        internal PredictorColumnEmbedded Clone() => new PredictorColumnEmbedded
        {
        
            Usage = Usage, 
            Token = Token.Clone(),
        
        };
    }

    public enum PredictorColumnNullHandling
    {
        Zero,
        Error,
        MinValue,
        AvgValue,
        MaxValue,
    }

    public enum PredictorColumnEncoding
    {
        None,
        OneHot,
        Codified
    }

    public enum PredictorState
    {
        Draft,
        Training, 
        Trained,
        Error,
    }

    public enum PredictorColumnUsage
    {
        Input,
        Output
    }


    [Serializable, EntityKind(EntityKind.Part, EntityData.Transactional)]
    public class PredictorSubQueryEntity : Entity
    {
        public PredictorSubQueryEntity()
        {
            RebindEvents();
        }

        [NotNullable]
        public Lite<PredictorEntity> Predictor { get; set; }

        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable, NotNullValidator]
        public QueryEntity Query { get; set; }

        [NotNullable, PreserveOrder]
        public MList<QueryFilterEmbedded> AdditionalFilters { get; set; } = new MList<QueryFilterEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator, NotifyChildProperty, NotifyCollectionChanged]
        public MList<PredictorGroupKeyEmbedded> GroupKeys { get; set; } = new MList<PredictorGroupKeyEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator, NotifyChildProperty, NotifyCollectionChanged]
        public MList<PredictorColumnEmbedded> Aggregates { get; set; } = new MList<PredictorColumnEmbedded>();

        public void ParseData(QueryDescription description)
        {
            if (AdditionalFilters != null)
                foreach (var f in AdditionalFilters)
                    f.ParseData(this, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate);

            if (GroupKeys != null)
                foreach (var k in GroupKeys)
                    k.ParseData(this, description, SubTokensOptions.CanElement);

            if (Aggregates != null)
                foreach (var a in Aggregates)
                    a.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate);
        }

        public PredictorSubQueryEntity Clone() => new PredictorSubQueryEntity
        {
            Query = Query,
            Name = Name,
            AdditionalFilters = AdditionalFilters.Select(f => f.Clone()).ToMList(),
            GroupKeys = GroupKeys.Select(f => f.Clone()).ToMList(),
            Aggregates = Aggregates.Select(f => f.Clone()).ToMList(),
        };


        static Expression<Func<PredictorSubQueryEntity, string>> ToStringExpression = @this => @this.Name;
        [ExpressionField]
        public override string ToString()
        {
            return ToStringExpression.Evaluate(this);
        }
    }

    [Serializable]
    public class PredictorAlgorithmSymbol : Symbol
    {
        private PredictorAlgorithmSymbol() { }

        public PredictorAlgorithmSymbol(Type declaringType, string fieldName) :
            base(declaringType, fieldName)
        {
        }
    }

    [AutoInit]
    public static class AccordPredictorAlgorithm
    {
        public static PredictorAlgorithmSymbol DiscreteNaiveBayes;
    }

    [AutoInit]
    public static class CNTKPredictorAlgorithm
    {
        public static PredictorAlgorithmSymbol NeuralNetwork;
    }
}
