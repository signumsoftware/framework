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
            var groupKey0Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.GroupKey0)).Size.Value;
            var groupKey1Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.GroupKey1)).Size.Value;
            var groupKey2Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.GroupKey2)).Size.Value;

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

                string GetGroupKey(int index, int limit)
                {
                    var token = pc.SubQuery?.GroupKeys.ElementAtOrDefault(index + 1)?.Token;
                    var obj = pc.Keys?.ElementAtOrDefault(index);
                    return ToStringValue(token?.Token, obj, limit);
                }

                return new PredictorCodificationEntity
                {
                    Predictor = ctx.Predictor.ToLite(),
                    Index = pc.Index,
                    Usage = pc.PredictorColumn.Usage,
                    SubQueryIndex = pc.SubQuery == null ? (int?)null : ctx.Predictor.SubQueries.IndexOf(pc.SubQuery),
                    OriginalColumnIndex = pc.SubQuery == null ?
                        ctx.Predictor.MainQuery.Columns.IndexOf(pc.PredictorColumn) :
                        pc.SubQuery.Aggregates.IndexOf(pc.PredictorColumn),
                    GroupKey0 = GetGroupKey(0, groupKey0Size),
                    GroupKey1 = GetGroupKey(1, groupKey1Size),
                    GroupKey2 = GetGroupKey(2, groupKey2Size),
                    IsValue = ToStringValue(pc.PredictorColumn.Token.Token, pc.IsValue, valueSize),
                    CodedValues = pc.CodedValues.EmptyIfNull().Select(v => ToStringValue(pc.PredictorColumn.Token.Token, v, valueSize)).ToMList(),
                    StdDev = pc.StdDev,
                    Mean = pc.Mean,
                };

            }).BulkInsertQueryIds(a => new { a.Index, a.Usage }, a => a.Predictor == ctx.Predictor.ToLite());
        }

        public static List<PredictorCodification> RetrievePredictorCodifications(this PredictorEntity predictor)
        {
            var list = predictor.Codifications().ToList();


            object ParseValue(string str, PredictorColumnEmbedded pce)
            {
                return FilterValueConverter.Parse(str, pce.Token.Token.Type, isList: false, allowSmart: false);
            }

            object ParseKey(string str, PredictorGroupKeyEmbedded key)
            {
                return FilterValueConverter.Parse(str, key.Token.Token.Type, isList: false, allowSmart: false);
            }

            object[] GetKeys(PredictorCodificationEntity cod, PredictorSubQueryEntity sq)
            {
                switch (sq.GroupKeys.Count - 1)
                {
                    case 0: return new object[0];
                    case 1:
                        return new[]{
                            ParseKey(cod.GroupKey0, sq.GroupKeys[0]),
                        };
                    case 2:
                        return new[]{
                            ParseKey(cod.GroupKey0, sq.GroupKeys[0]),
                            ParseKey(cod.GroupKey1, sq.GroupKeys[1]),
                        };
                    case 3:
                        return new[]{
                            ParseKey(cod.GroupKey0, sq.GroupKeys[0]),
                            ParseKey(cod.GroupKey1, sq.GroupKeys[1]),
                            ParseKey(cod.GroupKey2, sq.GroupKeys[2]),
                        };
                    default:
                        throw new InvalidOperationException("Unexpected Group count");
                }
            }

            return (from cod in list
                    let sq = cod.SubQueryIndex != null ? predictor.SubQueries[cod.SubQueryIndex.Value] : null
                    let pce = sq != null ? sq.Aggregates[cod.OriginalColumnIndex] : predictor.MainQuery.Columns[cod.OriginalColumnIndex]
                    select new PredictorCodification(pce, cod.OriginalColumnIndex)
                    {
                        Index = cod.Index,
                        SubQuery = sq,
                        Keys = sq == null ? null : GetKeys(cod, sq),
                        IsValue = pce.Encoding == PredictorColumnEncoding.OneHot ? ParseValue(cod.IsValue, pce) : null,
                        CodedValues = pce.Encoding == PredictorColumnEncoding.Codified ? cod.CodedValues.Select(a => ParseValue(a, pce)).ToArray() : null,
                        Mean = cod.Mean,
                        StdDev = cod.StdDev,
                    }).ToList();
        }

        public static double? CleanDouble(this double val)
        {
            if (double.IsInfinity(val) || double.IsNaN(val))
                return null;

            return val;
        }
    }
}
