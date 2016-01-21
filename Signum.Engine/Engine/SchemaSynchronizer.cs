using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Engine;
using System.Data;
using Signum.Engine.Maps;
using Signum.Utilities;
using Signum.Entities;
using Signum.Engine.SchemaInfoTables;
using System.Text.RegularExpressions;
using Signum.Engine.Linq;
using Signum.Entities.Reflection;

namespace Signum.Engine
{
    public static class SchemaSynchronizer
    {
        public static Func<SchemaName, bool> DropSchema = s => !s.Name.Contains(@"\"); 

        public static Action<Dictionary<string, DiffTable>> SimplifyDiffTables; 

        public static SqlPreCommand SynchronizeTablesScript(Replacements replacements)
        {
            Dictionary<string, ITable> model = Schema.Current.GetDatabaseTables().ToDictionary(a => a.Name.ToString(), "schema tables");
            HashSet<SchemaName> modelSchemas = Schema.Current.GetDatabaseTables().Select(a => a.Name.Schema).Where(a => !SqlBuilder.SystemSchemas.Contains(a.Name)).ToHashSet();

            Dictionary<string, DiffTable> database = DefaultGetDatabaseDescription(Schema.Current.DatabaseNames());
            HashSet<SchemaName> databaseSchemas = DefaultGetSchemas(Schema.Current.DatabaseNames());

            if (SimplifyDiffTables != null) 
                SimplifyDiffTables(database);

            replacements.AskForReplacements(database.Keys.ToHashSet(), model.Keys.ToHashSet(), Replacements.KeyTables);

            database = replacements.ApplyReplacementsToOld(database, Replacements.KeyTables);

            Dictionary<ITable, Dictionary<string, Index>> modelIndices = model.Values
                .ToDictionary(t => t, t => t.GeneratAllIndexes().ToDictionary(a => a.IndexName, "Indexes for {0}".FormatWith(t.Name)));

            model.JoinDictionaryForeach(database, (tn, tab, diff) =>
            {
                var key = Replacements.KeyColumnsForTable(tn);

                replacements.AskForReplacements(diff.Columns.Keys.ToHashSet(), tab.Columns.Keys.ToHashSet(), key);

                diff.Columns = replacements.ApplyReplacementsToOld(diff.Columns, key);

                diff.Indices = ApplyIndexAutoReplacements(diff, tab, modelIndices[tab]);
            });

            Func<ObjectName, ObjectName> ChangeName = (ObjectName objectName) =>
            {
                string name = replacements.Apply(Replacements.KeyTables, objectName.ToString());

                return model.TryGetC(name)?.Name ?? objectName;
            };

            Func<DiffTable, DiffIndex, Index, bool> columnsChanged = (dif, dix, mix) =>
            {
                if (dix.Columns.Count != mix.Columns.Length)
                    return true;

                var dixColumns = dif.Columns.Where(kvp => dix.Columns.Contains(kvp.Value.Name));

                return !dixColumns.All(kvp => dif.Columns.GetOrThrow(kvp.Key).ColumnEquals(mix.Columns.SingleEx(c => c.Name == kvp.Key), ignorePrimaryKey: true));
            };
            
              

            using (replacements.WithReplacedDatabaseName())
            {
                SqlPreCommand createSchemas = Synchronizer.SynchronizeScriptReplacing(replacements, "Schemas",
                    modelSchemas.ToDictionary(a => a.ToString()),
                    databaseSchemas.ToDictionary(a => a.ToString()),
                    (_, newSN) => SqlBuilder.CreateSchema(newSN),
                    null,
                    (_, newSN, oldSN) => newSN.Equals(oldSN) ? null : SqlBuilder.CreateSchema(newSN),
                    Spacing.Double);

                //use database without replacements to just remove indexes
                SqlPreCommand dropStatistics =
                    Synchronizer.SynchronizeScript(model, database,
                     null,
                    (tn, dif) => SqlBuilder.DropStatistics(tn, dif.Stats),
                    (tn, tab, dif) =>
                    {
                        var removedColums = dif.Columns.Keys.Except(tab.Columns.Keys).ToHashSet();

                        return SqlBuilder.DropStatistics(tn, dif.Stats.Where(a => a.Columns.Any(removedColums.Contains)).ToList());
                    },
                     Spacing.Double);
                
                SqlPreCommand dropIndices =
                    Synchronizer.SynchronizeScript(model, database,
                     null,
                    (tn, dif) => dif.Indices.Values.Select(ix => SqlBuilder.DropIndex(dif.Name, ix)).Combine(Spacing.Simple),
                    (tn, tab, dif) =>
                    {
                        Dictionary<string, Index> modelIxs = modelIndices[tab];

                        var removedColums = dif.Columns.Keys.Except(tab.Columns.Keys).ToHashSet();

                        var changes = Synchronizer.SynchronizeScript(modelIxs, dif.Indices,
                            null,
                            (i, dix) => dix.Columns.Any(removedColums.Contains) || dix.IsControlledIndex ? SqlBuilder.DropIndex(dif.Name, dix) : null,
                            (i, mix, dix) => (mix as UniqueIndex)?.ViewName != dix.ViewName || columnsChanged(dif, dix, mix) ? SqlBuilder.DropIndex(dif.Name, dix) : null,
                            Spacing.Simple);

                        return changes;
                    },
                     Spacing.Double);

                SqlPreCommand dropForeignKeys = Synchronizer.SynchronizeScript(
                     model,
                     database,
                     null,
                     (tn, dif) => dif.Columns.Values.Select(c => c.ForeignKey != null ? SqlBuilder.AlterTableDropConstraint(dif.Name, c.ForeignKey.Name) : null)
                         .Concat(dif.MultiForeignKeys.Select(fk => SqlBuilder.AlterTableDropConstraint(dif.Name, fk.Name))).Combine(Spacing.Simple),
                     (tn, tab, dif) => SqlPreCommand.Combine(Spacing.Simple,
                         Synchronizer.SynchronizeScript(
                         tab.Columns,
                         dif.Columns,
                         null,
                         (cn, colDb) => colDb.ForeignKey != null ? SqlBuilder.AlterTableDropConstraint(dif.Name, colDb.ForeignKey.Name) : null,
                         (cn, colModel, colDb) => colDb.ForeignKey == null ? null :
                             colModel.ReferenceTable == null || colModel.AvoidForeignKey || !colModel.ReferenceTable.Name.Equals(ChangeName(colDb.ForeignKey.TargetTable)) ?
                             SqlBuilder.AlterTableDropConstraint(dif.Name, colDb.ForeignKey.Name) :
                             null, Spacing.Simple),
                        dif.MultiForeignKeys.Select(fk => SqlBuilder.AlterTableDropConstraint(dif.Name, fk.Name)).Combine(Spacing.Simple)),
                        Spacing.Double);

                SqlPreCommand tables =
                    Synchronizer.SynchronizeScript(
                    model,
                    database,
                    (tn, tab) => SqlBuilder.CreateTableSql(tab),
                    (tn, dif) => SqlBuilder.DropTable(dif.Name),
                    (tn, tab, dif) =>
                        SqlPreCommand.Combine(Spacing.Simple,
                        !object.Equals(dif.Name, tab.Name) ? SqlBuilder.RenameOrMove(dif, tab) : null,
                        Synchronizer.SynchronizeScript(
                            tab.Columns,
                            dif.Columns,
                            (cn, tabCol) => SqlPreCommandSimple.Combine(Spacing.Simple,
                                tabCol.PrimaryKey && dif.PrimaryKeyName != null ? SqlBuilder.DropPrimaryKeyConstraint(tab.Name) : null,
                                AlterTableAddColumnDefault(tab, tabCol, replacements)),
                            (cn, difCol) => SqlPreCommandSimple.Combine(Spacing.Simple,
                                 difCol.Default != null ? SqlBuilder.DropDefaultConstraint(tab.Name, difCol.Name) : null,
                                SqlBuilder.AlterTableDropColumn(tab, cn)),
                            (cn, tabCol, difCol) => SqlPreCommand.Combine(Spacing.Simple,
                                difCol.Name == tabCol.Name ? null : SqlBuilder.RenameColumn(tab, difCol.Name, tabCol.Name),
                                difCol.ColumnEquals(tabCol, ignorePrimaryKey: true) ? null : SqlPreCommand.Combine(Spacing.Simple,
                                    tabCol.PrimaryKey && !difCol.PrimaryKey && dif.PrimaryKeyName != null ? SqlBuilder.DropPrimaryKeyConstraint(tab.Name) : null,
                                    SqlBuilder.AlterTableAlterColumn(tab, tabCol)),
                                difCol.DefaultEquals(tabCol) ? null : SqlPreCommand.Combine(Spacing.Simple,
                                    difCol.Default != null ? SqlBuilder.DropDefaultConstraint(tab.Name, tabCol.Name) : null,
                                    tabCol.Default != null ? SqlBuilder.AddDefaultConstraint(tab.Name, tabCol.Name, tabCol.Default) : null),
                                UpdateByFkChange(tn, difCol, tabCol, ChangeName)),
                            Spacing.Simple)),
                     Spacing.Double);

                var tableReplacements = replacements.TryGetC(Replacements.KeyTables);
                if (tableReplacements != null)
                    replacements[Replacements.KeyTablesInverse] = tableReplacements.Inverse();

                SqlPreCommand syncEnums;

                try
                {
                    syncEnums = SynchronizeEnumsScript(replacements);
                }
                catch(Exception e)
                {
                    syncEnums = new SqlPreCommandSimple("-- Exception synchronizing enums: " + e.Message);
                }

                SqlPreCommand addForeingKeys = Synchronizer.SynchronizeScript(
                     model,
                     database,
                     (tn, tab) => SqlBuilder.AlterTableForeignKeys(tab),
                     null,
                     (tn, tab, dif) => Synchronizer.SynchronizeScript(
                         tab.Columns,
                         dif.Columns,
                         (cn, colModel) => colModel.ReferenceTable == null || colModel.AvoidForeignKey ? null :
                             SqlBuilder.AlterTableAddConstraintForeignKey(tab, colModel.Name, colModel.ReferenceTable),
                         null,
                         (cn, colModel, coldb) =>
                         {
                             if (colModel.ReferenceTable == null || colModel.AvoidForeignKey)
                                 return null;

                             if (coldb.ForeignKey == null || !colModel.ReferenceTable.Name.Equals(ChangeName(coldb.ForeignKey.TargetTable)))
                                 return SqlBuilder.AlterTableAddConstraintForeignKey(tab, colModel.Name, colModel.ReferenceTable);

                             var name = SqlBuilder.ForeignKeyName(tab.Name.Name, colModel.Name);
                             return SqlPreCommand.Combine(Spacing.Simple,
                                name != coldb.ForeignKey.Name.Name ? SqlBuilder.RenameForeignKey(coldb.ForeignKey.Name, name) : null,
                                (coldb.ForeignKey.IsDisabled || coldb.ForeignKey.IsNotTrusted) && !replacements.SchemaOnly ? SqlBuilder.EnableForeignKey(tab.Name, name) : null);
                         },
                         Spacing.Simple),
                     Spacing.Double);

                bool? createMissingFreeIndexes = null;

                SqlPreCommand addIndices =
                    Synchronizer.SynchronizeScript(model, database,
                     (tn, tab) => modelIndices[tab].Values.Select(SqlBuilder.CreateIndex).Combine(Spacing.Simple),
                     null,
                    (tn, tab, dif) =>
                    {
                        var columnReplacements = replacements.TryGetC(Replacements.KeyColumnsForTable(tn));

                        Func<IColumn, bool> isNew = c => !dif.Columns.ContainsKey(columnReplacements?.TryGetC(c.Name) ?? c.Name);

                        Dictionary<string, Index> modelIxs = modelIndices[tab];

                        var controlledIndexes = Synchronizer.SynchronizeScript(modelIxs, dif.Indices,
                            (i, mix) => mix is UniqueIndex || mix.Columns.Any(isNew) || SafeConsole.Ask(ref createMissingFreeIndexes, "Create missing non-unique index {0} in {1}?".FormatWith(mix.IndexName, tab.Name)) ? SqlBuilder.CreateIndex(mix) : null,
                            null,
                            (i, mix, dix) => (mix as UniqueIndex)?.ViewName != dix.ViewName || columnsChanged(dif, dix, mix) ? SqlBuilder.CreateIndex(mix) :
                                mix.IndexName != dix.IndexName ? SqlBuilder.RenameIndex(tab, dix.IndexName, mix.IndexName) : null,
                            Spacing.Simple);

                        return SqlPreCommand.Combine(Spacing.Simple, controlledIndexes);
                    }, Spacing.Double);

                SqlPreCommand dropSchemas = Synchronizer.SynchronizeScriptReplacing(replacements, "Schemas",
                  modelSchemas.ToDictionary(a => a.ToString()),
                  databaseSchemas.ToDictionary(a => a.ToString()),
                  null,
                  (_, oldSN) => DropSchema(oldSN) ? SqlBuilder.DropSchema(oldSN) : null,
                  (_, newSN, oldSN) => newSN.Equals(oldSN) ? null : SqlBuilder.DropSchema(oldSN),
                  Spacing.Double);

                return SqlPreCommand.Combine(Spacing.Triple, createSchemas, dropStatistics, dropIndices, dropForeignKeys, tables, syncEnums, addForeingKeys, addIndices, dropSchemas);
            }
        }

        private static HashSet<SchemaName> DefaultGetSchemas(List<DatabaseName> list)
        {
            HashSet<SchemaName> result = new HashSet<SchemaName>();
            foreach (var db in list)
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    var schemaNames = Database.View<SysSchemas>().Select(s => s.name).ToList().Except(SqlBuilder.SystemSchemas);

                    result.AddRange(schemaNames.Select(sn => new SchemaName(db, sn)));
                }
            }
            return result;
        }

