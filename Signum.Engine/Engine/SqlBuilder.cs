using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Signum.Utilities;
using System.Data.SqlClient;
using Signum.Engine;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Engine.Properties;
using Signum.Engine.Maps;
using System.Globalization;


namespace Signum.Engine
{


    internal static class SqlBuilder
    {
        public readonly static SqlDbType PrimaryKeyType = SqlDbType.Int;
        public readonly static string PrimaryKeyName = "Id";

        public readonly static SqlDbType TicksType = SqlDbType.BigInt;
        public readonly static string TicksName = "Ticks";

        public readonly static SqlDbType ToStrType = SqlDbType.NVarChar;
        public readonly static string ToStrName = "ToStr";

        public readonly static int MaxParametersInSQL = 2000;

        public static SqlPreCommand CreateTable(string table, List<string> campos)
        {
            return new SqlPreCommandSimple("CREATE TABLE {0}(\r\n{1}\r\n)".Formato(table.SqlScape(), campos.ToString(",\r\n").Indent(2)));
        }

        internal static SqlPreCommand UpdateSetIdEntity(string table, List<SqlParameter> parameters, int id, long ticks)
        {
            SqlParameter ticksParam = SqlParameterBuilder.CreateParameter("ticks", SqlDbType.BigInt, false, ticks);

            return SqlPreCommand.Combine(Spacing.Simple,
                RestoreLastId(id),
                new SqlPreCommandSimple(
                    "UPDATE {0} SET \r\n{1}\r\n WHERE id = @LastEntityID AND ticks = {2}".Formato(table.SqlScape(),
                        parameters.ToString(p => "{0} = {1}".Formato(p.SourceColumn.SqlScape(), p.ParameterName).Indent(2), ",\r\n"),
                        ticksParam.ParameterName), parameters.And(ticksParam).ToList()),
                new SqlPreCommandSimple("IF @@ROWCOUNT=0\r\nRAISERROR('{0}', 16, 1)".Formato(
                        Resources.ConcurrencyErrorOnDatabaseTable0Id1.Formato(table, id)))
                    );
        }

        internal static SqlPreCommand UpdateSetId(string table, List<SqlParameter> parameters, int id)
        {
            return SqlPreCommand.Combine(Spacing.Simple,
                RestoreLastId(id),
                new SqlPreCommandSimple(
                    "UPDATE {0} SET \r\n{1}\r\n WHERE id = @LastEntityID".Formato(table.SqlScape(),
                    parameters.ToString(p => "{0} = {1}".Formato(p.SourceColumn.SqlScape(), p.ParameterName).Indent(2), ",\r\n")),
                    parameters.ToList()));
        }

        internal static SqlPreCommand UpdateId(string table, List<SqlParameter> parameters, int id, string oldToStr)
        {
            SqlParameter paramId = SqlParameterBuilder.CreateReferenceParameter(SqlBuilder.PrimaryKeyName, false, id);

            return new SqlPreCommandSimple(
                    "UPDATE {0} SET --{1}\r\n{2}\r\n WHERE id = {3}".Formato(
                        table.SqlScape(), oldToStr,
                        parameters.ToString(p => "{0} = {1}".Formato(p.SourceColumn.SqlScape(), p.ParameterName).Indent(2), ",\r\n"),
                        paramId.ParameterName),
                    parameters.And(paramId).ToList());
        }

        internal static SqlPreCommand Insert(string table, List<SqlParameter> parameters)
        {
            return new SqlPreCommandSimple("INSERT {0} ({1}) \r\n VALUES ({2})".Formato(table,
                parameters.ToString(p => p.SourceColumn.SqlScape(), ", "),
                parameters.ToString(p => p.ParameterName, ", ")), parameters);
        }

