using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities.Operations;
using Signum.Engine.Maps;
using System.Threading;
using Signum.Entities.Authorization;
using Signum.Entities;
using System.Diagnostics;
using Signum.Engine.SchemaInfoTables;
using Signum.Engine.Authorization;

namespace Signum.Engine.Operations
{
    public static class Graph
    {
        static Dictionary<Type, Dictionary<Type, IGraph>> entityStateGraph = new Dictionary<Type, Dictionary<Type, IGraph>>();
        static Dictionary<Type, Dictionary<Type, IGraph>> entityActionGraph = new Dictionary<Type, Dictionary<Type, IGraph>>();

        internal static void Register(IGraph graph)
        {
            entityStateGraph.GetOrCreate(graph.EntityType).Add(graph.StateType, graph);
            entityActionGraph.GetOrCreate(graph.EntityType).Add(graph.ActionType, graph);
        }

        public static List<IGraph> GraphsForType(Type entityType)
        {
            return entityStateGraph.TryGetC(entityType).TryCC(a => a.Values.ToList());
        }

        public static IGraph GraphEntityAndAction(Type entityType, Type actionType)
        {
            return entityActionGraph
                .GetOrThrow(entityType, "There are no graphs for entity {0}")
                .GetOrThrow(actionType, "There is no graph for {0} with actiontype {{0}}".Formato(entityType));
        }

        public static void CacheActions()
        {
            var groupsOfActions = Database.RetrieveAll<ActionDN>().GroupToDictionary(a => a.Key.Split('.')[0]);
            var allGraphs = entityActionGraph.SelectMany(d => d.Value.Select(p => p.Value)); 

            Synchronizer.JoinStrinct(
                groupsOfActions,
                allGraphs,
                pair => pair.Key,
                graph => graph.ActionType.Name,
                (pair, graph) => new { graph, actions = pair.Value }, "Caching ActionDN").ForEach(a => a.graph.SetActions(a.actions));
        }

        static List<ActionDN> GraphActions()
        {
            return (from dic in entityActionGraph.Values
                    from graph in dic.Values
                    from item in Enum.GetValues(graph.ActionType).Cast<object>()
                    select ActionDN.FromEnum(item)).ToList();
        }

        public static SqlPreCommand GenerateScript()
        {
            Table table = Schema.Current.Table<ActionDN>();

            return GraphActions().Select(a=>table.InsertSqlSimple(a)).Combine(Spacing.Simple);
        }

        const string ActionsKey = "Actions"; 

        public static SqlPreCommand SynchronizationScript(Replacements replacements)
        {
            Table table = Schema.Current.Table<ActionDN>();

            List<ActionDN> current = Administrator.TryRetrieveAll<ActionDN>();

            return Synchronizer.SyncronizeReplacing( replacements, ActionsKey,
                current.ToDictionary( c => c.Key),
                GraphActions().ToDictionary( s => s.Key),
                (k,c) => table.Delete(c.Id),
                (k,s) => table.InsertSqlSimple(s),
                (k,c,s) =>
                {
                    c.Name = s.Name;
                    c.Key = s.Key;
                    if (c.SelfModified)
                        return table.UpdateSqlSimple(c);
                    return null;
                }, Spacing.Double);
        }

        public static void Start(SchemaBuilder sb)
        {
            if (sb.NotDefined<ActionDN>())
            {
                AuthLogic.Start(sb); 

                sb.Include<ActionDN>();
                sb.Include<LogActionDN>();
                sb.Include<LogStateDN>();
                sb.Include<RuleActionDN>();

                sb.Schema.Initializing += s=>CacheActions();
                sb.Schema.Generating += GenerateScript;
                sb.Schema.Synchronizing += SynchronizationScript;
            }
        }
    }
}
