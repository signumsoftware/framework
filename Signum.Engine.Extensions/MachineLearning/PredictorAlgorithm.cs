using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using System.Collections.Concurrent;

namespace Signum.Engine.MachineLearning;

public class TrainingProgress
{
    public string? Message;
    public decimal? Progress;
    public bool Running;

    public PredictorState State { get; set; }

    public List<object?[]>? EpochProgresses { get; set; }
}

public class EpochProgress
{
    public long Ellapsed;
    public int TrainingExamples;
    public int Epoch;
    public double? LossTraining;
    public double? AccuracyTraining;

    public double? LossValidation;
    public double? AccuracyValidation;

    object?[]? array;
    public object?[] ToObjectArray()
    {
        return array ?? (array = new object?[]
        {
            Ellapsed,
            TrainingExamples,
            Epoch,
            LossTraining,
            AccuracyTraining,
            LossValidation,
            AccuracyValidation,
        });
    }

    public PredictorEpochProgressEntity SaveEntity(PredictorEntity predictor)
    {
        return new PredictorEpochProgressEntity
        {
            Predictor = predictor.ToLite(),
            Ellapsed = Ellapsed,
            Epoch = Epoch,
            TrainingExamples = TrainingExamples,
            LossTraining = LossTraining?.CleanDouble(),
            AccuracyTraining = AccuracyTraining?.CleanDouble(),
            LossValidation = LossValidation?.CleanDouble(),
            AccuracyValidation = AccuracyValidation?.CleanDouble(),
        }.Save();
    }
}

public class PredictorPredictContext
{
    public IPredictorAlgorithm Algorithm { get; }

    public PredictorEntity Predictor { get; }

    public List<PredictorCodification> Codifications { get; }
    public List<PredictorCodification> InputCodifications { get; }
    public Dictionary<PredictorColumnBase, List<PredictorCodification>> InputCodificationsByColumn { get; }

    public List<PredictorCodification> OutputCodifications { get; }
    public Dictionary<PredictorColumnEmbedded, List<PredictorCodification>> MainOutputCodifications { get; }
    public Dictionary<PredictorSubQueryEntity, PredictorPredictSubQueryContext> SubQueryOutputCodifications { get; }

    public object? Model { get; set; }

    public PredictorPredictContext(PredictorEntity predictor, IPredictorAlgorithm algorithm, List<PredictorCodification> codifications)
    {
        Predictor = predictor;
        Algorithm = algorithm;
        Codifications = codifications;

        InputCodifications = codifications.Where(a => a.Column.Usage == PredictorColumnUsage.Input).ToList();
        InputCodificationsByColumn = InputCodifications.GroupToDictionary(a => a.Column);

        OutputCodifications = codifications.Where(a => a.Column.Usage == PredictorColumnUsage.Output).ToList();
        MainOutputCodifications = OutputCodifications.Where(a => a.Column is PredictorColumnMain m).GroupToDictionary(a => ((PredictorColumnMain)a.Column).PredictorColumn);
        SubQueryOutputCodifications = OutputCodifications.Where(a => a.Column is PredictorColumnSubQuery).AgGroupToDictionary(a => ((PredictorColumnSubQuery)a.Column).SubQuery, sqGroup =>
        new PredictorPredictSubQueryContext(sqGroup.Key,
            sqGroup.AgGroupToDictionary(a => ((PredictorColumnSubQuery)a.Column).Keys!,
                keysGroup => keysGroup.GroupToDictionary(a => ((PredictorColumnSubQuery)a.Column).PredictorSubQueryColumn!), ObjectArrayComparer.Instance)
        ));
    }
}

public class PredictorPredictSubQueryContext
{
    public PredictorSubQueryEntity SubQuery;
    public Dictionary<object?[], Dictionary<PredictorSubQueryColumnEmbedded, List<PredictorCodification>>> Groups;

    public PredictorPredictSubQueryContext(PredictorSubQueryEntity subQuery, Dictionary<object?[], Dictionary<PredictorSubQueryColumnEmbedded, List<PredictorCodification>>> groups)
    {
        SubQuery = subQuery;
        Groups = groups;
    }
}

public class PredictorTrainingContext
{
    public PredictorEntity Predictor { get; set; }
    public CancellationToken CancellationToken { get; }
    public bool StopTraining { get; set; }

    public string? Message { get; set; }
    public decimal? Progress { get; set; }

    public List<PredictorCodification> Codifications { get; private set; } = null!;

    public List<PredictorCodification> InputCodifications { get; private set; } = null!;
    public Dictionary<PredictorColumnBase, List<PredictorCodification>> InputCodificationsByColumn { get; private set; } = null!;

    public List<PredictorCodification> OutputCodifications { get; private set; } = null!;
    public Dictionary<PredictorColumnBase, List<PredictorCodification>> OutputCodificationsByColumn { get; private set; } = null!;

    public List<ResultRow> Validation { get; internal set; } = null!;

    public MainQuery MainQuery { get; internal set; } = null!;
    public Dictionary<PredictorSubQueryEntity, SubQuery> SubQueries { get; internal set; } = null!;

    public ConcurrentQueue<EpochProgress> Progresses = new ConcurrentQueue<EpochProgress>();

    public PredictorTrainingContext(PredictorEntity predictor, CancellationToken cancellationToken)
    {
        this.Predictor = predictor;
        this.CancellationToken = cancellationToken;
    }

    public event Action<string, decimal?>? OnReportProgres;

