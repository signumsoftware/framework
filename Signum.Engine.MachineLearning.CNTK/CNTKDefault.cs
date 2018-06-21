using System;
using Signum.Entities.MachineLearning;
using Signum.Utilities;

namespace Signum.Engine.MachineLearning.CNTK
{
    public static class CNTKDefault
    {
        public static object GetDefaultValue(PredictorCodification c)
        {
            switch (c.Column.NullHandling)
            {
                case PredictorColumnNullHandling.Zero: return 0;
                case PredictorColumnNullHandling.Error: throw new Exception($"Null found on {c.Column.Token} of {(c.Column is PredictorColumnSubQuery pcsq ? pcsq.SubQuery.ToString() : "MainQuery")}");
                case PredictorColumnNullHandling.Average: return c.Average;
                case PredictorColumnNullHandling.Min: return c.Min;
                case PredictorColumnNullHandling.Max: return c.Max;
                default: throw new UnexpectedValueException(c.Column.NullHandling);
            }
        }
    }
}
