using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
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
        public double LossTraining;
        public double EvaluationTraining;
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
            InputColumns = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Input).ToList();
            MainQueryOutputColumn = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Output && a.SubQuery == null).GroupToDictionary(a => a.PredictorColumn);
            SubQueryOutputColumn = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Output && a.SubQuery != null).AgGroupToDictionary(a => a.SubQuery, gr => new PredictorPredictSubQueryContext
            {
                SubQuery = gr.Key,
                Groups = gr.AgGroupToDictionary(a => a.Keys, gr2 => gr.GroupToDictionary(a => a.PredictorColumn), ObjectArrayComparer.Instance)
            });
        }
    }

    public class PredictorPredictSubQueryContext
    {
        public PredictorSubQueryEntity SubQuery;
        public Dictionary<object[], Dictionary<PredictorColumnEmbedded, List<PredictorCodification>>> Groups;
    }

    public class PredictorTrainingContext
    {
        public PredictorEntity Predictor { get; }
        public CancellationToken CancellationToken { get; }
        public bool StopTraining { get; set; }

        public string Message { get; set; }
        public decimal? Progress { get; set; }

        public List<PredictorCodification> Columns { get; private set; }
        public List<PredictorCodification> InputColumns { get; private set; }
        public List<PredictorCodification> OutputColumns { get; private set; }

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

            this.InputColumns = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Input).ToList();
            for (int i = 0; i < this.InputColumns.Count; i++)
            {
                this.InputColumns[i].Index = i;
            }

            this.OutputColumns = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Output).ToList();
            for (int i = 0; i < this.OutputColumns.Count; i++)
            {
                this.OutputColumns[i].Index = i;
            }
        }

        public (List<ResultRow> training, List<ResultRow> test) SplitTrainValidation()
        {
            Random r = Predictor.Settings.Seed == null ? 
                new Random() : 
                new Random(Predictor.Settings.Seed.Value);

            List<ResultRow> training = new List<ResultRow>();
            List<ResultRow> test = new List<ResultRow>();

            foreach (var item in this.MainQuery.ResultTable.Rows)
            {
                if (r.NextDouble() < Predictor.Settings.TestPercentage)
                    test.Add(item);
                else
                    training.Add(item);
            }

            return (training, test);
        }

        public void AddPredictorProgress(int i, int examples, Stopwatch sw, double lossTraining, double evaluationTraining, double? lossValidation, double? evaluationValidation)
        {
            new PredictorEpochProgressEntity
            {
                Predictor = this.Predictor.ToLite(),
                Ellapsed = sw.ElapsedMilliseconds,
                Epoch = i,
                TrainingExamples = examples,
                LossTraining = lossTraining,
                EvaluationTraining = evaluationTraining,
                LossValidation = lossValidation,
                EvaluationValidation = evaluationValidation,
            }.Save();

            this.Progresses.Add(new EpochProgress
            {
                Ellapsed = sw.ElapsedMilliseconds,
                Epoch = i,
                TrainingExamples = examples,
                LossTraining = lossTraining,
                EvaluationTraining = evaluationTraining,
                LossValidation = lossValidation,
                EvaluationValidation = evaluationValidation,
            });
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
    }

    public class SubQuery
    {
        public PredictorSubQueryEntity SubQueryEntity;
        public QueryGroupRequest QueryGroupRequest;
        public ResultTable ResultTable;
        public Dictionary<Lite<Entity>, Dictionary<object[], object[]>> GroupedValues;

        public ResultColumn[] Aggregates { get; internal set; }
    }

    public interface IPredictorAlgorithm
    {
        string ValidateColumnProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEmbedded column, PropertyInfo pi);
        void Train(PredictorTrainingContext ctx);
        void LoadModel(PredictorPredictContext predictor);
        PredictDictionary Predict(PredictorPredictContext ctx, PredictDictionary input);
    }

    public class PredictDictionary
    {
        public Dictionary<QueryToken, object> MainQueryValues { get; set; }
        public Dictionary<PredictorSubQueryEntity, PredictSubQueryDictionary> SubQueries { get; set; }
    }

    public class PredictSubQueryDictionary
    {
        public PredictorSubQueryEntity SubQuery { get; set; }
        public Dictionary<object[], Dictionary<QueryToken, object>> SubQueryGroups { get; set; }
    }
}
