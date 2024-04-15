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
    public List<TreeNode> FindNodes(string typeName, [Required, FromBody]FindNodesRequest request) {

        Type type = TypeLogic.GetType(typeName);

        var list =  giFindNodesGeneric.GetInvoker(type)(request);

        return ToTreeNodes(list);
    }

    public class FindNodesRequest
    {
        public List<FilterTS> userFilters;
        public List<FilterTS> frozenFilters;
        public List<ColumnTS> columns;
        public List<Lite<TreeEntity>> expandedNodes;
        public bool loadDescendants;
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

        var list = QueryLogic.Queries.ExecuteQuery(qrFiltered).Rows?.ToList() ?? new();

        var listDescendants = new List<ResultRow>();
        if (request.loadDescendants)
            listDescendants.AddRange(QueryLogic.Queries.ExecuteQuery(qrFilteredDesc).Rows?.ToList() ?? new());

        var expandedChildren = request.expandedNodes.IsNullOrEmpty() ? new List<ResultRow>() :
            QueryLogic.Queries.ExecuteQuery(qrFrozen).Rows?.ToList() ?? new();

        var expandedChildrenDescendants = new List<ResultRow>();
        if (request.loadDescendants)
            expandedChildrenDescendants.AddRange(request.expandedNodes.IsNullOrEmpty() ? new List<ResultRow>() :
                       QueryLogic.Queries.ExecuteQuery(qrFrozenDesc).Rows?.ToList() ?? new());

        return list.Concat(listDescendants)
            .Concat(expandedChildren)
            .Concat(expandedChildrenDescendants)
            .ToList();
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
                    var token = c.token.TryAfter("Entity.") ?? c.token;
                    return new ColumnTS() { token = $"{prefix}.{token}", displayName = c.displayName };
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
        this.row = node.Value;
        this.name = ti.name;
        this.fullName = ti.fullName;
        this.lite = ti.lite;
        this.disabled = ti.disabled;
        this.childrenCount = ti.childrenCount;
        this.loadedChildren = node.Children.OrderBy(a => ((TreeInfo)a.Value[0]!).route).Select(a => new TreeNode(a)).ToList();
        this.level = ti.level;
    }

    public ResultRow row { get; set; }
    public string name { set; get; }
    public string fullName { get; set; }
    public Lite<TreeEntity> lite { set; get; }
    public bool disabled { get; set; }
    public int childrenCount { set; get; }
    public List<TreeNode> loadedChildren { set; get; }
    public short level { get; set; }
}
#pragma warning restore IDE1006 // Naming Styles
