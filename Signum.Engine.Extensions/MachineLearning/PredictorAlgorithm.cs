using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
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

        public PredictorState State { get; set; }
    }

    public class PredictorTrainingContext
    {
        public PredictorEntity Predictor { get; }
        public CancellationToken CancellationToken { get; }
        public bool StopTraining { get; set; }

        public string Message { get; set; }
        public decimal? Progress { get; set; }

        public List<PredictorResultColumn> Columns { get; private set; }
        public List<PredictorResultColumn> InputColumns { get; private set; }
        public List<PredictorResultColumn> OutputColumns { get; private set; }

        public MainQuery MainQuery { get; internal set; }
        public Dictionary<PredictorSubQueryEntity, SubQuery> SubQueries { get; internal set; }

        public List<PredictorProgressEntity> Progresses = new List<PredictorProgressEntity>();

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


        public void SetColums(PredictorResultColumn[] columns)
        {
            this.Columns = columns.ToList();
            this.InputColumns = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Input).ToList();
            this.OutputColumns = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Output).ToList();
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

        public void AddPredictorProgress(int i, int examples, Stopwatch sw, double lossTraining, double errorTraining, double? lossValidation, double? errorValidation)
        {
            this.Progresses.Add(new PredictorProgressEntity
            {
                Predictor = this.Predictor.ToLite(),
                Ellapsed = sw.ElapsedMilliseconds,
                MiniBatchIndex = i,
                TrainingExamples = examples,
                LossTraining = lossTraining,
                ErrorTraining = errorTraining,
                LossValidation = lossValidation,
                ErrorValidation = errorValidation,
            });
        }
    }

    public class MainQuery
    {
        public QueryRequest QueryRequest { get; internal set; }
        public ResultTable ResultTable { get; internal set; }
    }

    public class SubQuery
    {
        public PredictorSubQueryEntity MultiColumn;
        public QueryGroupRequest QueryGroupRequest;
        public ResultTable ResultTable;
        public Dictionary<Lite<Entity>, Dictionary<object[], object[]>> GroupedValues;

        public ResultColumn[] Aggregates { get; internal set; }
    }

    public abstract class PredictorAlgorithm
    {
        public virtual string ValidateColumnProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEmbedded column, PropertyInfo pi) => null;
        public abstract void Train(PredictorTrainingContext ctx);
        public abstract object[] Predict(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input);
    }

    public abstract class ClasificationPredictorAlgorithm : PredictorAlgorithm
    {
        public override object[] Predict(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input)
        {
            var singleOutput = PredictDecide(predictor, columns, input);
            return new[] { singleOutput };
        }

        public abstract object PredictDecide(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input);
        public abstract Dictionary<object, double> PredictProbabilities(PredictorEntity predictor, PredictorResultColumn[] columns, object[] input);
    }

    public static class PredictorAlgorithmValidation
    {
        public static IEnumerable<PredictorColumnEmbedded> GetAllPredictorColumnEmbeddeds(this PredictorEntity predictor)
        {
            return predictor.MainQuery.Columns.Concat(predictor.SubQueries.SelectMany(a => a.Aggregates));
        }
    }
}
