
using Signum.Engine.Sync;

namespace Signum.Engine.Maps;

public static class TableExtensions
{
    internal static string UnescapeSql(this string name, bool isPostgres)
    {
        if (isPostgres)
        {
            if (name.StartsWith('\"'))
                return name.Trim('\"');

            return name.ToLower();
        }

        return name.Trim('[', ']');
    }
}

public class ServerName : IEquatable<ServerName>
{
    public string Name { get; private set; }
    public bool IsPostgres { get; private set; }

    /// <summary>
    /// Linked Servers: http://msdn.microsoft.com/en-us/library/ms188279.aspx
    /// </summary>
    /// <param name="name"></param>
    public ServerName(string name, bool isPostgres)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        this.Name = name;
        this.IsPostgres = isPostgres;
    }

    public override string ToString()
    {
        return Name.SqlEscape(IsPostgres);
    }

    public override bool Equals(object? obj) => obj is ServerName sn && Equals(sn);
    public bool Equals(ServerName? other)
    {
        if (other == null)
            return false;

        return other.Name == Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public static ServerName? Parse(string? name, bool isPostgres)
    {
        if (!name.HasText())
            return null;

        return new ServerName(name.UnescapeSql(isPostgres), isPostgres);
    }
}

public class DatabaseName : IEquatable<DatabaseName>
{
    public string Name { get; private set; }
    public bool IsPostgres { get; private set; }

    public ServerName? Server { get; private set; }

    public DatabaseName(ServerName? server, string name, bool isPostgres)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        this.Name = name;
        this.Server = server;
        this.IsPostgres = isPostgres;
    }

    public override string ToString()
    {
        var options = ObjectName.CurrentOptions;

        var name = !options.DatabaseNameReplacement.HasText() ? Name.SqlEscape(IsPostgres): Name.Replace(Connector.Current.DatabaseName(), options.DatabaseNameReplacement).SqlEscape(IsPostgres);

        if (Server == null)
            return name;

        return Server.ToString() + "." + name;
    }


    public override bool Equals(object? obj) => obj is DatabaseName dn && Equals(dn);
    public bool Equals(DatabaseName? other)
    {
        if (other == null)
            return false;

        return other.Name == Name && object.Equals(Server, other.Server);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode() ^ (Server == null ? 0 : Server.GetHashCode());
    }

    public static DatabaseName? Parse(string? name, bool isPostgres)
    {
        if (!name.HasText())
            return null;

        var tuple = ObjectName.SplitLast(name, isPostgres);

        return new DatabaseName(ServerName.Parse(tuple.prefix, isPostgres), tuple.name, isPostgres);
    }
}

public class SchemaName : IEquatable<SchemaName>
{
    public string Name { get; private set; }
    public bool IsPostgres { get; private set; }

    readonly DatabaseName? database;

    public DatabaseName? Database
    {
        get
        {
            if (database == null || ObjectName.CurrentOptions.AvoidDatabaseName)
                return null;

            return database;
        }
    }

    static readonly SchemaName defaultSqlServer = new SchemaName(null, "dbo", isPostgres: false);
    static readonly SchemaName defaultPostgreSql = new SchemaName(null, "public", isPostgres: true);

    public static SchemaName Default(bool isPostgres) => isPostgres ? defaultPostgreSql : defaultSqlServer;

    public bool IsDefault()
    {
        return Database == null && (IsPostgres ? defaultPostgreSql : defaultSqlServer).Name == Name;
    }

    public SchemaName(DatabaseName? database, string name, bool isPostgres)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentNullException(nameof(name));

        this.Name = name;
        this.database = database;
        this.IsPostgres = isPostgres;
    }

    public override string ToString()
    {
        var result = Name.SqlEscape(IsPostgres);

        if (Database == null)
            return result;

        return Database.ToString() + "." + result;
    }

    public override bool Equals(object? obj) => obj is SchemaName sn && Equals(sn);
    public bool Equals(SchemaName? other)
    {

        if (other == null)
            return false;

        return other.Name == Name &&
            object.Equals(Database, other.Database);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode() ^ (Database == null ? 0 : Database.GetHashCode());
    }

    public static SchemaName Parse(string? name, bool isPostgres)
    {
        if (!name.HasText())
            return SchemaName.Default(isPostgres);

        var tuple = ObjectName.SplitLast(name, isPostgres);

        return new SchemaName(DatabaseName.Parse(tuple.prefix, isPostgres), tuple.name, isPostgres);
    }

    internal SchemaName OnDatabase(DatabaseName? database)
    {
        return new SchemaName(database, this.Name, this.IsPostgres);
    }
}

