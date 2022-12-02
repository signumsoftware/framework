using Signum.Entities.MachineLearning;
using Signum.Entities.UserQueries;

namespace Signum.Engine.MachineLearning;

public static class PredictorLogicQuery
{
    public static void RetrieveData(PredictorTrainingContext ctx)
    {
        using (HeavyProfiler.Log("RetrieveData"))
        {
            ctx.ReportProgress($"Executing MainQuery for {ctx.Predictor}");
            QueryRequest mainQueryRequest = GetMainQueryRequest(ctx.Predictor.MainQuery);
            ResultTable mainResult = QueryLogic.Queries.ExecuteQuery(mainQueryRequest);

            ctx.MainQuery = new MainQuery
            {
                QueryRequest = mainQueryRequest,
                ResultTable = mainResult,
                GetParentKey = null!,
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

            var algorithm = PredictorLogic.Algorithms.GetOrThrow(ctx.Predictor.Algorithm);

            ctx.SubQueries = new Dictionary<PredictorSubQueryEntity, SubQuery>();
            foreach (var sqe in ctx.Predictor.SubQueries)
            {
                ctx.ReportProgress($"Executing SubQuery {sqe}");
                QueryRequest queryGroupRequest = ToMultiColumnQuery(ctx.Predictor.MainQuery, sqe);
                ResultTable groupResult = QueryLogic.Queries.ExecuteQuery(queryGroupRequest);

                var pairs = groupResult.Columns.Zip(sqe.Columns, (rc, sqc) => (rc, sqc)).ToList();

                var parentKeys = pairs.Extract(a => a.sqc.Usage == PredictorSubQueryColumnUsage.ParentKey).Select(a => a.rc).ToArray();
                var splitKeys = pairs.Extract(a => a.sqc.Usage == PredictorSubQueryColumnUsage.SplitBy).Select(a => a.rc).ToArray();
                var values = pairs.Select(a => a.rc).ToArray();

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
                    ColumnIndexToValueIndex = values.Select((r, i) => KeyValuePair.Create(r.Index, i)).ToDictionary()
                });
            }

            ctx.ReportProgress($"Creating Columns");
            var codifications = new List<PredictorCodification>();

            using (HeavyProfiler.Log("MainQuery"))
            {
                for (int i = 0; i < mainResult.Columns.Length; i++)
                {
                    var col = ctx.Predictor.MainQuery.Columns[i];
                    using (HeavyProfiler.Log("Columns", () => col.Token.Token.ToString()))
                    {
                        var mainCol = new PredictorColumnMain(col, i);
                        var mainCodifications = algorithm.GenerateCodifications(col.Encoding, mainResult.Columns[i], mainCol);
                        codifications.AddRange(mainCodifications);
                    }
                }
            }

            foreach (var sq in ctx.SubQueries.Values)
            {
                using (HeavyProfiler.Log("SubQuery", () => sq.ToString()!))
                {
                    var distinctKeys = sq.GroupedValues.SelectMany(a => a.Value.Keys).Distinct(ObjectArrayComparer.Instance).ToList();

                    distinctKeys.Sort(ObjectArrayComparer.Instance);

                    foreach (var ks in distinctKeys)
                    {
                        using (HeavyProfiler.Log("Keys", () => ks.ToString(k => k?.ToString(), ", ")))
                        {
                            foreach (var vc in sq.ValueColumns)
                            {
                                var col = sq.SubQueryEntity.Columns[vc.Index];
                                using (HeavyProfiler.Log("Columns", () => col.Token.Token.ToString()))
                                {
                                    var subCol = new PredictorColumnSubQuery(col, vc.Index, sq.SubQueryEntity, ks);
                                    var subQueryCodifications = algorithm.GenerateCodifications(col.Encoding, vc, subCol);
                                    codifications.AddRange(subQueryCodifications);
                                }
                            }
                        }
                    }
                }
            }

            ctx.SetCodifications(codifications.ToArray());
        }
    }

    static QueryRequest GetMainQueryRequest(PredictorMainQueryEmbedded mq)
    {
        return new QueryRequest
        {
            QueryName = mq.Query.ToQueryName(),

            GroupResults = mq.GroupResults,

            Filters = mq.Filters.ToFilterList(),

            Columns = mq.Columns.Select(c => new Column(c.Token.Token, null)).ToList(),

            Pagination = new Pagination.All(),
            Orders = Enumerable.Empty<Order>().ToList(),
        };
    }

    public static QueryRequest ToMultiColumnQuery(PredictorMainQueryEmbedded mainQuery, PredictorSubQueryEntity sq)
    {
        var parentKey = sq.Columns.SingleEx(a => a.Usage == PredictorSubQueryColumnUsage.ParentKey);

        var filterList = mainQuery.Filters.ToFilterList();

        var mainFilters = mainQuery.Query.Is(sq.Query) ? filterList : filterList.Select(f => PrependToken(f, parentKey.Token.Token)).ToList();
        var additionalFilters = sq.Filters.ToFilterList();

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

    internal static Filter PrependToken(Filter filter, QueryToken prefix)
    {
        if (filter is FilterCondition fc)
            return new FilterCondition(prefix.Append(fc.Token), fc.Operation, fc.Value);

        if (filter is FilterGroup fg)
            return new FilterGroup(fg.GroupOperation, fg.Token == null ? null : prefix.Append(fg.Token),
                fg.Filters.Select(f => PrependToken(f, prefix)).ToList());

        throw new UnexpectedValueException(filter);
    }

    static QueryToken Append(this QueryToken baseToken, QueryToken suffix)
    {
        var steps = suffix.Follow(a => a.Parent).Reverse();
        QueryToken token = baseToken;
        foreach (var step in steps)
        {
            QueryToken? newToken = null;
            if (step.Key == "Entity" && step is ColumnToken)
            {
                if (token.Type.CleanType() == step.Type.CleanType())
                    continue;
                else
                    newToken = token.SubTokenInternal("[" + TypeLogic.GetCleanName(baseToken.Type.CleanType()) + "]", SubTokensOptions.CanElement)!;
            }

            newToken = token.SubTokenInternal(step.Key, SubTokensOptions.CanElement);
            token = newToken ?? throw new InvalidOperationException($"Token '{step}' not found in '{token.FullKey()}'");
        }

        return token;
    }

}

