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
            var tokens = document.MainDocumentPart.Document.Descendants<BaseNode>().ToList().Select(t => t.GetToken()).NotNull().ToList();

            var columns = tokens.Distinct().Select(qt => new Signum.Entities.DynamicQuery.Column(qt, null)).ToList();

            var filters = systemWordTemplate != null ? systemWordTemplate.GetFilters(this.queryDescription) :
                new List<Filter> { new Filter(QueryUtils.Parse("Entity", this.queryDescription, 0), FilterOperation.EqualTo, this.entity) };

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
            var parameters = new WordTemplateParameters
            {
                Columns = this.dicTokenColumn,
                CultureInfo = this.culture,
                Entity = this.entity,
                SystemWordTemplate = systemWordTemplate
            };

            foreach (var item in document.MainDocumentPart.Document.Descendants<BaseNode>().ToList())
            {
                item.RenderNode(parameters, this.table.Rows);
            }
        }
    }
}
