using Signum.Utilities.Reflection;
using Microsoft.SqlServer.Types;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.API.Filters;
using Signum.API.Json;
using Signum.API;

namespace Signum.Tree;

[ValidateModelFilter]
public class TreeController : ControllerBase
{
    [HttpGet("api/tree/findLiteLikeByName/{typeName}/{subString}/{count}")]
    public List<Lite<TreeEntity>> FindTreeLiteLikeByName(string typeName, string subString, int count)
    {
        Type type = TypeLogic.GetType(typeName);
        return giFindTreeLiteLikeByNameGeneric.GetInvoker(type)(subString, count);
    }

    static GenericInvoker<Func<string, int, List<Lite<TreeEntity>>>> giFindTreeLiteLikeByNameGeneric =
        new((subString, count) => FindTreeLiteLikeByNameGeneric<TreeEntity>(subString, count));
    static List<Lite<TreeEntity>> FindTreeLiteLikeByNameGeneric<T>(string subString, int count)
        where T : TreeEntity
    {
        var parts = subString.Trim().SplitNoEmpty(' ');

        return Database.Query<T>()
            .Where(a => a.Name.ContainsAll(parts))
            .OrderBy(a => a.Name.Length)
            .Take(count)
            .Select(a => a.ToLite())
            .OfType<Lite<TreeEntity>>()
            .ToList();
    }

    [HttpPost("api/tree/findNodes/{typeName}")]
    public FindNodesResponse FindNodes(string typeName, [Required, FromBody]FindNodesRequest request) {

        Type type = TypeLogic.GetType(typeName);

        var list = giFindNodesGeneric.GetInvoker(type)(request);

        var nodes = ToTreeNodes(list);

        return new FindNodesResponse()
        {
            columns = request.columns.Select(c => RebaseToken(c.token, "Entity.Ascendants.Element")).ToList(),
            nodes = nodes,
        };
    }

    public class FindNodesRequest
    {
        public List<FilterTS> userFilters;
        public List<FilterTS> frozenFilters;
        public List<ColumnTS> columns;
        public List<Lite<TreeEntity>> expandedNodes;
        public bool loadDescendants;
    }

    public class FindNodesResponse
    {
        public List<string> columns { get; set; }
        public List<TreeNode> nodes { get; set; }
    }

    static GenericInvoker<Func<FindNodesRequest, List<ResultRow>>> giFindNodesGeneric =
        new(request => FindNodesGeneric<TreeEntity>(request));
    static List<ResultRow> FindNodesGeneric<T>(FindNodesRequest request)
        where T : TreeEntity
    {
        var expandedNodesFilter = request.expandedNodes.IsNullOrEmpty() ? new List<FilterConditionTS>() : new List<FilterConditionTS>() {
            new FilterConditionTS()
            {
                token = "Entity.Parent",
                operation = FilterOperation.IsIn,
                value = request.expandedNodes,
            }
        };

        var qn = QueryUtils.GetKey(typeof(T));
        var reqTS = RebaseRequest<T>(request, "Entity.Ascendants.Element");
        var qrFiltered = reqTS.ToQueryRequest(qn, SignumServer.JsonSerializerOptions, null);
        var qrFrozen = reqTS.Let(r =>
        {
            r.filters = request.frozenFilters.Concat(expandedNodesFilter).ToList();
            return r;
        }).ToQueryRequest(qn, SignumServer.JsonSerializerOptions, null);

        var reqDescTS = RebaseRequest<T>(request, "Entity.Descendants.Element");
        var qrFilteredDesc = reqDescTS.ToQueryRequest(qn, SignumServer.JsonSerializerOptions, null);
        var qrFrozenDesc = reqDescTS.Let(r =>
        {
            r.filters = request.frozenFilters.Concat(expandedNodesFilter).ToList();
            return r;
        }).ToQueryRequest(qn, SignumServer.JsonSerializerOptions, null);

        var result = new List<ResultRow>();

        result.AddRange(QueryLogic.Queries.ExecuteQuery(qrFiltered).Rows?.ToList() ?? new());

        if (request.loadDescendants)
            result.AddRange(QueryLogic.Queries.ExecuteQuery(qrFilteredDesc).Rows?.ToList() ?? new());

        if (!request.expandedNodes.IsNullOrEmpty())
        {
            result.AddRange(QueryLogic.Queries.ExecuteQuery(qrFrozen).Rows?.ToList() ?? new());

            if (request.loadDescendants)
                result.AddRange(QueryLogic.Queries.ExecuteQuery(qrFrozenDesc).Rows?.ToList() ?? new());
        }

        return result;

        /*
        var result = new List<ResultTable>();
        result.Add(QueryLogic.Queries.ExecuteQuery(qrFiltered));

        if (request.loadDescendants)
            result.Add(QueryLogic.Queries.ExecuteQuery(qrFilteredDesc));

        if (!request.expandedNodes.IsNullOrEmpty()) {
            result.Add(QueryLogic.Queries.ExecuteQuery(qrFrozen));

            if (request.loadDescendants)
                result.Add(QueryLogic.Queries.ExecuteQuery(qrFrozenDesc));
        }

        return result;
        */
    }

