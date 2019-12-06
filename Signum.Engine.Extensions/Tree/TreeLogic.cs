using Microsoft.SqlServer.Types;
using Signum.Engine;
using Signum.Engine.Basics;
using Signum.Engine.Maps;
using Signum.Engine.Operations;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.Tree;
using Signum.Utilities;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Engine.Tree
{  
    public static class TreeLogic
    {
        public class DescendatsMethodExpander<T> : GenericMethodExpander
            where T: TreeEntity
        {
            public static Expression<Func<T, IQueryable<T>>> Expression =
                cp => Database.Query<T>().Where(cc => (bool)cc.Route.IsDescendantOf(cp.Route));
            public DescendatsMethodExpander() : base(Expression) { }
        }
        [MethodExpander(typeof(DescendatsMethodExpander<>))]
        public static IQueryable<T> Descendants<T>(this T e)
            where T : TreeEntity
        {
            return DescendatsMethodExpander<T>.Expression.Evaluate(e);
        }

     
        public class ChildrensMethodExpander<T> : GenericMethodExpander
            where T: TreeEntity
        {
            public static Expression<Func<T, IQueryable<T>>> Expression =
                cp => Database.Query<T>().Where(cc => (bool)(cc.Route.GetAncestor(1) == cp.Route));
            public ChildrensMethodExpander() : base(Expression) { }
        }
        [MethodExpander(typeof(ChildrensMethodExpander<>))]
        public static IQueryable<T> Children<T>(this T e)
             where T : TreeEntity
        {
            return ChildrensMethodExpander<T>.Expression.Evaluate(e);
        }

       
        public class AscendantsMethodExpander<T> : GenericMethodExpander
            where T: TreeEntity
        {
            public static Expression<Func<T, IQueryable<T>>> Expression =
                cc => Database.Query<T>().Where(cp => (bool)cc.Route.IsDescendantOf(cp.Route)).OrderBy(cp => (Int16)cp.Route.GetLevel());
            public AscendantsMethodExpander() : base(Expression) { }
        }
        [MethodExpander(typeof(AscendantsMethodExpander<>))]
        public static IQueryable<T> Ascendants<T>(this T e)
             where T : TreeEntity
        {
            return AscendantsMethodExpander<T>.Expression.Evaluate(e);
        }

     
        public class ParentMethodExpander<T> : GenericMethodExpander
            where T : TreeEntity
        {
            public static Expression<Func<T, T?>> Expression = 
                cc => Database.Query<T>().SingleOrDefaultEx(cp => (bool)(cp.Route == cc.Route.GetAncestor(1)));
            public ParentMethodExpander() : base(Expression) { }
        }
        [MethodExpander(typeof(ParentMethodExpander<>))]
        public static T? Parent<T>(this T e)
             where T : TreeEntity
        {
            return ParentMethodExpander<T>.Expression.Evaluate(e);
        }
       
        [AutoExpressionField]
        public static short Level(this TreeEntity t) => 
            As.Expression(() => (short)t.Route.GetLevel());

        public static void CalculateFullName<T>(T tree)
            where T : TreeEntity
        {
            tree.SetFullName(tree.Ascendants().Select(a => a.Name).ToString(" > "));
        }

        static SqlHierarchyId LastChild<T>(SqlHierarchyId node)
            where T : TreeEntity
        {
            using (ExecutionMode.Global())
                return Database.Query<T>()
                    .Select(c => (SqlHierarchyId?)c.Route)
                    .Where(n => (bool)((SqlHierarchyId)n.Value.GetAncestor(1) == node))
                    .OrderByDescending(n => n).FirstOrDefault() ?? SqlHierarchyId.Null;
        }

        static SqlHierarchyId FirstChild<T>(SqlHierarchyId node)
            where T : TreeEntity
        {
            using (ExecutionMode.Global())
                return Database.Query<T>()
                    .Select(c => (SqlHierarchyId?)c.Route)
                    .Where(n => (bool)((SqlHierarchyId)n.Value.GetAncestor(1) == node))
                    .OrderBy(n => n).FirstOrDefault() ?? SqlHierarchyId.Null;
        }

        private static SqlHierarchyId Next<T>(SqlHierarchyId node)
            where T : TreeEntity
        {
            using (ExecutionMode.Global())
                return Database.Query<T>()
                    .Select(t => (SqlHierarchyId?)t.Route)
                    .Where(n => (bool)(n.Value.GetAncestor(1) == node.GetAncestor(1)) && (bool)(n.Value > node))
                    .OrderBy(n => n).FirstOrDefault() ?? SqlHierarchyId.Null;
        }

        private static SqlHierarchyId Previous<T>(SqlHierarchyId node)
        where T : TreeEntity
        {
            using (ExecutionMode.Global())
                return Database.Query<T>()
                    .Select(t => (SqlHierarchyId?)t.Route)
                    .Where(n => (bool)(n.Value.GetAncestor(1) == node.GetAncestor(1)) && (bool)(n.Value < node))
                    .OrderByDescending(n => n).FirstOrDefault() ?? SqlHierarchyId.Null;
        }


        internal static int RemoveDescendants<T>(T t)
            where T : TreeEntity
        {
            return t.Descendants().UnsafeDelete();
        }


        public static void FixName<T>(T t)
            where T : TreeEntity
        {
            if (t.IsNew)
            {
                t.SetFullName(t.Name);
                t.Save();
                CalculateFullName(t);
            }
            else
            {
                t.Save();
                CalculateFullName(t);

                if (t.IsGraphModified)
                {
                    var list = t.Descendants().Where(c => c != t).ToList();
                    foreach (T h in list)
                    {
                        CalculateFullName(h);
                        h.Save();
                    }
                }
            }
        }

        internal static void FixRouteAndNames<T>(T t, MoveTreeModel model)
            where T : TreeEntity
        {
            var list = t.Descendants().Where(c => c != t).ToList();

            var oldNode = t.Route;
         
            t.Route = GetNewPosition<T>(model, t);

            t.Save();
            CalculateFullName(t);
            t.Save();

            foreach (T h in list)
            {
                h.Route = h.Route.GetReparentedValue(oldNode, t.Route);
                h.Save();
                CalculateFullName(h);
                h.Save();
            }
        }

        private static SqlHierarchyId GetNewPosition<T>(MoveTreeModel model, TreeEntity entity)
            where T : TreeEntity
        {
            var newParentRoute = model.NewParent == null ? SqlHierarchyId.GetRoot() : model.NewParent.InDB(a => a.Route);
            
            if (newParentRoute.IsDescendantOf(entity.Route))
                throw new Exception(TreeMessage.ImpossibleToMove0InsideOf1.NiceToString(entity, model.NewParent));

            if (model.InsertPlace == InsertPlace.FirstNode)
                return newParentRoute.GetDescendant(SqlHierarchyId.Null, FirstChild<T>(newParentRoute));

            if(model.InsertPlace == InsertPlace.LastNode)
                return newParentRoute.GetDescendant(LastChild<T>(newParentRoute), SqlHierarchyId.Null);

            var newSiblingRoute = model.Sibling!.InDB(a => a.Route);

            if (!newSiblingRoute.IsDescendantOf(newParentRoute) || 
                newSiblingRoute.GetLevel() != newParentRoute.GetLevel() + 1 || 
                newSiblingRoute == entity.Route)
                throw new Exception(TreeMessage.ImpossibleToMove01Of2.NiceToString(entity, model.InsertPlace.NiceToString(), model.NewParent));

            if (model.InsertPlace == InsertPlace.After)
                return newParentRoute.GetDescendant(newSiblingRoute, Next<T>(newSiblingRoute));
            
            if (model.InsertPlace == InsertPlace.Before)
                return newParentRoute.GetDescendant(Previous<T>(newSiblingRoute), newSiblingRoute);

            throw new InvalidOperationException("Unexpected InsertPlace " + model.InsertPlace);
        }

        public static FluentInclude<T> WithTree<T>(this FluentInclude<T> include, Func<T, MoveTreeModel, T>? copy = null) where T : TreeEntity, new()
        {
            RegisterExpressions<T>();
            RegisterOperations<T>(copy);
            include.WithUniqueIndex(n => new { n.ParentRoute, n.Name });
            return include;
        }

        public static void RegisterExpressions<T>()
            where T : TreeEntity
        {
            QueryLogic.Expressions.Register((T c) => c.Children(), () => TreeMessage.Children.NiceToString());
            QueryLogic.Expressions.Register((T c) => c.Parent(), () => TreeMessage.Parent.NiceToString());
            QueryLogic.Expressions.Register((T c) => c.Descendants(), () => TreeMessage.Descendants.NiceToString());
            QueryLogic.Expressions.Register((T c) => c.Ascendants(), () => TreeMessage.Ascendants.NiceToString());
            QueryLogic.Expressions.Register((T c) => c.Level(), () => TreeMessage.Level.NiceToString());
        }

        public static void RegisterOperations<T>(Func<T, MoveTreeModel, T>? copy) where T : TreeEntity, new()
        {
            Graph<T>.Construct.Untyped(TreeOperation.CreateRoot).Do(c =>
            {
                c.Construct = (_) => new T
                {
                    ParentOrSibling = null,
                    Level = 1,
                    IsSibling = false
                };
                c.Register();
            });

            Graph<T>.ConstructFrom<T>.Untyped(TreeOperation.CreateChild).Do(c =>
            {
                c.Construct = (t, _) => new T
                {
                    ParentOrSibling = t.ToLite(),
                    Level = (short)(t.Level + 1),
                    IsSibling = false
                    //                    
                };
                c.Register();
            });

            Graph<T>.ConstructFrom<T>.Untyped(TreeOperation.CreateNextSibling).Do(c =>
            {
                c.Construct = (t, _) => new T
                {
                    ParentOrSibling = t.ToLite(),
                    Level = t.Level,
                    IsSibling = true
                    //                    
                };
                c.Register();
            });

            new Graph<T>.Execute(TreeOperation.Save)
            {
                CanBeNew = true,
                CanBeModified = true,
                Execute = (t, _) =>
                {
                    if (t.IsNew)
                    {
                        t.Route = CalculateRoute(t);
                        if (MixinDeclarations.IsDeclared(typeof(T), typeof(DisabledMixin)) && t.ParentOrSibling != null)
                            t.Mixin<DisabledMixin>().IsDisabled = t.Parent()!.Mixin<DisabledMixin>().IsDisabled;
                    }

                    TreeLogic.FixName(t);
                }
            }.Register();

            new Graph<T>.Execute(TreeOperation.Move)
            {
                Execute = (t, args) =>
                {
                    var model = args.GetArg<MoveTreeModel>();

                    TreeLogic.FixRouteAndNames(t, model);
                    t.Save();

                    if (MixinDeclarations.IsDeclared(typeof(T), typeof(DisabledMixin)) && model.NewParent != null && model.NewParent.InDB(e => e.Mixin<DisabledMixin>().IsDisabled) && !t.Mixin<DisabledMixin>().IsDisabled)
                    {
                        t.Execute(DisableOperation.Disable);
                    }

                }
            }.Register();

            if(copy != null)
            {
                Graph<T>.ConstructFrom<T>.Untyped(TreeOperation.Copy).Do(c =>
                {
                    c.Construct = (t, args) =>
                     {
                         var model = args.GetArg<MoveTreeModel>();
                         var newRoute = GetNewPosition<T>(model, t);

                         var descendants = t.Descendants().OrderBy(a => a.Route).ToList();

                         var hasDisabledMixin = MixinDeclarations.IsDeclared(typeof(T), typeof(DisabledMixin));
                         var isParentDisabled = hasDisabledMixin  && model.NewParent != null && model.NewParent.InDB(e => e.Mixin<DisabledMixin>().IsDisabled);

                         var list = descendants.Select(oldNode =>
                         {
                             var newNode = copy!(oldNode, model);
                             if (hasDisabledMixin)
                                 newNode.Mixin<DisabledMixin>().IsDisabled = oldNode.Mixin<DisabledMixin>().IsDisabled || isParentDisabled;

                             newNode.ParentOrSibling = model.NewParent;
                             newNode.Route = oldNode.Route.GetReparentedValue(t.Route, newRoute);
                             newNode.SetFullName(newNode.Name);
                             return newNode;
                         }).ToList();

                         list.SaveList();

                         foreach (T h in list)
                         {
                             CalculateFullName(h);
                             h.Save();
                         }

                         return list.First();

                     };
                }).Register();
            }

            new Graph<T>.Delete(TreeOperation.Delete)
            {
                Delete = (f, args) =>
                {
                    TreeLogic.RemoveDescendants(f);
                }
            }.Register();
        }

        public static SqlHierarchyId CalculateRoute<T>(T t) where T : TreeEntity, new()
        {
            if (!t.IsSibling)
            {
                if (t.ParentOrSibling == null)
                    return SqlHierarchyId.GetRoot().GetDescendant(LastChild<T>(SqlHierarchyId.GetRoot()), SqlHierarchyId.Null);

                var parentRoute = t.ParentOrSibling.InDB(p => p.Route);
                return parentRoute.GetDescendant(LastChild<T>(parentRoute), SqlHierarchyId.Null);
            }
            else
            {
                var siblingRoute = t.ParentOrSibling!.InDB(p => p.Route);
                return siblingRoute.GetAncestor(1).GetDescendant(siblingRoute, Next<T>(siblingRoute));
            }
        }
    }
}
