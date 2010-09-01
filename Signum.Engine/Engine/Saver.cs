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
             Save(()=>GraphExplorer.FromRoots(idents), idents);
        }

        public static void Save(IdentifiableEntity ident) 
        {
            Save(() => GraphExplorer.FromRoot(ident), new[] { ident });
        }

        static readonly IdentifiableEntity[] None = new IdentifiableEntity[0];

        static void Save(Func<DirectedGraph<Modifiable>> createGraph, IdentifiableEntity[] roots)
        {
            DirectedGraph<Modifiable> modifiables = GraphExplorer.PreSaving(createGraph);

            Schema schema = ConnectionScope.Current.Schema;
            modifiables = GraphExplorer.ModifyGraph(modifiables, (Modifiable m, ref bool graphModified) =>
                {
                    IdentifiableEntity ident = m as IdentifiableEntity;

                    if (ident != null)
                        schema.OnPreSaving(ident, roots.Contains(ident), ref graphModified);
                }, createGraph);

            string error = GraphExplorer.Integrity(modifiables);
            if (error.HasText())
                throw new ApplicationException(error);
            
            
            GraphExplorer.PropagateModifications(modifiables.Inverse());

            //colapsa modifiables (collections and embeddeds) keeping indentifiables only
            DirectedGraph<IdentifiableEntity> identifiables = GraphExplorer.ColapseIdentifiables(modifiables);

            foreach (var node in identifiables)
                schema.OnSaving(node, roots.Contains(node));

            //Remove all the edges that doesn't mean a dependency
            identifiables.RemoveAll(identifiables.Edges.Where(e=>!e.To.IsNew).ToList());

            //Remove all the nodes that are not modified
            List<IdentifiableEntity> notModified = identifiables.Where(node => !node.Modified).ToList();

            notModified.ForEach(node=>identifiables.RemoveFullNode(node, None));

            //separa las conexiones 'prohibidas' de las buenas
            DirectedGraph<IdentifiableEntity> backEdges = identifiables.FeedbackEdgeSet();

            identifiables.RemoveAll(backEdges.Edges);

            IEnumerable<HashSet<IdentifiableEntity>> groups = identifiables.CompilationOrderGroups();

            foreach (var group in groups)
            {
                SaveGroup(group, schema, backEdges);
            }

            var postSavings = backEdges.Edges.Select(e => e.From).ToHashSet();

            SaveGroup(postSavings, schema, null);

            EntityCache.Add(identifiables);
            EntityCache.Add(notModified); 
        }


        static void SaveGroup(HashSet<IdentifiableEntity> group, Schema schema, DirectedGraph<IdentifiableEntity> backEdges)
        {
            List<SqlPreCommand> preCommands = new List<SqlPreCommand>(group.Count);
            foreach (var ident in group)
            {
                Table table = schema.Table(ident.GetType());

                Forbidden forbidden = new Forbidden();
                if (backEdges != null)
                    forbidden.UnionWith(backEdges.TryRelatedTo(ident));

                SqlPreCommand pc = table.Save(ident, forbidden);

                preCommands.Add(pc);
            }

            if (preCommands.Count == 0)
                return;

            SqlPreCommand total = preCommands.Combine(Spacing.Triple);

            int? lastId = null;
            foreach (var item in total.Splits(SqlBuilder.MaxParametersInSQL))
                ExecuteCommand(item, ref lastId);

        }

        public static void ExecuteCommand(SqlPreCommand command, ref int? lastId)
        {
            List<IdentifiableEntity> insertedEntities = command.Leaves().Select(ss => ss.EntityToUpdate).NotNull().ToList();

            SqlPreCommand combine = SqlPreCommand.Combine(Spacing.Triple,
                                            SqlBuilder.DeclareIDsMemoryTable(),
                                            SqlBuilder.DeclareLastEntityID(),
                                            lastId.TrySC(id => SqlBuilder.RestoreLastId(id)),
                                            command,
                                            SqlBuilder.SelectIDMemoryTable(),
                                            SqlBuilder.SelectLastEntityID()
                                         );

            DataSet ds = Executor.ExecuteDataSet(combine.ToSimple());

            DataTable dt = ds.Tables[0];
            if (dt.Rows.Count != insertedEntities.Count)
                throw new InvalidOperationException(Resources._0ObjectsInsertedButOnly1IdsAreGenerated.Formato(insertedEntities.Count, dt.Rows.Count));

            dt.Rows.Cast<DataRow>().ZipForeach(insertedEntities, (dr, ei) => ei.id = (int)dr[0]);

            EntityCache.Add<IdentifiableEntity>(insertedEntities);
            lastId = ds.Tables[1].Rows[0]["LastID"].Map(o => o == DBNull.Value ? (int?)null : (int?)o);

        }

    }
}
