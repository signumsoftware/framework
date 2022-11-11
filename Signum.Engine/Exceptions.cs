using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Signum.Engine.Maps;
using System.Collections.Concurrent;
using Signum.Utilities.Reflection;
using Signum.Engine.Basics;
using Signum.Entities.Basics;

namespace Signum.Engine;

public class UniqueKeyException : ApplicationException
{
    public string? TableName { get; private set; }
    public Table? Table { get; private set; }

    public string? IndexName { get; private set; }
    public UniqueTableIndex? Index { get; private set; }
    public List<PropertyInfo>? Properties { get; private set; }

    public string? Values { get; private set; }
    public object?[]? HumanValues { get; private set; }

    protected UniqueKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    static Regex[] regexes = new[]
    {
            new Regex(@"Cannot insert duplicate key row in object '(?<table>.*)' with unique index '(?<index>.*)'\. The duplicate key value is \((?<value>.*)\)"),
            new Regex(@"Eine Zeile mit doppeltem Schlüssel kann in das Objekt ""(?<table>.*)"" mit dem eindeutigen Index ""(?<index>.*)"" nicht eingefügt werden. Der doppelte Schlüsselwert ist \((?<value>.*)\)")
        };

    public UniqueKeyException(Exception inner) : base(null, inner)
    {
        foreach (var rx in regexes)
        {
            Match m = rx.Match(inner.Message);
            if (m.Success)
            {
                TableName = m.Groups["table"].Value;
                IndexName = m.Groups["index"].Value;
                Values = m.Groups["value"].Value;

                Table = cachedTables.GetOrAdd(TableName, tn => Schema.Current.Tables.Values.FirstOrDefault(t => t.Name.ToString() == tn));

                if (Table != null)
                {
                    var tuple = cachedLookups.GetOrAdd((Table, IndexName), tup =>
                    {
                        var index = tup.table.GeneratAllIndexes().OfType<UniqueTableIndex>().FirstOrDefault(ix => ix.IndexName == tup.indexName);

                        if (index == null)
                            return null;

                        var properties = (from f in tup.table.Fields.Values
                                          let cols = f.Field.Columns()
                                          where cols.Any() && cols.All(c => index.Columns.Contains(c))
                                          select Reflector.TryFindPropertyInfo(f.FieldInfo)).NotNull().ToList();

                        if (properties.IsEmpty())
                            return null;

                        return (index, properties);
                    });

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
                                    var f = Table.GetField(p);
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

    static ConcurrentDictionary<string, Table?> cachedTables = new ConcurrentDictionary<string, Table?>();
    static ConcurrentDictionary<(Table table, string indexName), (UniqueTableIndex index, List<PropertyInfo> properties)?> cachedLookups =
        new ConcurrentDictionary<(Table table, string indexName), (UniqueTableIndex index, List<PropertyInfo> properties)?>();

    public override string Message
    {
        get
        {
            if (Table == null)
                return InnerException!.Message;

            return EngineMessage.TheresAlreadyA0With1EqualsTo2_G.NiceToString().ForGenderAndNumber(Table?.Type.GetGender()).FormatWith(
                Table == null ? TableName : Table.Type.NiceName(),
                Index == null ? IndexName :
                Properties != null ? Properties!.CommaAnd(p => p.NiceName()) :
                Index.Columns.CommaAnd(c => c.Name),
                HumanValues != null ? HumanValues.CommaAnd(a => a is string ? $"'{a}'" : a == null ? "NULL" : a.ToString()) :
                Values);
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

    static Regex indexRegex = new Regex(@"['""]FK_(?<parts>.+?)['""]");

    static Regex referedTable = new Regex(@"table ""(?<referedTable>.+?)""");

    protected ForeignKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

    public ForeignKeyException(Exception inner) : base(null, inner)
    {
        Match m = indexRegex.Match(inner.Message);

        if (m.Success)
        {
            var parts = m.Groups["parts"].Value.Split("_");

            for (int i = 1; i < parts.Length; i++)
            {
                TableName = parts.Take(i).ToString("_");
                ColumnName = parts.Skip(i).ToString("_");

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

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected EntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

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

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
    protected ConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
#pragma warning restore CS8618 // Non-nullable field is uninitialized.

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
