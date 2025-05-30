using Signum.Authorization;
using Signum.Files;
using Signum.Processes;
using Signum.UserAssets.Queries;
using System.ComponentModel;

namespace Signum.MachineLearning;

[EntityKind(EntityKind.Main, EntityData.Transactional)]
public class PredictorEntity : Entity, IProcessDataEntity
{
    public PredictorEntity()
    {
        BindParent();
    }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string? Name { get; set; }


    public PredictorSettingsEmbedded Settings { get; set; }

    public PredictorAlgorithmSymbol Algorithm { get; set; }

    public PredictorResultSaverSymbol? ResultSaver { get; set; }

    public PredictorPublicationSymbol? Publication { get; set; }

    public Lite<ExceptionEntity>? TrainingException { get; set; }

    [ImplementedBy(typeof(UserEntity))]
    public Lite<IUserEntity>? User { get; set; }

    [ImplementedBy(typeof(NeuralNetworkSettingsEntity)), BindParent]
    public IPredictorAlgorithmSettings AlgorithmSettings { get; set; }

    public PredictorState State { get; set; }

    [BindParent]
    public PredictorMainQueryEmbedded MainQuery { get; set; }

    [Ignore, QueryableProperty] //virtual Mlist
    [BindParent]
    public MList<PredictorSubQueryEntity> SubQueries { get; set; } = new MList<PredictorSubQueryEntity>();

    [PreserveOrder]
    [NoRepeatValidator]
    public MList<FilePathEmbedded> Files { get; set; } = new MList<FilePathEmbedded>();

    public PredictorMetricsEmbedded? ResultTraining { get; set; }
    public PredictorMetricsEmbedded? ResultValidation { get; set; }
    public PredictorClassificationMetricsEmbedded? ClassificationTraining { get; set; }
    public PredictorClassificationMetricsEmbedded? ClassificationValidation { get; set; }
    public PredictorRegressionMetricsEmbedded? RegressionTraining { get; set; }
    public PredictorRegressionMetricsEmbedded? RegressionValidation { get; set; }


    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name!);
}



public class PredictorMainQueryEmbedded : EmbeddedEntity
{
    public PredictorMainQueryEmbedded()
    {
        BindParent();
    }


    public QueryEntity Query { get; set; }

    public bool GroupResults { get; set; }

    [PreserveOrder]
    public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

    [PreserveOrder]
    [NoRepeatValidator, BindParent]
    public MList<PredictorColumnEmbedded> Columns { get; set; } = new MList<PredictorColumnEmbedded>();

    public void ParseData(QueryDescription qd)
    {
        var canAggregate = this.GroupResults ? SubTokensOptions.CanAggregate : 0;

        if (Filters != null)
            foreach (var f in Filters)
                f.ParseData(this, qd, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | canAggregate);

        if (Columns != null)
            foreach (var c in Columns)
                c.ParseData(this, qd, SubTokensOptions.CanElement | canAggregate);
    }

    internal PredictorMainQueryEmbedded Clone() => new PredictorMainQueryEmbedded
    {
        Query = Query,
        GroupResults = GroupResults,
        Filters = Filters.Select(f => f.Clone()).ToMList(),
        Columns = Columns.Select(a => a.Clone()).ToMList(),
    };


    public PredictorColumnEmbedded FindColumn(string part)
    {
        return Columns.SingleEx(a => a.Token.Token.ContainsKey(part));
    }

    public PredictorColumnEmbedded? TryFindColumn(string part)
    {
        return Columns.SingleOrDefaultEx(a => a.Token.Token.ContainsKey(part));
    }
}

public class PredictorClassificationMetricsEmbedded : EmbeddedEntity
{
    public int TotalCount { get; set; }
    public int MissCount { get; set; }
    [Format("p2")]
    public double? MissRate { get; private set; }

    protected override void PreSaving(PreSavingContext ctx)
    {
        base.PreSaving(ctx);

        MissRate = TotalCount == 0 ? (double?)null : Math.Round(MissCount / (double)TotalCount, 2);
    }
}

public class PredictorRegressionMetricsEmbedded : EmbeddedEntity
{
    [Format("F4")]
    public double? MeanError { get; set; }

    [Format("F4"), Unit("±")]
    public double? MeanSquaredError { get; set; }

    [Format("F4"), Unit("±")]
    public double? MeanAbsoluteError { get; set; }

    [Format("F4"), Unit("±")]
    public double? RootMeanSquareError { get; set; }

