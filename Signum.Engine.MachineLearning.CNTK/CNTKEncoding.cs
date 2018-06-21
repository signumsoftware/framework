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
    public interface ICNTKEncoding
    {
        string ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token);
        List<PredictorCodification> ExpandColumns(ResultColumn rc);
        float EncodeValue(object value, PredictorCodification c);
        object DecodeValue(QueryToken token, List<PredictorCodification> cols, float[] outputValues);
    }

    public static class CNTKDefault
    {
        public static object GetDefaultValue(PredictorCodification c)
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
    }

    public class NoneCNTKEncoding : ICNTKEncoding
    {
        public string ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            var nn = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            if (!ReflectionTools.IsNumber(token.Token.Type) && token.Token.Type.UnNullify() != typeof(bool))
                return PredictorMessage._0IsRequiredFor1.NiceToString(DefaultColumnEncodings.OneHot.NiceToString(), token.Token.NiceTypeName);

            if (usage == PredictorColumnUsage.Output && (nn.PredictionType == PredictionType.Classification || nn.PredictionType == PredictionType.MultiClassification))
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), nn.PredictionType.NiceToString());

            return null;
        }

        public List<PredictorCodification> ExpandColumns(ResultColumn rc)
        {
            return new List<PredictorCodification> { new PredictorCodification() };
        }

        public float EncodeValue(object value, PredictorCodification c)
        {
            var valueDefault = value ?? CNTKDefault.GetDefaultValue(c);
            return Convert.ToSingle(valueDefault);
        }

        public object DecodeValue(QueryToken token, List<PredictorCodification> cols, float[] outputValues)
        {
            var c = cols.SingleEx();
            return ReflectionTools.ChangeType(outputValues[c.Index], token.Type);
        }
 
    }

    public class OneHotCNTKEncoding : ICNTKEncoding
    {
        public string ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            var nn = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            if (ReflectionTools.IsDecimalNumber(token.Token.Type))
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), predictor.Algorithm.NiceToString());

            if (usage == PredictorColumnUsage.Output && (nn.PredictionType == PredictionType.Regression || nn.PredictionType == PredictionType.MultiRegression))
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), nn.PredictionType.NiceToString());

            return null;
        }

        public List<PredictorCodification> ExpandColumns(ResultColumn rc)
        {
            return rc.Values.Cast<object>().NotNull().Distinct().Select(v => new PredictorCodification { IsValue = v }).ToList();
        }

        public float EncodeValue(object value, PredictorCodification c)
        {
            var valueDefault = value ?? CNTKDefault.GetDefaultValue(c);
            return Object.Equals(valueDefault, c.IsValue) ? 1 : 0;
        }

        public object DecodeValue(QueryToken token, List<PredictorCodification> cols, float[] outputValues)
        {
            return cols.WithMax(c => outputValues[c.Index]).IsValue;
        }
    }

    public abstract class BaseNormalizeCNTKEncoding : ICNTKEncoding
    {
        public string ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            return null;
        }

        public List<PredictorCodification> ExpandColumns(ResultColumn rc)
        {
            var values = rc.Values.Cast<object>().NotNull().Select(a => Convert.ToSingle(a)).ToList();
            return new List<PredictorCodification>
            {
                new PredictorCodification
                {
                    Average = values.Count == 0 ? 0 : values.Average(),
                    StdDev = values.Count == 0 ? 1 : values.StdDev(),
                    Min = values.Count == 0 ? 0 : values.Min(),
                    Max = values.Count == 0 ? 1 : values.Max(),
                }
            };
        }

        public abstract float EncodeValue(object value, PredictorCodification c);

        public abstract object DecodeValue(QueryToken token, List<PredictorCodification> cols, float[] outputValues);
    }

    public class NormalizeZScoreCNTKEncoding : BaseNormalizeCNTKEncoding
    {
        public override float EncodeValue(object value, PredictorCodification c)
        {
            var valueDefault = value ?? CNTKDefault.GetDefaultValue(c);
            return (Convert.ToSingle(valueDefault) - c.Average.Value) / c.StdDev.Value;
        }

        public override object DecodeValue(QueryToken token, List<PredictorCodification> cols, float[] outputValues)
        {
            var c = cols.SingleEx();
            var value = outputValues[c.Index];
            var newValue = c.Average.Value + (c.StdDev.Value * value);
            return ReflectionTools.ChangeType(newValue, token.Type);
        }
    }

    public class NormalizeMinMaxCNTKEncoding : BaseNormalizeCNTKEncoding
    {
        public override float EncodeValue(object value, PredictorCodification c)
        {
            var valueDefault = value ?? CNTKDefault.GetDefaultValue(c);
            return (Convert.ToSingle(valueDefault) - c.Min.Value) / (c.Max.Value - c.Min.Value);
        }

        public override object DecodeValue(QueryToken token, List<PredictorCodification> cols, float[] outputValues)
        {
            var c = cols.SingleEx();
            var value = outputValues[c.Index];
            var newValue = c.Min.Value + ((c.Max.Value - c.Min.Value) * value);
            return ReflectionTools.ChangeType(newValue, token.Type);
        }
    }

    class NormalizeLogCNTKEncoding : BaseNormalizeCNTKEncoding
    {
        public float MinLog = -5;

        public override float EncodeValue(object value, PredictorCodification c)
        {
            var valueDefault = value ?? CNTKDefault.GetDefaultValue(c);
            var val = Convert.ToDouble(valueDefault);
            return (val <= 0 ? MinLog : Math.Max(MinLog, (float)Math.Log(val)));
        }

        public override object DecodeValue(QueryToken token, List<PredictorCodification> cols, float[] outputValues)
        {
            var c = cols.SingleEx();
            var value = outputValues[c.Index];
            var newValue = (float)Math.Exp((double)value);
            return ReflectionTools.ChangeType(newValue, token.Type);
        }
    }
}
