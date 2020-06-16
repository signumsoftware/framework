using Signum.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using Signum.Engine.Basics;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Microsoft.SqlServer.Types;
using Signum.React.Filters;
using Signum.React.ApiControllers;
using Signum.Entities.Tree;
using Signum.Engine.Tree;
using Signum.Entities.DynamicQuery;
using Signum.Entities.Basics;
using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Signum.Engine.DynamicQuery;
using Signum.Engine;

namespace Signum.React.Tree
{
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
            new GenericInvoker<Func<string, int, List<Lite<TreeEntity>>>>((subString, count) => FindTreeLiteLikeByNameGeneric<TreeEntity>(subString, count));
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
            public List<Lite<TreeEntity>> expandedNodes;
        }

        static GenericInvoker<Func<FindNodesRequest, List<TreeInfo>>> giFindNodesGeneric =
            new GenericInvoker<Func<FindNodesRequest, List<TreeInfo>>>(request => FindNodesGeneric<TreeEntity>(request));
        static List<TreeInfo> FindNodesGeneric<T>(FindNodesRequest request)
            where T : TreeEntity
        {
            var qd = QueryLogic.Queries.QueryDescription(typeof(T));
            var userFilters = request.userFilters.Select(f => f.ToFilter(qd, false)).ToList();
            var frozenFilters = request.frozenFilters.Select(f => f.ToFilter(qd, false)).ToList();


            var frozenQuery = QueryLogic.Queries.GetEntities(new QueryEntitiesRequest { QueryName = typeof(T), Filters = frozenFilters, Orders = new List<Order>() })
                            .Select(a => (T)a.Entity);

            var filteredQuery = QueryLogic.Queries.GetEntities(new QueryEntitiesRequest { QueryName = typeof(T), Filters = userFilters.Concat(frozenFilters).ToList(), Orders = new List<Order>() })
                            .Select(a => (T)a.Entity);

            var disabledMixin = MixinDeclarations.IsDeclared(typeof(T), typeof(DisabledMixin));
            var list = filteredQuery
                            .SelectMany(t => t.Ascendants())
                            .Select(t => new TreeInfo
                            {
                                route = t.Route,
                                name = t.Name,
                                lite = t.ToLite(),
                                level = t.Level(),
                                disabled = disabledMixin && t.Mixin<DisabledMixin>().IsDisabled,
                                childrenCount = frozenQuery.Count(a => (bool)(a.Route.GetAncestor(1) == t.Route)),
                            }).ToList();

            var expandedChildren = request.expandedNodes.IsNullOrEmpty() ? new List<TreeInfo>() :
                            frozenQuery
                           .Where(t => request.expandedNodes.Contains(t.Parent()!.ToLite()))
                           .SelectMany(t => t.Ascendants())
                           .Select(t => new TreeInfo
                           {
                               route = t.Route,
                               name = t.Name,
                               lite = t.ToLite(),
                               level = t.Level(),
                               disabled = disabledMixin && t.Mixin<DisabledMixin>().IsDisabled,
                               childrenCount = frozenQuery.Count(a => (bool)(a.Route.GetAncestor(1) == t.Route)),
                           }).ToList();

            return list.Concat(expandedChildren).ToList();
        }

        static List<TreeNode> ToTreeNodes(List<TreeInfo> infos)
        {
            var dictionary = infos.Distinct(a => a.route).ToDictionary(a => a.route);

            var parentNodes = TreeHelper.ToTreeC(dictionary.Values, a => a.route.GetLevel() == 1 ? null :
                 dictionary.GetOrThrow(a.route.GetAncestor(1)));

            return parentNodes.OrderBy(a => a.Value.route).Select(n => new TreeNode(n)).ToList();
        }

    }

#pragma warning disable IDE1006 // Naming Styles
    class TreeInfo
    {
        public string name { get; set; }
        public Lite<TreeEntity> lite { get; set; }
        public bool disabled { get; set; }
        public int childrenCount { get; set; }
        public SqlHierarchyId route { get; set; }
        public short level { get; set; }
    }

    public class TreeNode
    {
        public TreeNode() { }
        internal TreeNode(Node<TreeInfo> node)
        {
            this.name = node.Value.name;
            this.lite = node.Value.lite;
            this.disabled = node.Value.disabled;
            this.childrenCount = node.Value.childrenCount;
            this.loadedChildren = node.Children.OrderBy(a => a.Value.route).Select(a => new TreeNode(a)).ToList();
            this.level = node.Value.level;
        }

        public string name { set; get; }
        public Lite<TreeEntity> lite { set; get; }
        public bool disabled { get; set; }
        public int childrenCount { set; get; }
        public List<TreeNode> loadedChildren { set; get; }
        public short level { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
