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

namespace Signum.Engine
{
    internal static class Saver
    {
        public static void Save(Entity entity)
        {
            Save(new []{entity});
        }

        static readonly Entity[] None = new Entity[0];

        public static void Save(Entity[] entities)
        {
            if (entities == null || entities.Any(e => e == null))
                throw new ArgumentNullException("entity");

            using (var log = HeavyProfiler.LogNoStackTrace("PreSaving"))
            {
                Schema schema = Schema.Current;
                DirectedGraph<Modifiable> modifiables = PreSaving(() => GraphExplorer.FromRoots(entities));
                
                HashSet<Entity> wasNew = modifiables.OfType<Entity>().Where(a=>a.IsNew).ToHashSet(ReferenceEqualityComparer<Entity>.Default);
                HashSet<Entity> wasSelfModified = modifiables.OfType<Entity>().Where(a => a.Modified == ModifiedState.SelfModified).ToHashSet(ReferenceEqualityComparer<Entity>.Default);

                log.Switch("Integrity");

                var error = GraphExplorer.FullIntegrityCheck(modifiables);
                if (error != null)
                    throw new IntegrityCheckException(error);

                log.Switch("Graph");

                GraphExplorer.PropagateModifications(modifiables.Inverse());

                //colapsa modifiables (collections and embeddeds) keeping indentifiables only
                DirectedGraph<Entity> identifiables = GraphExplorer.ColapseIdentifiables(modifiables);

                foreach (var node in identifiables)
                    schema.OnSaving(node);

                //Remove all the edges that doesn't mean a dependency
                identifiables.RemoveEdges(identifiables.Edges.Where(e => !e.To.IsNew).ToList());

                //Remove all the nodes that are not modified
                List<Entity> notModified = identifiables.Where(node => !node.IsGraphModified).ToList();

                notModified.ForEach(node => identifiables.RemoveFullNode(node, None));

                log.Switch("SaveGroups");

                SaveGraph(schema, identifiables);

                foreach (var node in identifiables)
                    schema.OnSaved(node, new SavedEventArgs
                    {
                        IsRoot = entities.Contains(node),
                        WasNew = wasNew.Contains(node),
                        WasSelfModified = wasSelfModified.Contains(node),
                    });

                EntityCache.Add(identifiables);
                EntityCache.Add(notModified);

                GraphExplorer.CleanModifications(modifiables);
            }
        }

        private static void SaveGraph(Schema schema, DirectedGraph<Entity> identifiables)
        {
            //takes apart the 'forbidden' connections from the good ones
            DirectedGraph<Entity> backEdges = identifiables.FeedbackEdgeSet();

            if (backEdges.IsEmpty())
                backEdges = null;
            else
                identifiables.RemoveEdges(backEdges.Edges);

            Dictionary<TypeNew, int> stats = identifiables.GroupCount(ident => new TypeNew(ident.GetType(), ident.IsNew));

            DirectedGraph<Entity> clone = identifiables.Clone();
            DirectedGraph<Entity> inv = identifiables.Inverse();

            while (clone.Count > 0)
            {
                IGrouping<TypeNew, Entity> group = clone.Sinks()
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

        private static void SaveGroup(Schema schema, IGrouping<TypeNew, Entity> group, DirectedGraph<Entity> backEdges)
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

            public override string ToString()
            {
                return $"{Type} IsNew={IsNew}";
            }
        }

        internal static DirectedGraph<Modifiable> PreSaving(Func<DirectedGraph<Modifiable>> recreate)
        {
            Schema schema = Schema.Current;
            return GraphExplorer.PreSaving(recreate, (Modifiable m, PreSavingContext ctx) =>
            {
                if (m is ModifiableEntity me)
                    me.SetTemporalErrors(null);

                m.PreSaving(ctx);

                if (m is Entity ident)
                    schema.OnPreSaving(ident, ctx);
            });
        }
    }
}
