using Signum.Entities;
using Signum.Entities.MachineLearning;
using System;
using System.Collections.Generic;
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

        public string Message { get; set; }
        public decimal? Progress { get; set; }

        public RowSelection AllRows { get; set; }
        public RowSelection TrainigRows { get; set; }
        public RowSelection TestRows { get; set; }

        public List<PredictorResultColumn> Columns { get; private set; }
        public List<PredictorResultColumn> InputColumns { get; private set; }
        public List<PredictorResultColumn> OutputColumns { get; private set; }
                        
        public PredictorTrainingContext(PredictorEntity predictor, CancellationToken cancellationToken)
        {
            this.Predictor = predictor;
            this.CancellationToken = cancellationToken;
        }

        public event Action<string, decimal?> OnReportProgres;

        public void ReportProgress(string message, decimal? progress)
        {
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

        public void SetRows(object[][] rows)
        {
            double testPercentage = Predictor.Settings.TestPercentage;

            List<object[]> training = new List<object[]>();
            List<object[]> test = new List<object[]>();

            Random rand = new Random();
            foreach (var row in rows)
            {
                if (rand.NextDouble() < testPercentage)
                    test.Add(row);
                else
                    training.Add(row);
            }

            this.AllRows = CreateRowSelection(rows);
            this.TrainigRows = CreateRowSelection(training.ToArray());
            this.TestRows = CreateRowSelection(test.ToArray());
        }

        RowSelection CreateRowSelection(object[][] rows)
        {
            return new RowSelection(
                rows: rows,
                inputRows: RowSelection.FilteredTable(rows, this.InputColumns),
                outputRows: RowSelection.FilteredTable(rows, this.OutputColumns)
            );
        }
    }

    public class RowSelection
    {
        public RowSelection(object[][] rows, object[][] inputRows, object[][] outputRows)
        {
            Rows = rows;
            Input = inputRows;
            Output = outputRows;
        }

        public object[][] Rows { get; private set; }
        public object[][] Input { get; private set; }
        public object[][] Output { get; private set; }

        public static object[][] FilteredTable(object[][] rows, List<PredictorResultColumn> cols)
        {
            object[][] newTable = new object[rows.Length][];
            for (int i = 0; i < rows.Length; i++)
            {
                newTable[i] = FilteredRow(rows[i], cols);
            }
            return newTable;
        }

        static object[] FilteredRow(object[] row, List<PredictorResultColumn> cols)
        {
            object[] newRow = new object[cols.Count];
            for (int j = 0; j < cols.Count; j++)
            {
                newRow[j] = row[cols[j].Index];
            }
            return newRow;
        }
    }

    public abstract class PredictorAlgorithm
    {
        public virtual string ValidatePredictor(PredictorEntity predictor) => null;
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
            return predictor.SimpleColumns.Concat(predictor.MultiColumns.SelectMany(a => a.Aggregates));
        }
    }
}
