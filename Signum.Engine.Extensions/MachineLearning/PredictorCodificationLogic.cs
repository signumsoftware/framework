using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.MachineLearning
{
    public static class PredictorCodificationLogic
    {
        public static void CreatePredictorCodifications(PredictorTrainingContext ctx)
        {
            var isValueSize = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.IsValue)).Size.Value;
            var valueSize = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.CodedValues[0])).Size.Value;
            var groupKey0Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.SplitKey0)).Size.Value;
            var groupKey1Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.SplitKey1)).Size.Value;
            var groupKey2Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.SplitKey2)).Size.Value;

            ctx.ReportProgress($"Saving Codifications");


            ctx.Columns.Select(pc =>
            {
                string ToStringValue(QueryToken token, object obj, int limit)
                {
                    if (token == null || obj == null)
                        return null;

                    if (obj is Lite<Entity> lite)
                        return lite.KeyLong().TryStart(limit);

                    return FilterValueConverter.ToString(obj, token.Type, allowSmart: false).TryStart(limit);
                }

                string GetSplitpKey(int index, int limit)
                {
                    if (pc.SubQuery == null)
                        return null;

                    var token = ctx.SubQueries[pc.SubQuery].SplitBy?.ElementAtOrDefault(index)?.Column.Token;
                    var obj = pc.Keys?.ElementAtOrDefault(index);
                    return ToStringValue(token, obj, limit);
                }

                var valueToken = pc.Token;

                return new PredictorCodificationEntity
                {
                    Predictor = ctx.Predictor.ToLite(),
                    Index = pc.Index,
                    Usage = pc.Usage,
                    SubQueryIndex = pc.SubQuery == null ? (int?)null : ctx.Predictor.SubQueries.IndexOf(pc.SubQuery),
                    OriginalColumnIndex = pc.PredictorColumnIndex,
                    SplitKey0 = GetSplitpKey(0, groupKey0Size),
                    SplitKey1 = GetSplitpKey(1, groupKey1Size),
                    SplitKey2 = GetSplitpKey(2, groupKey2Size),
                    IsValue = ToStringValue(valueToken, pc.IsValue, valueSize),
                    CodedValues = pc.CodedValues.EmptyIfNull().Select(v => ToStringValue(valueToken, v, valueSize)).ToMList(),
                    Average = pc.Average,
                    StdDev = pc.StdDev,
                    Min = pc.Min,
                    Max = pc.Max,
                };

            }).BulkInsertQueryIds(a => new { a.Index, a.Usage }, a => a.Predictor == ctx.Predictor.ToLite());
        }

        public static List<PredictorCodification> RetrievePredictorCodifications(this PredictorEntity predictor)
        {
            var list = predictor.Codifications().ToList();


            object ParseValue(string str, QueryToken token)
            {
                return FilterValueConverter.Parse(str, token.Type, isList: false, allowSmart: false);
            }

            object ParseKey(string str, PredictorSubQueryColumnEmbedded key)
            {
                return FilterValueConverter.Parse(str, key.Token.Token.Type, isList: false, allowSmart: false);
            }

            object[] GetKeys(PredictorCodificationEntity cod, List<PredictorSubQueryColumnEmbedded> keys)
            {
                switch (keys.Count)
                {
                    case 0: return new object[0];
                    case 1:
                        return new[]{
                            ParseKey(cod.SplitKey0, keys[0]),
                        };
                    case 2:
                        return new[]{
                            ParseKey(cod.SplitKey0, keys[0]),
                            ParseKey(cod.SplitKey1, keys[1]),
                        };
                    case 3:
                        return new[]{
                            ParseKey(cod.SplitKey0, keys[0]),
                            ParseKey(cod.SplitKey1, keys[1]),
                            ParseKey(cod.SplitKey2, keys[2]),
                        };
                    default:
                        throw new InvalidOperationException("Unexpected Group count");
                }
            }

            PredictorCodification MainColumnCodification(PredictorCodificationEntity cod)
            {
                var col = predictor.MainQuery.Columns[cod.OriginalColumnIndex];
                return new PredictorCodification
                {
                    PredictorColumnIndex = cod.OriginalColumnIndex,
                    PredictorColumn = col,
                    PredictorSubQueryColumn = null,
                    Index = cod.Index,
                    SubQuery = null,
                    Keys = null,
                    IsValue = col.Encoding == PredictorColumnEncoding.OneHot ? ParseValue(cod.IsValue, col.Token.Token) : null,
                    CodedValues = col.Encoding == PredictorColumnEncoding.Codified ? cod.CodedValues.Select(a => ParseValue(a, col.Token.Token)).ToArray() : null,
                    Average = cod.Average,
                    StdDev = cod.StdDev,
                    Min = cod.Min,
                    Max = cod.Max,
                };
            }

            PredictorCodification SubQueryColumnCodification(PredictorCodificationEntity cod)
            {
                var sq = predictor.SubQueries[cod.SubQueryIndex.Value];
                var col = sq.Columns[cod.OriginalColumnIndex];
                if (col.Usage == PredictorSubQueryColumnUsage.SplitBy || col.Usage == PredictorSubQueryColumnUsage.ParentKey)
                    throw new InvalidOperationException("Unexpected codification usage");

                return new PredictorCodification
                {
                    PredictorColumnIndex = cod.OriginalColumnIndex,
                    PredictorColumn = null,
                    PredictorSubQueryColumn = col ,
                    Index = cod.Index,
                    SubQuery = sq,
                    Keys = GetKeys(cod, sq.Columns.Where(a=>a.Usage == PredictorSubQueryColumnUsage.SplitBy).ToList()),
                    IsValue = col.Encoding.Value == PredictorColumnEncoding.OneHot ? ParseValue(cod.IsValue, col.Token.Token) : null,
                    CodedValues = col.Encoding.Value == PredictorColumnEncoding.Codified ? cod.CodedValues.Select(a => ParseValue(a, col.Token.Token)).ToArray() : null,
                    Average = cod.Average,
                    StdDev = cod.StdDev,
                    Min = cod.Min,
                    Max = cod.Max,
                };
            }

            return (from cod in list
                    select cod.SubQueryIndex == null ? MainColumnCodification(cod) : SubQueryColumnCodification(cod))
                    .ToList();
        }

        public static double? CleanDouble(this double val)
        {
            if (double.IsInfinity(val) || double.IsNaN(val))
                return null;

            return val;
        }
    }
}
