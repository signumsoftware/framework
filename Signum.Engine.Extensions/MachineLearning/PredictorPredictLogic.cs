using Signum.Entities;
using Signum.Entities.MachineLearning;
using Signum.Utilities.DataStructures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.MachineLearning
{
    public class PredictorPredictLogic
    {
        public static RecentDictionary<Lite<PredictorEntity>, PredictorPredictContext> TrainedPredictorCache = new RecentDictionary<Lite<PredictorEntity>, PredictorPredictContext>(50);

        public static PredictorPredictContext GetPredictContext(Lite<PredictorEntity> predictor)
        {
            return TrainedPredictorCache.GetOrCreate(predictor, () =>
            {
                using (ExecutionMode.Global())
                using (var t = Transaction.ForceNew())
                {
                    var p = predictor.Retrieve();
                    var codifications = p.RetrievePredictorCodifications();
                    var ppc = new PredictorPredictContext(p, Algorithms.GetOrThrow(p.Algorithm), codifications);

                    return t.Commit(ppc);
                }
            });
        }

        public static PredictorDictionary GetInputs(Lite<PredictorEntity> predictor)
        {
            var ctx = GetPredictContext(predictor);

            

        }

        public static PredictorDictionary GetOutputs(Lite<PredictorEntity> predictor)
        {

        }

        public static PredictorDictionary Predict(Lite<PredictorEntity> predictor, PredictorDictionary input)
        {

        } 

        public static PredictorDictionary GetDefaultIDictionary(Lite<PredictorEntity> predictor)
        {

        }
    }
}