public abstract class PredictorColumnBase
{
    //Index of PredictorColumn in the MainQuery/SubQuery
    public int PredictorColumnIndex;
    public object? ColumnModel;

    protected PredictorColumnBase(int predictorColumnIndex)
    {
        PredictorColumnIndex = predictorColumnIndex;
    }

    public abstract PredictorColumnUsage Usage { get; }
    public abstract QueryToken Token { get; }
    public abstract PredictorColumnNullHandling NullHandling { get; }
    public abstract PredictorColumnEncodingSymbol Encoding { get; }
}

public class PredictorColumnMain : PredictorColumnBase
{
    public PredictorColumnEmbedded PredictorColumn;

    public PredictorColumnMain(PredictorColumnEmbedded predictorColumn, int predictorColumnIndex)
        :base(predictorColumnIndex)
    {
        PredictorColumn = predictorColumn;
    }

    public override PredictorColumnUsage Usage => PredictorColumn.Usage;
    public override QueryToken Token => PredictorColumn.Token.Token;
    public override PredictorColumnNullHandling NullHandling => PredictorColumn.NullHandling;
    public override PredictorColumnEncodingSymbol Encoding => PredictorColumn.Encoding;

    public override string ToString()
    {
        return $"{Usage} {Token}";
    }
}

public class PredictorColumnSubQuery : PredictorColumnBase
{
    public PredictorSubQueryColumnEmbedded PredictorSubQueryColumn;
    public PredictorSubQueryEntity SubQuery;
    public object?[] Keys;

    public PredictorColumnSubQuery(PredictorSubQueryColumnEmbedded predictorSubQueryColumn, int predictorColumnIndex, PredictorSubQueryEntity subQuery, object?[] keys) :
        base(predictorColumnIndex)
    {
        PredictorSubQueryColumn = predictorSubQueryColumn;
        SubQuery = subQuery;
        Keys = keys;
    }

    public override PredictorColumnUsage Usage => PredictorSubQueryColumn.Usage.ToPredictorColumnUsage();
    public override QueryToken Token => PredictorSubQueryColumn.Token.Token;
    public override PredictorColumnNullHandling NullHandling => PredictorSubQueryColumn.NullHandling!.Value;
    public override PredictorColumnEncodingSymbol Encoding => PredictorSubQueryColumn.Encoding;

    public override string ToString()
    {
        return $"{Usage} {Token}{(Keys == null ? null : $" (Keys={Keys.ToString(", ")})")}";
    }
}

public class PredictorCodification
{
    public PredictorCodification(PredictorColumnBase column)
    {
        this.Column = column;
    }

    public int Index;

    public PredictorColumnBase Column;
    
    //Only for 1-hot encoding in the column (i.e: Neuronal Networks)
    public object? IsValue;
    
    public float? Average;
    public float? StdDev;

    public float? Min;
    public float? Max;

    public override string ToString()
    {
        return $"{Index} {Column}{(IsValue == null ? null : $" (IsValue={IsValue})")}";
    }
}

public class ObjectArrayComparer : IEqualityComparer<object?[]>, IComparer<object?[]>
{
    public static readonly ObjectArrayComparer Instance = new ObjectArrayComparer();

    public int Compare(object?[]? x, object?[]? y)
    {
        if (x == null && y == null)
            return 0;

        if (x == null)
            return -1;

        if (y == null)
            return 1;

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

    private int CompareValue(object? v1, object? v2)
    {
        if (v1 == null && v2 == null)
            return 0;

        if (v1 == null)
            return -1;

        if (v2 == null)
            return 1;

        return ((IComparable)v1).CompareTo(v2);
    }

    public bool Equals(object?[]? x, object?[]? y)
    {
        if (x == null && y == null)
            return true;

        if (x == null || y == null)
            return false;

        if (x.Length != y.Length)
            return false;

        for (int i = 0; i < x.Length; i++)
        {
            if (!object.Equals(x[i], y[i]))
                return false;
        }

        return true;
    }

    public int GetHashCode(object?[] array)
    {
        int hash = 17;
        foreach (var item in array)
        {
            hash = hash * 23 + ((item != null) ? item.GetHashCode() : 0);
        }
        return hash;
    }
}
