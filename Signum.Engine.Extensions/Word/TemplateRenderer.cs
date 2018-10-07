using DocumentFormat.OpenXml.Packaging;
using D = DocumentFormat.OpenXml.Drawing;
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
using System.Data;
using Signum.Entities.Word;
using DocumentFormat.OpenXml;
using System.Threading;
using Signum.Engine.Basics;

namespace Signum.Engine.Word
{
    class TemplateRenderer
    {
         OpenXmlPackage document;
         QueryDescription queryDescription;
         Entity entity;
         CultureInfo culture;
         ISystemWordTemplate systemWordTemplate;
        WordTemplateEntity template;

        public TemplateRenderer(OpenXmlPackage document, QueryDescription queryDescription, Entity entity, CultureInfo culture, ISystemWordTemplate systemWordTemplate, WordTemplateEntity template)
        {
            this.document = document;
            this.entity = entity;
            this.culture = culture;
            this.systemWordTemplate = systemWordTemplate;
            this.queryDescription = queryDescription;
            this.template = template;
        }

        ResultTable table;
        Dictionary<QueryToken, ResultColumn> dicTokenColumn;

        internal void MakeQuery()
        {
            List<QueryToken> tokens = new List<QueryToken>();

            foreach (var root in document.AllRootElements())
            {
                foreach (var item in root.Descendants<BaseNode>())
                {
                    item.FillTokens(tokens);
                }
            }

            var columns = tokens.NotNull().Distinct().Select(qt => new Signum.Entities.DynamicQuery.Column(qt, null)).ToList();

            var filters = systemWordTemplate != null ? systemWordTemplate.GetFilters(this.queryDescription) :
                entity != null ? new List<Filter> { new FilterCondition(QueryUtils.Parse("Entity", this.queryDescription, 0), FilterOperation.EqualTo, this.entity.ToLite()) } :
                throw new InvalidOperationException($"Impossible to create a Word report if '{nameof(entity)}' and '{nameof(systemWordTemplate)}' are both null");

            this.table = QueryLogic.Queries.ExecuteQuery(new QueryRequest
            {
                QueryName = this.queryDescription.QueryName,
                Columns = columns,
                Pagination = systemWordTemplate?.GetPagination() ?? new Pagination.All(),
                Filters = filters,
                Orders = systemWordTemplate?.GetOrders(this.queryDescription) ?? new List<Order>(),
            });

            var dt = this.table.ToDataTable();

            this.dicTokenColumn = table.Columns.ToDictionary(rc => rc.Column.Token);
        }

        internal void RenderNodes()
        {
            var parameters = new WordTemplateParameters(this.entity, this.culture, this.dicTokenColumn, this.table.Rows)
            {
                SystemWordTemplate = systemWordTemplate,
                Template = template,
            };
            
            foreach (var part in document.AllParts().Where(p => p.RootElement != null))
            {
                var root = part.RootElement;
                var baseNodes = root.Descendants<BaseNode>().ToList(); //eager
                foreach (var node in baseNodes)
                {
                    node.RenderNode(parameters);
                }

                TableBinder.ProcessTables(part, parameters);
                
                foreach (var item in root.Descendants<D.Charts.ExternalData>().ToList())
                {
                    item.Remove();
                }
            }

            foreach (var item in document.AllParts().OfType<EmbeddedPackagePart>().ToList())
            {
                foreach (var p in item.GetParentParts().ToList())
                {
                    p.DeletePart(item);
                }
            }
            
        }

        public void AssertClean()
        {
            foreach (var root in this.document.AllRootElements())
            {
                var list = root.Descendants<BaseNode>().ToList();

                if (list.Any())
                    throw new InvalidOperationException("{0} unexpected BaseNode instances found: {1}".FormatWith(list.Count, list.ToString(l => l.LocalName, ", ")));
            }
        }

     
    }
}
