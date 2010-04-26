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
            var collectionFields = Fields.Values.Select(f=>f.Field).OfType<FieldMList>();

            SqlPreCommand entity = ident.IsNew ? InsertSql(ident, forbidden) : UpdateSql(ident, forbidden);

            SqlPreCommand cols = (from ef in Fields.Values
                                  where ef.Field is FieldMList
                                  select ((FieldMList)ef.Field).RelationalTable.RelationalInserts((Modifiable)ef.Getter(ident), ident.IsNew, forbidden)).Combine(Spacing.Simple);

            if (forbidden.Count == 0)
                ident.Modified = false;

            return SqlPreCommand.Combine(Spacing.Double, entity, cols);
        }

        public SqlPreCommand InsertSqlSync(IdentifiableEntity ident)
        {
            bool dirty = false; 
            ident.PreSaving(ref dirty); 

            List<SqlParameter> parameters = new List<SqlParameter>();

            foreach (var v in Fields.Values)
                v.Field.CreateParameter(parameters, v.Getter(ident), SqlCommandType.Insert, Forbidden.None);
            
            return SqlBuilder.Insert(Name, parameters);
        }

        SqlPreCommand InsertSql(IdentifiableEntity ident, Forbidden forbidden)
        {
            Entity ent = ident as Entity;
            if (ent != null)
                ent.Ticks = Transaction.StartTime.Ticks;

            List<SqlParameter> parameters = new List<SqlParameter>();
            foreach (var v in Fields.Values)
                v.Field.CreateParameter(parameters, v.Getter(ident), SqlCommandType.Insert, forbidden);

            ident.IsNew = false; 

            if (Identity)
            {
                if (ident.IdOrNull != null)
                    throw new InvalidOperationException(Resources.IsNewButHasId1AndIdentityIsTrue.Formato(ident, ident.IdOrNull)); 

                return SqlBuilder.InsertSaveId(Name, parameters, ident);
            }
            else
            {
                return SqlBuilder.Insert(Name, parameters);
            }                               
        }

        public SqlPreCommand UpdateSqlSync(IdentifiableEntity ident)
        {
            string oldToStr = ident.ToStr;

            bool dirty = false;
            ident.PreSaving(ref dirty);

            if (!ident.SelfModified)
                return null;

            List<SqlParameter> parameters = new List<SqlParameter>();
            foreach (var v in Fields.Values)
                v.Field.CreateParameter(parameters, v.Getter(ident), SqlCommandType.Update, Forbidden.None);

            return SqlBuilder.UpdateId(Name, parameters, ident.Id, oldToStr);
        }

        SqlPreCommand UpdateSql(IdentifiableEntity ident, Forbidden forbidden)
        {
            if (ident is Entity)
            {
                Entity entity = (Entity)ident;

                long oldTicks = entity.Ticks;
                entity.Ticks = Transaction.StartTime.Ticks;

                List<SqlParameter> parameters = new List<SqlParameter>();
                foreach (var v in Fields.Values)
                    v.Field.CreateParameter(parameters, v.Getter(entity), SqlCommandType.Update, forbidden);
                return SqlBuilder.UpdateSetIdEntity(Name, parameters, entity.Id, oldTicks);
            }
            else
            {
                List<SqlParameter> parameters = new List<SqlParameter>();
                foreach (var v in Fields.Values)
                    v.Field.CreateParameter(parameters, v.Getter(ident), SqlCommandType.Update, forbidden);
                return SqlBuilder.UpdateSetId(Name, parameters, ident.Id);
            }
        }
    }

    public partial class RelationalTable
    { 
        internal SqlPreCommand RelationalInserts(Modifiable collection, bool newEntity, Forbidden forbidden)
        {
            if (collection == null)
                return newEntity? null: SqlBuilder.RelationalDeleteScope(Name, BackReference.Name); 

            if (!collection.Modified) // no es modificado ??
                return null;

            if (forbidden.Count == 0)
                collection.Modified = false;

            var clean = newEntity ? null : SqlBuilder.RelationalDeleteScope(Name, BackReference.Name);

            var inserts = ((IEnumerable)collection).Cast<object>()
                .Select(o => SqlBuilder.RelationalInsertScope(Name, BackReference.Name,
                    new List<SqlParameter>().Do(lp => Field.CreateParameter(lp, o, SqlCommandType.Insert, forbidden))))
                .Combine(Spacing.Simple);

            return SqlPreCommand.Combine(Spacing.Double, clean, inserts); 
        }
    }


    public enum SqlCommandType
    {
        Insert,
        Update
    }

    public abstract partial class Field
    {
        protected internal virtual void CreateParameter(List<SqlParameter> parameters, object value, SqlCommandType type, Forbidden forbidden) { }
    }

    public partial class FieldPrimaryKey
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, SqlCommandType type, Forbidden forbidden)
        {
            if (!table.Identity && type == SqlCommandType.Insert)
                parameters.Add(SqlParameterBuilder.CreateReferenceParameter(Name, false, (int?)value));
        }
    }

    public partial class FieldValue 
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, SqlCommandType type, Forbidden forbidden)
        {
            parameters.Add(SqlParameterBuilder.CreateParameter(Name, SqlDbType, Nullable, value)); 
        }
    }

    public static partial class ReferenceFieldExtensions
    {
        public static int? GetIdForLite(this IFieldReference cr, object value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            if (cr.IsLite)
            {
                Lite l = (Lite)value;
                return l.UntypedEntityOrNull == null ? l.Id :
                       forbidden.Contains(l.UntypedEntityOrNull) ? (int?)null :
                       l.RefreshId();
            }
            else
            {
                IdentifiableEntity ie = (IdentifiableEntity)value;
                return forbidden.Contains(ie) ? (int?)null : ie.Id;
            }
        }

        public static Type GetTypeForLite(this IFieldReference cr, object value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            if (cr.IsLite)
            {
                Lite l = (Lite)value;
                return l.UntypedEntityOrNull == null ? l.RuntimeType :
                     forbidden.Contains(l.UntypedEntityOrNull) ? null :
                     l.RuntimeType;
            }
            else
            {
                IdentifiableEntity ie = (IdentifiableEntity)value;
                return forbidden.Contains(ie) ? null : ie.GetType();
            }
        }
    }

    public partial class FieldReference
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, SqlCommandType type, Forbidden forbidden)
        {
            parameters.Add(SqlParameterBuilder.CreateReferenceParameter(Name, Nullable, this.GetIdForLite(value, forbidden)));
        }
    }

    public partial class FieldEnum
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, SqlCommandType type, Forbidden forbidden)
        {
            base.CreateParameter(parameters, EnumProxy.FromEnum((Enum)value), type, forbidden);
        }
    }

    public partial class FieldMList
    {
    }

    public partial class FieldEmbedded
    {
        SqlParameter HasValueParameter(bool hasValue)
        {
            return SqlParameterBuilder.CreateParameter(HasValue.Name, HasValue.SqlDbType, HasValue.Nullable, hasValue);
        }

        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, SqlCommandType type, Forbidden forbidden)
        {
            if (value == null)
            {
                if (HasValue != null)
                    parameters.Add(HasValueParameter(false));
                else
                    throw new InvalidOperationException(Resources.ImpossibleToSaveNullOnANotNullableEmbeddedFieldUseNullable);
                
                foreach (var v in EmbeddedFields.Values)
                    v.Field.CreateParameter(parameters, null, type, forbidden);
            }
            else
            {
                 if (HasValue != null)
                     parameters.Add(HasValueParameter(true));

                EmbeddedEntity ec = (EmbeddedEntity)value;
                if (forbidden.Count == 0)
                    ec.Modified = false;
                foreach (var v in EmbeddedFields.Values)
                    v.Field.CreateParameter(parameters, v.Getter(value), type, forbidden);
            }
        }      
    }

    public partial class FieldImplementedBy
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, SqlCommandType type, Forbidden forbidden)
        {
            Type valType = value == null ? null :
                value is Lite ? ((Lite)value).RuntimeType :
                value.GetType();

            if (valType != null && !ImplementationColumns.ContainsKey(valType))
                throw new InvalidOperationException(Resources.Type0IsNotAMappedType1.Formato(valType, ImplementationColumns.Keys.ToString(k => k.Name, ", ")));

            var param = ImplementationColumns.Select(p =>
                SqlParameterBuilder.CreateReferenceParameter(p.Value.Name, true,
                       p.Key != valType ? null : this.GetIdForLite(value, forbidden))).ToList();

            parameters.AddRange(param);          
        }     
    }

    public partial class ImplementationColumn
    {

    }

    public partial class FieldImplementedByAll
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, SqlCommandType type, Forbidden forbidden)
        {
            if (value != null)
            {   
                parameters.Add(SqlParameterBuilder.CreateReferenceParameter(Column.Name, Column.Nullable, this.GetIdForLite(value, forbidden)));
                parameters.Add(SqlParameterBuilder.CreateReferenceParameter(ColumnTypes.Name, ColumnTypes.Nullable, this.GetTypeForLite(value, forbidden).TryCS(t => Schema.Current.IDsForType[t])));
            }
        }
    }

}