        private static SqlPreCommand AlterTableAddColumnDefault(ITable table, IColumn column, Replacements rep)
        {
            bool temporalDefault = !column.Nullable && !column.Identity && column.Default == null;

            if (!temporalDefault)
                return SqlBuilder.AlterTableAddColumn(table, column);

            string defaultValue = SafeConsole.AskString("Default value for '{0}.{1}'? (or press enter) ".FormatWith(table.Name.Name, column.Name), stringValidator: str => null); ;
            if (defaultValue == "null")
                return SqlBuilder.AlterTableAddColumn(table, column);

            try
            {
                column.Default = defaultValue;

                if (column.Default.HasText() && SqlBuilder.IsString(column.SqlDbType) && !column.Default.Contains("'"))
                    column.Default = "'" + column.Default + "'";

                if (string.IsNullOrEmpty(column.Default))
                    column.Default = SqlBuilder.IsNumber(column.SqlDbType) ? "0" :
                        SqlBuilder.IsString(column.SqlDbType) ? "''" :
                        SqlBuilder.IsDate(column.SqlDbType) ? "GetDate()" :
                        "?";

                return SqlPreCommand.Combine(Spacing.Simple,
                    SqlBuilder.AlterTableAddColumn(table, column),
                    SqlBuilder.DropDefaultConstraint(table.Name, column.Name));
            }
            finally
            {
                column.Default = null;
            }
        }

