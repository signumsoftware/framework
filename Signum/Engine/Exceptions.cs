using System.Text.RegularExpressions;
using Signum.Engine.Maps;
using System.Collections.Concurrent;
using Signum.Utilities.Reflection;
using Npgsql;
using System.Globalization;

namespace Signum.Engine;

public class UniqueKeyException : ApplicationException
{
    public string? TableName { get; private set; }
    public ITable? Table { get; private set; }

    public string? IndexName { get; private set; }
    public TableIndex? Index { get; private set; }
    public List<PropertyInfo>? Properties { get; private set; }

    public string? Values { get; private set; }
    public object?[]? HumanValues { get; private set; }

    static Regex[] regexes = new[]
    {
            new Regex(@"Cannot insert duplicate key row in object '(?<table>.*)' with unique index '(?<index>.*)'\. The duplicate key value is \((?<value>.*)\)"),
            new Regex(@"Eine Zeile mit doppeltem Schlüssel kann in das Objekt ""(?<table>.*)"" mit dem eindeutigen Index ""(?<index>.*)"" nicht eingefügt werden. Der doppelte Schlüsselwert ist \((?<value>.*)\)")
    };


    public UniqueKeyException(Exception inner) : base(null, inner)
    {
        if (inner is PostgresException pg)
        {
            TableName = new ObjectName(new SchemaName(null, pg.SchemaName!, true), pg.TableName!, true).ToString();
            IndexName = pg.ConstraintName;
            Table = FindTable(TableName);
            if(Table != null && IndexName != null)
            {
                (TableIndex index, List<PropertyInfo> properties)? tuple = GetIndexInfo(Table, IndexName);
                if(tuple != null)
                {
                    Index = tuple.Value.index;
                    Properties = tuple.Value.properties;
                }
            }
        }
        else
        {
            foreach (var rx in regexes)
            {
                Match m = rx.Match(inner.Message);
                if (m.Success)
                {
                    TableName = m.Groups["table"].Value;
                    IndexName = m.Groups["index"].Value;
                    Values = m.Groups["value"].Value;
                    Table = FindTable(TableName);

                    if (Table != null && IndexName != null)
                    {
                        (TableIndex index, List<PropertyInfo> properties)? tuple = GetIndexInfo(Table, IndexName);

                        if (tuple != null)
                        {
                            Index = tuple.Value.index;
                            Properties = tuple.Value.properties;

                            try
                            {
                                var values = Values.Split(", ");

                                if (values.Length == Index.Columns.Length)
                                {
                                    var colValues = Index.Columns.Zip(values).ToDictionary(a => a.First, a => a.Second == "<NULL>" ? null : a.Second.Trim().Trim('\''));

                                    HumanValues = Properties.Select(p =>
                                    {
                                        var f = 
                                        Table is Table t ? t.GetField(p) : 
                                        Table is TableMList tm ? tm.GetField(p) : 
                                        throw new NotImplementedException();
                                        if (f is FieldValue fv)
                                            return colValues.GetOrThrow(fv);

                                        if (f is FieldEnum fe)
                                            return colValues.GetOrThrow(fe)?.Let(a => ReflectionTools.ChangeType(a, fe.Type));

                                        if (f is FieldReference fr)
                                        {
                                            var id = colValues.GetOrThrow(fr);
                                            if (id == null)
                                                return null;

                                            var type = fr.FieldType.CleanType();

                                            return Database.RetrieveLite(type, PrimaryKey.Parse(id, type));
                                        }

                                        if (f is FieldImplementedBy ib)
                                        {
                                            var imp = ib.ImplementationColumns.SingleOrDefault(ic => colValues.TryGetCN(ic.Value) != null);
                                            if (imp.Key == null)
                                                return null;

                                            return Database.RetrieveLite(imp.Key, PrimaryKey.Parse(colValues.GetOrThrow(imp.Value)!, imp.Key));
                                        }

                                        if (f is FieldImplementedByAll iba)
                                        {
                                            var typeId = colValues.GetOrThrow(iba.TypeColumn);
                                            if (typeId == null)
                                                return null;

                                            var type = TypeLogic.IdToType.GetOrThrow(PrimaryKey.Parse(typeId, typeof(TypeEntity)));

                                            var id = iba.IdColumns.Values.Select(c => colValues.GetOrThrow(c)).NotNull().Single();

                                            return Database.RetrieveLite(type, PrimaryKey.Parse(id, type));
                                        }
                                        throw new UnexpectedValueException(f);
                                    }).ToArray();
                                }

                            }
                            catch
                            {
                                //
                            }

                        }
                    }
                }
            }
        }
    }

