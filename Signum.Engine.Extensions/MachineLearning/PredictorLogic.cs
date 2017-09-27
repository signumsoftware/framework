using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.MachineLearning
{
    public interface IPredictorAlgorithm
    {
        bool Initialize(PredictorEntity predictor);
        bool TrainPredictor(PredictorEntity predictor, ColumnDescription[] columns, object[][] data);
    }

    public static class PredictorLogic
    {
        static Expression<Func<PredictorEntity, IQueryable<PredictorCodificationEntity>>> CodificationsExpression =
        e => Database.Query<PredictorCodificationEntity>().Where(a => a.Predictor.RefersTo(e));
        [ExpressionField]
        public static IQueryable<PredictorCodificationEntity> Codifications(this PredictorEntity e)
        {
            return CodificationsExpression.Evaluate(e);
        }

        public static Dictionary<PredictorAlgorithmSymbol, IPredictorAlgorithm> Algorithms = new Dictionary<PredictorAlgorithmSymbol, IPredictorAlgorithm>();
        public static void RegisterAlgorithm(PredictorAlgorithmSymbol symbol, IPredictorAlgorithm algorithm)
        {
            Algorithms.Add(symbol, algorithm);
        }

        public static void Start(SchemaBuilder sb, DynamicQueryManager dqm)
        {
            if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
            {
                sb.Include<PredictorEntity>()
                    .WithSave(PredictorOperation.Save)
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Name,
                        e.Query,
                        InputCount = e.Filters.Count,
                        OutputCount = e.Filters.Count,
                    });
                
                sb.Include<PredictorCodificationEntity>()
                    .WithExpressionFrom(dqm, (PredictorEntity e) => e.Codifications())
                    .WithQuery(dqm, () => e => new
                    {
                        Entity = e,
                        e.Id,
                        e.Predictor,
                        e.ColumnIndex,
                        e.OriginalColumnIndex,
                        e.GroupKey0,
                        e.GroupKey1,
                        e.GroupKey2,
                        e.IsValue,
                    });

                sb.Schema.EntityEvents<PredictorEntity>().Retrieved += PredictorEntity_Retrieved;
                sb.Schema.EntityEvents<PredictorMultiColumnEntity>().Retrieved += PredictorMultiColumnEntity_Retrieved;
            }
        }

        public class PredictorGraph : Graph<PredictorEntity, PredictorState>
        {
            public static void Register()
            {
                GetState = f => f.State;

                new Execute(PredictorOperation.Save)
                {
                    FromStates = { PredictorState.Draft },
                    ToStates = { PredictorState.Draft },
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();

                new Execute(PredictorOperation.Train)
                {
                    FromStates = { PredictorState.Draft },
                    ToStates = { PredictorState.Trained },
                    AllowsNew = true,
                    Lite = false,
                    Execute = (e, _) => { },
                }.Register();

                new Execute(PredictorOperation.Untrain)
                {
                    FromStates = { PredictorState.Trained },
                    ToStates = { PredictorState.Draft },
                    Execute = (e, _) => { },
                }.Register();

                new Delete(PredictorOperation.Delete)
                {
                    FromStates = { PredictorState.Draft, PredictorState.Trained },
                    Delete = (e, _) =>
                    {
                        e.Codifications().UnsafeDelete();
                        e.Delete();
                    },
                }.Register();
            }
        }

        public static void TrainPredictor(PredictorEntity predictor)
        {
            List<ColumnDescription> columnDescriptions;
            var design = RetrieveData(predictor, out columnDescriptions);

            predictor.Codifications().UnsafeDelete();
            columnDescriptions.Select((a, i) =>
            {
                string GetGroupKey(int index)
                {
                    var token = a.MultiColumn?.Aggregates.ElementAtOrDefault(index)?.Token;
                    if (token == null)
                        return null;

                    var obj = a.Keys.ElementAtOrDefault(index);

                    if (obj == null)
                        return null;

                    return FilterValueConverter.ToString(obj, token.Type, allowSmart: false);
                }

                return new PredictorCodificationEntity
                {
                    Predictor = predictor.ToLite(),
                    ColumnIndex = i,
                    GroupKey0 = GetGroupKey(0),
                    GroupKey1 = GetGroupKey(1),
                    GroupKey2 = GetGroupKey(2),
                    IsValue = GetGroupKey(2),
                };
            });
        }

        public static byte[] GetTsvMetadata(this PredictorEntity predictor)
        {
            return new byte[0];
        }

        public static byte[] GetTsv(this PredictorEntity predictor)
        {
            List<ColumnDescription> columnDescriptions;
            return Tsv.ToTsvBytes(RetrieveData(predictor, out columnDescriptions));
        }

        public static byte[] GetCsv(this PredictorEntity predictor)
        {
            List<ColumnDescription> columnDescriptions;
            return Csv.ToCsvBytes(RetrieveData(predictor, out columnDescriptions));
        }
        


        public static QueryRequest GetMainQueryRequest(PredictorEntity predictor)
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

        public static QueryGroupRequest ToMultiColumnQuery(this PredictorEntity predictor, Implementations mainQueryImplementations, PredictorMultiColumnEntity mc)
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

        private static Filter ToFilter(QueryFilterEmbedded f)
        {
            return new Filter(f.Token.Token, f.Operation, 
                FilterValueConverter.Parse(f.ValueString, f.Token.Token.Type, allowSmart: false, isList: f.Operation.IsList()));
        }

        private static Filter ToFilterAppend(QueryFilterEmbedded f, QueryToken mainQueryKey)
        {
            QueryToken token = mainQueryKey.Append(f.Token.Token);

            return new Filter(token, f.Operation,
                FilterValueConverter.Parse(f.ValueString, token.Type, allowSmart: false, isList: f.Operation.IsList()));
        }

        private static QueryToken Append(this QueryToken baseToken, QueryToken suffix)
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


        private static bool Compatible(Implementations? multiImplementations, Implementations mainQueryImplementations)
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

       

        public static object[][] AssembleResults(List<ResultTable> queries)
        {
            return null;
        }

        static void PredictorEntity_Retrieved(PredictorEntity predictor)
        {
            object queryName = QueryLogic.ToQueryName(predictor.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            predictor.ParseData(description);
        }

        static void PredictorMultiColumnEntity_Retrieved(PredictorMultiColumnEntity mc)
        {
            object queryName = QueryLogic.ToQueryName(mc.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            mc.ParseData(description);
        }
    }
}


class MultiColumnQuery
{
    public PredictorMultiColumnEntity MultiColumnEntity;
    public QueryGroupRequest Query;
    public ResultTable Result;
    public Dictionary<Lite<Entity>, Dictionary<object[], object[]>> GroupedValues;

    public ResultColumn[] Aggregates { get; internal set; }
}


public static class PredictorLogicQuery
{
    public static object[][] RetrieveData(PredictorEntity predictor, out List<ColumnDescription> columns)
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

        columns = new List<ColumnDescription>();
        columns.AddRange(mainResult.Columns.Select(rc => new ColumnDescription { Column = rc }));
        foreach (var mc in mcqs)
        {
            var distinctKeys = mc.GroupedValues.SelectMany(a => a.Value.Values).Distinct(ObjectArrayComparer.Instance).ToList();

            distinctKeys.Sort(ObjectArrayComparer.Instance);

            foreach (var k in distinctKeys)
            {
                for (int i = 0; i < mc.Aggregates.Length; i++)
                {
                    columns.Add(new ColumnDescription
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

    public class ColumnDescription
    {
        //Main Case
        public ResultColumn Column;

        //Multu Column case
        public PredictorMultiColumnEntity MultiColumn;
        public object[] Keys;
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