        private static Dictionary<string, DiffIndex> ApplyIndexAutoReplacements(DiffTable diff, ITable tab, Dictionary<string, Index> dictionary)
        {
            List<string> oldOnly = diff.Indices.Keys.Where(n => !dictionary.ContainsKey(n)).ToList();
            List<string> newOnly = dictionary.Keys.Where(n => !diff.Indices.ContainsKey(n)).ToList();

            if (oldOnly.Count == 0 || newOnly.Count == 0)
                return diff.Indices;

            Dictionary<string, string> replacements = new Dictionary<string, string>();
            foreach (var o in oldOnly)
            {
                var oldIx = diff.Indices[o];

                var nIx = newOnly.FirstOrDefault(n =>
                {
                    var newIx = dictionary[n];

                    if (oldIx.IsUnique != (newIx is UniqueIndex))
                        return false;

                    if (oldIx.ViewName != null || (newIx is UniqueIndex) && ((UniqueIndex)newIx).ViewName != null)
                        return false;

                    var news = newIx.Columns.Select(c => diff.Columns.TryGetC(c.Name)?.Name).NotNull().ToHashSet();

                    if (!news.SetEquals(oldIx.Columns))
                        return false;

                    var uix = newIx as UniqueIndex;
                    if (uix != null && uix.Where != null && !oldIx.IndexName.EndsWith(StringHashEncoder.Codify(uix.Where)))
                        return false;

                    return true;
                });

                if (nIx != null)
                {
                    replacements.Add(o, nIx);
                    newOnly.Remove(nIx);
                }
            }

            if (replacements.IsEmpty())
                return diff.Indices;

            return diff.Indices.SelectDictionary(on => replacements.TryGetC(on) ?? on, dif => dif);
        }