    static string RebaseToken(string token, string prefix)
    {
        var _token = token.TryAfter("Entity.") ?? token;
        return $"{prefix}.{_token}";
    }

    static QueryRequestTS RebaseRequest<T>(FindNodesRequest request, string prefix) where T : TreeEntity
    {
        var result = new QueryRequestTS()
        {
            queryKey = QueryUtils.GetKey(typeof(T)),
            groupResults = false,
            columns = (new List<ColumnTS>() { new ColumnTS() { token = $"{prefix}.TreeInfo", displayName = TreeMessage.TreeInfo.NiceToString() } })
                .Concat(request.columns.Select(c =>
                {
                    var token = RebaseToken(c.token, prefix);
                    return new ColumnTS() { token = token, displayName = c.displayName };
                }))
                .ToList(),
            filters = request.userFilters.Concat(request.frozenFilters).ToList(),
            orders = new(),
            pagination = new PaginationTS() { mode = PaginationMode.All },
        };

        return result;
    }

    static List<TreeNode> ToTreeNodes(List<ResultRow> infos)
    {
        var dictionary = infos.Distinct(a => ((TreeInfo)a[0]!).route).ToDictionary(a => ((TreeInfo)a[0]!).route);

        var parentNodes = TreeHelper.ToTreeC(dictionary.Values, a => ((TreeInfo)a[0]!).route.GetLevel() == 1 ? null :
             dictionary.GetOrThrow(((TreeInfo)a[0]!).route.GetAncestor(1)));

        return parentNodes.OrderBy(a => ((TreeInfo)a.Value[0]!).route).Select(n => new TreeNode(n)).ToList();
    }

}

#pragma warning disable IDE1006 // Naming Styles
public class TreeNode
{
    public TreeNode() { }
    internal TreeNode(Node<ResultRow> node)
    {
        var ti = (TreeInfo)node.Value[0]!;
        this.values = node.Value.GetValues(node.Value.Table.Columns).Skip(1).ToArray();
        this.name = ti.name;
        this.fullName = ti.fullName;
        this.lite = ti.lite;
        this.disabled = ti.disabled;
        this.childrenCount = ti.childrenCount;
        this.loadedChildren = node.Children.OrderBy(a => ((TreeInfo)a.Value[0]!).route).Select(a => new TreeNode(a)).ToList();
        this.level = ti.level;
    }

    public object?[] values { get; set; }
    public string name { set; get; }
    public string fullName { get; set; }
    public Lite<TreeEntity> lite { set; get; }
    public bool disabled { get; set; }
    public int childrenCount { set; get; }
    public List<TreeNode> loadedChildren { set; get; }
    public short level { get; set; }
}
#pragma warning restore IDE1006 // Naming Styles