    public void ReportProgress(string message, decimal? progress = null)
    {
        this.CancellationToken.ThrowIfCancellationRequested();

        this.Message = message;
        this.Progress = progress;
        this.OnReportProgres?.Invoke(message, progress);
    }


    public void SetCodifications(PredictorCodification[] codifications)
    {
        this.Codifications = codifications.ToList();

        this.InputCodifications = codifications.Where(a => a.Column.Usage == PredictorColumnUsage.Input).ToList();
        this.InputCodificationsByColumn = this.InputCodifications.GroupToDictionary(a => a.Column);
        for (int i = 0; i < this.InputCodifications.Count; i++)
        {
            this.InputCodifications[i].Index = i;
        }


        this.OutputCodifications = codifications.Where(a => a.Column.Usage == PredictorColumnUsage.Output).ToList();
        this.OutputCodificationsByColumn = this.OutputCodifications.GroupToDictionary(a => a.Column);
        for (int i = 0; i < this.OutputCodifications.Count; i++)
        {
            this.OutputCodifications[i].Index = i;
        }
    }

    public (List<ResultRow> training, List<ResultRow> validation) SplitTrainValidation(Random r)
    {
        List<ResultRow> training = new List<ResultRow>();
        List<ResultRow> validation = new List<ResultRow>();

        foreach (var item in this.MainQuery.ResultTable.Rows)
        {
            if (r.NextDouble() < Predictor.Settings.TestPercentage)
                validation.Add(item);
            else
                training.Add(item);
        }

        this.Validation = validation;

        return (training, validation);
    }

    public List<object?[]> GetProgessArray()
    {
        return Progresses.Select(a => a.ToObjectArray()).ToList();
    }
}

public class MainQuery
{
    public required QueryRequest QueryRequest { get; set; }
    public required ResultTable ResultTable { get; set; }
    public required Func<ResultRow, object?[]> GetParentKey { get; set; }
}

public class SubQuery
{
    public required PredictorSubQueryEntity SubQueryEntity;
    public required QueryRequest QueryGroupRequest;
    public required ResultTable ResultTable;
    public required Dictionary<object?[], Dictionary<object?[], object?[]>> GroupedValues;


    public required ResultColumn[] SplitBy { get; set; }
    public required ResultColumn[] ValueColumns { get; set; }
    //From ColumnIndex (i.e: [3->0, 4->1)
    public required Dictionary<int, int> ColumnIndexToValueIndex { get; set; }
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

public interface IPredictorAlgorithm
{
    string? ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity? subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token);
    void Train(PredictorTrainingContext ctx);
    void LoadModel(PredictorPredictContext predictor);
    PredictDictionary Predict(PredictorPredictContext ctx, PredictDictionary input);
    List<PredictDictionary> PredictMultiple(PredictorPredictContext ctx, List<PredictDictionary> inputs);
    List<PredictorCodification> GenerateCodifications(PredictorColumnEncodingSymbol encoding, ResultColumn resultColumn, PredictorColumnBase column);
    IEnumerable<PredictorColumnEncodingSymbol> GetRegisteredEncodingSymbols();
}

public interface IPredictorResultSaver
{
    void AssertValid(PredictorEntity predictor);
    void SavePredictions(PredictorTrainingContext ctx);
}

public class PredictDictionary
{
    public PredictDictionary(PredictorEntity predictor, PredictionOptions? options = null, Lite<Entity>? entity = null)
    {
        Predictor = predictor;
        Options = options;
        Entity = entity;
    }

    public Lite<Entity>? Entity { get; set; }

    public PredictionOptions? Options { get; set; }

    public PredictorEntity Predictor { get; set; }
    public Dictionary<PredictorColumnEmbedded, object?> MainQueryValues { get; set; } = new Dictionary<PredictorColumnEmbedded, object?>();
    public Dictionary<PredictorSubQueryEntity, PredictSubQueryDictionary> SubQueries { get; set; } = new Dictionary<PredictorSubQueryEntity, PredictSubQueryDictionary>();

    public PredictDictionary Clone()
    {
        var result = new PredictDictionary(Predictor, this.Options, this.Entity);
        result.MainQueryValues.AddRange(MainQueryValues.ToDictionaryEx());
        result.SubQueries.AddRange(SubQueries, kvp => kvp.Key, kvp => kvp.Value.Clone());
        return result;
    }
}

public class PredictionOptions
{
    public int? AlternativeCount;
    public List<PredictorCodification>? FilteredCodifications;
}

public class AlternativePrediction
{
    public AlternativePrediction(float probability, object? value)
    {
        Probability = probability;
        Value = value;
    }

    public float Probability { get; set; }
    public object? Value { get; set; }
}

public class PredictSubQueryDictionary
{
    public PredictSubQueryDictionary(PredictorSubQueryEntity subQuery)
    {
        SubQuery = subQuery;
    }

    public PredictorSubQueryEntity SubQuery { get; set; }
    public Dictionary<object?[], Dictionary<PredictorSubQueryColumnEmbedded, object?>> SubQueryGroups { get; set; } = new Dictionary<object?[], Dictionary<PredictorSubQueryColumnEmbedded, object?>>();

    public PredictSubQueryDictionary Clone()
    {
        var result = new PredictSubQueryDictionary(SubQuery);
        result.SubQueryGroups.AddRange(this.SubQueryGroups, kvp => kvp.Key, kvp => kvp.Value.ToDictionary());
        return result;
    }
}
