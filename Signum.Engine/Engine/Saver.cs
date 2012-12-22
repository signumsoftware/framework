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
        public static void SaveAll(IdentifiableEntity[] entities)
        {
            if (entities == null || entities.Any(e => e == null))
                throw new ArgumentNullException("entity");

            Save(() => GraphExplorer.FromRoots(entities));
        }

        public static void Save(IdentifiableEntity entity)
        {
            Save(() => GraphExplorer.FromRoot(entity));
        }

        static readonly IdentifiableEntity[] None = new IdentifiableEntity[0];

        static void Save(Func<DirectedGraph<Modifiable>> createGraph)
        {
            DirectedGraph<Modifiable> modifiables = GraphExplorer.PreSaving(createGraph);

            Schema schema = Schema.Current;
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

            //takes apart the 'forbidden' connections from the good ones
            DirectedGraph<IdentifiableEntity> backEdges = identifiables.FeedbackEdgeSet();

            if (backEdges.IsEmpty())
                backEdges = null;
            else
                identifiables.RemoveAll(backEdges.Edges);

            IEnumerable<HashSet<IdentifiableEntity>> groups = identifiables.CompilationOrderGroups();

            foreach (var group in groups)
            {
                SaveGroup(schema, group, backEdges);
            }

            if (backEdges != null)
            {
                var postSavings = backEdges.Edges.Select(e => e.From).ToHashSet();

                SaveGroup(schema, postSavings, null);
            }

            EntityCache.Add(identifiables);
            EntityCache.Add(notModified);
        }

        private static void SaveGroup(Schema schema, HashSet<IdentifiableEntity> group, DirectedGraph<IdentifiableEntity> backEdges)
        {
            foreach (var gr in group.GroupBy(a => new { Type = a.GetType(), a.IsNew }).ToList())
            {
                var table = schema.Table(gr.Key.Type);
                if (gr.Key.IsNew)
                {
                    table.InsertMany(gr.ToList(), backEdges);
                }
                else
                {
                    table.UpdateMany(gr.ToList(), backEdges);
                }
            }
        }
    }
}
