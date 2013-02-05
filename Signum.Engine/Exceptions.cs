using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.Serialization;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using Signum.Utilities;
using Signum.Engine.Properties;
using Signum.Engine.Maps;
using Signum.Entities;
using Signum.Entities.Reflection;
using System.Data.SqlServerCe;

namespace Signum.Engine.Exceptions
{
    [Serializable]
    public class UniqueKeyException : ApplicationException
    {
        public string TableName { get; private set; }
        public string[] Fields { get; private set; }

        protected UniqueKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        static Regex indexRegex = new Regex(@"\'IX_(?<table>[^_]+)(_(?<field>[^_]+))+.*\'"); 

        public UniqueKeyException(Exception inner) : base(null, inner) 
        {
            Match m = indexRegex.Match(inner.Message);
            if (m.Success)
            {
                TableName = m.Groups["table"].Value;
                Fields = m.Groups["field"].Captures.Cast<Capture>().Select(a => a.Value).ToArray();
            }
        }

        public override string Message
        {
            get
            {
                if (TableName == null)
                    return InnerException.Message;

                string fieldStr = Fields.Length == 1 ? Fields[0] :
                        Fields.Take(Fields.Length - 1).ToString(", ") + " and " + Fields[Fields.Length - 1]; 

                return "There's already a '{0}' with the same {1}".Formato(TableName, fieldStr);
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

        static Regex indexRegex = new Regex(@"""FK_(?<table>[^_]+)_(?<field>[^_""]+)""");

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
                        "The column {0} on table {1} does not reference {2}".Formato(Field, TableName, ReferedTableName) :
                        "The column {0} of the {1} does not refer to a valid {2}".Formato(Field, TableType.NiceName(), ReferedTableType.NiceName());
                else
                    return (TableType == null) ?
                        EngineMessage.ThereAreRecordsIn0PointingToThisTableByColumn1.NiceToString().Formato(TableName, Field) :
                        EngineMessage.ThereAre0ThatReferThisEntity.NiceToString().Formato(TableType.NicePluralName());
            }
        }
    }


    [Serializable]
    public class EntityNotFoundException : Exception
    {
        public Type Type { get; private set; }
        public int[] Ids { get; private set; }

        protected EntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public EntityNotFoundException(Type type, params int[] ids)
            : base(EngineMessage.EntityWithType0AndId1NotFound.NiceToString().Formato(type.Name, ids.ToString(", ")))
        {
            this.Type = type;
            this.Ids = ids;
        }
    }

    [Serializable]
    public class ConcurrencyException: Exception
    {
        public Type Type { get; private set; }
        public int[] Ids { get; private set; }

        protected ConcurrencyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public ConcurrencyException(Type type, params int[] ids)
            : base(EngineMessage.ConcurrencyErrorOnDatabaseTable0Id1.NiceToString().Formato(type.NiceName(), ids.ToString(", ")))
        {
            this.Type = type;
            this.Ids = ids;
        }
    }
}
