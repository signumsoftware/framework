using Signum.Utilities.DataStructures;
using Signum.Engine.Maps;

namespace Signum.Engine;

internal static class Saver
{
    public static void Save(Entity entity)
    {
        Save(new[] { entity });
    }

    static readonly Entity[] None = Array.Empty<Entity>();

    public static void Save(Entity[] entities)
    {
        if (entities == null || entities.Any(e => e == null))
            throw new ArgumentNullException(nameof(entities));

        using (var log = HeavyProfiler.LogNoStackTrace("PreSaving"))
        {
            Schema schema = Schema.Current;
            DirectedGraph<Modifiable> modifiables = PreSaving(() => GraphExplorer.FromRoots(entities));

            HashSet<Entity> wasNew = modifiables.OfType<Entity>().Where(a => a.IsNew).ToHashSet(ReferenceEqualityComparer<Entity>.Default);
            log.Switch("Integrity");

            var error = GraphExplorer.FullIntegrityCheck(modifiables);
            if (error != null)
            {
#if DEBUG
                var withEntities = error.WithEntities(modifiables);
                throw new IntegrityCheckException(withEntities);
#else
                throw new IntegrityCheckException(error);
#endif
            }

            log.Switch("Graph");

            GraphExplorer.PropagateModifications(modifiables.Inverse());
            
            HashSet<Entity> wasModified = modifiables.OfType<Entity>().Where(a => a.IsGraphModified).ToHashSet(ReferenceEqualityComparer<Entity>.Default);

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

            EntityCache.Add(identifiables);
            EntityCache.Add(notModified);

            GraphExplorer.CleanModifications(modifiables);

            foreach (var node in identifiables)
                schema.OnSaved(node, new SavedEventArgs
                {
                    IsRoot = entities.Contains(node),
                    WasNew = wasNew.Contains(node),
                    WasModified = wasModified.Contains(node),
                });
        }
    }

    private static void SaveGraph(Schema schema, DirectedGraph<Entity> identifiables)
    {
        //takes apart the 'forbidden' connections from the good ones
        DirectedGraph<Entity>? backEdges = identifiables.FeedbackEdgeSet();

        if (backEdges.IsEmpty())
            backEdges = null;
        else
            identifiables.RemoveEdges(backEdges.Edges);

        Dictionary<(Type type, bool isNew), int> stats = identifiables.GroupCount(ident => (ident.GetType(), ident.IsNew));

        DirectedGraph<Entity> clone = identifiables.Clone();
        DirectedGraph<Entity> inv = identifiables.Inverse();

        while (clone.Count > 0)
        {
            IGrouping<(Type type, bool isNew), Entity> group = clone.Sinks()
                .GroupBy(ident => (ident.GetType(), ident.IsNew))
                .MinBy(g => stats[g.Key] - g.Count())!;

            foreach (var node in group)
                clone.RemoveFullNode(node, inv.RelatedTo(node));

            stats[group.Key] -= group.Count();

            SaveGroup(schema, group, backEdges);
        }

        if (backEdges != null)
        {
            foreach (var gr in backEdges.Edges.Select(e => e.From).Distinct().GroupBy(ident => (ident.GetType(), ident.IsNew)))
                SaveGroup(schema, gr, null);
        }
    }

    private static void SaveGroup(Schema schema, IGrouping<(Type type, bool isNew), Entity> group, DirectedGraph<Entity>? backEdges)
    {
        Table table = schema.Table(group.Key.type);

        if (group.Key.isNew)
            table.InsertMany(group.ToList(), backEdges);
        else
            table.UpdateMany(group.ToList(), backEdges);
    }

    internal static DirectedGraph<Modifiable> PreSaving(Func<DirectedGraph<Modifiable>> recreate)
    {
        Schema schema = Schema.Current;
        return GraphExplorer.PreSaving(recreate, (m, ctx) =>
        {
            if (m is ModifiableEntity me)
                me.SetTemporalErrors(null);

            m.PreSaving(ctx);

            if (m is Entity ident)
                schema.OnPreSaving(ident, ctx);


        });
    }
}
