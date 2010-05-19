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

namespace Signum.Engine.Exceptions
{
    [Serializable]
    public class UniqueKeyException : ApplicationException
    {
        public string TableName { get; private set; }
        public string[] Fields { get; private set; }

        protected UniqueKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        static Regex indexRegex = new Regex(@"\'IX_(?<table>[^_]+)(_(?<field>[^_]+))+\'"); 

        public UniqueKeyException(SqlException inner) : base(null, inner) 
        {
            Match m = indexRegex.Match(inner.Message);
            if (m != null)
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
        public Type RelatedType { get; private set; }

        static Regex indexRegex = new Regex(@"""FK_(?<table>[^_]+)_(?<field>[^_""]+)"""); 

        protected ForeignKeyException(SerializationInfo info, StreamingContext context) : base(info, context) { }
       
        public ForeignKeyException(SqlException inner) : base(null, inner) 
        {
            Match m = indexRegex.Match(inner.Message);
            if (m != null)
            {
                TableName = m.Groups["table"].Value;
                Field = m.Groups["field"].Value;
                RelatedType = Schema.Current.Tables
                    .Where(kvp => kvp.Value.Name == TableName)
                    .Select(p => p.Key)
                    .SingleOrDefault();
            }
        }

        public override string Message
        {
            get
            {
                if (TableName == null)
                    return InnerException.Message;

                return (RelatedType == null) ?
                    Resources.ThereAreRecordsIn0PointingToThisTableByColumn1.Formato(TableName, Field) :
                    Resources.ThereAre0ThatReferThisEntity.Formato(RelatedType.NicePluralName());
            }
        }
    }


    [Serializable]
    public class EntityNotFoundException : Exception
    {
        public Type Type { get; private set; }
        public int[] Ids { get; private set; }

        protected EntityNotFoundException(SerializationInfo info, StreamingContext context) : base(info, context) { }

        public EntityNotFoundException(Type type, int[] ids)
            : base(Resources.EntityWithType0AndId1NotFound.Formato(type.Name, ids.ToString(", ")))
        {
            this.Type = type;
            this.Ids = ids;
        }
    }

    [Serializable]
    public class OrderByNotLastException : Exception
    {
        public OrderByNotLastException(SqlException inner)
            : base("The translated query has an ORDERBY in a innner SELECT statement, write the query in a different way", inner)
        {

        }
    }
}
