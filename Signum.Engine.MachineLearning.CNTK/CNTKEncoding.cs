using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Engine.MachineLearning.CNTK
{
    class CNTKEncoding
    {
        public static string ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEncoding encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            var nn = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            switch (encoding)
            {
                case PredictorColumnEncoding.None:
                    if (!ReflectionTools.IsNumber(token.Token.Type) && token.Token.Type.UnNullify() != typeof(bool))
                        return PredictorMessage._0IsRequiredFor1.NiceToString(PredictorColumnEncoding.OneHot.NiceToString(), token.Token.NiceTypeName);

                    if (usage == PredictorColumnUsage.Output && (nn.PredictionType == PredictionType.Classification || nn.PredictionType == PredictionType.MultiClassification))
                        return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), nn.PredictionType.NiceToString());

                    break;
                case PredictorColumnEncoding.OneHot:
                    if (ReflectionTools.IsDecimalNumber(token.Token.Type))
                        return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), predictor.Algorithm.NiceToString());

                    if (usage == PredictorColumnUsage.Output && (nn.PredictionType == PredictionType.Regression || nn.PredictionType == PredictionType.MultiRegression))
                        return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), nn.PredictionType.NiceToString());
                    break;
                case PredictorColumnEncoding.Codified:
                    return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), predictor.Algorithm.NiceToString());
            }

            return null;
        }

        public static float minLog = -5;

        public static float GetFloat(object value, PredictorCodification c)
        {
            var valueDefault = value ?? GetDefaultValue(c);

            //TODO: Codification
            switch (c.Encoding)
            {
                case PredictorColumnEncoding.None: return Convert.ToSingle(valueDefault);
                case PredictorColumnEncoding.OneHot: return Object.Equals(valueDefault, c.IsValue) ? 1 : 0;
                case PredictorColumnEncoding.Codified: throw new NotImplementedException("Codified is not usable for Neural Networks");
                case PredictorColumnEncoding.NormalizeZScore: return (Convert.ToSingle(valueDefault) - c.Average.Value) / c.StdDev.Value;
                case PredictorColumnEncoding.NormalizeMinMax: return (Convert.ToSingle(valueDefault) - c.Min.Value) / (c.Max.Value - c.Min.Value);
                case PredictorColumnEncoding.NormalizeLog:
                    {
                        var val = Convert.ToDouble(valueDefault);
                        return (val <= 0 ? minLog : Math.Max(minLog, (float)Math.Log(val)));
                    }
                default: throw new NotImplementedException("Unexpected encoding " + c.Encoding);
            }
        }

        private static object GetDefaultValue(PredictorCodification c)
        {
            switch (c.NullHandling)
            {
                case PredictorColumnNullHandling.Zero: return 0;
                case PredictorColumnNullHandling.Error: throw new Exception($"Null found on {c.Token} of {c.SubQuery?.ToString() ?? "MainQuery"}");
                case PredictorColumnNullHandling.Average: return c.Average;
                case PredictorColumnNullHandling.Min: return c.Min;
                case PredictorColumnNullHandling.Max: return c.Max;
                default: throw new NotImplementedException("Unexpected NullHanndling " + c.NullHandling);
            }
        }

        public static object FloatToValue(PredictorColumnEncoding encoding, QueryToken token, List<PredictorCodification> cols, float[] outputValues)
        {
            switch (encoding)
            {
                case PredictorColumnEncoding.None:
                    {
                        var c = cols.SingleEx();
                        return ReflectionTools.ChangeType(outputValues[c.Index], token.Type);
                    }
                case PredictorColumnEncoding.OneHot: return cols.WithMax(c => outputValues[c.Index]).IsValue;
                case PredictorColumnEncoding.NormalizeZScore:
                case PredictorColumnEncoding.NormalizeMinMax:
                case PredictorColumnEncoding.NormalizeLog:
                    {
                        var c = cols.SingleEx();
                        var value = outputValues[c.Index];
                        var newValue = c.Denormalize(value);
                        return ReflectionTools.ChangeType(newValue, token.Type);
                    }
                case PredictorColumnEncoding.Codified: throw new InvalidOperationException("Codified");
                default:
                    throw new InvalidOperationException("Unexpected encoding");
            }
        }

    }
}