public class ObjectName : IEquatable<ObjectName>
{
    public static int MaxPostgresSize = 63; 

    public string Name { get; private set; }
    public bool IsPostgres { get; private set; }

    public SchemaName Schema { get; private set; } // null only for postgres temporary



    public ObjectName(SchemaName schema, string name, bool isPostgres)
    {
        this.Name = name.HasText() ? name : throw new ArgumentNullException(nameof(name));
        if (isPostgres && this.Name.Length > MaxPostgresSize)
            throw new InvalidOperationException($"The name '{name}' is too long, consider using TableNameAttribute/ColumnNameAttribute");

        this.Schema = schema ?? (isPostgres && name.StartsWith("#") ? (SchemaName)null! : throw new ArgumentNullException(nameof(schema)));
        this.IsPostgres = isPostgres;
    }

    ObjectName(string name, bool isPostgres)
    : this(SchemaName.Default(isPostgres), name, isPostgres)
    {
        this.Name = name;
        this.Schema = null!;
        this.IsPostgres = isPostgres;
    }

    public static ObjectName Raw(string name, bool isPostgres)
    {
        return new ObjectName(name, isPostgres);
    }   

    public override string ToString()
    {
        if (Schema == null)
            return Name.SqlEscape(IsPostgres);

        return Schema.ToString() + "." + Name.SqlEscape(IsPostgres);
    }

    public override bool Equals(object? obj) => obj is ObjectName on && Equals(on);
    public bool Equals(ObjectName? other)
    {
        if (other == null)
            return false;

        return other.Name == Name &&
            object.Equals(Schema, other.Schema);
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode() ^ Schema?.GetHashCode() ?? 0;
    }

    public static ObjectName Parse(string? name, bool isPostgres)
    {
        if (!name.HasText())
            throw new ArgumentNullException(nameof(name));

        var tuple = SplitLast(name, isPostgres);

        return new ObjectName(SchemaName.Parse(tuple.prefix, isPostgres), tuple.name, isPostgres);
    }

    //FROM "[a.b.c].[d.e.f].[a.b.c].[c.d.f]"
    //TO   ("[a.b.c].[d.e.f].[a.b.c]", "c.d.f")
    internal static (string? prefix, string name) SplitLast(string str, bool isPostgres)
    {
        if (isPostgres)
        {
            if (!str.EndsWith('\"'))
            {
                return (
                    prefix: str.TryBeforeLast('.'),
                    name: str.TryAfterLast('.') ?? str
                    );
            }

            var index = str.LastIndexOf('\"', str.Length - 2);
            return (
                prefix: index == 0 ? null : str.Substring(0, index - 1),
                name: str.Substring(index).UnescapeSql(isPostgres)
            );
        }
        else
        {

            if (!str.EndsWith("]"))
            {
                return (
                    prefix: str.TryBeforeLast('.'),
                    name: str.TryAfterLast('.') ?? str
                    );
            }

            var index = str.LastIndexOf('[');
            return (
                prefix: index == 0 ? null : str.Substring(0, index - 1),
                name: str.Substring(index).UnescapeSql(isPostgres)
            );
        }
    }

    public ObjectName OnDatabase(DatabaseName? databaseName)
    {
        if (databaseName != null && databaseName.IsPostgres != this.IsPostgres)
            throw new Exception("Inconsitent IsPostgres");

        return new ObjectName(new SchemaName(databaseName, Schema!.Name, IsPostgres), Name, IsPostgres);
    }

    public ObjectName OnSchema(SchemaName schemaName)
    {
        if (schemaName.IsPostgres != this.IsPostgres)
            throw new Exception("Inconsitent IsPostgres");

        return new ObjectName(schemaName, Name, IsPostgres);
    }

    static readonly AsyncThreadVariable<ObjectNameOptions> optionsVariable = Statics.ThreadVariable<ObjectNameOptions>("objectNameOptions");
    public static IDisposable OverrideOptions(ObjectNameOptions options)
    {
        var old = optionsVariable.Value;
        optionsVariable.Value = options;
        return new Disposable(() => optionsVariable.Value = old);
    }

    public static ObjectNameOptions CurrentOptions
    {
        get { return optionsVariable.Value; }
    }

    public bool IsTemporal => this.Name.StartsWith("#");
}

public struct ObjectNameOptions
{
    public string? DatabaseNameReplacement;
    public bool AvoidDatabaseName;
}
