using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities;
using Signum.Entities.MachineLearning;
using Signum.Utilities.Reflection;
using Signum.Utilities;

namespace Signum.Engine.MachineLearning
{
    public class PredictorSimpleClassificationSaver : IPredictorResultSaver
    {
        public void AssertValid(PredictorEntity predictor)
        {
            AssertOnlyOutput(predictor);
        }

        public PredictorColumnEmbedded AssertOnlyOutput(PredictorEntity predictor)
        {
            var outputs = predictor.MainQuery.Columns.Where(a => a.Usage == PredictorColumnUsage.Output);
            if (outputs.Count() != 1)
                throw new InvalidOperationException($"{PredictorSimpleResultSaver.Classification} requires the predictor to have only one output (instead of {outputs.Count()})");

            var outColumn = outputs.SingleEx();
            if (!outColumn.Token.Token.IsGroupable)
                throw new InvalidOperationException($"{PredictorSimpleResultSaver.Classification} rerqires the only output to be grupable ({outColumn.Token.Token.NiceTypeName} is not)");

            return outColumn;
        }

        public void SavePredictions(PredictorTrainingContext ctx)
        {
            var p = ctx.Predictor.ToLite();
            var output = AssertOnlyOutput(ctx.Predictor);
            ctx.ReportProgress($"Deleting old {typeof(PredictSimpleClassificationEntity).NicePluralName()}");
            Database.Query<PredictSimpleClassificationEntity>().Where(a => a.Predictor == p).UnsafeDelete();

            ctx.ReportProgress($"Creating {typeof(PredictSimpleClassificationEntity).NicePluralName()}");
            var dictionary = ctx.ToPredictDictionaries();
            var toInsert = new List<PredictSimpleClassificationEntity>();
            foreach (var kvp in dictionary)
            {
                toInsert.Add(new PredictSimpleClassificationEntity
                {
                    Predictor = p,
                    Target = kvp.Key.Entity,
                    Type = ctx.Validation.Contains(kvp.Key) ? PredictionSet.Validation : PredictionSet.Training,
                    PredictedValue = PredictorPredictLogic.PredictBasic(p, kvp.Value).MainQueryValues.GetOrThrow(output)?.ToString(),
                });
            }

            ctx.ReportProgress($"Inserting {typeof(PredictSimpleClassificationEntity).NicePluralName()}");
            toInsert.BulkInsert();
        }
    }

    public class PredictorSimpleRegressionSaver : IPredictorResultSaver
    {
        public void AssertValid(PredictorEntity predictor)
        {
            AssertOnlyOutput(predictor);
        }

        public PredictorColumnEmbedded AssertOnlyOutput(PredictorEntity predictor)
        {
            var outputs = predictor.MainQuery.Columns.Where(a => a.Usage == PredictorColumnUsage.Output);
            if (outputs.Count() != 1)
                throw new InvalidOperationException($"{PredictorSimpleResultSaver.Regression} requires the predictor to have only one output (instead of {outputs.Count()})");

            var outColumn = outputs.SingleEx();
            if (!ReflectionTools.IsNumber(outColumn.Token.Token.Type))
                throw new InvalidOperationException($"{PredictorSimpleResultSaver.Regression} rerqires the only output to be numeric ({outColumn.Token.Token.NiceTypeName} is not)");

            return outColumn;
        }

        public void SavePredictions(PredictorTrainingContext ctx)
        {
            var p = ctx.Predictor.ToLite();
            var output = AssertOnlyOutput(ctx.Predictor);
            ctx.ReportProgress($"Deleting old {typeof(PredictSimpleRegressionEntity).NicePluralName()}");
            Database.Query<PredictSimpleRegressionEntity>().Where(a => a.Predictor == p).UnsafeDelete();

            ctx.ReportProgress($"Creating {typeof(PredictSimpleRegressionEntity).NicePluralName()}");
            var dictionary = ctx.ToPredictDictionaries();
            var toInsert = new List<PredictSimpleRegressionEntity>();
            foreach (var kvp in dictionary)
            {
                toInsert.Add(new PredictSimpleRegressionEntity
                {
                    Predictor = p,
                    Target = kvp.Key.Entity,
                    Type = ctx.Validation.Contains(kvp.Key) ? PredictionSet.Validation : PredictionSet.Training,
                    PredictedValue = ReflectionTools.ChangeType<decimal?>(PredictorPredictLogic.PredictBasic(p, kvp.Value).MainQueryValues.GetOrThrow(output)),
                });
            }

            ctx.ReportProgress($"Inserting {typeof(PredictSimpleRegressionEntity).NicePluralName()}");
            toInsert.BulkInsert();
        }
    }
}
