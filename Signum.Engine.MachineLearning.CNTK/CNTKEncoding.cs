using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using Signum.Utilities.Reflection;

namespace Signum.Engine.MachineLearning.CNTK
{
    public interface ICNTKEncoding
    {
        string? ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity? subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token);
        List<PredictorCodification> GenerateCodifications(ResultColumn rc, PredictorColumnBase column);
        object? DecodeValue(PredictorColumnBase column, List<PredictorCodification> codifications, float[] outputValues, PredictionOptions options);
        void EncodeValue(object? value, PredictorColumnBase column, List<PredictorCodification> codifications, float[] inputValues, int offset); //Span<T>
    }

    public class NoneCNTKEncoding : ICNTKEncoding
    {
        public string? ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity? subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            var nn = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            if (!ReflectionTools.IsNumber(token.Token.Type) && token.Token.Type.UnNullify() != typeof(bool))
                return PredictorMessage._0IsRequiredFor1.NiceToString(DefaultColumnEncodings.OneHot.NiceToString(), token.Token.NiceTypeName);

            if (usage == PredictorColumnUsage.Output && (nn.PredictionType == PredictionType.Classification || nn.PredictionType == PredictionType.MultiClassification))
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), nn.PredictionType.NiceToString());

            return null;
        }

        public List<PredictorCodification> GenerateCodifications(ResultColumn rc, PredictorColumnBase column)
        {
            return new List<PredictorCodification> { new PredictorCodification(column) };
        }

        public void EncodeValue(object? value, PredictorColumnBase column, List<PredictorCodification> codifications, float[] inputValues, int offset)
        {
            var c = codifications.SingleEx();
            inputValues[offset + c.Index]  = Convert.ToSingle(value);
        }

        public object? DecodeValue(PredictorColumnBase column, List<PredictorCodification> codifications, float[] outputValues, PredictionOptions options)
        {
            var c = codifications.SingleEx();
            return ReflectionTools.ChangeType(outputValues[c.Index], column.Token.Type);
        }
    }

    public class OneHotCNTKEncoding : ICNTKEncoding
    {
        public string? ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity? subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            var nn = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            if (ReflectionTools.IsDecimalNumber(token.Token.Type))
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), predictor.Algorithm.NiceToString());

            if (usage == PredictorColumnUsage.Output && (nn.PredictionType == PredictionType.Regression || nn.PredictionType == PredictionType.MultiRegression))
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), nn.PredictionType.NiceToString());

            return null;
        }

        public List<PredictorCodification> GenerateCodifications(ResultColumn rc, PredictorColumnBase column)
        {
            return rc.Values.Cast<object>().NotNull().Distinct().Select(v => new PredictorCodification(column) { IsValue = v }).ToList();
        }

        public void EncodeValue(object? value, PredictorColumnBase column, List<PredictorCodification> codifications, float[] inputValues, int offset)
        {
            var dic = GetCodificationDictionary(column, codifications);
            if (value != null && dic.TryGetValue(value, out int index))
                inputValues[offset + index] = 1;
        }

        public virtual Dictionary<object, int> GetCodificationDictionary(PredictorColumnBase column, List<PredictorCodification> codifications)
        {
            if (column.ColumnModel != null)
                return (Dictionary<object, int>)column.ColumnModel;

            column.ColumnModel = codifications.ToDictionary(a => a.IsValue, a => a.Index);

            return (Dictionary<object, int>)column.ColumnModel;
        }

        public object? DecodeValue(PredictorColumnBase column, List<PredictorCodification> codifications, float[] outputValues, PredictionOptions options)
        {
            var cods = options?.FilteredCodifications ?? codifications;

            if (options?.AlternativeCount == null)
            {
                var max = float.MinValue;
                PredictorCodification? best = null;
                foreach (var c in cods)
                {
                    if (max < outputValues[c.Index])
                    {
                        best = c;
                        max = outputValues[c.Index];
                    }
                }
                return best?.IsValue;
            }
            else
            {
                //Softmax
                var sum = cods.Sum(cod => Math.Exp(outputValues[cod.Index]));

                return cods.OrderByDescending(c => outputValues[c.Index]).Take(options.AlternativeCount.Value).Select(c => new AlternativePrediction(
                    probability: (float)(Math.Exp(outputValues[c.Index]) / sum),
                    value: c.IsValue
                )).ToList();
            }
        }
    }

    public abstract class BaseNormalizeCNTKEncoding : ICNTKEncoding
    {
        public string? ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity? subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            if (!ReflectionTools.IsDecimalNumber(token.Token.Type) && !ReflectionTools.IsNumber(token.Token.Type))
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), predictor.Algorithm.NiceToString());

            return null;
        }

        public List<PredictorCodification> GenerateCodifications(ResultColumn rc, PredictorColumnBase column)
        {
            var values = rc.Values.Cast<object>().NotNull().Select(a => Convert.ToSingle(a)).ToList();
            return new List<PredictorCodification>
            {
                new PredictorCodification(column)
                {
                    Average = values.Count == 0 ? 0 : values.Average(),
                    StdDev = values.Count == 0 ? 1 : values.StdDev(),
                    Min = values.Count == 0 ? 0 : values.Min(),
                    Max = values.Count == 0 ? 1 : values.Max(),
                }
            };
        }

        public abstract float EncodeSingleValue(object? valueDefault, PredictorCodification c);
        public void EncodeValue(object? value, PredictorColumnBase column, List<PredictorCodification> codifications, float[] inputValues, int offset)
        {
            PredictorCodification c = codifications.SingleEx();
            inputValues[offset + c.Index] = EncodeSingleValue(value, c);
        }

        public abstract object? DecodeSingleValue(float value, PredictorCodification c);
        public object? DecodeValue(PredictorColumnBase column, List<PredictorCodification> codifications, float[] outputValues, PredictionOptions options)
        {
            PredictorCodification cod = codifications.SingleEx();
            float value = outputValues[cod.Index];
            return DecodeSingleValue(value, cod);
        }
    }

    public class NormalizeZScoreCNTKEncoding : BaseNormalizeCNTKEncoding
    {
        public override float EncodeSingleValue(object? value, PredictorCodification c)
        {
            return (Convert.ToSingle(value) - c.Average!.Value) / c.StdDev!.Value;
        }

        public override object? DecodeSingleValue(float value, PredictorCodification c)
        {
            var newValue = c.Average! + (c.StdDev! * value);
            return ReflectionTools.ChangeType(newValue, c.Column.Token.Type);
        }
    }

    public class NormalizeMinMaxCNTKEncoding : BaseNormalizeCNTKEncoding
    {
        public override float EncodeSingleValue(object? value, PredictorCodification c)
        {
            return (Convert.ToSingle(value) - c.Min!.Value) / (c.Max!.Value - c.Min!.Value);
        }

        public override object? DecodeSingleValue(float value, PredictorCodification c)
        {
            var newValue = c.Min!.Value + ((c.Max!.Value - c.Min.Value) * value);
            return ReflectionTools.ChangeType(newValue, c.Column.Token.Type);
        }
    }

    class NormalizeLogCNTKEncoding : BaseNormalizeCNTKEncoding
    {
        public float MinLog = -5;

        public override float EncodeSingleValue(object? value, PredictorCodification c)
        {
            var dValue = Convert.ToDouble(value);
            return (dValue <= 0 ? MinLog : Math.Max(MinLog, (float)Math.Log(dValue)));
        }

        public override object? DecodeSingleValue(float value, PredictorCodification c)
        {
            var newValue = (float)Math.Exp((double)value);
            return ReflectionTools.ChangeType(newValue, c.Column.Token.Type);
        }
    }

    public class SplitWordsCNTKEncoding : ICNTKEncoding
    {
        public string? ValidateEncodingProperty(PredictorEntity predictor, PredictorSubQueryEntity? subQuery, PredictorColumnEncodingSymbol encoding, PredictorColumnUsage usage, QueryTokenEmbedded token)
        {
            var nn = (NeuralNetworkSettingsEntity)predictor.AlgorithmSettings;
            if (token.Token.Type != typeof(string))
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), predictor.Algorithm.NiceToString());

            if (usage == PredictorColumnUsage.Output)
                return PredictorMessage._0NotSuportedFor1.NiceToString(encoding.NiceToString(), usage.NiceToString());

            return null;
        }

        public virtual List<PredictorCodification> GenerateCodifications(ResultColumn rc, PredictorColumnBase column)
        {
            var distinctStrings = rc.Values.Cast<string>().NotNull().Distinct();

            var allWords = distinctStrings.SelectMany(str => SplitWords(str)).Distinct(StringComparer.CurrentCultureIgnoreCase).ToList();

            return allWords.Select(v => new PredictorCodification(column) { IsValue = v }).ToList();
        }

        public char[] separators = new[] { '.', ',', ' ', '-', '(', ')', '/', '\\', ';', ':' };

        public virtual string[] SplitWords(string str)
        {
            return str.SplitNoEmpty(separators);
        }

        public void EncodeValue(object? value, PredictorColumnBase column, List<PredictorCodification> codifications, float[] inputValues, int offset)
        {
            var words = SplitWords((string)value! ?? "");

            var dic = GetCodificationDictionary(column, codifications);

            foreach (var w in words)
            {
                if (dic.TryGetValue(w, out int index))
                    inputValues[index + offset] = 1;
            }
        }

        public int MaxDecodedWords = 5;
        public float MinDecodedWordValue = 0.1f;

        public object? DecodeValue(PredictorColumnBase column, List<PredictorCodification> codifications, float[] outputValues, PredictionOptions options)
        {
            var bestCodifications = codifications
                .Where(c => outputValues[c.Index] > MinDecodedWordValue)
                .OrderByDescending(a => outputValues[a.Index])
                .Take(MaxDecodedWords)
                .ToList();

            return bestCodifications.ToString(a => a.IsValue!.ToString(), ", ");
        }

        public virtual Dictionary<string, int> GetCodificationDictionary(PredictorColumnBase column, List<PredictorCodification> codifications)
        {
            if (column.ColumnModel != null)
                return (Dictionary<string, int>)column.ColumnModel;

            column.ColumnModel = codifications.ToDictionaryEx(a => (string)a.IsValue!, a => a.Index, StringComparer.CurrentCultureIgnoreCase);

            return (Dictionary<string, int>)column.ColumnModel;
        }
    }
}
