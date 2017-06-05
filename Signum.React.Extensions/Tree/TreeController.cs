using Signum.Engine;
using Signum.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using Signum.Engine.Basics;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using Microsoft.SqlServer.Types;
using System.Data.SqlTypes;
using Signum.React.Filters;
using Signum.Engine.DynamicQuery;
using Signum.React.ApiControllers;
using Signum.Entities.Tree;
using Signum.Engine.Tree;
using Signum.Entities.DynamicQuery;

namespace Signum.React.Tree
{
    public class TreeController : ApiController
    {
        [Route("api/tree/children/{typeName}/{id}"), HttpGet]
        public List<TreeNode> GetChildren(string typeName, string id) {
            Type type = TypeLogic.GetType(typeName);

            var lite = (Lite<TreeEntity>)Lite.ParsePrimaryKey(type, id);

            return giGetChildrenGeneric.GetInvoker(type)(lite);
        }

        [Route("api/tree/roots/{typeName}"), HttpGet]
        public List<TreeNode> GetRoots(string typeName)
        {
            Type type = TypeLogic.GetType(typeName);
            
            return giGetChildrenGeneric.GetInvoker(type)(null);
        }

        static GenericInvoker<Func<Lite<TreeEntity>, List<TreeNode>>> giGetChildrenGeneric =
            new GenericInvoker<Func<Lite<TreeEntity>, List<TreeNode>>>(lite => GetChildrenGeneric<TreeEntity>(lite));
        static List<TreeNode> GetChildrenGeneric<T>(Lite<T> lite) 
            where T: TreeEntity
        {
            var parentRoute = lite == null ? SqlHierarchyId.GetRoot() : lite.InDB(a => a.Route);
            //var parentRoute = lite == null ? SqlHierarchyId.GetRoot() : (lite.Id.ToString() == "6" ? SqlHierarchyId.Parse("/1/") : SqlHierarchyId.Parse("/1/1/"));

            return Database.Query<T>()
                .Where(t => (bool)(t.Route.GetAncestor(1) == parentRoute))
                .Select(t => new TreeNode
                {
                    lite = t.ToLite(),
                    level = t.Level(),
                    childrenCount = t.Children().Count(),
                    loadedChildren = new List<TreeNode>()
                })
                .ToList();
        }

        [Route("api/tree/findNodes/{typeName}"), HttpPost]
        public List<TreeNode> FindNodes(string typeName, List<FilterTS> filters) {

            Type type = TypeLogic.GetType(typeName);

            if (filters.Count == 0)
                return giGetChildrenGeneric.GetInvoker(type)(null);
            else
                return giFindNodesGeneric.GetInvoker(type)(filters);
        }

        static GenericInvoker<Func<List<FilterTS>, List<TreeNode>>> giFindNodesGeneric =
            new GenericInvoker<Func<List<FilterTS>, List<TreeNode>>>(filters => FindNodesGeneric<TreeEntity>(filters));
        static List<TreeNode> FindNodesGeneric<T>(List<FilterTS> filtersTs)
            where T : TreeEntity
        {
            var qd = DynamicQueryManager.Current.QueryDescription(typeof(T));
            var filters = filtersTs.Select(f => f.ToFilter(qd, false)).ToList();

            var dictionary = DynamicQueryManager.Current.GetEntities(new QueryEntitiesRequest { QueryName = typeof(T), Filters = filters, Orders = new List<Order>() })
                            .Select(a => (T)a.Entity)
                            .SelectMany(t => t.Ascendants())
                            .Distinct()
                            .Select(tp => new TreeInfo
                            {
                                route = tp.Route,
                                lite = tp.ToLite(),
                                childrenCount = tp.Children().Count(),
                            }).ToDictionary(a => a.route);

            var parentNodes = TreeHelper.ToTreeC(dictionary.Values, a => a.route.GetLevel() == 1 ? null :
                    dictionary.GetOrThrow(a.route.GetAncestor(1)));

            return parentNodes.Select(n => new TreeNode(n)).ToList();
        }
    }

#pragma warning disable IDE1006 // Naming Styles
    class TreeInfo
    {
        public int childrenCount { get; set; }
        public Lite<TreeEntity> lite { get; set; }
        public SqlHierarchyId route { get; set; }
        public short level { get; set; }
    }

    public class TreeNode
    {
        public TreeNode() { }
        internal TreeNode(Node<TreeInfo> node)
        {
            this.lite = node.Value.lite;
            this.childrenCount = node.Value.childrenCount;
            this.loadedChildren = node.Children.Select(a => new TreeNode(a)).ToList();
            this.level = node.Value.level;
        }

        public Lite<TreeEntity> lite { set; get; }
        public int childrenCount { set; get; }
        public List<TreeNode> loadedChildren { set; get; }
        public short level { get; set; }
    }
#pragma warning restore IDE1006 // Naming Styles
}
