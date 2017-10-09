using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
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
            foreach (var c in predictor.Columns.Where(a => a.Type == PredictorColumnType.MultiColumn))
            {
                QueryGroupRequest multiColumnQuery = predictor.ToMultiColumnQuery(mainQueryImplementations, c.MultiColumn);
                ResultTable multiColumnResult = DynamicQueryManager.Current.ExecuteGroupQuery(multiColumnQuery);

                var entityGroupKey = multiColumnResult.Columns.FirstEx();
                var remainingKeys = multiColumnResult.Columns.Take(c.MultiColumn.GroupKeys.Count).Skip(1).ToArray();
                var aggregates = multiColumnResult.Columns.Take(c.MultiColumn.GroupKeys.Count).Skip(1).ToArray();

                var groupedValues = multiColumnResult.Rows.AgGroupToDictionary(row => (Lite<Entity>)row[entityGroupKey], gr =>
                    gr.ToDictionaryEx(
                        row => row.GetValues(remainingKeys),
                        row => row.GetValues(aggregates),
                        ObjectArrayComparer.Instance));

                mcqs.Add(new MultiColumnQuery
                {
                    MultiColumnEntity = c.MultiColumn,
                    Query = multiColumnQuery,
                    Result = multiColumnResult,
                    GroupedValues = groupedValues,
                    Aggregates = aggregates,
                });
            }

            var dicMultiColumns = mcqs.ToDictionary(a => a.MultiColumnEntity);

            columns = new List<PredictorResultColumn>();
            columns.AddRange(mainResult.Columns.Select(rc => new PredictorResultColumn { Column = rc }));
            foreach (var mc in mcqs)
            {
                var distinctKeys = mc.GroupedValues.SelectMany(a => a.Value.Values).Distinct(ObjectArrayComparer.Instance).ToList();

                distinctKeys.Sort(ObjectArrayComparer.Instance);

                foreach (var k in distinctKeys)
                {
                    for (int i = 0; i < mc.Aggregates.Length; i++)
                    {
                        columns.Add(new PredictorResultColumn
                        {
                            MultiColumn = mc.MultiColumnEntity,
                            Keys = k,
                            Column = mc.Aggregates[i],
                            AggregateIndex = i,
                        });
                    }
                }
            }

            object[][] rows = new object[mainResult.Rows.Length][];

            for (int i = 0; i < rows.Length; i++)
            {
                var mainRow = mainResult.Rows[i];

                var row = new object[columns.Count];
                for (int j = 0; j < columns.Count; j++)
                {
                    var c = columns[j];

                    if (c.MultiColumn == null)
                        row[j] = mainRow[c.Column];
                    else
                    {
                        var dic = dicMultiColumns[c.MultiColumn].GroupedValues;
                        var array = dic.TryGetC(mainRow.Entity)?.TryGetC(c.Keys);
                        row[j] = array == null ? null : array[c.AggregateIndex];
                    }
                }
                rows[i] = row;
            }

            return rows;
        }

        static QueryRequest GetMainQueryRequest(PredictorEntity predictor)
        {
            return new QueryRequest
            {
                QueryName = predictor.Query.ToQueryName(),

                Filters = predictor.Filters.Select(f => ToFilter(f)).ToList(),

                Columns = predictor.Columns
                .Where(c => c.Type == PredictorColumnType.SimpleColumn)
                .Select(c => new Column(c.Token.Token, null)).ToList(),

                Pagination = new Pagination.All(),
                Orders = Enumerable.Empty<Order>().ToList(),
            };
        }


        static QueryGroupRequest ToMultiColumnQuery(this PredictorEntity predictor, Implementations mainQueryImplementations, PredictorMultiColumnEntity mc)
        {
            var mainQueryKey = mc.GroupKeys.FirstEx();

            if (!Compatible(mainQueryKey.Token.GetImplementations(), mainQueryImplementations))
                throw new InvalidOperationException($"{mainQueryKey.Token} of {mc.Query} should be of type {mainQueryImplementations}");

            var mainFilters = predictor.Filters.Select(f => predictor.Query.Is(mc.Query) ? ToFilter(f) : ToFilterAppend(f, mainQueryKey.Token));
            var additionalFilters = mc.AdditionalFilters.Select(f => ToFilter(f)).ToList();

            var groupKeys = mc.GroupKeys.Select(c => new Column(c.Token, null)).ToList();
            var aggregates = mc.Aggregates.Select(c => new Column(c.Token, null)).ToList();

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
        //Main Case
        public ResultColumn Column;

        public PredictorColumnEmbedded PredictorColumn; 
        //Multu Column case
        public PredictorMultiColumnEntity MultiColumn;

        //Only for multi columns (values inside of collections)
        public object[] Keys;
        
        //Only for 1-hot encoding in the column (i.e: Neuronal Networks)
        public object IsValue;

        //Serves as Codification (i.e: Bayes)
        public string[] Values;

        public int AggregateIndex;
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
