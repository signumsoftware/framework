using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Signum.Utilities;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using System.Collections.Concurrent;
using System.Reflection;

namespace Signum.Engine
{
    [Serializable]
    public class UniqueKeyException : ApplicationException
    {
        public string TableName { get; private set; }
        public Table Table { get; private set; }

        public string IndexName { get; private set; }
        public UniqueIndex Index { get; private set; }
        public List<PropertyInfo> Properties { get; private set; }

        public string Values { get; private set; }

        protected UniqueKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        static Regex[] regexes = new []
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

                    if(Table != null)
                    {
                        var tuple = cachedLookups.GetOrAdd((Table, IndexName), tup=>
                        {
                            var index = tup.table.GeneratAllIndexes().OfType<UniqueIndex>().FirstOrDefault(ix => ix.IndexName == tup.indexName);

                            if(index == null)
                                return null;

                            var properties = (from f in tup.Item1.Fields.Values
                                              let cols = f.Field.Columns()
                                              where cols.Any() && cols.All(index.Columns.Contains)
                                              select Reflector.TryFindPropertyInfo(f.FieldInfo)).NotNull().ToList();

                            if (properties.IsEmpty())
                                return null;

                            return (index, properties); 
                        });
 
                        if(tuple != null)
                        {
                            Index = tuple.Value.index;
                            Properties = tuple.Value.properties;
                        }
                    }
                }
            }
        }

        static ConcurrentDictionary<string, Table> cachedTables = new ConcurrentDictionary<string, Table>();
        static ConcurrentDictionary<(Table table, string indexName), (UniqueIndex index, List<PropertyInfo> properties)?> cachedLookups = 
            new ConcurrentDictionary<(Table table, string indexName), (UniqueIndex index, List<PropertyInfo> properties)?>();

        public override string Message
        {
            get
            {
                if (Table == null)
                    return InnerException.Message;

                return EngineMessage.TheresAlreadyA0With1EqualsTo2_G.NiceToString().ForGenderAndNumber(Table?.Type.GetGender()).FormatWith(
                    Table == null ? TableName : Table.Type.NiceName(),
                    Index == null ? IndexName :
                    Properties.IsNullOrEmpty() ? Index.Columns.CommaAnd(c => c.Name) : 
                    Properties.CommaAnd(p=>p.NiceName()),
                    Values);
            }
        }
    }

  
    [Serializable]
    public class ForeignKeyException : ApplicationException
    {
        public string TableName { get; private set; }
        public string Field { get; private set; }
        public Type TableType { get; private set; }

        public string ReferedTableName { get; private set; }
        public Type ReferedTableType { get; private set; }

        public bool IsInsert { get; private set; }

        static Regex indexRegex = new Regex(@"['""]FK_(?<table>[^_]+)_(?<field>[^_""]+)['""]");

        static Regex referedTable = new Regex(@"table ""(?<referedTable>.+?)""");

        protected ForeignKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
       
        public ForeignKeyException(Exception inner) : base(null, inner) 
        {
            Match m = indexRegex.Match(inner.Message);
            
            if (m.Success)
            {
                TableName = m.Groups["table"].Value;
                Field = m.Groups["field"].Value;
                TableType = Schema.Current.Tables
                    .Where(kvp => kvp.Value.Name.Name == TableName)
                    .Select(p => p.Key)
                    .SingleOrDefaultEx();
            }

            if(inner.Message.Contains("INSERT"))
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

                    ReferedTableType = EnumEntity.Extract(ReferedTableType) ?? ReferedTableType; 
                }
            }
        }

        public override string Message
        {
            get
            {
                if (TableName == null)
                    return InnerException.Message;

                if (IsInsert)
                    return (TableType == null || ReferedTableType == null) ?
                        "The column {0} on table {1} does not reference {2}".FormatWith(Field, TableName, ReferedTableName) :
                        "The column {0} of the {1} does not refer to a valid {2}".FormatWith(Field, TableType.NiceName(), ReferedTableType.NiceName());
                else
                    return (TableType == null) ?
                        EngineMessage.ThereAreRecordsIn0PointingToThisTableByColumn1.NiceToString().FormatWith(TableName, Field) :
                        EngineMessage.ThereAre0ThatReferThisEntity.NiceToString().FormatWith(TableType.NicePluralName());
            }
        }
    }


    [Serializable]
    public class EntityNotFoundException : Exception
    {
        public Type Type { get; private set; }
        public PrimaryKey[] Ids { get; private set; }

        protected EntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public EntityNotFoundException(Type type, params PrimaryKey[] ids)
            : base(EngineMessage.EntityWithType0AndId1NotFound.NiceToString().FormatWith(type.Name, ids.ToString(", ")))
        {
            this.Type = type;
            this.Ids = ids;
        }
    }

    [Serializable]
    public class ConcurrencyException: Exception
    {
        public Type Type { get; private set; }
        public PrimaryKey[] Ids { get; private set; }

        protected ConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ConcurrencyException(Type type, params PrimaryKey[] ids)
            : base(EngineMessage.ConcurrencyErrorOnDatabaseTable0Id1.NiceToString().FormatWith(type.NiceName(), ids.ToString(", ")))
        {
            this.Type = type;
            this.Ids = ids;
        }
    }
}
