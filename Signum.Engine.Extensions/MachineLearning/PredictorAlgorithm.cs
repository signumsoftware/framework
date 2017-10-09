using Signum.Entities.MachineLearning;

namespace Signum.Engine.MachineLearning
{
    public abstract class PredictorAlgorithm
    {
        public abstract void Initialize(PredictorEntity predictor);
        public abstract bool TrainPredictor(PredictorEntity predictor, PredictorResultColumn[] columnDescriptions, object[][] data);

        //public (int[][] inputs, int[][] outputs) SplitAsCategories(object[][] data, PredictorResultColumn[] columnDescriptions)
        //{
        //    int[][] inputs = new int[data.Length][];
        //    int[][] outputs = new int[data.Length][];
            
        //    columnDescriptions.

        //    for (int i = 0; i < data.Length; i++)
        //    {
                
        //    }
        //}
    }
}
