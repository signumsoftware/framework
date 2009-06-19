using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using Signum.Engine.Properties;
using Signum.Entities.Reflection;

namespace Signum.Engine.Maps
{
    public class Forbidden: HashSet<IdentifiableEntity>
    {
        internal static readonly Forbidden None = new Forbidden();

        public IdentifiableEntity Filter(IdentifiableEntity ei)
        {
            return Contains(ei) ? null : ei; 
        }
    }

    public partial class Table
    {
        public SqlPreCommand Save(IdentifiableEntity ident, Forbidden forbidden)
        {   
            var collectionFields = Fields.Values.Select(f=>f.Field).OfType<MListField>();

            SqlPreCommand entity = ident.IsNew ? InsertSql(ident, forbidden) : UpdateSql(ident, forbidden);

            SqlPreCommand cols = (from ef in Fields.Values
                                  where ef.Field is MListField
                                  select ((MListField)ef.Field).RelationalTable.RelationalInserts((Modifiable)ef.Getter(ident), forbidden)).Combine(Spacing.Simple);

            ident.Modified = forbidden.Count > 0;

            return SqlPreCommand.Combine(Spacing.Double, entity, cols);
        }

        public SqlPreCommand InsertSqlSync(IdentifiableEntity ident)
        {
            ident.PreSaving(); 

            List<SqlParameter> parameters = new List<SqlParameter>();
            Fields.Values.ForEach(v => v.Field.CreateParameter(parameters, v.Getter(ident), Forbidden.None));

            return SqlBuilder.Insert(Name, parameters);
        }

        SqlPreCommand InsertSql(IdentifiableEntity ident, Forbidden forbidden)
        {
            Entity ent = ident as Entity;
            if (ent != null)
                ent.Ticks = Transaction.StartTime.Ticks;

            List<SqlParameter> parameters = new List<SqlParameter>();
            Fields.Values.ForEach(v => v.Field.CreateParameter(parameters, v.Getter(ident), forbidden));

            return Identity  ? SqlBuilder.InsertSaveId(Name, parameters, ident) :
                               SqlBuilder.Insert(Name, parameters);
        }

        public SqlPreCommand UpdateSqlSync(IdentifiableEntity ident)
        {
            ident.PreSaving(); 

            if (!ident.SelfModified)
                return null;

            List<SqlParameter> parameters = new List<SqlParameter>();
            Fields.ForEach(c => c.Value.Field.CreateParameter(parameters, c.Value.Getter(ident), Forbidden.None));
            return SqlBuilder.UpdateId(Name, parameters, ident.Id);
        }

        SqlPreCommand UpdateSql(IdentifiableEntity ident, Forbidden forbidden)
        {
            if (ident is Entity)
            {
                Entity entity = (Entity)ident;

                long oldTicks = entity.Ticks;
                entity.Ticks = Transaction.StartTime.Ticks;

                List<SqlParameter> parameters = new List<SqlParameter>();
                Fields.ForEach(c => c.Value.Field.CreateParameter(parameters, c.Value.Getter(entity), forbidden));
                return SqlBuilder.UpdateSetIdEntity(Name, parameters, entity.Id, oldTicks);
            }
            else
            {
                List<SqlParameter> parameters = new List<SqlParameter>();
                Fields.ForEach(c => c.Value.Field.CreateParameter(parameters, c.Value.Getter(ident), forbidden));
                return SqlBuilder.UpdateSetId(Name, parameters, ident.Id);
            }
        }
    }