    [Format("P2")]
    public double? MeanPercentageError { get; set; }

    [Format("P2"), Unit("±")]
    public double? MeanAbsolutePercentageError { get; set; }
}

public class PredictorMetricsEmbedded : EmbeddedEntity
{
    [Format("F4")]
    public double? Loss { get; set; }

    [Format("F4")]
    public double? Accuracy { get; set; }
}

public class PredictorSettingsEmbedded : EmbeddedEntity
{
    [Format("p")]
    public double TestPercentage { get; set; } = 0.2;

    public int? Seed { get; set; }

    internal PredictorSettingsEmbedded Clone() => new PredictorSettingsEmbedded
    {
        TestPercentage = TestPercentage,
        Seed = Seed
    };
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
    public static readonly ExecuteSymbol<PredictorEntity> Publish;
    public static readonly ConstructSymbol<Entity>.From<PredictorEntity> AfterPublishProcess;
    public static readonly DeleteSymbol<PredictorEntity> Delete;
    public static readonly ConstructSymbol<PredictorEntity>.From<PredictorEntity> Clone;
    public static readonly ConstructSymbol<ProcessEntity>.From<PredictorEntity> AutoconfigureNetwork;
}

public class PredictorColumnEmbedded : EmbeddedEntity, IEquatable<PredictorColumnEmbedded>
{
    public PredictorColumnUsage Usage { get; set; }

    public QueryTokenEmbedded Token { get; set; }

    public PredictorColumnEncodingSymbol Encoding { get; set; }

    public PredictorColumnNullHandling NullHandling { get; set; }

    public void ParseData(ModifiableEntity context, QueryDescription description, SubTokensOptions options)
    {
        if (Token != null)
            Token.ParseData(context, description, options);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {
        return base.PropertyValidation(pi);
    }

    internal PredictorColumnEmbedded Clone() => new PredictorColumnEmbedded
    {
        Usage = Usage,
        Token = Token.Clone(),
        Encoding = Encoding,
        NullHandling = NullHandling
    };

    public override string ToString() => $"{Usage} {Token} {Encoding}";

    public override bool Equals(object? obj) => obj is PredictorColumnEmbedded c && Equals(c);
    public bool Equals(PredictorColumnEmbedded? other)
    {
        if (other == null)
            return false;

        return object.Equals(this.Token, other.Token) && this.Usage == other.Usage;
    }

    public override int GetHashCode()
    {
        return (this.Token?.GetHashCode() ?? 0) ^ this.Usage.GetHashCode();
    }

}

public enum PredictorColumnNullHandling
{
    Zero,
    Error,
    Average,
    Min,
    Max,
}



[AutoInit]
public static class DefaultColumnEncodings
{
    public static PredictorColumnEncodingSymbol None;
    public static PredictorColumnEncodingSymbol OneHot;

    [Description("Normalize Z-Score")]
    public static PredictorColumnEncodingSymbol NormalizeZScore;

    [Description("Normalize Min-Max")]
    public static PredictorColumnEncodingSymbol NormalizeMinMax;

    [Description("Normalize Log")]
    public static PredictorColumnEncodingSymbol NormalizeLog;

    [Description("Split Words")]
    public static PredictorColumnEncodingSymbol SplitWords;
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


[EntityKind(EntityKind.Part, EntityData.Transactional)]
public class PredictorSubQueryEntity : Entity, ICanBeOrdered
{
    public PredictorSubQueryEntity()
    {
        BindParent();
    }

    [NotNullValidator(Disabled = true)]
    public Lite<PredictorEntity> Predictor { get; set; }

    [StringLengthValidator(Min = 3, Max = 100)]
    public string Name { get; set; }


    public QueryEntity Query { get; set; }

    [PreserveOrder]
    public MList<QueryFilterEmbedded> Filters { get; set; } = new MList<QueryFilterEmbedded>();

    [PreserveOrder]
    [NoRepeatValidator, BindParent]
    public MList<PredictorSubQueryColumnEmbedded> Columns { get; set; } = new MList<PredictorSubQueryColumnEmbedded>();

    public int Order { get; set; }

    public void ParseData(QueryDescription description)
    {
        foreach (var f in Filters)
            f.ParseData(this, description, SubTokensOptions.CanAnyAll | SubTokensOptions.CanElement | SubTokensOptions.CanAggregate);

        foreach (var a in Columns)
            a.ParseData(this, description, SubTokensOptions.CanElement | SubTokensOptions.CanAggregate);
    }

