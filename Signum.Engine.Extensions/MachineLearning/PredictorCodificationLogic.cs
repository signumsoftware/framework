using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Engine.MachineLearning
{
    public static class PredictorCodificationLogic
    {
        public static void CreatePredictorCodifications(PredictorTrainingContext ctx)
        {
            var isValueSize = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.IsValue)).Size.Value;
            var groupKey0Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.SplitKey0)).Size.Value;
            var groupKey1Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.SplitKey1)).Size.Value;
            var groupKey2Size = ((FieldValue)Schema.Current.Field((PredictorCodificationEntity e) => e.SplitKey2)).Size.Value;

            ctx.ReportProgress($"Saving Codifications");

            ctx.Codifications.Select(pc =>
            {
                string? ToStringValue(QueryToken? token, object? obj, int limit)
                {
                    if (token == null || obj == null)
                        return null;

                    if (obj is Lite<Entity> lite)
                        return lite.KeyLong().TryStart(limit);

                    return FilterValueConverter.ToString(obj, token.Type)?.TryStart(limit);
                }

                var valueToken = pc.Column.Token;
                
                var result = new PredictorCodificationEntity
                {
                    Predictor = ctx.Predictor.ToLite(),
                    Index = pc.Index,
                    Usage = pc.Column.Usage,
                    OriginalColumnIndex = pc.Column.PredictorColumnIndex,
                    IsValue = ToStringValue(valueToken, pc.IsValue, isValueSize),
                    Average = pc.Average,
                    StdDev = pc.StdDev,
                    Min = pc.Min,
                    Max = pc.Max,
                };

                if (pc.Column is PredictorColumnSubQuery pcsq)
                {
                    string? GetSplitpKey(int index, int limit)
                    {
                        var token = ctx.SubQueries[pcsq.SubQuery].SplitBy?.ElementAtOrDefault(index)?.Column.Token;
                        var obj = pcsq.Keys?.ElementAtOrDefault(index);
                        return ToStringValue(token, obj, limit);
                    }

                    result.SubQueryIndex = ctx.Predictor.SubQueries.IndexOf(pcsq.SubQuery);
                    result.SplitKey0 = GetSplitpKey(0, groupKey0Size);
                    result.SplitKey1 = GetSplitpKey(1, groupKey1Size);
                    result.SplitKey2 = GetSplitpKey(2, groupKey2Size);
                }

                return result;

            }).BulkInsertQueryIds(a => new { a.Index, a.Usage }, a => a.Predictor == ctx.Predictor.ToLite());
        }

        public static List<PredictorCodification> RetrievePredictorCodifications(this PredictorEntity predictor)
        {
            var list = predictor.Codifications().ToList();


            object? ParseValue(string str, QueryToken token)
            {
                return FilterValueConverter.Parse(str, token.Type, isList: false);
            }

            object? ParseKey(string? str, PredictorSubQueryColumnEmbedded key)
            {
                return FilterValueConverter.Parse(str, key.Token.Token.Type, isList: false);
            }

            object?[] GetKeys(PredictorCodificationEntity cod, List<PredictorSubQueryColumnEmbedded> keys)
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

            Dictionary<int, PredictorColumnMain> mainColumns = new Dictionary<int, PredictorColumnMain>();
            PredictorColumnMain GetPredictorColumnMain(PredictorCodificationEntity cod)
            {
                return mainColumns.GetOrCreate(cod.OriginalColumnIndex, () => new PredictorColumnMain(
                    predictorColumnIndex : cod.OriginalColumnIndex,
                    predictorColumn : predictor.MainQuery.Columns[cod.OriginalColumnIndex]
                ));
            }

            Dictionary<int, Dictionary<int, PredictorColumnSubQuery>> subColumns = new Dictionary<int, Dictionary<int, PredictorColumnSubQuery>>();
            PredictorColumnSubQuery GetPredictorColumnSubQuery(PredictorCodificationEntity cod)
            {
                return subColumns.GetOrCreate(cod.SubQueryIndex.Value).GetOrCreate(cod.OriginalColumnIndex, () => {

                    var sq = predictor.SubQueries[cod.SubQueryIndex.Value];
                    var col = sq.Columns[cod.OriginalColumnIndex];
                    if (col.Usage == PredictorSubQueryColumnUsage.SplitBy || col.Usage == PredictorSubQueryColumnUsage.ParentKey)
                        throw new InvalidOperationException("Unexpected codification usage");

                    var keys = GetKeys(cod, sq.Columns.Where(a => a.Usage == PredictorSubQueryColumnUsage.SplitBy).ToList());

                    return new PredictorColumnSubQuery(col, cod.OriginalColumnIndex, sq, keys);
                });
            }

            return (from cod in list
                    let col = cod.SubQueryIndex == null ?
                        (PredictorColumnBase)GetPredictorColumnMain(cod) :
                        (PredictorColumnBase)GetPredictorColumnSubQuery(cod)
                    select new PredictorCodification(col)
                    {
                        Index = cod.Index,
                        IsValue = cod.IsValue != null ? ParseValue(cod.IsValue, col.Token) : null,
                        Average = cod.Average,
                        StdDev = cod.StdDev,
                        Min = cod.Min,
                        Max = cod.Max,
                    })
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
