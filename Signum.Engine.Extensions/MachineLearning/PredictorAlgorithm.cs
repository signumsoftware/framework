using Signum.Entities;
using Signum.Entities.MachineLearning;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Signum.Engine.MachineLearning
{
    public class TrainingState
    {
        public string Message;
        public decimal Progress;
    }

    public class PredictorTrainingContext
    {
        public PredictorEntity Predictor { get; }
        public CancellationToken CancellationToken { get; }
        public string Message { get; set; }
        public decimal? Progress { get; set; }

        public object[][] Rows { get; private set; }
        public object[][] Input { get; private set; }
        public object[][] Output { get; private set; }
        public PredictorResultColumn[] Columns { get; private set; }
        public PredictorResultColumn[] InputColumns { get; private set; }
        public PredictorResultColumn[] OutputColumns { get; private set; }
                        
        public PredictorTrainingContext(PredictorEntity predictor, CancellationToken cancellationToken)
        {
            this.Predictor = predictor;
            this.CancellationToken = cancellationToken;
        }

        public void ReportProgress(string message, decimal? progress)
        {
            this.Message = message;
            this.Progress = progress;
        }


        public void SetColums(PredictorResultColumn[] columns)
        {
            this.Columns = columns.ToArray();
            this.InputColumns = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Input).ToArray();
            this.OutputColumns = columns.Where(a => a.PredictorColumn.Usage == PredictorColumnUsage.Output).ToArray();
        }

        public void SetRows(object[][] rows)
        {
            this.Rows = rows;
            this.Input = FilteredTable(rows, InputColumns);
            this.Output = FilteredTable(rows, OutputColumns);
        }

        static object[][] FilteredTable(object[][] rows, PredictorResultColumn[] cols)
        {
            object[][] newTable = new object[rows.Length][];
            for (int i = 0; i < rows.Length; i++)
            {
                newTable[i] = FilteredRow(rows[i], cols);
            }
            return newTable;
        }

        static object[] FilteredRow(object[] row, PredictorResultColumn[] cols)
        {
            object[] newRow = new object[cols.Length];
            for (int j = 0; j < cols.Length; j++)
            {
                newRow[j] = row[cols[j].Index];
            }
            return newRow;
        }
    }

    public abstract class PredictorAlgorithm
    {
        public virtual string ValidatePredictor(PredictorEntity predictor) => null;
        public abstract void Train(PredictorEntity predictor, PredictorResultColumn[] columns, object[][] input, object[][] output, TrainingState state);
        public abstract EvaluateResult Evaluate(PredictorEntity predictor, PredictorResultColumn[] columns, object[][] input, object[][] output);
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

    public class EvaluateResult
    {
        public EvaluateStats Training;
        public EvaluateStats Validation;
    }

    public class EvaluateStats
    {
        public double Mean;
        public double Variance;
        public double StandartDeviation; 
    }

    public static class PredictorAlgorithmValidation
    {
        public static IEnumerable<PredictorColumnEmbedded> GetAllPredictorColumnEmbeddeds(this PredictorEntity predictor)
        {
            return predictor.SimpleColumns.Concat(predictor.MultiColumns.SelectMany(a => a.Aggregates));
        }
    }
}
