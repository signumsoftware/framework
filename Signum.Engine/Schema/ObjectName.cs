using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;

namespace Signum.Engine.Maps
{
    public static class TableExtensions
    {
        internal static string UnScapeSql(this string name)
        {
            return name.Trim('[', ']');
        }
    }

    public class ServerName : IEquatable<ServerName>
    {
        public string Name { get; private set; }

        /// <summary>
        /// Linked Servers: http://msdn.microsoft.com/en-us/library/ms188279.aspx
        /// </summary>
        /// <param name="name"></param>
        public ServerName(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
        }

        public override string ToString()
        {
            return Name.SqlEscape();
        }

        public bool Equals(ServerName other)
        {
            return other.Name == Name;
        }

        public override bool Equals(object obj)
        {
            var db = obj as ServerName;
            return db != null && Equals(db);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode();
        }

        public static ServerName Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return new ServerName(name.UnScapeSql());
        }
    }

    public class DatabaseName : IEquatable<DatabaseName>
    {
        public string Name { get; private set; }

        public ServerName Server { get; private set; }

        public DatabaseName(ServerName server, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
            this.Server = server;
        }

        public override string ToString()
        {
            var options = ObjectName.CurrentOptions;

            var name = !options.DatabaseNameReplacement.HasText() ? Name.SqlEscape():
                ("[" + Name.Replace(Connector.Current.DatabaseName(), options.DatabaseNameReplacement) + "]");

            if (Server == null)
                return name;

            return Server.ToString() + "." + name;
        }

        public bool Equals(DatabaseName other)
        {
            return other.Name == Name &&
                object.Equals(Server, other.Server);
        }

        public override bool Equals(object obj)
        {
            var db = obj as DatabaseName;
            return db != null && Equals(db);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (Server == null ? 0 : Server.GetHashCode());
        }

        public static DatabaseName Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            return new DatabaseName(ServerName.Parse(name.TryBeforeLast('.')), (name.TryAfterLast('.') ?? name).UnScapeSql());
        }
    }

    public class SchemaName : IEquatable<SchemaName>
    {
        public string Name { get; private set; }

        readonly DatabaseName database;

        public DatabaseName Database
        {
            get
            {
                if (database == null || ObjectName.CurrentOptions.AvoidDatabaseName)
                    return null;

                return database;
            }
        }

        public static readonly SchemaName Default = new SchemaName(null, "dbo");

        public bool IsDefault()
        {
            return Name == "dbo" && Database == null;
        }

        public SchemaName(DatabaseName database, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            this.Name = name;
            this.database = database;
        }

        public override string ToString()
        {
            var result = Name.SqlEscape();

            if (Database == null)
                return result;

            return Database.ToString() + "." + result;
        }

        public bool Equals(SchemaName other)
        {
            return other.Name == Name &&
                object.Equals(Database, other.Database);
        }

        public override bool Equals(object obj)
        {
            var sc = obj as SchemaName;
            return sc != null && Equals(sc);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ (Database == null ? 0 : Database.GetHashCode());
        }

        public static SchemaName Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                return SchemaName.Default;

            return new SchemaName(DatabaseName.Parse(name.TryBeforeLast('.')), (name.TryAfterLast('.') ?? name).UnScapeSql());
        }

    }

    public class ObjectName : IEquatable<ObjectName>
    {
        public string Name { get; private set; }

        public SchemaName Schema { get; private set; }

        public ObjectName(SchemaName schema, string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            if (schema == null)
                throw new ArgumentNullException("schema");

            this.Name = name;
            this.Schema = schema;
        }

        public override string ToString()
        {
            return Schema.ToString() + "." + Name.SqlEscape();
        }

        public bool Equals(ObjectName other)
        {
            return other.Name == Name &&
                object.Equals(Schema, other.Schema);
        }

        public override bool Equals(object obj)
        {
            var sc = obj as ObjectName;
            return sc != null && Equals(sc);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Schema.GetHashCode();
        }

        public static ObjectName Parse(string name)
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentNullException("name");

            return new ObjectName(SchemaName.Parse(name.TryBeforeLast('.')), (name.TryAfterLast('.') ?? name).UnScapeSql());
        }

        public ObjectName OnDatabase(DatabaseName databaseName)
        {
            return new ObjectName(new SchemaName(databaseName, Schema.Name), Name);
        }

        public ObjectName OnSchema(SchemaName schemaName)
        {
            return new ObjectName(schemaName, Name);
        }

        static readonly ThreadVariable<ObjectNameOptions> optionsVariable = Statics.ThreadVariable<ObjectNameOptions>("objectNameOptions");
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
    }

    public struct ObjectNameOptions
    {
        public string DatabaseNameReplacement;
        public bool AvoidDatabaseName;
    }
}
