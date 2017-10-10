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
        public static object[][] RetrieveData(PredictorEntity predictor, out List<PredictorResultColumn> columns)
        {
            QueryRequest mainQuery = GetMainQueryRequest(predictor);
            ResultTable mainResult = DynamicQueryManager.Current.ExecuteQuery(mainQuery);

            Implementations mainQueryImplementations = DynamicQueryManager.Current.GetEntityImplementations(predictor.Query.ToQueryName());

            List<MultiColumnQuery> mcqs = new List<MultiColumnQuery>();
            foreach (var mc in predictor.MultiColumns)
            {
                QueryGroupRequest multiColumnQuery = ToMultiColumnQuery(predictor.Query, predictor.Filters.ToList(), mainQueryImplementations, mc);
                ResultTable multiColumnResult = DynamicQueryManager.Current.ExecuteGroupQuery(multiColumnQuery);

                var entityGroupKey = multiColumnResult.Columns.FirstEx();
                var remainingKeys = multiColumnResult.Columns.Take(mc.GroupKeys.Count).Skip(1).ToArray();
                var aggregates = multiColumnResult.Columns.Take(mc.GroupKeys.Count).Skip(1).ToArray();

                var groupedValues = multiColumnResult.Rows.AgGroupToDictionary(row => (Lite<Entity>)row[entityGroupKey], gr =>
                    gr.ToDictionaryEx(
                        row => row.GetValues(remainingKeys),
                        row => row.GetValues(aggregates),
                        ObjectArrayComparer.Instance));

                mcqs.Add(new MultiColumnQuery
                {
                    MultiColumn = mc,
                    QueryGroupRequest = multiColumnQuery,
                    ResultTable = multiColumnResult,
                    GroupedValues = groupedValues,
                    Aggregates = aggregates,
                });
            }

            var dicGroupQuery = mcqs.ToDictionary(a => a.MultiColumn);

            columns = new List<PredictorResultColumn>();
            columns.AddRange(mainResult.Columns.ZipStrict(predictor.SimpleColumns, (rc, pc) => (rc, pc)).SelectMany(t => ExpandColumns(t.rc, t.pc)));

            foreach (var mcq in mcqs)
            {
                var distinctKeys = mcq.GroupedValues.SelectMany(a => a.Value.Values).Distinct(ObjectArrayComparer.Instance).ToList();

                distinctKeys.Sort(ObjectArrayComparer.Instance);

                foreach (var k in distinctKeys)
                {
                    var list = mcq.Aggregates.ZipStrict(mcq.MultiColumn.Aggregates, (rc, pc) => (rc, pc))
                        .SelectMany(t => ExpandColumns(t.rc, t.pc))
                        .Select(a=> new PredictorResultColumn
                        {
                            MultiColumn = mcq.MultiColumn,
                            Keys = k,

                            PredictorColumn = a.PredictorColumn,
                            IsValue = a.IsValue,
                            Values = a.Values,
                        });

                    columns.AddRange(list);
                }
            }

            object[][] rows = new object[mainResult.Rows.Length][];

            for (int i = 0; i < rows.Length; i++)
            {
                var mainRow = mainResult.Rows[i];

                object[] row = CreateRow(columns, dicGroupQuery, mainRow);
                rows[i] = row;
            }

            return rows;
        }

        private static object[] CreateRow(List<PredictorResultColumn> columns, Dictionary<PredictorMultiColumnEntity, MultiColumnQuery> dicGroupQuery, ResultRow mainRow)
        {
            var row = new object[columns.Count];
            for (int j = 0; j < columns.Count; j++)
            {
                var c = columns[j];
                object value;
                if (c.MultiColumn == null)
                    value = mainRow[c.PredictorColumnIndex.Value];
                else
                {
                    var dic = dicGroupQuery[c.MultiColumn].GroupedValues;
                    var aggregateValues = dic.TryGetC(mainRow.Entity)?.TryGetC(c.Keys);
                    value = aggregateValues == null ? null : aggregateValues[c.PredictorColumnIndex.Value];
                }
                //TODO: Codification
                switch (c.PredictorColumn.Encoding)
                {
                    case PredictorColumnEncoding.None:
                        row[j] = value;
                        break;
                    case PredictorColumnEncoding.OneHot:
                        row[j] = Object.Equals(value, c.IsValue) ? 1 : 0;
                        break;
                    case PredictorColumnEncoding.Codified:
                        row[j] = c.Values.GetOrThrow(value);
                        break;
                    default:
                        break;
                }
            }

            return row;
        }

        private static IEnumerable<PredictorResultColumn> ExpandColumns(ResultColumn rc, PredictorColumnEmbedded pc)
        {
            switch (pc.Encoding)
            {
                case PredictorColumnEncoding.None:
                    return new[] { new PredictorResultColumn { PredictorColumn = pc } };
                case PredictorColumnEncoding.OneHot:
                    return rc.Values.Cast<object>().Distinct().Select(v => new PredictorResultColumn { PredictorColumn = pc, IsValue = v }).ToList();
                case PredictorColumnEncoding.Codified:
                    int i = 0;
                    return new[] { new PredictorResultColumn { PredictorColumn = pc, Values = rc.Values.Cast<object>().Distinct().ToDictionary(v => v, v => i++) } };
                default:
                    throw new InvalidOperationException("Unexcpected Encoding");
            }
        }

        static QueryRequest GetMainQueryRequest(PredictorEntity predictor)
        {
            return new QueryRequest
            {
                QueryName = predictor.Query.ToQueryName(),

                Filters = predictor.Filters.Select(f => ToFilter(f)).ToList(),

                Columns = predictor.SimpleColumns.Select(c => new Column(c.Token.Token, null)).ToList(),

                Pagination = new Pagination.All(),
                Orders = Enumerable.Empty<Order>().ToList(),
            };
        }

        static QueryGroupRequest ToMultiColumnQuery(QueryEntity query, List<QueryFilterEmbedded> filters, Implementations mainQueryImplementations, PredictorMultiColumnEntity mc)
        {
            var mainQueryKey = mc.GroupKeys.FirstEx();

            if (!Compatible(mainQueryKey.Token.GetImplementations(), mainQueryImplementations))
                throw new InvalidOperationException($"{mainQueryKey.Token} of {mc.Query} should be of type {mainQueryImplementations}");

            var mainFilters = filters.Select(f => query.Is(mc.Query) ? ToFilter(f) : ToFilterAppend(f, mainQueryKey.Token));
            var additionalFilters = mc.AdditionalFilters.Select(f => ToFilter(f)).ToList();

            var groupKeys = mc.GroupKeys.Select(c => new Column(c.Token, null)).ToList();
            var aggregates = mc.Aggregates.Select(c => new Column(c.Token.Token, null)).ToList();

            return new QueryGroupRequest
            {
                QueryName = mc.Query.ToQueryName(),
                Filters = mainFilters.Concat(additionalFilters).ToList(),
                Columns = groupKeys.Concat(aggregates).ToList(),
                Orders = new List<Order>()
            };
        }

        static Filter ToFilter(QueryFilterEmbedded f)
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


        static bool Compatible(Implementations? multiImplementations, Implementations mainQueryImplementations)
        {
            if (multiImplementations == null)
                return false;

            if (multiImplementations.Value.IsByAll ||
                mainQueryImplementations.IsByAll)
                return false;

            if (multiImplementations.Value.Types.Count() != 1 ||
                mainQueryImplementations.Types.Count() != 1)
                return false;

            return multiImplementations.Value.Types.SingleEx().Equals(mainQueryImplementations.Types.SingleEx());
        }
    }

    public class PredictorResultColumn
    {
        public PredictorMultiColumnEntity MultiColumn;

        public PredictorColumnEmbedded PredictorColumn;
        public int? PredictorColumnIndex { get; set; }

        //Only for multi columns (values inside of collections)
        public object[] Keys;
        
        //Only for 1-hot encoding in the column (i.e: Neuronal Networks)
        public object IsValue;

        //Serves as Codification (i.e: Bayes)
        public Dictionary<object, int> Values;
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