    public partial class RelationalTable
    { 
        internal SqlPreCommand RelationalInserts(Modifiable collection, Forbidden forbidden)
        {
            if (collection == null)
                return SqlBuilder.RelationalDeleteScope(Name, BackReference.Name); 

            if (!collection.Modified) // no es modificado ??
                return null;

            collection.Modified = forbidden.Count > 0;


            var clean = SqlBuilder.RelationalDeleteScope(Name, BackReference.Name); 

            var inserts =  ((IEnumerable)collection).Cast<object>()
                .Where(o => (o as IdentifiableEntity).TryCS(e => !forbidden.Contains(e)) ?? true)
                .Select(o => SqlBuilder.RelationalInsertScope(Name, BackReference.Name,
                    new List<SqlParameter>().Do(lp => Field.CreateParameter(lp, o, forbidden))))
                .Combine(Spacing.Simple);

            return SqlPreCommand.Combine(Spacing.Double, clean, inserts); 
        }
    }

    public abstract partial class Field
    {
        protected internal virtual void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden) { }
    }

    public partial class PrimaryKeyField
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            if (!Identity)
                parameters.Add(SqlParameterBuilder.CreateReferenceParameter(Name, false, (int?)value));
        }
    }

    public partial class ValueField 
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            parameters.Add(SqlParameterBuilder.CreateParameter(Name, SqlDbType, Nullable, value)); 
        }
    }

    public static partial class ReferenceFieldExtensions
    {
        public static int? GetIdForLazy(this IReferenceField cr, object value, Forbidden forbidden)
        {
            return value == null ? null :
                      cr.IsLazy ? ((Lazy)value).Map(l => l.UntypedEntityOrNull == null ? l.Id :
                                             forbidden.Contains(l.UntypedEntityOrNull) ? (int?)null :
                                             l.RefreshId()) :
                     ((IdentifiableEntity)value).Map(ei => forbidden.Contains(ei) ? (int?)null : ei.Id);
        }

        public static Type GetTypeForLazy(this IReferenceField cr, object value, Forbidden forbidden)
        {
            return value == null ? null :
                      cr.IsLazy ? ((Lazy)value).Map(l => l.UntypedEntityOrNull == null ? l.RuntimeType :
                                             forbidden.Contains(l.UntypedEntityOrNull) ? null :
                                             l.RuntimeType) :
                     ((IdentifiableEntity)value).Map(ei => forbidden.Contains(ei) ? null : ei.GetType());
        }
    }

    public partial class ReferenceField
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            parameters.Add(SqlParameterBuilder.CreateReferenceParameter(Name, Nullable, this.GetIdForLazy(value, forbidden)));
        }
    }

    public partial class EnumField
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            base.CreateParameter(parameters, EnumProxy.FromEnum((Enum)value), forbidden);
        }
    }

    public partial class MListField
    {
    }

    public partial class EmbeddedField
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            if (value != null)
            {
                EmbeddedEntity ec = (EmbeddedEntity)value;
                ec.Modified = false; 
                EmbeddedFields.ForEach(c => c.Value.Field.CreateParameter(parameters, c.Value.Getter(value), forbidden));
            }
        }      
    }

    public partial class ImplementedByField
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            Type valType = value == null ? null :
                value is Lazy ? ((Lazy)value).RuntimeType :
                value.GetType();

            var param = ImplementationColumns.Select(p =>
                SqlParameterBuilder.CreateReferenceParameter(p.Value.Name, true,
                       p.Key != valType ? null : this.GetIdForLazy(value, forbidden))).ToList();

            if (value != null && !param.Any(p => p.Value != null))
                throw new InvalidOperationException(Resources.Type0IsNotAMappedType1.Formato(value.GetType(), ImplementationColumns.Keys.ToString(k => k.Name, ", ")));

            parameters.AddRange(param);          
        }     
    }

    public partial class ImplementationColumn
    {

    }

    public partial class ImplementedByAllField
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            if (value != null)
            {   
                parameters.Add(SqlParameterBuilder.CreateReferenceParameter(Column.Name, Column.Nullable, this.GetIdForLazy(value, forbidden)));
                parameters.Add(SqlParameterBuilder.CreateReferenceParameter(ColumnTypes.Name,ColumnTypes.Nullable, this.GetTypeForLazy(value, forbidden).TryCS(t => Schema.Current.IDsForType[t])));
            }
        }
    }

}
