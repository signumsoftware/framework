using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Engine;
using System.Data;
using Signum.Entities.Reflection;
using Signum.Engine.Maps;
using Signum.Engine.Properties;

namespace Signum.Engine
{
    internal static class Saver
    {
        public static void SaveAll(IdentifiableEntity[] idents)
        {
             Save(()=>GraphExplorer.FromRoots(idents));
        }

        public static void Save(IdentifiableEntity ident) 
        {
            Save(() =>{ using(HeavyProfiler.Log("GraphExplorer")) return GraphExplorer.FromRoot(ident);});
        }

        static readonly IdentifiableEntity[] None = new IdentifiableEntity[0];

        static void Save(Func<DirectedGraph<Modifiable>> createGraph)
        {
            DirectedGraph<Modifiable> modifiables = GraphExplorer.PreSaving(createGraph);

            Schema schema = ConnectionScope.Current.Schema;
            modifiables = GraphExplorer.ModifyGraph(modifiables, (Modifiable m, ref bool graphModified) =>
                {
                    IdentifiableEntity ident = m as IdentifiableEntity;

                    if (ident != null)
                        schema.OnPreSaving(ident, ref graphModified);
                }, createGraph);

            string error = GraphExplorer.Integrity(modifiables);
            if (error.HasText())
                throw new ApplicationException(error);

            GraphExplorer.PropagateModifications(modifiables.Inverse());

            //colapsa modifiables (collections and embeddeds) keeping indentifiables only
            DirectedGraph<IdentifiableEntity> identifiables = GraphExplorer.ColapseIdentifiables(modifiables);

            foreach (var node in identifiables)
                schema.OnSaving(node);

            //Remove all the edges that doesn't mean a dependency
            identifiables.RemoveAll(identifiables.Edges.Where(e => !e.To.IsNew).ToList());

            //Remove all the nodes that are not modified
            List<IdentifiableEntity> notModified = identifiables.Where(node => node.Modified == false).ToList();

            notModified.ForEach(node => identifiables.RemoveFullNode(node, None));

            //separa las conexiones 'prohibidas' de las buenas
            DirectedGraph<IdentifiableEntity> backEdges = identifiables.FeedbackEdgeSet();

            if (backEdges.IsEmpty())
                backEdges = null;
            else
                identifiables.RemoveAll(backEdges.Edges);

            IEnumerable<HashSet<IdentifiableEntity>> groups = identifiables.CompilationOrderGroups();

            Forbidden forbidden = new Forbidden();

            foreach (var group in groups)
            {
                foreach (var ident in group)
                {
                    forbidden.Clear();
                    if (backEdges != null)
                        forbidden.UnionWith(backEdges.TryRelatedTo(ident));

                    schema.Table(ident.GetType()).Save(ident, forbidden);
                }
            }

            if (backEdges != null)
            {
                var postSavings = backEdges.Edges.Select(e => e.From).ToHashSet();
                foreach (var ident in postSavings)
                {
                    forbidden.Clear();
                    if (backEdges != null)
                        forbidden.UnionWith(backEdges.TryRelatedTo(ident));

                    schema.Table(ident.GetType()).Save(ident, forbidden);
                }
            }

            EntityCache.Add(identifiables);
            EntityCache.Add(notModified);
        }


       
    }
}
