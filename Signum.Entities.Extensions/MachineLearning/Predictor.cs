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

namespace Signum.Entities.MachineLearning
{
    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class PredictorEntity : Entity
    {
        [Ignore]
        internal object queryName;

        [NotNullValidator]
        public QueryEntity Query { get; set; }

        [SqlDbType(Size = 100),]
        [StringLengthValidator(AllowNulls = true, Min = 3, Max = 100)]
        public string Name { get; set; }

        [NotNullable]
        [NotNullValidator]
        public PredictorSettingsEmbedded Settings { get; set; }

        [NotNullable, NotNullValidator]
        public PredictorAlgorithmSymbol Algorithm { get; set; }

        [ImplementedBy(typeof(NeuronalNetworkSettingsEntity))]
        public IPredictorAlgorithmSettings AlgorithmSettings { get; set; }

        public PredictorState State { get; set; }

        [NotNullable, PreserveOrder]
        public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<PredictorColumnEmbedded> SimpleColumns { get; set; } = new MList<PredictorColumnEmbedded>();
        
        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<PredictorMultiColumnEntity> MultiColumns { get; set; } = new MList<PredictorMultiColumnEntity>();


        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<PredictorFileEmbedded> Files { get; set; } = new MList<PredictorFileEmbedded>();

        internal void ParseData(QueryDescription qd)
        {
            if (Filters != null)
                foreach (var f in Filters)
                    f.ParseData(this, qd, SubTokensOptions.CanAnyAll);

            if (SimpleColumns != null)
                foreach (var c in SimpleColumns)
                    c.ParseData(this, qd, SubTokensOptions.CanElement);
        }
    }

    [Serializable]
    public class PredictorSettingsEmbedded : EmbeddedEntity
    {
        public int Bar { get; set; }


        internal PredictorSettingsEmbedded Clone()
        {
            throw new NotImplementedException();
        }
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
        public static readonly ExecuteSymbol<PredictorEntity> Untrain;
        public static readonly DeleteSymbol<PredictorEntity> Delete;
        public static readonly ConstructSymbol<PredictorEntity>.From<PredictorEntity> Clone;
    }

    [Serializable]
    public class PredictorColumnEmbedded : EmbeddedEntity
    {
        public PredictorColumnUsage Usage { get; set; }

        public QueryTokenEmbedded Token { get; set; }

        public PredictorColumnEncoding Encoding { get; set; }

        public void ParseData(Entity context, QueryDescription description, SubTokensOptions options)
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

    public enum PredictorColumnEncoding
    {
        None,
        OneHot,
        Codified
    }

    public enum PredictorState
    {
        Draft, 
        Trained,
    }

    

    public enum PredictorColumnUsage
    {
        Input,
        Output
    }


    [Serializable, EntityKind(EntityKind.Part, EntityData.Transactional)]
    public class PredictorMultiColumnEntity : Entity
    {
        [NotNullable, NotNullValidator]
        public QueryEntity Query { get; set; }

        [NotNullable, PreserveOrder]
        public MList<QueryFilterEmbedded> AdditionalFilters { get; set; } = new MList<QueryFilterEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<QueryTokenEmbedded> GroupKeys { get; set; } = new MList<QueryTokenEmbedded>();

        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
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

        public PredictorMultiColumnEntity Clone() => new PredictorMultiColumnEntity
        {
            Query = Query,
            AdditionalFilters = AdditionalFilters.Select(f => f.Clone()).ToMList(),
            GroupKeys = GroupKeys.Select(f => f.Clone()).ToMList(),
            Aggregates = Aggregates.Select(f => f.Clone()).ToMList(),
        };
    }

   

    [Serializable, EntityKind(EntityKind.Main, EntityData.Transactional)]
    public class PredictorCodificationEntity : Entity
    {
        [NotNullable]
        public Lite<PredictorEntity> Predictor { get; set; }

        public int ColumnIndex { get; set; }


        public int? OriginalMultiColumnIndex{ get; set; }

        public int OriginalColumnIndex { get; set; }

        

        //For flatting collections
        [SqlDbType(Size = 100)]
        public string GroupKey0 { get; set; }

        [SqlDbType(Size = 100)]
        public string GroupKey1 { get; set; }

        [SqlDbType(Size = 100)]
        public string GroupKey2 { get; set; }


        //For 1-hot encoding
        [SqlDbType(Size = 100)]
        public string IsValue { get; set; }


        //For encoding values
        [NotNullable, PreserveOrder]
        [NotNullValidator, NoRepeatValidator]
        public MList<string> CodedValues { get; set; } = new MList<string>();
    }

    [Serializable]
    public class PredictorFileEmbedded : EmbeddedEntity
    {
        [NotNullable, SqlDbType(Size = 100)]
        [StringLengthValidator(AllowNulls = false, Min = 3, Max = 100)]
        public string Key { get; set; }

        public FilePathEmbedded File { get; set; }
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
}