        internal static SqlPreCommand InsertSaveId(string table, List<SqlParameter> parameters, IdentifiableEntity entityToUpdate)
        {
            return SqlPreCommand.Combine(Spacing.Simple,
                new SqlPreCommandSimple("INSERT INTO {0} ({1})\r\n OUTPUT INSERTED.id INTO @MyIdTable\r\n VALUES ({2})".Formato(table.SqlScape(),
                       parameters.ToString(p => p.SourceColumn.SqlScape(), ", "),
                       parameters.ToString(p => p.ParameterName, ", ")), parameters) { EntityToUpdate = entityToUpdate },
                new SqlPreCommandSimple("SET @LastEntityID = SCOPE_IDENTITY() "));
        }

        internal static SqlPreCommand RestoreLastId(int id)
        {
            SqlParameter pid = SqlParameterBuilder.CreateReferenceParameter(SqlBuilder.PrimaryKeyName, false, id);

            return new SqlPreCommandSimple("SET @LastEntityID = {0}".Formato(pid.ParameterName), new List<SqlParameter> { pid });
        }

        internal static SqlPreCommand DeleteSql(string table, SqlParameter backId)
        {
            return new SqlPreCommandSimple("DELETE {0} WHERE id = {1}".Formato(table.SqlScape(), backId.ParameterName), new List<SqlParameter> { backId });
        }

        internal static SqlPreCommand DeleteSql(string table, List<SqlParameter> backIds)
        {
            return new SqlPreCommandSimple("DELETE {0} WHERE id IN ({1})".Formato(table.SqlScape(), backIds.ToString(p => p.ParameterName, ", ")), backIds);
        }

        internal static SqlPreCommand DeleteSql(string table, int id, string oldToStr)
        {
            return new SqlPreCommandSimple("DELETE {0} WHERE id = {1} --{2}".Formato(table.SqlScape(), id, oldToStr));
        }

        internal static SqlPreCommand RelationalDelete(string table, string backIdColumn, SqlParameter backId)
        {
            return new SqlPreCommandSimple("DELETE {0} WHERE {1} = {2}".Formato(table.SqlScape(), backIdColumn, backId.ParameterName));
        }

        internal static SqlPreCommand RelationalDelete(string table, string backIdColumn, List<SqlParameter> backIds)
        {
            return new SqlPreCommandSimple("DELETE {0} WHERE {1} IN ({2})".Formato(table.SqlScape(), backIdColumn, backIds.ToString(p => p.ParameterName, ", ")));
        }

        internal static SqlPreCommand DeclareIDsMemoryTable()
        {
            return new SqlPreCommandSimple("DECLARE @MyIdTable table(id int)");
        }

        internal static SqlPreCommandSimple DeclareLastEntityID()
        {
            return new SqlPreCommandSimple("DECLARE @LastEntityID int");
        }

        internal static SqlPreCommand SelectIDMemoryTable()
        {
            return new SqlPreCommandSimple("SELECT id FROM @MyIdTable as InsertedIdTable");
        }

        internal static SqlPreCommandSimple SelectLastEntityID()
        {
            return new SqlPreCommandSimple("SELECT @LastEntityID as LastID");
        }

        internal static SqlPreCommand RelationalDeleteScope(string table, string backIdColumn)
        {
            return new SqlPreCommandSimple("DELETE {0} WHERE {1} = @LastEntityID".Formato(table.SqlScape(), backIdColumn));
        }

        internal static SqlPreCommand RelationalInsertScope(string table, string backIdColumn, List<SqlParameter> campoParameters)
        {
            return new SqlPreCommandSimple("INSERT INTO {0} ({1}, {2}) VALUES ( @LastEntityID, {3})".Formato(table.SqlScape(), backIdColumn.SqlScape(),
                campoParameters.ToString(p => p.SourceColumn.SqlScape(), ", "),
                campoParameters.ToString(p => p.ParameterName, ", ")), campoParameters);
        }

