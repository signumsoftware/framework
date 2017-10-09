using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.MachineLearning;
using Accord.MachineLearning.Performance;
using Accord.MachineLearning.Bayes;
using Accord.Math.Optimization.Losses;

namespace Signum.Engine.MachineLearning.AccordNet
{
    public class NaiveBayes : PredictorAlgorithm
    {
        public override void Initialize(PredictorEntity predictor)
        {
            
        }

        public override bool TrainPredictor(PredictorEntity predictor, PredictorResultColumn[] columnDescriptions, object[][] data)
        {

           
        }

        
    }

    public class AccordNetPredictorAlgorithmBase
    {

    }
}
