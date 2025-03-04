#pragma warning disable CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.


namespace Signum.Engine.Sync.Postgres;

[TableName("pg_catalog.pg_namespace")]
public class PgNamespace : IView
{
    [ViewPrimaryKey]
    public int oid;

    public string nspname;

    [AutoExpressionField]
    public bool IsInternal() => As.Expression(() => nspname == "information_schema" || nspname.StartsWith("pg_"));

    [AutoExpressionField]
    public IQueryable<PgClass> Tables() =>
        As.Expression(() => Database.View<PgClass>().Where(t => t.relnamespace == oid && t.relkind == RelKind.Table));
}


[TableName("pg_catalog.pg_class")]
public class PgClass : IView
{
    [ViewPrimaryKey]
    public int oid;

    public string relname;
    public int relnamespace;
    public char relkind;
    public int reltuples;

    [AutoExpressionField]
    public IQueryable<PgTrigger> Triggers() =>
        As.Expression(() => Database.View<PgTrigger>().Where(t => t.tgrelid == oid));

    [AutoExpressionField]
    public IQueryable<PgIndex> Indices() =>
        As.Expression(() => Database.View<PgIndex>().Where(t => t.indrelid == oid));

    [AutoExpressionField]
    public IQueryable<PgAttribute> Attributes() =>
        As.Expression(() => Database.View<PgAttribute>().Where(t => t.attrelid == oid));

    [AutoExpressionField]
    public IQueryable<PgConstraint> Constraints() =>
        As.Expression(() => Database.View<PgConstraint>().Where(t => t.conrelid == oid));


    [AutoExpressionField]
    public PgNamespace? Namespace() =>
        As.Expression(() => Database.View<PgNamespace>().SingleOrDefault(t => t.oid == relnamespace));
}

public static class RelKind
{
    public const char Table = 'r';
    public const char Index = 'i';
    public const char Sequence = 's';
    public const char Toast = 't';
    public const char View = 'v';
    public const char MaterializedView = 'n';
    public const char CompositeType = 'c';
    public const char ForeignKey = 'f';
    public const char PartitionTable = 'p';
    public const char PartitionIndex = 'I';
}

[TableName("pg_catalog.pg_attribute")]
public class PgAttribute : IView
{
    [ViewPrimaryKey]
    public int attrelid;

    [ViewPrimaryKey]
    public string attname;

    public int atttypid;
    public int atttypmod;

    public short attlen;
    public short attnum;
    public bool attnotnull;
    public char attidentity;

    [AutoExpressionField]
    public PgType? Type() => As.Expression(() => Database.View<PgType>().SingleOrDefault(t => t.oid == atttypid));

    [AutoExpressionField]
    public PgAttributeDef? Default() => As.Expression(() => Database.View<PgAttributeDef>().SingleOrDefault(d => d.adrelid == attrelid && d.adnum == attnum));
}

[TableName("pg_catalog.pg_attrdef")]
public class PgAttributeDef : IView
{
    [ViewPrimaryKey]
    public int oid;

    public int adrelid;

    public short adnum;

    public string /*not really*/ adbin;
}

[TableName("pg_catalog.pg_type")]
public class PgType : IView
{
    [ViewPrimaryKey]
    public int oid;

    public string typname;

    public int typnamespace;
    public short typlen;
    public bool typbyval;
}

[TableName("pg_catalog.pg_trigger")]
public class PgTrigger : IView
{
    [ViewPrimaryKey]
    public int oid;

    public int tgrelid;
    public string tgname;
    public int tgfoid;
    public byte[] tgargs;

    [AutoExpressionField]
    public PgProc? Proc() => As.Expression(() => Database.View<PgProc>().SingleOrDefault(p => p.oid == tgfoid));
}

[TableName("pg_catalog.pg_proc")]
public class PgProc : IView
{
    [ViewPrimaryKey]
    public int oid;

    public int pronamespace;

    [AutoExpressionField]
    public PgNamespace? Namespace() => As.Expression(() => Database.View<PgNamespace>().SingleOrDefault(t => t.oid == pronamespace));

    [AutoExpressionField]
    public PgExtension? Extension() =>
       As.Expression(() => Database.View<PgDepend>().Where(t => t.deptype == "e" && t.objid == this.oid)
       .Select(d => Database.View<PgExtension>().SingleEx(e => e.oid == d.refobjid))
       .SingleOrDefault());

    public string proname;
}


[TableName("pg_catalog.pg_extension")]
public class PgExtension : IView
{
    [ViewPrimaryKey]
    public int oid;

    public string extname;
}

[TableName("pg_catalog.pg_depend")]
public class PgDepend : IView
{
    [ViewPrimaryKey]
    public int objid;

    public string deptype;

    public int refobjid;

}

[TableName("pg_catalog.pg_index")]
public class PgIndex : IView
{
    [ViewPrimaryKey]
    public int indexrelid;

    public int indrelid;

    public short indnatts;

    public short indnkeyatts;

    public bool indisunique;

    public bool indisprimary;

    public short[] indkey;

    public string? indexprs;
    public string? indpred;

    [AutoExpressionField]
    public PgClass Class() =>
     As.Expression(() => Database.View<PgClass>().Single(t => t.oid == indexrelid));
}


[TableName("pg_catalog.pg_constraint")]
public class PgConstraint : IView
{
    [ViewPrimaryKey]
    public int oid;

    public string conname;

    public int connamespace;

    public char contype;

    public int conrelid;

    public short[] conkey;

    public int confrelid;
    public short[] confkey;

    [AutoExpressionField]
    public PgClass TargetTable() =>
        As.Expression(() => Database.View<PgClass>().Single(t => t.oid == confrelid));

    [AutoExpressionField]
    public PgNamespace? Namespace() =>
        As.Expression(() => Database.View<PgNamespace>().SingleOrDefault(t => t.oid == connamespace));
}

public static class ConstraintType
{
    public const char Check = 'c';
    public const char ForeignKey = 'f';
    public const char PrimaryKey = 'p';
    public const char Unique = 'u';
    public const char Trigger = 't';
    public const char Exclusion = 'x';
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized. Consider declaring as nullable.
