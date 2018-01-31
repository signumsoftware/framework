using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Engine.MachineLearning
{
    public static class PredictorLogicQuery
    {
        public static void RetrieveData(PredictorTrainingContext ctx)
        {
            ctx.ReportProgress($"Executing MainQuery for {ctx.Predictor}");
            QueryRequest mainQueryRequest = GetMainQueryRequest(ctx.Predictor.MainQuery);
            ResultTable mainResult = DynamicQueryManager.Current.ExecuteQuery(mainQueryRequest);

            ctx.MainQuery = new MainQuery
            {
                QueryRequest = mainQueryRequest,
                ResultTable = mainResult,
            };

            if (!mainQueryRequest.GroupResults)
            {
                ctx.MainQuery.GetParentKey = (ResultRow row) => new object[] { row.Entity };
            }
            else
            {

                var rcs = mainResult.Columns.Where(a => !(a.Column.Token is AggregateToken)).ToArray();
                ctx.MainQuery.GetParentKey = (ResultRow row) => row.GetValues(rcs);
            }
            
            ctx.SubQueries = new Dictionary<PredictorSubQueryEntity, SubQuery>();
            foreach (var sqe in ctx.Predictor.SubQueries)
            {
                ctx.ReportProgress($"Executing SubQuery {sqe}");
                QueryRequest queryGroupRequest = ToMultiColumnQuery(ctx.Predictor.MainQuery, sqe);
                ResultTable groupResult = DynamicQueryManager.Current.ExecuteQuery(queryGroupRequest);

                var pairs = groupResult.Columns.Zip(sqe.Columns, (rc, sqc) => (rc, sqc)).ToList();

                var parentKeys = pairs.Extract(a => a.sqc.Usage == PredictorSubQueryColumnUsage.ParentKey).Select(a => a.rc).ToArray();
                var splitKeys = pairs.Extract(a => a.sqc.Usage == PredictorSubQueryColumnUsage.SplitBy).Select(a => a.rc).ToArray();
                var values = pairs.Select(a=>a.rc).ToArray();

                var groupedValues = groupResult.Rows.AgGroupToDictionary(
                    row => row.GetValues(parentKeys),
                    gr => gr.ToDictionaryEx(
                        row => row.GetValues(splitKeys),
                        row => row.GetValues(values),
                        ObjectArrayComparer.Instance));

                ctx.SubQueries.Add(sqe, new SubQuery
                {
                    SubQueryEntity = sqe,
                    QueryGroupRequest = queryGroupRequest,
                    ResultTable = groupResult,
                    GroupedValues = groupedValues,
                    SplitBy = splitKeys,
                    ValueColumns = values,
                    ColumnIndexToValueIndex = values.Select((r, i) => KVP.Create(r.Index, i)).ToDictionary()
                });
            }

            ctx.ReportProgress($"Creating Columns");
            var columns = new List<PredictorCodification>();

            for (int i = 0; i < mainResult.Columns.Length; i++)
            {
                var col = ctx.Predictor.MainQuery.Columns[i];
                var list = ExpandColumns(col.Encoding, mainResult.Columns[i], () => new PredictorCodification
                {
                    PredictorColumnIndex = i,
                    PredictorColumn = col
                });
                columns.AddRange(list);
            }
            
            foreach (var sq in ctx.SubQueries.Values)
            {
                var distinctKeys = sq.GroupedValues.SelectMany(a => a.Value.Keys).Distinct(ObjectArrayComparer.Instance).ToList();

                distinctKeys.Sort(ObjectArrayComparer.Instance);

                foreach (var k in distinctKeys)
                {
                    foreach (var vc in sq.ValueColumns)
                    {
                        var col = sq.SubQueryEntity.Columns[vc.Index];
                        var list = ExpandColumns(col.Encoding.Value, vc, () => new PredictorCodification
                        {
                            PredictorColumnIndex = vc.Index,
                            PredictorSubQueryColumn = col,
                            SubQuery = sq.SubQueryEntity,
                            Keys = k
                        });
                        columns.AddRange(list);
                    }
                }
            }
            
            ctx.SetColums(columns.ToArray());
        }

        private static List<PredictorCodification> ExpandColumns(PredictorColumnEncoding encoding, ResultColumn rc, Func<PredictorCodification> factory)
        {
            switch (encoding)
            {
                case PredictorColumnEncoding.None:
                    return new List<PredictorCodification>
                    {
                        factory()
                    };
                case PredictorColumnEncoding.OneHot:
                    return rc.Values.Cast<object>().NotNull().Distinct().Select(v =>
                    {
                        var pc = factory();
                        pc.IsValue = v;
                        return pc;
                    }).ToList();
                case PredictorColumnEncoding.Codified:
                    {
                        var pc = factory();
                        pc.CodedValues = rc.Values.Cast<object>().Distinct().ToArray();
                        pc.ValuesToIndex = pc.CodedValues.Select((v, i) => KVP.Create(v, i)).ToDictionary();
                        return new List<PredictorCodification> { pc };
                    }
                case PredictorColumnEncoding.NormalizeZScore:
                case PredictorColumnEncoding.NormalizeMinMax:
                case PredictorColumnEncoding.NormalizeLog:
                    {
                        var values = rc.Values.Cast<object>().NotNull().Select(a => Convert.ToSingle(a)).ToList();
                        var pc = factory();
                        pc.Average = values.Count == 0 ? 0 : values.Average();
                        pc.StdDev = values.Count == 0 ? 1 : values.StdDev();
                        pc.Min = values.Count == 0 ? 0 : values.Min();
                        pc.Max = values.Count == 0 ? 1 : values.Max();
                        return new List<PredictorCodification> { pc };
                    };
                default:
                    throw new InvalidOperationException("Unexcpected Encoding");
            }
        }

        static QueryRequest GetMainQueryRequest(PredictorMainQueryEmbedded mq)
        {
            return new QueryRequest
            {
                QueryName = mq.Query.ToQueryName(),

                GroupResults = mq.GroupResults,

                Filters = mq.Filters.Select(f => ToFilter(f)).ToList(),

                Columns = mq.Columns.Select(c => new Column(c.Token.Token, null)).ToList(),

                Pagination = new Pagination.All(),
                Orders = Enumerable.Empty<Order>().ToList(),
            };
        }

        public static QueryRequest ToMultiColumnQuery(PredictorMainQueryEmbedded mainQuery, PredictorSubQueryEntity sq)
        {
            var parentKey = sq.Columns.SingleEx(a => a.Usage == PredictorSubQueryColumnUsage.ParentKey);

            var mainFilters = mainQuery.Query.Is(sq.Query) ?
                    mainQuery.Filters.Select(f => ToFilter(f)) :
                    mainQuery.Filters.Select(f => ToFilterAppend(f, parentKey.Token.Token));
            var additionalFilters = sq.Filters.Select(f => ToFilter(f)).ToList();

            var columns = sq.Columns.Select(c => new Column(c.Token.Token, null)).ToList();

            return new QueryRequest
            {
                GroupResults = true,
                QueryName = sq.Query.ToQueryName(),
                Filters = mainFilters.Concat(additionalFilters).ToList(),
                Columns = columns,
                Orders = new List<Order>(),
                Pagination = new Pagination.All(),
            };
        }

        internal static Filter ToFilter(QueryFilterEmbedded f)
        {
            return new Filter(f.Token.Token, f.Operation,
                FilterValueConverter.Parse(f.ValueString, f.Token.Token.Type,  f.Operation.IsList(), allowSmart: false));
        }

        static Filter ToFilterAppend(QueryFilterEmbedded f, QueryToken mainQueryKey)
        {
            QueryToken token = mainQueryKey.Append(f.Token.Token);

            return new Filter(token, f.Operation,
                FilterValueConverter.Parse(f.ValueString, token.Type, f.Operation.IsList(), allowSmart: false));
        }

        static QueryToken Append(this QueryToken baseToken, QueryToken suffix)
        {
            var steps = suffix.Follow(a => a.Parent).Reverse();
            var token = baseToken;
            foreach (var step in steps)
            {
                if (step.Key == "Entity" && step is ColumnToken)
                {
                    if (token.Type.CleanType() == step.Type.CleanType())
                        continue;
                    else
                        token = token.SubTokenInternal("[" + TypeLogic.GetCleanName(baseToken.Type.CleanType()) + "]", SubTokensOptions.CanElement);
                }

                token = token.SubTokenInternal(step.Key, SubTokensOptions.CanElement);
            }

            return token;
        }

    }

    

    public class PredictorCodification
    {
        public int Index;

        //Index of PredictorColumn in the MainQuery/SubQuery
        public int PredictorColumnIndex;

        //Only for MainQuery
        public PredictorColumnEmbedded PredictorColumn;
        
        //Only for sub queries (values inside of collections)
        public PredictorSubQueryColumnEmbedded PredictorSubQueryColumn;
        public PredictorSubQueryEntity SubQuery;
        public object[] Keys;
        
        //Only for 1-hot encoding in the column (i.e: Neuronal Networks)
        public object IsValue;

        //Serves as Codification (i.e: Bayes)
        public Dictionary<object, int> ValuesToIndex;

        public float? Average;
        public float? StdDev;

        public float? Min;
        public float? Max;

        public object[] CodedValues;

        public QueryToken Token => PredictorColumn?.Token.Token ?? PredictorSubQueryColumn.Token.Token;
        public PredictorColumnNullHandling NullHandling => PredictorColumn?.NullHandling ?? PredictorSubQueryColumn.NullHandling.Value; 
        public PredictorColumnEncoding Encoding => PredictorColumn?.Encoding ?? PredictorSubQueryColumn.Encoding.Value;
        public PredictorColumnUsage Usage => PredictorColumn?.Usage ?? PredictorSubQueryColumn.Usage.ToPredictorColumnUsage();

        public override string ToString()
        {
            return new[]
            {
                Usage.ToString(),
                Index.ToString(),
                Token.ToString(),
                Keys == null ? null : $"(Key={Keys.ToString(", ")})",
                IsValue == null ? null : $"(IsValue={IsValue})",
                CodedValues == null ? null : $"(Values={CodedValues.Length})",
            }.NotNull().ToString(" ");
        }

        public float Denormalize(float value)
        {
            if (Encoding == PredictorColumnEncoding.NormalizeZScore)
                return Average.Value + (StdDev.Value * value);

            if (Encoding == PredictorColumnEncoding.NormalizeMinMax)
                return Min.Value + ((Max.Value - Min.Value) * value);

            if (Encoding == PredictorColumnEncoding.NormalizeLog)
                return (float)Math.Exp((double)value);

            throw new InvalidOperationException();
        }
    }

    public class ObjectArrayComparer : IEqualityComparer<object[]>, IComparer<object[]>
    {
        public static readonly ObjectArrayComparer Instance = new ObjectArrayComparer();

        public int Compare(object[] x, object[] y)
        {
            if (x.Length != y.Length)
                return x.Length.CompareTo(y.Length);

            for (int i = 0; i < x.Length; i++)
            {
                var result = CompareValue(x[i], y[i]);
                if (result != 0)
                    return result;
            }
            return 0;
        }

        private int CompareValue(object v1, object v2)
        {
            if (v1 == null && v2 == null)
                return 0;

            if (v1 == null)
                return -1;

            if (v2 == null)
                return 1;

            return ((IComparable)v1).CompareTo(v2);

        }

        public bool Equals(object[] x, object[] y)
        {
            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (!object.Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(object[] array)
        {
            int hash = 17;
            foreach (var item in array)
            {
                hash = hash * 23 + ((item != null) ? item.GetHashCode() : 0);
            }
            return hash;
        }
    }
}
