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
    public class PredictorSimpleSaver : IPredictorResultSaver
    {
        public void AssertValid(PredictorEntity predictor)
        {
            AssertOnlyOutput(predictor);
        }

        public PredictorColumnEmbedded AssertOnlyOutput(PredictorEntity predictor)
        {
            var outputs = predictor.MainQuery.Columns.Where(a => a.Usage == PredictorColumnUsage.Output);
            if (outputs.Count() != 1)
                throw new InvalidOperationException($"{PredictorSimpleResultSaver.OneOutput} requires the predictor to have only one output (instead of {outputs.Count()})");

            return outputs.SingleEx();
        }

        public void SavePredictions(PredictorTrainingContext ctx)
        {
            var p = ctx.Predictor.ToLite();
            var outputColumn = AssertOnlyOutput(ctx.Predictor);
            var isCategorical = outputColumn.Encoding == PredictorColumnEncoding.OneHot ||outputColumn.Encoding == PredictorColumnEncoding.Codified;

            ctx.ReportProgress($"Deleting old {typeof(PredictSimpleResultEntity).NicePluralName()}");
            Database.Query<PredictSimpleResultEntity>().Where(a => a.Predictor == p).UnsafeDelete();

            ctx.ReportProgress($"Creating {typeof(PredictSimpleResultEntity).NicePluralName()}");
            var dictionary = ctx.ToPredictDictionaries();
            var toInsert = new List<PredictSimpleResultEntity>();

            var pc = PredictorPredictLogic.CreatePredictContext(ctx.Predictor);
            int i = 0;
            foreach (var kvp in dictionary)
            {
                if (i++ % 100 == 0)
                    ctx.ReportProgress($"Creating {typeof(PredictSimpleResultEntity).NicePluralName()}", i / (decimal)dictionary.Count);

                var output = pc.Algorithm.Predict(pc, kvp.Value);

                var value = output.MainQueryValues.GetOrThrow(outputColumn);

                toInsert.Add(new PredictSimpleResultEntity
                {
                    Predictor = p,
                    Target = ctx.Predictor.MainQuery.GroupResults? null : kvp.Key.Entity,
                    Type = ctx.Validation.Contains(kvp.Key) ? PredictionSet.Validation : PredictionSet.Training,
                    PredictedValue = isCategorical ? null : ReflectionTools.ChangeType<double?>(value),
                    PredictedCategory = isCategorical ? value?.ToString() : null,
                });
            }
            
            var groups = toInsert.GroupsOf(1000).ToList();
            foreach (var iter in groups.Iterate())
            {
                ctx.ReportProgress($"Inserting {typeof(PredictSimpleResultEntity).NicePluralName()}", (iter.Position) / (decimal)groups.Count);
                iter.Value.BulkInsert();
            }
        }
    }
}