        internal static SqlPreCommand SelectByIds(string table, string[] columns, string column, int[] ids)
        {
            List<SqlParameter> parameters = ids.Select(id => SqlParameterBuilder.CreateReferenceParameter("val", false, id)).ToList();

            return new SqlPreCommandSimple("SELECT {0} FROM {1} WHERE {2} IN ({3})".Formato(
            columns.ToString(a => a.SqlScape(), ", "), table.SqlScape(), column.SqlScape(),
            parameters.ToString(p => p.ParameterName, ", ")), parameters);
        }

        internal static SqlPreCommand SelectCount(string table, int id)
        {
            SqlParameter idParam = SqlParameterBuilder.CreateReferenceParameter("id", false, id);

            return new SqlPreCommandSimple("SELECT COUNT(id) FROM {0} WHERE  id = {1}".Formato(table.SqlScape(), idParam.ParameterName), new List<SqlParameter> { idParam });
        }

        internal static SqlPreCommandSimple SelectAll(string table, string[] columns)
        {
            return new SqlPreCommandSimple("SELECT {0} FROM {1}".Formato(
            columns.ToString(a => a.SqlScape(), ", "),
            table.SqlScape()));
        }

        #region Create Tables

        public static SqlPreCommand CreateTableSql(ITable t)
        {
            return CreateTable(t.Name, t.Columns.Values.Select(c => SqlBuilder.CreateField(c)).ToList());
        }

        internal static SqlPreCommand DropTable(string table)
        {
            return new SqlPreCommandSimple("DROP TABLE {0}".Formato(table.SqlScape()));
        }

        internal static SqlPreCommand AlterTableDropColumn(string table, string columnName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP COLUMN {1}".Formato(table.SqlScape(), columnName.SqlScape()));
        }

        internal static SqlPreCommand AlterTableAddColumn(string table, IColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD {1} -- DEFAULT( )".Formato(table, CreateField(column)));
        }

        internal static SqlPreCommand AlterTableAlterColumn(string table, IColumn column)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ALTER COLUMN {1}".Formato(table.SqlScape(), CreateField(column)));
        }

        public static string CreateField(IColumn c)
        {
            return SqlBuilder.CreateField(c.Name, c.SqlDbType, c.Size, c.Scale, c.Nullable, c.PrimaryKey, c.Identity);
        }

        public static string CreatePrimaryKeyField(bool identity)
        {
            return CreateField(PrimaryKeyName, PrimaryKeyType, null, null, false, true, identity);
        }

        public static string CreateReferenceField(string name, bool nullable)
        {
            return CreateField(name, PrimaryKeyType, null, null, nullable, false, false);
        }

        public static string CreateField(string name, SqlDbType type, int? size, int? scale, bool nullable, bool primaryKey, bool identity)
        {
            return "{0} {1}{2} {3}{4}{5}".Formato(
                name.SqlScape(),
                type.ToString().ToUpper(CultureInfo.InvariantCulture),
                GetSizeScale(size, scale),
                identity ? "IDENTITY " : "",
                nullable ? "NULL" : "NOT NULL",
                primaryKey ? " PRIMARY KEY" : "");
        }

        private static string GetSizeScale(int? size, int? scale)
        {
            if (size == null)
                return "";

            if (size == int.MaxValue)
                return "(MAX)";

            if (scale == null)
                return "({0})".Formato(size);

            return "({0},{1})".Formato(size, scale);
        }

        public static SqlPreCommand AlterTableForeignKeys(ITable t)
        {
            return t.Columns.Values.Select(c =>
                c.ReferenceTable == null ? null : SqlBuilder.AlterTableAddForeignKey(t.Name, c.Name, c.ReferenceTable.Name)).Combine(Spacing.Simple);
        }

        public static SqlPreCommand CreateIndicesSql(ITable t)
        {
            return t.Columns.Values.Select(c => SqlBuilder.CreateIndex(c.Index, t.Name, c.Name)).Combine(Spacing.Simple);
        }

        internal static SqlPreCommand DropIndex(string table, string indexName)
        {
            return new SqlPreCommandSimple("DROP INDEX {0}.{1}".Formato(table.SqlScape(), indexName.SqlScape()));
        }

