using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.MachineLearning
{
    public static class PredictorLogic
    {
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

                sb.Schema.EntityEvents<PredictorEntity>().Retrieved += PredictorLogic_Retrieved;

            }
        }

        public static byte[] GetTsvMetadata(this PredictorEntity predictor)
        {
            return new byte[0];
        }

        public static byte[] GetTsv(this PredictorEntity predictor)
        {
            ResultTable result = DynamicQueryManager.Current.ExecuteQuery(predictor.ToQueryRequest());

            /** convert the data table into a list structure, so that we can pass it to the CSV serializer */
            var matrix = result.Rows.Select(r => result.Columns.Select(c => r[c]).ToList()).ToList();

            return Tsv.ToTsvBytes(matrix);
        }

        public static byte[] GetCsv(this PredictorEntity predictor)
        {
            ResultTable result = DynamicQueryManager.Current.ExecuteQuery(predictor.ToQueryRequest().QueryRequest);

            /** convert the data table into a list structure, so that we can pass it to the CSV serializer */
            var matrix = result.Rows.Select(r => result.Columns.Select(c => r[c]).ToList()).ToList();

            return Csv.ToCsvBytes(matrix);
        }

        public static PartialQuery ToQueryRequest(this PredictorEntity predictor)
        {
            var columns = predictor.Columns.Where(c => !c.Token.Token.HasAllOrAny()).ToList();

            return new PartialQuery
            {
                IsMList = false,
                Columns = columns,
                QueryRequest = new QueryRequest()
                {
                    QueryName = predictor.Query.ToQueryName(),
                    Filters = predictor.Filters.Select(f => new Filter(f.Token.Token, f.Operation, FilterValueConverter.Parse(f.ValueString, f.Token.Token.Type, f.Operation.IsList()))).ToList(),
                    Columns = columns.Select(c => new Column(c.Token.Token, null)).ToList(),
                    Pagination = new Pagination.All(),
                    Orders = Enumerable.Empty<Order>().ToList(),
                },
            };
        }

        public static PartialQuery ToQueryRequest<T>(this MList<T> list)
        {
            return null;
        }

        public static List<PartialQuery> ToQueryRequests(this PredictorEntity predictor)
        {
            //var simpleColumns = predictor.Columns.Where(c => !c.Token.Token.HasMListPredecessor()).ToList();
            //var listColumns = predictor.Columns.Where(c => c.Token.Token.HasMListPredecessor()).ToList();

            return null;
        }

        public static object[][] AssembleResults(List<PartialQuery> queries)
        {
            return null;
        }

        public class PartialQuery {
            public bool IsMList;
            public List<PredictorColumnEmbedded> Columns;
            public QueryRequest QueryRequest;
            public ResultTable ResultTable;
            object[][] Data;
        }

        static void PredictorLogic_Retrieved(PredictorEntity predictor)
        {
            object queryName = QueryLogic.ToQueryName(predictor.Query.Key);
            QueryDescription description = DynamicQueryManager.Current.QueryDescription(queryName);

            predictor.ParseData(description);
        }
    }
}
