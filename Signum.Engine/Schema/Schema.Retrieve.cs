using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using System.Data;
using Signum.Utilities.Reflection;
using Signum.Engine.Properties;
using Signum.Entities.Reflection;

namespace Signum.Engine.Maps
{
    public partial class Table
    {
        internal SqlPreCommand SelectAllIDs()
        {
            return SqlBuilder.SelectAll(Name, new[] { SqlBuilder.PrimaryKeyName });
        }

        internal SqlPreCommand BatchSelect(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, this.Columns.Values.Select(a => a.Name).ToArray(), SqlBuilder.PrimaryKeyName, ids);
        }

        internal SqlPreCommand BatchSelectLite(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, new[] { SqlBuilder.PrimaryKeyName, SqlBuilder.ToStrName }, SqlBuilder.PrimaryKeyName, ids); 
        }

        internal void Fill(FieldReader reader, IdentifiableEntity ei, Retriever retriever)
        {
            foreach (EntityField ef in Fields.Values)
            {
                ef.Setter(ei, ef.Field.GenerateValue(reader, retriever)); 
            }
        }
    }

    public partial class RelationalTable
    {
        internal SqlPreCommand BatchSelect(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, this.Columns.Values.Select(a => a.Name).ToArray(), BackReference.Name, ids);
        }

        internal object GenerateValue(FieldReader reader, Retriever recuperador)
        {
            return Field.GenerateValue(reader, recuperador); 
        }
    }

    public abstract partial class Field
    {
        internal abstract object GenerateValue(FieldReader reader, Retriever retriever);
    }

    public partial class FieldPrimaryKey
    {
        internal override object GenerateValue(FieldReader reader, Retriever retriever)
        {
            return reader.GetInt32(this.Position);
        }
    }

    public partial class FieldValue
    {
        Func<FieldReader, int, object> func;

        internal override object GenerateValue(FieldReader reader, Retriever retriever)
        {
            return func(reader, Position);
        }
    }

    public partial class FieldReference
    {
        internal override object GenerateValue(FieldReader reader, Retriever retriever)
        {
            int? id = reader.GetNullableInt32(Position);

            if (!id.HasValue)
                if (!Nullable) throw new InvalidOperationException("Field {0} has null but is not nullable".Formato(Name));
                else return null;

            if (IsLite)
                return retriever.GetLite(ReferenceTable, Reflector.ExtractLite(FieldType), id.Value);
            else
                return retriever.GetIdentifiable(ReferenceTable, id.Value, false); 
        }
    }

    public partial class FieldEnum
    {
        internal override object GenerateValue(FieldReader reader, Retriever retriever)
        {
            int? id = reader.GetNullableInt32(Position);
            if (!id.HasValue)
                if (!Nullable) throw new InvalidOperationException("Field {0} has null value but is not nullable".Formato(Name));
                else return null;

            return Enum.ToObject(FieldType.UnNullify(), id);
        }     
    }

    public partial class FieldMList
    {
        internal override object GenerateValue(FieldReader reader, Retriever retriever)
        {
            int myID = reader.GetInt32(0);

            return retriever.GetList(RelationalTable, myID); 
        }
    }

    public partial class FieldEmbedded
    {
        internal override object GenerateValue(FieldReader reader, Retriever retriever)
        {
            if (HasValue != null && !reader.GetBoolean(HasValue.Position))
                return null;

            EmbeddedEntity result = Constructor();

            foreach (EntityField ef in EmbeddedFields.Values)
            {
                ef.Setter(result, ef.Field.GenerateValue(reader, retriever));
            }

            result.Modified = null;
            retriever.PostRetrieving.Add(result); 

            return result; 
        }
    }

    public partial class FieldImplementedBy
    {
        internal override object GenerateValue(FieldReader reader, Retriever retriever)
        {
            var columns = ImplementationColumns.Where(c => reader.GetNullableInt32(c.Value.Position) != null).ToArray();

            if(columns.Length == 0)
                return null;
            
            if (columns.Length > 1)
                throw new InvalidOperationException("Fields {0} are set at the same time".Formato(columns.ToString(c => c.Value.Name, ", ")));

            ImplementationColumn col = columns[0].Value;

            int? id = (int?)reader.GetNullableInt32(col.Position);

             if(IsLite)
                return retriever.GetLite(col.ReferenceTable, Reflector.ExtractLite(FieldType), id.Value);
            else
                return retriever.GetIdentifiable(col.ReferenceTable, id.Value, false); 
        }
    }

    public partial class FieldImplementedByAll
    {
        internal override object GenerateValue(FieldReader reader, Retriever retriever)
        {
            int? id = reader.GetNullableInt32(Column.Position);
            int? idTipo = reader.GetNullableInt32(ColumnTypes.Position);

            if (id.HasValue != idTipo.HasValue)
                throw new InvalidOperationException("ImplementedByAll {0} = {1} pero {2} = {3}".Formato(
                    Column.Name, id.TryToString() ?? "[null]", 
                    ColumnTypes.Name, idTipo.TryToString() ?? "[null]"));

            if (id == null)
                return null;

            if (IsLite)
                return retriever.GetLite(Schema.Current.TablesForID[idTipo.Value], Reflector.ExtractLite(FieldType), id.Value);
            else
                return retriever.GetIdentifiable(Schema.Current.TablesForID[idTipo.Value], id.Value, false); 
        }
    }

    public static class CleanUtil
    {
        public static object Cell(this DataRow dt, string campo)
        {
            if(dt.IsNull(campo))
                return null;
            return dt[campo]; 
        }
    }
}
