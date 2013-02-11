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
            using (var log = HeavyProfiler.LogNoStackTrace("PreSaving"))
            {
                DirectedGraph<Modifiable> modifiables = GraphExplorer.PreSaving(createGraph);

                Schema schema = Schema.Current;
                modifiables = GraphExplorer.ModifyGraph(modifiables, (Modifiable m, ref bool graphModified) =>
                    {
                        IdentifiableEntity ident = m as IdentifiableEntity;

                        if (ident != null)
                            schema.OnPreSaving(ident, ref graphModified);
                    }, createGraph);

                log.Switch("Integrity");

                string error = GraphExplorer.FullIntegrityCheck(modifiables, withIndependentEmbeddedEntities: false);
                if (error.HasText())
                    throw new ApplicationException(error);

                log.Switch("Graph");

                GraphExplorer.PropagateModifications(modifiables.Inverse());

                //colapsa modifiables (collections and embeddeds) keeping indentifiables only
                DirectedGraph<IdentifiableEntity> identifiables = GraphExplorer.ColapseIdentifiables(modifiables);

                foreach (var node in identifiables)
                    schema.OnSaving(node);

                //Remove all the edges that doesn't mean a dependency
                identifiables.RemoveEdges(identifiables.Edges.Where(e => !e.To.IsNew).ToList());

                //Remove all the nodes that are not modified
                List<IdentifiableEntity> notModified = identifiables.Where(node => node.Modified == false).ToList();

                notModified.ForEach(node => identifiables.RemoveFullNode(node, None));

                log.Switch("SaveGroups");

                SaveGraph(schema, identifiables);

                EntityCache.Add(identifiables);
                EntityCache.Add(notModified);

                GraphExplorer.CleanModifications(modifiables);
            }
        }

        private static void SaveGraph(Schema schema, DirectedGraph<IdentifiableEntity> identifiables)
        {
            //takes apart the 'forbidden' connections from the good ones
            DirectedGraph<IdentifiableEntity> backEdges = identifiables.FeedbackEdgeSet();

            if (backEdges.IsEmpty())
                backEdges = null;
            else
                identifiables.RemoveEdges(backEdges.Edges);

            Dictionary<TypeNew, int> stats = identifiables.GroupCount(ident => new TypeNew(ident.GetType(), ident.IsNew));

            DirectedGraph<IdentifiableEntity> clone = identifiables.Clone();
            DirectedGraph<IdentifiableEntity> inv = identifiables.Inverse();

            while (clone.Count > 0)
            {
                IGrouping<TypeNew, IdentifiableEntity> group = clone.Sinks()
                    .GroupBy(ident => new TypeNew(ident.GetType(), ident.IsNew))
                    .WithMin(g => stats[g.Key] - g.Count());

                foreach (var node in group)
                    clone.RemoveFullNode(node, inv.RelatedTo(node));

                stats[group.Key] -= group.Count();

                SaveGroup(schema, group, backEdges);
            }

            if (backEdges != null)
            {
                foreach (var gr in backEdges.Edges.Select(e => e.From).Distinct().GroupBy(ident => new TypeNew(ident.GetType(), ident.IsNew)))
                    SaveGroup(schema, gr, null);
            }
        }

        private static void SaveGroup(Schema schema, IGrouping<TypeNew, IdentifiableEntity> group, DirectedGraph<IdentifiableEntity> backEdges)
        {
            Table table = schema.Table(group.Key.Type);

            if(group.Key.IsNew)
                table.InsertMany(group.ToList(), backEdges);
            else 
                table.UpdateMany(group.ToList(), backEdges); 
        }

        struct TypeNew : IEquatable<TypeNew>
        {
            public readonly Type Type;
            public readonly bool IsNew;

            public TypeNew(Type type, bool isNew)
            {
                this.Type = type;
                this.IsNew = isNew;
            }

            public bool Equals(TypeNew other)
            {
                return Type == other.Type &&
                    IsNew == other.IsNew;
            }

            public override int GetHashCode()
            {
                return Type.GetHashCode() ^ (IsNew ? 1 : 0);
            }
        }
    }
}