        public static SqlPreCommand CreateIndex(Index index, string table, params string[] fieldNames)
        {
            if (index == Index.None)
                return null;


            if (index == Index.Multiple || index == Index.Unique)
            {
                return new SqlPreCommandSimple("CREATE {0}INDEX {1} ON {2}({3})".Formato(
                    index == Index.Unique ? "UNIQUE " : "",
                    IndexName(table, fieldNames),
                    table.SqlScape(),
                    fieldNames.ToString(a => a.SqlScape(), ", ")));
            }

            if (index == Index.UniqueMultiNulls)
            {
                string field = fieldNames.Single("UniqueMultiNulls works with one field only. Use Administrator.AddMultiColumnUniqueTriggerNullable instead.");

                string triggerName = "v_{0}_{1}".Formato(table, fieldNames.ToString("_"));

                //                return new SqlPreCommandSimple(
                //@"CREATE  trigger {0} on {1} for insert, update as 
                //BEGIN  
                //    IF (select max(cnt) from 
                //            (select count(i.{2}) as cnt from {1}, inserted i where {1}.{2}=i.{2} group by i.{2}) x) > 1 
                //    raiserror('{3}',16,1) 
                //END".Formato(triggerName.SqlScape(), table.SqlScape(), fieldNames.Single().SqlScape(), Resources._0RepeatedOnTable1.Formato(fieldNames.Single(), table)));



                string sql1 = @" CREATE VIEW {0} WITH SCHEMABINDING
                                AS
                                SELECT {2}
                                FROM dbo.{1}
                                WHERE {2} IS NOT NULL;
                                ".Formato(triggerName.SqlScape(), table.SqlScape(), fieldNames.Single().SqlScape());

                string sql2 = @"CREATE UNIQUE CLUSTERED INDEX  UT_{0} ON {0}({2});
                                ".Formato(triggerName.SqlScape(), table.SqlScape(), fieldNames.Single().SqlScape());

                System.Diagnostics.Debug.WriteLine(sql1);
                System.Diagnostics.Debug.WriteLine(sql2);

               return SqlPreCommand.Combine(Spacing.Simple, new SqlPreCommandSimple(sql1),new SqlPreCommandSimple(sql2)   );
         

              



            }

            return null;
        }

        public static SqlPreCommand CreateMultiColumnUniqueTriggerNullable(string table, string[] nullableFields,
            string[] notNullableFields)
        {
            if (nullableFields == null)
                nullableFields = new string[0];
            if (notNullableFields == null)
                notNullableFields = new string[0];

            if (nullableFields.Count() == 0)
            {
                throw new ArgumentNullException("At least one nullable field must be passed");
            }

            string tableName = table.SqlScape();

            IEnumerable<string> allCols = nullableFields.Union(notNullableFields);
            if (allCols.Count() < 2)
            {
                throw new ArgumentNullException("There must be more than one field for the MultiColumn trigger");
            }

            string triggerName = "UT_{0}_{1}".Formato(tableName, allCols.Select(c => c.SqlScape()).ToString("_"));

            string columns = allCols.ToString(c => "i.{0} = p.{0}".Formato(c.SqlScape()), " AND ");

            string nullableColumns = nullableFields.ToString(c =>
                "i.{0} IS NOT NULL ".Formato(c.SqlScape()), " AND ");

            string trigger =
@"CREATE TRIGGER {0}
   ON  {1}
   AFTER INSERT
AS 
BEGIN
    SET NOCOUNT ON

    IF EXISTS(SELECT 1
        FROM {1} p
            JOIN INSERTED i On {2}
        WHERE {3} 
    ) 
    BEGIN
        RAISERROR ('{4}', 16, 1)
        ROLLBACK TRANSACTION
    END 
END".Formato(triggerName.SqlScape(), tableName, columns, nullableColumns,
   Resources.CannotInsertDuplicatedFields0On1Table.Formato(allCols.ToString(c => c.SqlScape(), ", "), tableName));