    private ITable? FindTable(string tableName)
    {
        return cachedTables.GetOrAdd(tableName, tn => Schema.Current.GetDatabaseTables().FirstOrDefault(t => t.Name.ToString() == tn));
    }

    private (TableIndex index, List<PropertyInfo> properties)? GetIndexInfo(ITable table, string indexName)
    {
        return cachedLookups.GetOrAdd((table, indexName), tup =>
        {
            var index = tup.table.AllIndexes().FirstOrDefault(ix => ix.Unique == true && ix.IndexName == tup.indexName);

            if (index == null)
                return null;

            if (table is Table t)
            {
                var properties = (from f in t.Fields.Values
                                  let cols = f.Field.Columns()
                                  where cols.Any() && cols.Any(c => index.Columns.Contains(c))
                                  select Reflector.TryFindPropertyInfo(f.FieldInfo)).NotNull().ToList();

                if (properties.IsEmpty())
                    return null;

                return (index, properties);
            }
            else if (table is TableMList tm)
            {
                if (tm.Field is FieldEmbedded fe)
                {
                    var properties = (from f in fe.EmbeddedFields.Values
                                      let cols = f.Field.Columns()
                                      where cols.Any(c => index.Columns.Contains(c))
                                      select Reflector.TryFindPropertyInfo(f.FieldInfo)).NotNull().ToList();

                    if (properties.IsEmpty())
                        return null;

                    return (index, properties);
                }
                else
                    return null;
            }
            else
            {
                throw new UnexpectedValueException(table);
            }
        });
    }

    static ConcurrentDictionary<string, ITable?> cachedTables = new ConcurrentDictionary<string, ITable?>();
    static ConcurrentDictionary<(ITable table, string indexName), (TableIndex index, List<PropertyInfo> properties)?> cachedLookups =
        new ConcurrentDictionary<(ITable table, string indexName), (TableIndex index, List<PropertyInfo> properties)?>();

    public override string Message
    {
        get
        {
            if (Table == null)
                return InnerException!.Message;

            var type = Table is Table t ? t.Type :
                       Table is TableMList tm ? tm.PropertyRoute.RootType :
                       null;

            if (type == null)
                return InnerException!.Message;

            var typeName = " / ".Combine(type.NiceName(), Table is TableMList tm2 ? tm2.PropertyRoute.PropertyInfo?.NiceName() : null);

            var columns = Index == null ? IndexName :
                Properties != null ? Properties!.CommaAnd(p => $"[{p.NiceName()}]") :
                Index.Columns.CommaAnd(c => $"[{c.Name}]");

            var values = HumanValues != null ? HumanValues.CommaAnd(a => a is string ? $"'{a}'" : a == null ? "NULL" : a.ToString()) :
                Values;

            if (values == null)
                return EngineMessage.ThereIsAlreadyA0WithTheSame1_G.NiceToString().ForGenderAndNumber(type.GetGender())
                    .FormatWith(typeName, columns);

            return EngineMessage.ThereIsAlreadyA0With1EqualsTo2_G.NiceToString().ForGenderAndNumber(type.GetGender())
                .FormatWith(typeName, columns, values);
        }
    }
}


public class ForeignKeyException : ApplicationException
{
    public string? TableName { get; private set; }
    public Type? TableType { get; private set; }
    public string? ColumnName { get; private set; }
    public PropertyInfo? PropertyInfo { get; private set; }


    public string? ReferedTableName { get; private set; }
    public Type? ReferedTableType { get; private set; }

    public bool IsInsert { get; private set; }

    static Regex indexRegex = new Regex(@"['""»]FK_(?<parts>.+?)['""«]", RegexOptions.IgnoreCase);

    static Regex referedTable = new Regex(@"table ""(?<referedTable>.+?)""");

