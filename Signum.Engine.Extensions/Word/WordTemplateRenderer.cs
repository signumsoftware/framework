using DocumentFormat.OpenXml.Packaging;
using D = DocumentFormat.OpenXml.Drawing;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Signum.Entities.Word;
using Signum.Engine.Basics;
using Signum.Engine.Templating;

namespace Signum.Engine.Word
{
    class WordTemplateRenderer
    {
        OpenXmlPackage document;
        QueryDescription queryDescription;
        Entity? entity;
        CultureInfo culture;
        WordTemplateEntity template;
        IWordModel? model;
        TextTemplateParser.BlockNode? fileNameBlock;

        public WordTemplateRenderer(OpenXmlPackage document, QueryDescription queryDescription, CultureInfo culture, WordTemplateEntity template, IWordModel? model, Entity? entity, TextTemplateParser.BlockNode? fileNameBlock)
        {
            this.document = document;
            this.culture = culture;
            this.queryDescription = queryDescription;
            this.template = template;
            this.entity = entity;
            this.model = model;
            this.fileNameBlock = fileNameBlock;
        }

        ResultTable? table;
        Dictionary<QueryToken, ResultColumn>? dicTokenColumn;

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

            if (this.fileNameBlock != null)
                this.fileNameBlock.FillQueryTokens(tokens);

            var columns = tokens.NotNull().Distinct().Select(qt => new Signum.Entities.DynamicQuery.Column(qt, null)).ToList();

            var filters = model != null ? model.GetFilters(this.queryDescription) :
                entity != null ? new List<Filter> { new FilterCondition(QueryUtils.Parse("Entity", this.queryDescription, 0), FilterOperation.EqualTo, this.entity.ToLite()) } :
                throw new InvalidOperationException($"Impossible to create a Word report if '{nameof(entity)}' and '{nameof(model)}' are both null");

            this.table = QueryLogic.Queries.ExecuteQuery(new QueryRequest
            {
                QueryName = this.queryDescription.QueryName,
                Columns = columns,
                Pagination = model?.GetPagination() ?? new Pagination.All(),
                Filters = filters,
                Orders = model?.GetOrders(this.queryDescription) ?? new List<Order>(),
            });

            var dt = this.table.ToDataTable();

            this.dicTokenColumn = table.Columns.ToDictionary(rc => rc.Column.Token);
        }

        internal void RenderNodes()
        {
            var parameters = new WordTemplateParameters(this.entity, this.culture, this.dicTokenColumn!, this.table!.Rows, template, model);
            
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

        internal string RenderFileName()
        {
            return this.fileNameBlock!.Print(new TextTemplateParameters(this.entity, this.culture, this.dicTokenColumn!, this.table!.Rows));
        }
    }
}
