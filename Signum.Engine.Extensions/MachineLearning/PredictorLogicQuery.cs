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

            Implementations mainQueryImplementations = DynamicQueryManager.Current.GetEntityImplementations(mainQueryRequest.QueryName);

            ctx.SubQueries = new Dictionary<PredictorSubQueryEntity, SubQuery>();
            foreach (var mc in ctx.Predictor.SubQueries)
            {
                ctx.ReportProgress($"Executing SubQuery {mc}");
                QueryGroupRequest multiColumnQuery = ToMultiColumnQuery(ctx.Predictor.MainQuery, mainQueryImplementations, mc);
                ResultTable multiColumnResult = DynamicQueryManager.Current.ExecuteGroupQuery(multiColumnQuery);

                var entityGroupKey = multiColumnResult.Columns.FirstEx();
                var remainingKeys = multiColumnResult.Columns.Take(mc.GroupKeys.Count).Skip(1).ToArray();
                var aggregates = multiColumnResult.Columns.Take(mc.GroupKeys.Count).Skip(1).ToArray();

                var groupedValues = multiColumnResult.Rows.AgGroupToDictionary(row => (Lite<Entity>)row[entityGroupKey], gr =>
                    gr.ToDictionaryEx(
                        row => row.GetValues(remainingKeys),
                        row => row.GetValues(aggregates),
                        ObjectArrayComparer.Instance));

                ctx.SubQueries.Add(mc, new SubQuery
                {
                    MultiColumn = mc,
                    QueryGroupRequest = multiColumnQuery,
                    ResultTable = multiColumnResult,
                    GroupedValues = groupedValues,
                    Aggregates = aggregates,
                });
            }

            ctx.ReportProgress($"Creating Columns");
            var columns = new List<PredictorResultColumn>();

            for (int i = 0; i < mainResult.Columns.Length; i++)
            {
                columns.AddRange(ExpandColumns(ctx.Predictor.MainQuery.Columns[i], i, mainResult.Columns[i]));
            }
            
            foreach (var mcq in ctx.SubQueries.Values)
            {
                var distinctKeys = mcq.GroupedValues.SelectMany(a => a.Value.Values).Distinct(ObjectArrayComparer.Instance).ToList();

                distinctKeys.Sort(ObjectArrayComparer.Instance);

                foreach (var k in distinctKeys)
                {
                    for (int i = 0; i < mcq.Aggregates.Length; i++)
                    {
                        var list = ExpandColumns(mcq.MultiColumn.Aggregates[i], i, mcq.Aggregates[i]).ToList();
                        list.ForEach(a =>
                        {
                            a.MultiColumn = mcq.MultiColumn;
                            a.Keys = k;
                        });

                        columns.AddRange(list);
                    }
                }
            }

            for (int i = 0; i < columns.Count; i++)
            {
                columns[i].Index = i;
            }

            ctx.SetColums(columns.ToArray());
        }

        private static IEnumerable<PredictorResultColumn> ExpandColumns(PredictorColumnEmbedded pc, int pcIndex, ResultColumn rc)
        {
            switch (pc.Encoding)
            {
                case PredictorColumnEncoding.None:
                    return new[] { new PredictorResultColumn { PredictorColumn = pc, PredictorColumnIndex = pcIndex } };
                case PredictorColumnEncoding.OneHot:
                    return rc.Values.Cast<object>().Distinct().Select(v => new PredictorResultColumn { PredictorColumn = pc, IsValue = v, PredictorColumnIndex = pcIndex }).ToList();
                case PredictorColumnEncoding.Codified:
                    
                    var values = rc.Values.Cast<object>().Distinct().ToArray();
                    var valuesToIndex = values.Select((v, i) => KVP.Create(v, i)).ToDictionary();
                    return new[] { new PredictorResultColumn { PredictorColumn = pc, ValuesToIndex = valuesToIndex, Values = values, PredictorColumnIndex = pcIndex } };
                default:
                    throw new InvalidOperationException("Unexcpected Encoding");
            }
        }

        static QueryRequest GetMainQueryRequest(PredictorMainQueryEmbedded mainQuery)
        {
            return new QueryRequest
            {
                QueryName = mainQuery.Query.ToQueryName(),

                Filters = mainQuery.Filters.Select(f => ToFilter(f)).ToList(),

                Columns = mainQuery.Columns.Select(c => new Column(c.Token.Token, null)).ToList(),

                Pagination = new Pagination.All(),
                Orders = Enumerable.Empty<Order>().ToList(),
            };
        }

        static QueryGroupRequest ToMultiColumnQuery(PredictorMainQueryEmbedded mainQuery, Implementations mainQueryImplementations, PredictorSubQueryEntity mc)
        {
            var mainQueryKey = mc.GroupKeys.FirstEx();

            if (!Compatible(mainQueryKey.Token.Token.GetImplementations(), mainQueryImplementations))
                throw new InvalidOperationException($"{mainQueryKey.Token} of {mc.Query} should be of type {mainQueryImplementations}");

            var mainFilters = mainQuery.Filters.Select(f => mainQuery.Query.Is(mc.Query) ? ToFilter(f) : ToFilterAppend(f, mainQueryKey.Token.Token));
            var additionalFilters = mc.AdditionalFilters.Select(f => ToFilter(f)).ToList();

            var groupKeys = mc.GroupKeys.Select(c => new Column(c.Token.Token, null)).ToList();
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
        //Unique index for all the PredictorResultColumn
        public int Index;
        
        public PredictorColumnEmbedded PredictorColumn;
        //Index of PredictorColumn in the SimpleColumns/Aggregates
        public int? PredictorColumnIndex;

        //Only for multi columns (values inside of collections)
        public PredictorSubQueryEntity MultiColumn;
        public object[] Keys;
        
        //Only for 1-hot encoding in the column (i.e: Neuronal Networks)
        public object IsValue;

        //Serves as Codification (i.e: Bayes)
        public Dictionary<object, int> ValuesToIndex;

        public object[] Values;
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
