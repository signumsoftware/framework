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
            ctx.ReportProgress($"Saving Codifications");
            ctx.Columns.Select(a =>
            {
                string ToStringKey(QueryToken token, object obj)
                {
                    if (token == null || obj == null)
                        return null;

                    return FilterValueConverter.ToString(obj, token.Type, allowSmart: false);
                }

                string GetGroupKey(int index)
                {
                    var token = a.SubQuery?.GroupKeys.ElementAtOrDefault(index + 1)?.Token;
                    var obj = a.Keys?.ElementAtOrDefault(index);
                    return ToStringKey(token?.Token, obj);
                }

                return new PredictorCodificationEntity
                {
                    Predictor = ctx.Predictor.ToLite(),
                    Index = a.Index,
                    Usage = a.Usage,
                    SubQueryIndex = a.SubQuery == null ? (int?)null : ctx.Predictor.SubQueries.IndexOf(a.SubQuery),
                    OriginalColumnIndex = a.SubQuery == null ?
                        ctx.Predictor.MainQuery.Columns.IndexOf(a.PredictorColumn) :
                        a.SubQuery.Aggregates.IndexOf(a.PredictorColumn),
                    GroupKey0 = GetGroupKey(0),
                    GroupKey1 = GetGroupKey(1),
                    GroupKey2 = GetGroupKey(2),
                    IsValue = ToStringKey(a.PredictorColumn.Token.Token, a.IsValue),
                    CodedValues = a.Values.EmptyIfNull().Select(v => ToStringKey(a.PredictorColumn.Token.Token, v)).ToMList()
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

            object[] GetKeys(PredictorCodificationEntity cod)
            {
                var sq = predictor.SubQueries[cod.SubQueryIndex.Value];

                switch (sq.GroupKeys.Count)
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
                        throw new InvalidOperationException("Unexcpected Group count");
                }
            }

            return (from cod in list
                    let se = cod.SubQueryIndex != null ? predictor.SubQueries[cod.SubQueryIndex.Value] : null
                    let pce = se != null ? se.Aggregates[cod.OriginalColumnIndex] : predictor.MainQuery.Columns[cod.OriginalColumnIndex]
                    select new PredictorCodification
                    {
                        Index = cod.Index,
                        PredictorColumn = pce,
                        PredictorColumnIndex = cod.Index,
                        SubQuery = se,
                        Usage = pce.Usage,
                        Keys = GetKeys(cod),
                        IsValue = ParseValue(cod.IsValue, pce),
                        Values = cod.CodedValues.Select(a => ParseValue(a, pce)).ToArray()
                    }).ToList();
        }

    }
}