    public PredictorSubQueryEntity Clone() => new PredictorSubQueryEntity
    {
        Name = Name,
        Query = Query,
        Filters = Filters.Select(f => f.Clone()).ToMList(),
        Columns = Columns.Select(f => f.Clone()).ToMList(),
    };

    [AutoExpressionField]
    public override string ToString() => As.Expression(() => Name);

    public PredictorSubQueryColumnEmbedded FindColumn(string part)
    {
        return Columns.SingleEx(a => a.Token.Token.ContainsKey(part));
    }
}

public class PredictorSubQueryColumnEmbedded : EmbeddedEntity, IEquatable<PredictorSubQueryColumnEmbedded>
{
    public PredictorSubQueryColumnUsage Usage { get; set; }


    public QueryTokenEmbedded Token { get; set; }

    public PredictorColumnEncodingSymbol Encoding { get; set; }

    public PredictorColumnNullHandling? NullHandling { get; set; }

    public void ParseData(ModifiableEntity context, QueryDescription description, SubTokensOptions options)
    {
        if (Token != null)
            Token.ParseData(context, description, options);
    }

    protected override string? PropertyValidation(PropertyInfo pi)
    {


        return stateValidator.Validate(this, pi) ?? base.PropertyValidation(pi);
    }

    public static StateValidator<PredictorSubQueryColumnEmbedded, PredictorSubQueryColumnUsage> stateValidator =
        new StateValidator<PredictorSubQueryColumnEmbedded, PredictorSubQueryColumnUsage>
        (a => a.Usage, a => a.Encoding, a => a.NullHandling)
    {
        { PredictorSubQueryColumnUsage.Input, true, true },
        { PredictorSubQueryColumnUsage.Output,true, true },
        { PredictorSubQueryColumnUsage.SplitBy, false, false},
        { PredictorSubQueryColumnUsage.ParentKey, false, false },
    };

    internal PredictorSubQueryColumnEmbedded Clone() => new PredictorSubQueryColumnEmbedded
    {
        Usage = Usage,
        Token = Token.Clone(),
        Encoding = Encoding,
        NullHandling = NullHandling
    };

    public override string ToString() => $"{Usage} {Token} {Encoding}";

    public override bool Equals(object? obj) => obj is PredictorSubQueryColumnEmbedded c && Equals(c);
    public bool Equals(PredictorSubQueryColumnEmbedded? other)
    {
        if (other == null)
            return false;

        return object.Equals(this.Token, other.Token) && this.Usage == other.Usage;
    }

    public override int GetHashCode()
    {
        return (this.Token?.GetHashCode() ?? 0) ^ this.Usage.GetHashCode();
    }
}

public enum PredictorSubQueryColumnUsage
{
    ParentKey,
    SplitBy,
    Input,
    Output
}

public static class PredictorColumnUsageExtensions
{
    public static PredictorColumnUsage ToPredictorColumnUsage(this PredictorSubQueryColumnUsage usage)
    {
        return usage == PredictorSubQueryColumnUsage.Input ? PredictorColumnUsage.Input :
            usage == PredictorSubQueryColumnUsage.Output ? PredictorColumnUsage.Output :
            throw new InvalidOperationException("Unexcpected " + nameof(PredictorSubQueryColumnUsage));
    }
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class PredictorPublicationSymbol : Symbol
{
    private PredictorPublicationSymbol() { }

    public PredictorPublicationSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class PredictorAlgorithmSymbol : Symbol
{
    private PredictorAlgorithmSymbol() { }

    public PredictorAlgorithmSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class PredictorResultSaverSymbol : Symbol
{
    private PredictorResultSaverSymbol() { }

    public PredictorResultSaverSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[EntityKind(EntityKind.SystemString, EntityData.Master, IsLowPopulation = true)]
public class PredictorColumnEncodingSymbol : Symbol
{
    private PredictorColumnEncodingSymbol() { }

    public PredictorColumnEncodingSymbol(Type declaringType, string fieldName) :
        base(declaringType, fieldName)
    {
    }
}

[AutoInit]
public static class PredictorProcessAlgorithm
{
    public static ProcessAlgorithmSymbol AutoconfigureNeuralNetwork;
}

[AutoInit]
public static class TensorFlowPredictorAlgorithm
{
    public static PredictorAlgorithmSymbol NeuralNetworkGraph;
}

[AutoInit]
public static class PredictorSimpleResultSaver
{
    public static PredictorResultSaverSymbol StatisticsOnly;
    public static PredictorResultSaverSymbol Full;
}
