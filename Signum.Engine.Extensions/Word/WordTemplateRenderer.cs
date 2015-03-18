using DocumentFormat.OpenXml.Packaging;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Signum.Engine.Word
{
    class WordTemplateRenderer
    {
         WordprocessingDocument document;
         QueryDescription queryDescription;
         Entity entity;
         CultureInfo culture;
         ISystemWordTemplate systemWordTemplate;

         public WordTemplateRenderer(WordprocessingDocument document, QueryDescription queryDescription, Entity entity, CultureInfo culture, ISystemWordTemplate systemWordTemplate)
         {
             this.document = document;
             this.entity = entity;
             this.culture = culture;
             this.systemWordTemplate = systemWordTemplate;
             this.queryDescription = queryDescription;
         }

        ResultTable table;
        Dictionary<QueryToken, ResultColumn> dicTokenColumn;

        internal void MakeQuery()
        {
            List<QueryToken> tokens = new List<QueryToken>();

            foreach (var root in document.RecursivePartsRootElements())
            {
                foreach (var item in root.Descendants<BaseNode>())
                {
                    item.FillTokens(tokens);
                }
            }

            var columns = tokens.NotNull().Distinct().Select(qt => new Signum.Entities.DynamicQuery.Column(qt, null)).ToList();

            var filters = systemWordTemplate != null ? systemWordTemplate.GetFilters(this.queryDescription) :
                new List<Filter> { new Filter(QueryUtils.Parse("Entity", this.queryDescription, 0), FilterOperation.EqualTo, this.entity.ToLite()) };

            this.table = DynamicQueryManager.Current.ExecuteQuery(new QueryRequest
            {
                QueryName = this.queryDescription.QueryName,
                Columns = columns,
                Pagination = new Pagination.All(),
                Filters = filters,
                Orders = new List<Order>(),
            });

            this.dicTokenColumn = table.Columns.ToDictionary(rc => rc.Column.Token);
        }

        internal void RenderNodes()
        {
            var parameters = new WordTemplateParameters(this.entity, this.culture, this.dicTokenColumn, this.table.Rows)
            {
                SystemWordTemplate = systemWordTemplate
            };

            foreach (var root in document.RecursivePartsRootElements())
            {
                var list = root.Descendants<BaseNode>().ToList(); //eager

                foreach (var node in list)
                {
                    node.RenderNode(parameters);
                }
            }           
        }

        public void AssertClean()
        {
            foreach (var root in this.document.RecursivePartsRootElements())
            {
                var list = root.Descendants<BaseNode>().ToList();

                if (list.Any())
                    throw new InvalidOperationException("{0} unexpected BaseNode instances found: {1}".FormatWith(list.Count, list.ToString(l => l.LocalName, ", ")));
            }
        }
    }
}
