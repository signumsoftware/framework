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
        internal void Save(IdentifiableEntity ident, Forbidden forbidden)
        {
            bool isNew = ident.IsNew;

            if (ident.IsNew)
                Insert(ident, forbidden); 
            else
                Update(ident, forbidden); 

            if (forbidden.Count == 0)
                ident.Modified = null;

            foreach (var ef in Fields.Values.Where(e=>e.Field is FieldMList))
            {
                ((FieldMList)ef.Field).RelationalTable.RelationalInserts((Modifiable)ef.Getter(ident), isNew, ident, forbidden);
            }
        }

        public SqlPreCommand InsertSqlSync(IdentifiableEntity ident, string comment = null)
        {
            bool dirty = false; 
            ident.PreSaving(ref dirty); 

            List<SqlParameter> parameters = new List<SqlParameter>();

            if (!Identity)
                parameters.Add(SqlParameterBuilder.CreateIdParameter(ident.Id));

            foreach (var v in Fields.Values)
                v.Field.CreateParameter(parameters, v.Getter(ident), Forbidden.None);

            return SqlBuilder.InsertSync(Name, parameters, comment);
        }

        void Insert(IdentifiableEntity ident, Forbidden forbidden)
        {
            Entity ent = ident as Entity;
            if (ent != null)
                ent.Ticks = Transaction.StartTime.Ticks;

            List<SqlParameter> parameters = new List<SqlParameter>();
            foreach (var v in Fields.Values.Where(a=>!(a.Field is FieldPrimaryKey)))
                v.Field.CreateParameter(parameters, v.Getter(ident), forbidden);

            ident.IsNew = false;

            if (Identity)
            {
                if (ident.IdOrNull != null)
                    throw new InvalidOperationException("{0} is new, but has Id {1}".Formato(ident, ident.IdOrNull));

                ident.id = (int)SqlBuilder.InsertIdentity(Name, parameters).ExecuteScalar();
            }
            else
            {
                if (ident.IdOrNull == null)
                    throw new InvalidOperationException("{0} should have an Id, since the table has no Identity".Formato(ident, ident.IdOrNull));

                SqlParameter pid = SqlParameterBuilder.CreateIdParameter(ident.Id);

                parameters.Insert(0, pid);

                SqlBuilder.InsertNoIdentity(Name, parameters).ExecuteNonQuery();
            }
        }

        public SqlPreCommand UpdateSqlSync(IdentifiableEntity ident, string comment = null)
        {   
            if(comment == null)
                comment = ident.ToStr;

            bool dirty = false;
            ident.PreSaving(ref dirty);

            if (!ident.SelfModified)
                return null;

            List<SqlParameter> parameters = new List<SqlParameter>();
            foreach (var v in Fields.Values)
                v.Field.CreateParameter(parameters, v.Getter(ident), Forbidden.None);

            return SqlBuilder.UpdateSync(Name, parameters, ident.Id, comment);
        }

        void Update(IdentifiableEntity ident, Forbidden forbidden)
        {
            if (ident is Entity)
            {
                Entity entity = (Entity)ident;

                long oldTicks = entity.Ticks;
                entity.Ticks = Transaction.StartTime.Ticks;

                List<SqlParameter> parameters = new List<SqlParameter>();
                foreach (var v in Fields.Values)
                    v.Field.CreateParameter(parameters, v.Getter(entity), forbidden);

                SqlBuilder.UpdateEntity(Name, parameters, entity.Id, oldTicks).ToSimple().ExecuteNonQuery();
            }
            else
            {
                List<SqlParameter> parameters = new List<SqlParameter>();
                foreach (var v in Fields.Values)
                    v.Field.CreateParameter(parameters, v.Getter(ident), forbidden);

                SqlBuilder.Update(Name, parameters, ident.Id).ExecuteNonQuery();
            }
        }
    }

    public partial class RelationalTable
    { 
        internal void RelationalInserts(Modifiable collection, bool newEntity, IdentifiableEntity ident, Forbidden forbidden)
        {
            if (collection == null)
            {
                if (!newEntity)
                    SqlBuilder.RelationalDelete(Name, BackReference.Name, ident.Id).ExecuteNonQuery();
            }
            else
            {
                if (collection.Modified == false) // no es modificado ??
                    return;

                if (forbidden.Count == 0)
                    collection.Modified = null;

                if (!newEntity)
                    SqlBuilder.RelationalDelete(Name, BackReference.Name, ident.Id).ExecuteNonQuery();

                foreach (object item in (IEnumerable)collection)
                {
                    var list = new List<SqlParameter>();
                    BackReference.CreateParameter(list, ident, forbidden);
                    Field.CreateParameter(list, item, forbidden);

                    SqlBuilder.InsertIdentity(Name, list).ExecuteNonQuery();
                }
            }
        }
    }


    public abstract partial class Field
    {
        protected internal virtual void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden) { }
    }

    public partial class FieldPrimaryKey
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
        }
    }

    public partial class FieldValue 
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
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
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            parameters.Add(SqlParameterBuilder.CreateReferenceParameter(Name, Nullable, this.GetIdForLite(value, forbidden)));
        }
    }

    public partial class FieldEnum
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            base.CreateParameter(parameters, EnumProxy.FromEnum((Enum)value), forbidden);
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

        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            if (value == null)
            {
                if (HasValue != null)
                    parameters.Add(HasValueParameter(false));
                else
                    throw new InvalidOperationException("Impossible to save null on a not-nullable embedded field");
                
                foreach (var v in EmbeddedFields.Values)
                    v.Field.CreateParameter(parameters, null, forbidden);
            }
            else
            {
                 if (HasValue != null)
                     parameters.Add(HasValueParameter(true));

                EmbeddedEntity ec = (EmbeddedEntity)value;
                if (forbidden.Count == 0)
                    ec.Modified = null;
                foreach (var v in EmbeddedFields.Values)
                    v.Field.CreateParameter(parameters, v.Getter(value), forbidden);
            }
        }      
    }

    public partial class FieldImplementedBy
    {
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            Type valType = value == null ? null :
                value is Lite ? ((Lite)value).RuntimeType :
                value.GetType();

            if (valType != null && !ImplementationColumns.ContainsKey(valType))
                throw new InvalidOperationException("Type {0} is not a mapped type ({1})".Formato(valType, ImplementationColumns.Keys.ToString(k => k.Name, ", ")));

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
        protected internal override void CreateParameter(List<SqlParameter> parameters, object value, Forbidden forbidden)
        {
            parameters.Add(SqlParameterBuilder.CreateReferenceParameter(Column.Name, Column.Nullable, this.GetIdForLite(value, forbidden)));
            parameters.Add(SqlParameterBuilder.CreateReferenceParameter(ColumnTypes.Name, ColumnTypes.Nullable, this.GetTypeForLite(value, forbidden).TryCS(t => Schema.Current.TypeToId[t])));
        }
    }

}
