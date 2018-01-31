using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Signum.Engine.MachineLearning
{
    public class TrainingProgress
    {
        public string Message;
        public decimal? Progress;
        public bool Running;

        public PredictorState State { get; set; }

        public List<object[]> EpochProgresses { get; set; }
    }

    public class EpochProgress
    {
        public long Ellapsed;
        public int TrainingExamples;
        public int Epoch;
        public double? LossTraining;
        public double? EvaluationTraining;
        public double? LossValidation;
        public double? EvaluationValidation;

        object[] array;
        public object[] ToObjectArray()
        {
            return array ?? (array = new object[]
            {
                Ellapsed,
                TrainingExamples,
                Epoch,
                LossTraining,
                EvaluationTraining,
                LossValidation,
                EvaluationValidation,
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
                EvaluationTraining = EvaluationTraining?.CleanDouble(),
                LossValidation = LossValidation?.CleanDouble(),
                EvaluationValidation = EvaluationValidation?.CleanDouble(),
            }.Save();
        }

        
    }

    public class PredictorPredictContext
    {
        public IPredictorAlgorithm Algorithm { get; }
        
        public PredictorEntity Predictor { get; }

        public List<PredictorCodification> Columns { get; }
        public List<PredictorCodification> InputColumns { get; }
        public Dictionary<PredictorColumnEmbedded, List<PredictorCodification>> MainQueryOutputColumn { get; }
        public Dictionary<PredictorSubQueryEntity, PredictorPredictSubQueryContext> SubQueryOutputColumn { get; }

        public object Model { get; set; }

        public PredictorPredictContext(PredictorEntity predictor, IPredictorAlgorithm algorithm, List<PredictorCodification> columns)
        {
            Predictor = predictor;
            Algorithm = algorithm;
            Columns = columns;
            InputColumns = columns.Where(a => a.Usage == PredictorColumnUsage.Input).ToList();
            MainQueryOutputColumn = columns.Where(a => a.SubQuery == null && a.PredictorColumn.Usage == PredictorColumnUsage.Output).GroupToDictionary(a => a.PredictorColumn);
            SubQueryOutputColumn = columns.Where(a => a.SubQuery != null).AgGroupToDictionary(a => a.SubQuery, sqGroup => new PredictorPredictSubQueryContext
            {
                SubQuery = sqGroup.Key,
                Groups = sqGroup.AgGroupToDictionary(a => a.Keys, keysGroup => keysGroup.GroupToDictionary(a => a.PredictorSubQueryColumn), ObjectArrayComparer.Instance)
            });
        }
    }

    public class PredictorPredictSubQueryContext
    {
        public PredictorSubQueryEntity SubQuery;
        public Dictionary<object[], Dictionary<PredictorSubQueryColumnEmbedded, List<PredictorCodification>>> Groups;
    }

    public class PredictorTrainingContext
    {
        public PredictorEntity Predictor { get; set; }
        public CancellationToken CancellationToken { get; }
        public bool StopTraining { get; set; }

        public string Message { get; set; }
        public decimal? Progress { get; set; }

        public List<PredictorCodification> Columns { get; private set; }
        public List<PredictorCodification> InputColumns { get; private set; }
        public List<PredictorCodification> OutputColumns { get; private set; }

        public List<ResultRow> Validation { get; internal set; }

        public MainQuery MainQuery { get; internal set; }
        public Dictionary<PredictorSubQueryEntity, SubQuery> SubQueries { get; internal set; }

        public List<EpochProgress> Progresses = new List<EpochProgress>();

        public PredictorTrainingContext(PredictorEntity predictor, CancellationToken cancellationToken)
        {
            this.Predictor = predictor;
            this.CancellationToken = cancellationToken;
        }

        public event Action<string, decimal?> OnReportProgres;

        public void ReportProgress(string message, decimal? progress = null)
        {
            this.CancellationToken.ThrowIfCancellationRequested();

            this.Message = message;
            this.Progress = progress;
            this.OnReportProgres?.Invoke(message, progress);
        }


        public void SetColums(PredictorCodification[] columns)
        {
            this.Columns = columns.ToList();

            this.InputColumns = columns.Where(a => a.Usage == PredictorColumnUsage.Input).ToList();
            for (int i = 0; i < this.InputColumns.Count; i++)
            {
                this.InputColumns[i].Index = i;
            }

            this.OutputColumns = columns.Where(a => a.Usage == PredictorColumnUsage.Output).ToList();
            for (int i = 0; i < this.OutputColumns.Count; i++)
            {
                this.OutputColumns[i].Index = i;
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

        public List<object[]> GetProgessArray()
        {
            var list = new List<object[]>(Progresses.Count);
            for (int i = 0; i < Progresses.Count; i++) //Using a for to avoid collection modified protection
            {
                list.Add(Progresses[i].ToObjectArray());
            }
            return list;
        }
    }

    public class MainQuery
    {
        public QueryRequest QueryRequest { get; internal set; }
        public ResultTable ResultTable { get; internal set; }
        public Func<ResultRow, object[]> GetParentKey { get; internal set; }
    }

    public class SubQuery
    {
        public PredictorSubQueryEntity SubQueryEntity;
        public QueryRequest QueryGroupRequest;
        public ResultTable ResultTable;
        public Dictionary<object[], Dictionary<object[], object[]>> GroupedValues;


        public ResultColumn[] SplitBy { get; internal set; }
        public ResultColumn[] ValueColumns { get; internal set; }
        //From ColumnIndex (i.e: [3->0, 4->1)
        public Dictionary<int, int> ColumnIndexToValueIndex { get; internal set; }
    }

    public interface IPredictorAlgorithm
    {
        string ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEncoding encoding, PredictorColumnUsage usage, QueryTokenEmbedded token);
        void Train(PredictorTrainingContext ctx);
        void LoadModel(PredictorPredictContext predictor);
        PredictDictionary Predict(PredictorPredictContext ctx, PredictDictionary input);
        string[] GetAvailableDevices();
    }

    public interface IPredictorResultSaver
    {
        void AssertValid(PredictorEntity predictor);
        void SavePredictions(PredictorTrainingContext ctx);
    }

    public class PredictDictionary
    {
        public PredictDictionary(PredictorEntity predictor)
        {
            Predictor = predictor;
        }

        public Lite<Entity> Entity { get; set; } //Optional

        public PredictorEntity Predictor { get; set; }
        public Dictionary<PredictorColumnEmbedded, object> MainQueryValues { get; set; } = new Dictionary<PredictorColumnEmbedded, object>();
        public Dictionary<PredictorSubQueryEntity, PredictSubQueryDictionary> SubQueries { get; set; } = new Dictionary<PredictorSubQueryEntity, PredictSubQueryDictionary>();
    }

    public class PredictSubQueryDictionary
    {
        public PredictSubQueryDictionary(PredictorSubQueryEntity subQuery)
        {
            SubQuery = subQuery;
        }

        public PredictorSubQueryEntity SubQuery { get; set; }
        public Dictionary<object[], Dictionary<PredictorSubQueryColumnEmbedded, object>> SubQueryGroups { get; set; } = new Dictionary<object[], Dictionary<PredictorSubQueryColumnEmbedded, object>>();
    }
}
