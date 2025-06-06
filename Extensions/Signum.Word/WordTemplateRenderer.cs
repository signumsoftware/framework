using DocumentFormat.OpenXml.Packaging;
using D = DocumentFormat.OpenXml.Drawing;
using System.Globalization;
using Signum.Templating;
using Signum.DynamicQuery.Tokens;
using Signum.UserAssets.Queries;

namespace Signum.Word;

class WordTemplateRenderer
{
    OpenXmlPackage document;
    Entity? entity;
    CultureInfo culture;
    WordTemplateEntity template;
    IWordModel? model;
    TextTemplateParser.BlockNode? fileNameBlock;

    public WordTemplateRenderer(OpenXmlPackage document, QueryDescription? queryDescription, CultureInfo culture, WordTemplateEntity template, IWordModel? model, Entity? entity, TextTemplateParser.BlockNode? fileNameBlock)
    {
        this.document = document;
        this.culture = culture;
        this.queryDescription = queryDescription;
        this.template = template;
        this.entity = entity;
        this.model = model;
        this.fileNameBlock = fileNameBlock;
    }

    QueryDescription? queryDescription;
    QueryContext? queryContext;

    internal void ExecuteQuery()
    {
        var qd = this.queryDescription!;
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

        var columns = tokens.NotNull().Distinct().Select(qt => new Signum.DynamicQuery.Column(qt, null)).ToList();

        var filters = model != null ? model.GetFilters(qd) :
            entity != null ? new List<Filter> { new FilterCondition(QueryUtils.Parse("Entity", qd, 0), FilterOperation.EqualTo, this.entity.ToLite()) } :
            throw new InvalidOperationException($"Impossible to create a Word report if '{nameof(entity)}' and '{nameof(model)}' are both null");
        

        filters.AddRange(template.Filters.ToFilterList());

        var orders = model?.GetOrders(qd) ?? new List<Order>();
        orders.AddRange(template.Orders.Select(qo => new Order(qo.Token.Token, qo.OrderType)).ToList());

        var table = QueryLogic.Queries.ExecuteQuery(new QueryRequest
        {
            QueryName = qd.QueryName,
            GroupResults = template.GroupResults,
            Columns = columns,
            Pagination = model?.GetPagination() ?? new Pagination.All(),
            Filters = filters,
            Orders = orders,
        });

        this.queryContext = new QueryContext(qd, table);
    }

    internal void RenderNodes()
    {
        var parameters = new WordTemplateParameters(this.entity, this.culture, this.queryContext, template, model, document);
        
        foreach (var part in document.AllParts().Where(p => p.RootElement != null))
        {
            var root = part.RootElement!;
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
        return this.fileNameBlock!.Print(new TextTemplateParameters(this.entity, this.culture, this.queryContext));
    }
}