        private static SqlPreCommandSimple UpdateByFkChange(string tn, DiffColumn difCol, IColumn tabCol, Func<ObjectName, ObjectName> changeName)
        {
            if (difCol.ForeignKey == null || tabCol.ReferenceTable == null || tabCol.AvoidForeignKey)
                return null;

            ObjectName oldFk = changeName(difCol.ForeignKey.TargetTable);

            if (oldFk.Equals(tabCol.ReferenceTable.Name))
                return null;

            AliasGenerator ag = new AliasGenerator();

            return new SqlPreCommandSimple(
@"UPDATE {2}
SET {0} =  -- get {5} id from {4}.Id
FROM {1} {2}
JOIN {3} {4} ON {2}.{0} = {4}.Id".FormatWith(tabCol.Name,
                tn, ag.NextTableAlias(tn),
                oldFk, ag.NextTableAlias(oldFk.Name),
                tabCol.ReferenceTable.Name.Name));
        }

        public static Dictionary<string, DiffTable> DefaultGetDatabaseDescription(List<DatabaseName> databases)
        {
            List<DiffTable> allTables = new List<DiffTable>();

            foreach (var db in databases)
            {
                using (Administrator.OverrideDatabaseInSysViews(db))
                {
                    var tables =
                        (from s in Database.View<SysSchemas>()
                         from t in s.Tables().Where(t => !t.ExtendedProperties().Any(a => a.name == "microsoft_database_tools_support")) //IntelliSense bug
                         select new DiffTable
                         {
                             Name = new ObjectName(new SchemaName(db, s.name), t.name),

                             PrimaryKeyName = (from k in t.KeyConstraints()
                                               where k.type == "PK"
                                               select k.name == null ? null : new ObjectName(new SchemaName(db, k.Schema().name), k.name))
                                               .SingleOrDefaultEx(),

                             Columns = (from c in t.Columns()
                                        join userType in Database.View<SysTypes>().DefaultIfEmpty() on c.user_type_id equals userType.user_type_id
                                        join sysType in Database.View<SysTypes>().DefaultIfEmpty() on c.system_type_id equals sysType.user_type_id
                                        join ctr in Database.View<SysDefaultConstraints>().DefaultIfEmpty() on c.default_object_id equals ctr.object_id
                                        select new DiffColumn
                                        {
                                            Name = c.name,
                                            SqlDbType = sysType == null ? SqlDbType.Udt : ToSqlDbType(sysType.name),
                                            UserTypeName = sysType == null ? userType.name : null,
                                            Nullable = c.is_nullable,
                                            Length = c.max_length,
                                            Precission = c.precision,
                                            Scale = c.scale,
                                            Identity = c.is_identity,
                                            Default = ctr.definition,
                                            PrimaryKey = t.Indices().Any(i => i.is_primary_key && i.IndexColumns().Any(ic => ic.column_id == c.column_id)),
                                        }).ToDictionary(a => a.Name, "columns"),

                             MultiForeignKeys = (from fk in t.ForeignKeys()
                                                 join rt in Database.View<SysTables>() on fk.referenced_object_id equals rt.object_id
                                                 select new DiffForeignKey
                                                 {
                                                     Name = new ObjectName(new SchemaName(db, fk.Schema().name), fk.name),
                                                     IsDisabled = fk.is_disabled,
                                                     TargetTable = new ObjectName(new SchemaName(db, rt.Schema().name), rt.name),
                                                     Columns = fk.ForeignKeyColumns().Select(fkc => new DiffForeignKeyColumn
                                                     {
                                                         Parent = t.Columns().Single(c => c.column_id == fkc.parent_column_id).name,
                                                         Referenced = rt.Columns().Single(c => c.column_id == fkc.referenced_column_id).name
                                                     }).ToList(),
                                                 }).ToList(),

                             SimpleIndices = (from i in t.Indices()
                                              where !i.is_primary_key && i.type != 0  /*heap indexes*/
                                              select new DiffIndex
                                              {
                                                  IsUnique = i.is_unique,
                                                  IndexName = i.name,
                                                  FilterDefinition = i.filter_definition,
                                                  Type = (DiffIndexType)i.type,
                                                  Columns = (from ic in i.IndexColumns()
                                                             join c in t.Columns() on ic.column_id equals c.column_id
                                                             select c.name).ToList()
                                              }).ToList(),

                             ViewIndices = (from v in Database.View<SysViews>()
                                            where v.name.StartsWith("VIX_" + t.name + "_")
                                            from i in v.Indices()
                                            select new DiffIndex
                                            {
                                                IsUnique = i.is_unique,
                                                ViewName = v.name,
                                                IndexName = i.name,
                                                Columns = (from ic in i.IndexColumns()
                                                           join c in v.Columns() on ic.column_id equals c.column_id
                                                           select c.name).ToList()

                                            }).ToList(),

                             Stats = (from st in t.Stats()
                                      where st.user_created
                                      select new DiffStats
                                      {
                                          StatsName = st.name,
                                          Columns = (from ic in st.StatsColumns()
                                                     join c in t.Columns() on ic.column_id equals c.column_id
                                                     select c.name).ToList()
                                      }).ToList(),

                         }).ToList();

                    tables.ForEach(t => t.ForeignKeysToColumns());

                    allTables.AddRange(tables);
                }
            }

            var database = allTables.ToDictionary(t => t.Name.ToString());

            return database;
        }


        public static SqlDbType ToSqlDbType(string str)
        {
            if (str == "numeric")
                return SqlDbType.Decimal;

            return str.ToEnum<SqlDbType>(true);
        }


        static SqlPreCommand SynchronizeEnumsScript(Replacements replacements)
        {
            Schema schema = Schema.Current;

            List<SqlPreCommand> commands = new List<SqlPreCommand>();

            foreach (var table in schema.Tables.Values)
            {
                Type enumType = EnumEntity.Extract(table.Type);
                if (enumType != null)
                {
                    IEnumerable<Entity> should = EnumEntity.GetEntities(enumType);
                    Dictionary<string, Entity> shouldByName = should.ToDictionary(a => a.ToString());

                    List<Entity> current = Administrator.TryRetrieveAll(table.Type, replacements);
                    Dictionary<string, Entity> currentByName = current.ToDictionary(a => a.toStr, table.Name.Name);

                    string key = Replacements.KeyEnumsForTable(table.Name.Name);

                    replacements.AskForReplacements(currentByName.Keys.ToHashSet(), shouldByName.Keys.ToHashSet(), key);

                    currentByName = replacements.ApplyReplacementsToOld(currentByName, key);

                    var mix = shouldByName.JoinDictionary(currentByName, (n, s, c) => new { s, c }).Where(a => a.Value.s.id != a.Value.c.id).ToDictionary();

                    HashSet<PrimaryKey> usedIds = current.Select(a => a.Id).ToHashSet();

                    Dictionary<string, Entity> middleByName = mix.Where(kvp => usedIds.Contains(kvp.Value.s.Id)).ToDictionary(kvp => kvp.Key, kvp => Clone(kvp.Value.c));

                    if (middleByName.Any())
                    {
                        var moveToAux = SyncEnums(schema, table, 
                            currentByName.Where(a => middleByName.ContainsKey(a.Key)).ToDictionary(), 
                            middleByName);
                        if (moveToAux != null)
                            commands.Add(moveToAux);
                    }

                    var com = SyncEnums(schema, table, 
                        currentByName.Where(a=>!middleByName.ContainsKey(a.Key)).ToDictionary(), 
                        shouldByName.Where(a=>!middleByName.ContainsKey(a.Key)).ToDictionary());
                    if (com != null)
                        commands.Add(com);

                    if (middleByName.Any())
                    {
                        var backFromAux = SyncEnums(schema, table, 
                            middleByName,
                            shouldByName.Where(a => middleByName.ContainsKey(a.Key)).ToDictionary());
                        if (backFromAux != null)
                            commands.Add(backFromAux);
                    }
                }
            }

            return SqlPreCommand.Combine(Spacing.Double, commands.ToArray());
        }

        private static SqlPreCommand SyncEnums(Schema schema, Table table, Dictionary<string, Entity> current, Dictionary<string, Entity> should)
        {
            var deletes = Synchronizer.SynchronizeScript(
                       should,
                       current,
                       null,
                       (str, c) => table.DeleteSqlSync(c, comment: c.toStr),
                       null, Spacing.Double);

            var moves = Synchronizer.SynchronizeScript(
                       should,
                       current,
                       null,
                       null,
                       (str, s, c) =>
                       {
                           if (s.id == c.id)
                               return table.UpdateSqlSync(c, comment: c.toStr);

                           var insert = table.InsertSqlSync(s);

                           var move = (from t in schema.GetDatabaseTables()
                                       from col in t.Columns.Values
                                       where col.ReferenceTable == table
                                       select new SqlPreCommandSimple("UPDATE {0} SET {1} = {2} WHERE {1} = {3} -- {4} re-indexed"
                                           .FormatWith(t.Name, col.Name, s.Id, c.Id, c.toStr)))
                                        .Combine(Spacing.Simple);

                           var delete = table.DeleteSqlSync(c, comment: c.toStr);

                           return SqlPreCommand.Combine(Spacing.Simple, insert, move, delete);
                       }, Spacing.Double);

            var creates = Synchronizer.SynchronizeScript(
                   should,
                   current,
                  (str, s) => table.InsertSqlSync(s),
                   null,
                   null, Spacing.Double);

            return SqlPreCommand.Combine(Spacing.Double, deletes, moves, creates);
        }

        private static Entity Clone(Entity current)
        {
            var instance = (Entity)Activator.CreateInstance(current.GetType());
            instance.toStr = current.toStr;
            instance.id = (int)current.id.Value + 1000000;
            return instance;
        }

        public static SqlPreCommand SnapshotIsolation(Replacements replacements)
        {
            if (replacements.SchemaOnly)
                return null;

            var list = Schema.Current.DatabaseNames().Select(a => a?.ToString()).ToList();

            if (list.Contains(null))
            {
                list.Remove(null);
                list.Add(Connector.Current.DatabaseName());
            }

            var results = Database.View<SysDatabases>()
                .Where(d => list.Contains(d.name))
                .Select(d => new { d.name, d.snapshot_isolation_state, d.is_read_committed_snapshot_on }).ToList();

            var cmd = replacements.WithReplacedDatabaseName().Using(_ => results.Select((a, i) =>
                SqlPreCommand.Combine(Spacing.Simple,
                !a.snapshot_isolation_state || !a.is_read_committed_snapshot_on ? DisconnectUsers(a.name, "SPID" + i) : null,
                !a.snapshot_isolation_state ? SqlBuilder.SetSnapshotIsolation(a.name, true) : null,
                !a.is_read_committed_snapshot_on ? SqlBuilder.MakeSnapshotIsolationDefault(a.name, true) : null)).Combine(Spacing.Double));

            if (cmd == null)
                return null;

            return SqlPreCommand.Combine(Spacing.Double,
                new SqlPreCommandSimple("use master -- Start Snapshot"),
                cmd,
                new SqlPreCommandSimple("use {0} -- Stop Snapshot".FormatWith(Connector.Current.DatabaseName())));
        }

        public static SqlPreCommandSimple DisconnectUsers(string databaseName, string variableName)
        {
            return new SqlPreCommandSimple(@"DECLARE @{1} VARCHAR(7000)
SELECT @{1} = COALESCE(@{1},'')+'KILL '+CAST(SPID AS VARCHAR)+'; 'FROM master..SysProcesses WHERE DB_NAME(DBId) = '{0}'
EXEC(@{1})".FormatWith(databaseName, variableName));
        }
    }

    public class DiffTable
    {
        public ObjectName Name;

        public ObjectName PrimaryKeyName;

        public Dictionary<string, DiffColumn> Columns;

        public List<DiffIndex> SimpleIndices
        {
            get { return Indices.Values.ToList(); }
            set { Indices.AddRange(value, a => a.IndexName, a => a); }
        }

        public List<DiffIndex> ViewIndices
        {
            get { return Indices.Values.ToList(); }
            set { Indices.AddRange(value, a => a.IndexName, a => a); }
        }

        public Dictionary<string, DiffIndex> Indices = new Dictionary<string, DiffIndex>();

        public List<DiffStats> Stats = new List<DiffStats>();

        public List<DiffForeignKey> MultiForeignKeys = new List<DiffForeignKey>();

        public void ForeignKeysToColumns()
        {
            foreach (var fk in MultiForeignKeys.Where(a => a.Columns.Count == 1).ToList())
            {
                this.Columns[fk.Columns.SingleEx().Parent].ForeignKey = fk;
                MultiForeignKeys.Remove(fk);
            }
        }

        public override string ToString()
        {
            return Name.ToString();
        }
    }

    public class DiffStats
    {
        public string StatsName;

        public List<string> Columns;
    }

    public class DiffIndex
    {
        public bool IsUnique;
        public string IndexName;
        public string ViewName;
        public string FilterDefinition;
        public DiffIndexType? Type;

        public List<string> Columns;

        public override string ToString()
        {
            return "{0} ({1})".FormatWith(IndexName, Columns.ToString(", "));
        }

        public bool IsControlledIndex
        {
            get { return IndexName.StartsWith("IX_") || IndexName.StartsWith("UIX_"); }
        }
    }

    public enum DiffIndexType
    {
        Heap = 0,
        Clustered = 1,
        NonClustered = 2,
        Xml = 3, 
        Spatial = 4,
        ClusteredColumnstore = 5,
        NonClusteredColumnstore = 6,
        NonClusteredHash = 7,
    }

    public class DiffColumn
    {
        public string Name;
        public SqlDbType SqlDbType;
        public string UserTypeName;
        public bool Nullable;
        public int Length;
        public int Precission;
        public int Scale;
        public bool Identity;
        public bool PrimaryKey;

        public DiffForeignKey ForeignKey;

        public string Default;

        public bool ColumnEquals(IColumn other, bool ignorePrimaryKey)
        {
            var result =
                   SqlDbType == other.SqlDbType
                && StringComparer.InvariantCultureIgnoreCase.Equals(UserTypeName, other.UserDefinedTypeName)
                && Nullable == other.Nullable
                && (other.Size == null || other.Size.Value == Precission || other.Size.Value == Length / BytesPerChar(other.SqlDbType) || other.Size.Value == int.MaxValue && Length == -1)
                && (other.Scale == null || other.Scale.Value == Scale)
                && Identity == other.Identity
                && (ignorePrimaryKey || PrimaryKey == other.PrimaryKey);

            return result;
        }

        public static int BytesPerChar(System.Data.SqlDbType sqlDbType)
        {
            if (sqlDbType == System.Data.SqlDbType.NChar || sqlDbType == System.Data.SqlDbType.NText || sqlDbType == System.Data.SqlDbType.NVarChar)
                return 2;

            return 1;
        }

        public bool DefaultEquals(IColumn other)
        {
            if (other.Default == null && this.Default == null)
                return true;

            var result = CleanParenthesis(this.Default) == CleanParenthesis(other.Default);

            return result;
        }

        private string CleanParenthesis(string p)
        {
            if (p == null)
                return null;

            while (
                p.StartsWith("(") && p.EndsWith(")") || 
                p.StartsWith("'") && p.EndsWith("'"))
                p = p.Substring(1, p.Length - 2);

            return p.ToLower();
        }

        public DiffColumn Clone()
        {
            return new DiffColumn
            {
                Name = Name,
                ForeignKey = ForeignKey,
                Default = Default,
                Identity = Identity,
                Length = Length,
                PrimaryKey = PrimaryKey,
                Nullable = Nullable,
                Precission = Precission,
                Scale = Scale,
                SqlDbType = SqlDbType,
                UserTypeName = UserTypeName,
            };
        }
    }

    public class DiffForeignKey
    {
        public ObjectName Name;
        public ObjectName TargetTable;
        public bool IsDisabled;
        public bool IsNotTrusted;
        public List<DiffForeignKeyColumn> Columns;
    }

    public class DiffForeignKeyColumn
    {
        public string Parent;
        public string Referenced;
    }
}
