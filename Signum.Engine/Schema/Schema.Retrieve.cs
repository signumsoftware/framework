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
            return SqlBuilder.SelectByIds(Name, new[] { "*" }, SqlBuilder.PrimaryKeyName, ids); 
        }

        internal SqlPreCommand BatchSelectLazy(int[] ids)
        {
            return SqlBuilder.SelectByIds(Name, new[] { SqlBuilder.PrimaryKeyName, SqlBuilder.ToStrName }, SqlBuilder.PrimaryKeyName, ids); 
        }

        internal void Fill(DataRow row, IdentifiableEntity ei, Retriever retriever)
        {
            foreach (Field field in Fields.Values)
            {
                field.Setter(ei, field.GenerateValue(row, retriever)); 
            }
            
            ei.Modified = false; 
        }

        internal void FillLazy(DataRow row, Lazy lazy)
        {
            ValueField campo = (ValueField)Fields[ ReflectionTools.GetFieldInfo<IdentifiableEntity>(ei=>ei.toStr).Name];
            lazy.ToStr = (string)campo.GenerateValue(row, null);
            lazy.Modified = false;
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

    public partial class PrimaryKeyField
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            return row.Cell(Name); 
        }
    }

    public partial class ValueField
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            return row.Cell(Name); 
        }
    }

    public partial class ReferenceField
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            int? id = (int?)row.Cell(Name);

            if (!id.HasValue)
                if(!Nullable)throw new InvalidOperationException(Resources.Field0HasNullAndOsNotNullable.Formato(Name));
                else return null;

            if (IsLazy)
                return retriever.GetLazy(ReferenceTable, FieldType, id.Value);
            else
                return retriever.GetIdentifiable(ReferenceTable, id.Value); 
        }
    }

    public partial class EnumField
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            int? id = (int?)row.Cell(Name);
            if (!id.HasValue)
                if (!Nullable) throw new InvalidOperationException(Resources.Field0HasNullAndOsNotNullable.Formato(Name));
                else return null;

            return Enum.ToObject(FieldType, id);
        }     
    }

    public partial class CollectionField
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            int myID = (int)row.Cell(SqlBuilder.PrimaryKeyName);

            return retriever.GetList(RelationalTable, FieldType, myID); 
        }
    }

    public partial class EmbeddedField
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            EmbeddedEntity result = Constructor();

            foreach (Field field in EmbeddedFields.Values)
            {
                field.Setter(result, field.GenerateValue(row, retriever)); 
            }

            result.Modified = false;
            retriever.PostRetrieving.Add(result); 

            return result; 
        }
    }

    public partial class ImplementedByField
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            return ImplementationColumns.Select(c => c.Value.GenerateValue(row, retriever, FieldType, IsLazy)).NotNull().SingleOrDefault(Resources.ThereIsMoreThanOneValueFor0.Formato(FieldInfo.Name)); 
        }
    }

    public partial class ImplementationColumn
    {
        internal object GenerateValue(DataRow row, Retriever retriever, Type type, bool isLazy)
        {
            int? id = (int?)row.Cell(Name);

            if (!id.HasValue)
                return null;

            if (isLazy)
                return retriever.GetLazy(ReferenceTable, type, id.Value);
            else
                return retriever.GetIdentifiable(ReferenceTable, id.Value); 
        }
    }

    public partial class ImplementedByAllField
    {
        internal override object GenerateValue(DataRow row, Retriever retriever)
        {
            int? id = (int?)row.Cell(Column.Name);
            int? idTipo = (int?)row.Cell(ColumnTypes.Name);

            if (id.HasValue != idTipo.HasValue)
                throw new ApplicationException("ImplementedByAll {0} = {1} pero {2} = {3}".Formato(Column.Name, id, ColumnTypes.Name, idTipo));

            if (id == null)
                return null;

            if (IsLazy)
                return retriever.GetLazy(Schema.Current.TablesForID[idTipo.Value], FieldType, id.Value);
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