            return new SqlPreCommandSimple(trigger);
        }

        public static string IndexName(string table, params string[] fieldNames)
        {
            return "IX_{0}_{1}".Formato(table, fieldNames.ToString("_")).SqlScape();
        }

        public static SqlPreCommand AlterTableDropForeignKey(string table, string foreingKeyName)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} DROP CONSTRAINT {1} ".Formato(
                table.SqlScape(),
                foreingKeyName.SqlScape()));
        }

        public static SqlPreCommand AlterTableAddForeignKey(string table, string fieldName, string foreignTable)
        {
            return new SqlPreCommandSimple("ALTER TABLE {0} ADD CONSTRAINT {1} FOREIGN KEY ({2}) REFERENCES {3}({4})".Formato(
                table.SqlScape(),
                ForeignKeyName(table, fieldName),
                fieldName.SqlScape(),
                foreignTable.SqlScape(),
                PrimaryKeyName.SqlScape()));
        }

        public static string ForeignKeyName(string table, string fieldName)
        {
            return "FK_{0}_{1}".Formato(table, fieldName).SqlScape();
        }

        internal static SqlPreCommand RenameTable(string oldName, string newName)
        {
            return new SqlPreCommandSimple("EXEC SP_RENAME '{0}' , '{1}'".Formato(oldName, newName));
        }

        internal static SqlPreCommand RenameColumn(string tableName, string oldName, string newName)
        {
            return new SqlPreCommandSimple("EXEC SP_RENAME '{0}.{1}' , '{2}', 'COLUMN' ".Formato(tableName, oldName, newName));
        }
        #endregion

        internal static SqlPreCommandSimple SetIdentityInsert(string table, bool value)
        {
            return new SqlPreCommandSimple("SET IDENTITY_INSERT {0} {1}".Formato(
                table.SqlScape(), value ? "ON" : "OFF"));
        }

        internal static SqlPreCommand ShrinkDatabase(string schemaName)
        {
            return  
                new[]{
                    ConnectionScope.Current.DBMS == DBMS.SqlServer2005 ?  
                        new SqlPreCommandSimple("BACKUP LOG {0} WITH TRUNCATE_ONLY".Formato(schemaName)):
                        new []
                        {
                            new SqlPreCommandSimple("ALTER DATABASE {0} SET RECOVERY SIMPLE WITH NO_WAIT".Formato(schemaName)),
                            new[]{
                                new SqlPreCommandSimple("DECLARE @fileID BIGINT"),
                                new SqlPreCommandSimple("SET @fileID = (SELECT FILE_IDEX((SELECT TOP(1)name FROM sys.database_files WHERE type = 1)))"),
                                new SqlPreCommandSimple("DBCC SHRINKFILE(@fileID, 1)"),
                            }.Combine(Spacing.Simple).ToSimple(),
                            new SqlPreCommandSimple("ALTER DATABASE {0} SET RECOVERY FULL WITH NO_WAIT".Formato(schemaName)),                  
                        }.Combine(Spacing.Simple),
                    new SqlPreCommandSimple("DBCC SHRINKDATABASE ( {0} , TRUNCATEONLY )".Formato(schemaName))
                }.Combine(Spacing.Simple); 
            
        }

        internal static SqlPreCommandSimple SetSnapshotIsolation(string schemaName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET ALLOW_SNAPSHOT_ISOLATION {1}".Formato(schemaName, value ? "ON" : "OFF"));
        }

        internal static SqlPreCommandSimple MakeSnapshotIsolationDefault(string schemaName, bool value)
        {
            return new SqlPreCommandSimple("ALTER DATABASE {0} SET READ_COMMITTED_SNAPSHOT {1}".Formato(schemaName, value ? "ON" : "OFF"));
        }

        internal static SqlPreCommandSimple SelectRowCount()
        {
            return new SqlPreCommandSimple("select @@rowcount;"); 
        }
    }
}
