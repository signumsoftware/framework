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
        internal SqlPreCommand BatchSelect(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, new[] { "*" }, SqlBuilder.PrimaryKeyName, ids); 
        }

        internal SqlPreCommand BatchSelectLite(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, new[] { SqlBuilder.PrimaryKeyName, SqlBuilder.ToStrName }, SqlBuilder.PrimaryKeyName, ids); 
        }

        internal void Fill(DataRow row, IdentifiableEntity ei, Retriever retriever)
        {
            foreach (EntityField ef in Fields.Values)
            {
                ef.Setter(ei, ef.Field.GenerateValue(row, retriever)); 
            }
            
            ei.Modified = false;
            ei.IsNew = false; 
        }

        static string toStr = ReflectionTools.GetFieldInfo((IdentifiableEntity ei) => ei.toStr).Name;

        internal void FillLite(DataRow row, Lite lite)
        {
            FieldValue campo = (FieldValue)Fields[toStr].Field;
            lite.ToStr = (string)campo.GenerateValue(row, null);
            lite.Modified = false;
        }
    }

    public partial class RelationalTable
    {
        internal SqlPreCommand BatchSelect(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, new[] { "*" }, BackReference.Name, ids);
        }

        internal object FillItem(DataRow row, Retriever recuperador)
        {
            return Field.GenerateValue(row, recuperador); 
        }
    }

    public abstract partial class Field
    {
        internal abstract object GenerateValue(DataRow row, Retriever retriever);
    }

    public partial class FieldPrimaryKey
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            return row.Cell(Name); 
        }
    }

    public partial class FieldValue
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            return row.Cell(Name); 
        }
    }

    public partial class FieldReference
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            int? id = (int?)row.Cell(Name);

            if (!id.HasValue)
                if(!Nullable)throw new InvalidOperationException(Resources.Field0HasNullAndOsNotNullable.Formato(Name));
                else return null;

            if (IsLite)
                return retriever.GetLite(ReferenceTable, Reflector.ExtractLite(FieldType), id.Value);
            else
                return retriever.GetIdentifiable(ReferenceTable, id.Value); 
        }
    }

    public partial class FieldEnum
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            int? id = (int?)row.Cell(Name);
            if (!id.HasValue)
                if (!Nullable) throw new InvalidOperationException(Resources.Field0HasNullAndOsNotNullable.Formato(Name));
                else return null;

            return Enum.ToObject(FieldType.UnNullify(), id);
        }     
    }

    public partial class FieldMList
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            int myID = (int)row.Cell(SqlBuilder.PrimaryKeyName);

            return retriever.GetList(RelationalTable, myID); 
        }
    }

    public partial class FieldEmbedded
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            EmbeddedEntity result = Constructor();

            foreach (EntityField ef in EmbeddedFields.Values)
            {
                ef.Setter(result, ef.Field.GenerateValue(row, retriever)); 
            }

            result.Modified = false;
            retriever.PostRetrieving.Add(result); 

            return result; 
        }
    }

    public partial class FieldImplementedBy
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            var columns = ImplementationColumns.Where(c => row.Cell(c.Value.Name) != null).ToArray();

            if(columns.Length == 0)
                return null;
            
            if (columns.Length > 1)
                throw new ApplicationException(Resources.Fields0AreSetAtTheSameTime.Formato(columns.ToString(c=>c.Value.Name, ", ")));

            ImplementationColumn col = columns[0].Value;

            int? id = (int?)row.Cell(col.Name);

             if(IsLite)
                return retriever.GetLite(col.ReferenceTable, Reflector.ExtractLite(FieldType), id.Value);
            else
                return retriever.GetIdentifiable(col.ReferenceTable, id.Value); 
        }
    }

    public partial class FieldImplementedByAll
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            int? id = (int?)row.Cell(Column.Name);
            int? idTipo = (int?)row.Cell(ColumnTypes.Name);

            if (id.HasValue != idTipo.HasValue)
                throw new ApplicationException("ImplementedByAll {0} = {1} but {2} = {3}".Formato(Column.Name, id, ColumnTypes.Name, idTipo));

            if (id == null)
                return null;

            if (IsLite)
                return retriever.GetLite(Schema.Current.TablesForID[idTipo.Value], Reflector.ExtractLite(FieldType), id.Value);
            else
                return retriever.GetIdentifiable(Schema.Current.TablesForID[idTipo.Value], id.Value); 
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