    public ForeignKeyException(Exception inner) : base(null, inner)
    {
        Match m = indexRegex.Match(inner.Message);

        if (m.Success)
        {
            
            var parts = m.Groups["parts"].Value.Split("_");

            for (int i = 1; i < parts.Length; i++)
            {
                if (inner is PostgresException pg)
                {
                    TableName = pg.TableName!;
                    ColumnName = pg.ConstraintName!.After($"fk_{pg.TableName}_");
                }
                else
                {
                    TableName = parts.Take(i).ToString("_");
                    ColumnName = parts.Skip(i).ToString("_");
                }

                var table = Schema.Current.GetDatabaseTables().FirstOrDefault(table => table.Name.Name == TableName);

                if (table is TableMList tmle)
                {
                    var column = tmle.Columns.TryGetC(ColumnName);
                    TableType = tmle.BackReference.ReferenceTable.Type;
                    PropertyInfo =
                         tmle.Field == column ? tmle.PropertyRoute.PropertyInfo :
                         tmle.Field is FieldEmbedded fe ?
                         (from f in fe.EmbeddedFields.Values
                          where f.Field.Columns().Contains(column)
                          select Reflector.TryFindPropertyInfo(f.FieldInfo)).NotNull().FirstOrDefault() :
                          null;

                    break;
                }
                else if (table is Table t)
                {
                    var column = t.Columns.TryGetC(ColumnName);
                    TableType = t.Type;
                    PropertyInfo = (from f in t.Fields.Values
                                    where f.Field.Columns().Contains(column)
                                    select Reflector.TryFindPropertyInfo(f.FieldInfo)).NotNull().FirstOrDefault();
                    break;
                }
            }
        }

        if (inner.Message.Contains("INSERT") || inner.Message.Contains("UPDATE"))
        {
            IsInsert = true;

            Match m2 = referedTable.Match(inner.Message);
            if (m2.Success)
            {
                ReferedTableName = m2.Groups["referedTable"].Value.Split('.').Last();
                ReferedTableType = Schema.Current.Tables
                                .Where(kvp => kvp.Value.Name.Name == ReferedTableName)
                                .Select(p => p.Key)
                                .SingleOrDefaultEx();

                ReferedTableType = ReferedTableType == null ? null : EnumEntity.Extract(ReferedTableType) ?? ReferedTableType;
            }
        }
    }

    public override string Message
    {
        get
        {
            if (TableName == null)
                return InnerException!.Message;

            if (IsInsert)
                return (TableType == null || ReferedTableType == null) ?
                    "The column {0} on table {1} does not reference {2}".FormatWith(ColumnName, TableName, ReferedTableName) :
                    "The column {0} of the {1} does not refer to a valid {2}".FormatWith(ColumnName, TableType.NiceName(), ReferedTableType.NiceName());
            else
                return (TableType == null) ?
                    EngineMessage.ThereAreRecordsIn0PointingToThisTableByColumn1.NiceToString().FormatWith(TableName, ColumnName) :
                    EngineMessage.ThereAre0ThatReferThisEntityByProperty1.NiceToString().FormatWith(TableType.NicePluralName(), PropertyInfo?.NiceName() ?? ColumnName);
        }
    }
}


public class EntityNotFoundException : Exception
{
    public Type Type { get; private set; }
    public PrimaryKey[] Ids { get; private set; }

    public EntityNotFoundException(Type type, params PrimaryKey[] ids)
        : base(EngineMessage.EntityWithType0AndId1NotFound.NiceToString().FormatWith(type.Name, ids.ToString(", ")))
    {
        this.Type = type;
        this.Ids = ids;
    }
}

public class ConcurrencyException : Exception
{
    public Type Type { get; private set; }
    public PrimaryKey[] Ids { get; private set; }

    public ConcurrencyException(Type type, params PrimaryKey[] ids)
        : base(EngineMessage.ConcurrencyErrorOnDatabaseTable0Id1.NiceToString().FormatWith(type.NiceName(), ids.ToString(", ")))
    {
        this.Type = type;
        this.Ids = ids;
    }
}


public class ModelRequestedException : Exception
{
    public ModelRequestedException(ModelEntity model) : base("Model Requested")
    {
        this.Model = model;
    }

    public ModelEntity Model { get; set; }
}
