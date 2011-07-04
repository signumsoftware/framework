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
using Signum.Engine.Exceptions;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities.Reflection;
using System.Data;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Engine.Maps
{
    public class Forbidden: HashSet<IdentifiableEntity>
    {
        internal static readonly Forbidden None = new Forbidden();
    }

    public partial class Table
    {
      
        string sqlInsert;
        Func<IdentifiableEntity, Forbidden, List<SqlParameter>> insertParameters;
        Action<IdentifiableEntity, Forbidden> insert;

        void InitializeInsert()
        {
            var trios = new List<Table.Trio>();
            var assigments = new List<BinaryExpression>();
            var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
            var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");

            var cast = Expression.Parameter(Type, "casted");
            assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, Type)));

            foreach (var item in Fields.Values.Where(a=>!Identity || !(a.Field is FieldPrimaryKey)))
            {
                item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden);
            }

            sqlInsert = "INSERT {0} ({1})\r\n VALUES ({2})".Formato(Name.SqlScape(),
                trios.ToString(p => p.SourceColumn.SqlScape(), ", "),
                trios.ToString(p => p.ParameterName, ", "));

            if (Identity)
                sqlInsert += ";SELECT CONVERT(Int,SCOPE_IDENTITY()) AS [newID]";

            var expr = Expression.Lambda<Func<IdentifiableEntity, Forbidden, List<SqlParameter>>>(
                CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), paramIdent, paramForbidden);

            insertParameters = expr.Compile();

            if(Identity)
            {
                if(typeof(Entity).IsAssignableFrom(this.Type))
                {
                    insert = (ident, forbidden)=>
                    {
                        if (ident.IdOrNull != null)
                            throw new InvalidOperationException("{0} is new, but has Id {1}".Formato(ident, ident.IdOrNull));

                        ((Entity)ident).Ticks = Transaction.StartTime.Ticks;

                        ident.id = (int)new SqlPreCommandSimple(sqlInsert, insertParameters(ident, forbidden)).ExecuteScalar();
                    };
                }
                else
                {
                    insert = (ident, forbidden)=>
                    {
                        if (ident.IdOrNull != null)
                            throw new InvalidOperationException("{0} is new, but has Id {1}".Formato(ident, ident.IdOrNull));

                        ident.id = (int)new SqlPreCommandSimple(sqlInsert, insertParameters(ident, forbidden)).ExecuteScalar();
                    };
                }
            }
            else
            {
                if (typeof(Entity).IsAssignableFrom(this.Type))
                {
                    insert = (ident, forbidden) =>
                    {
                        if (ident.IdOrNull == null)
                            throw new InvalidOperationException("{0} should have an Id, since the table has no Identity".Formato(ident, ident.IdOrNull));

                        ((Entity)ident).Ticks = Transaction.StartTime.Ticks;

                        new SqlPreCommandSimple(sqlInsert, insertParameters(ident, forbidden)).ExecuteNonQuery();
                    };
                }
                else
                {
                    insert = (ident, forbidden) =>
                    {
                        if (ident.IdOrNull == null)
                            throw new InvalidOperationException("{0} should have an Id, since the table has no Identity".Formato(ident, ident.IdOrNull));

                        new SqlPreCommandSimple(sqlInsert, insertParameters(ident, forbidden)).ExecuteNonQuery();
                    };
                }
            }   
        }

        static FieldInfo fiId = ReflectionTools.GetFieldInfo((IdentifiableEntity i) => i.id);
        static FieldInfo fiTicks = ReflectionTools.GetFieldInfo((Entity i) => i.ticks);

        string sqlUpdate;
        Func<IdentifiableEntity, long, Forbidden, List<SqlParameter>> updateParameters;
        Action<IdentifiableEntity, Forbidden> update; 

        void InitializeUpdate()
        {
            var trios = new List<Trio>();
            var assigments = new List<BinaryExpression>();
            var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
            var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
            var paramOldTicks = Expression.Parameter(typeof(long), "oldTicks");

            var cast = Expression.Parameter(Type);
            assigments.Add(Expression.Assign(cast, Expression.Convert(paramIdent, Type)));

            foreach (var item in Fields.Values.Where(a =>!(a.Field is FieldPrimaryKey)))
            {
                item.Field.CreateParameter(trios, assigments, Expression.Field(cast, item.FieldInfo), paramForbidden);
            }

            string idParamName = SqlParameterBuilder.GetParameterName("id");

            sqlUpdate = "UPDATE {0} SET \r\n{1}\r\n WHERE id = {2}".Formato(Name.SqlScape(),
                    trios.ToString(p => "{0} = {1}".Formato(p.SourceColumn.SqlScape(), p.ParameterName).Indent(2), ",\r\n"),
                    idParamName);


            List<Expression> parameters = trios.Select(a => (Expression)a.ParameterBuilder).ToList();

            parameters.Add(SqlParameterBuilder.ParameterFactory(idParamName, SqlBuilder.PrimaryKeyType, false, Expression.Field(paramIdent, fiId)));

            if (typeof(Entity).IsAssignableFrom(this.Type))
            {
                string oldTicksParamName = SqlParameterBuilder.GetParameterName("old_ticks");

                sqlUpdate += " AND ticks = {0}".Formato(oldTicksParamName);

                parameters.Add(SqlParameterBuilder.ParameterFactory(oldTicksParamName, SqlDbType.BigInt, false, paramOldTicks));
            }

            var expr = Expression.Lambda<Func<IdentifiableEntity, long, Forbidden, List<SqlParameter>>>(
                CreateBlock(parameters, assigments), paramIdent, paramOldTicks, paramForbidden);

            updateParameters = expr.Compile();

            if (typeof(Entity).IsAssignableFrom(this.Type))
            {
                update = (ident, forbidden) =>
                {
                    Entity entity = (Entity)ident;

                    long oldTicks = entity.Ticks;
                    entity.Ticks = Transaction.StartTime.Ticks;

                    int num = (int)new SqlPreCommandSimple(sqlUpdate, updateParameters(ident, oldTicks, forbidden)).ExecuteNonQuery();
                    if (num != 1)
                        throw new ConcurrencyException(ident.GetType(), ident.Id);
                };
            }
            else
            {
                update = (ident, forbidden) =>
                {
                    int num = (int)new SqlPreCommandSimple(sqlUpdate, updateParameters(ident, -1, forbidden)).ExecuteNonQuery();
                    if (num != 1)
                        throw new EntityNotFoundException(ident.GetType(), ident.Id);
                };
            }
        }

        Action<IdentifiableEntity, Forbidden, bool> saveCollections;

        static MethodInfo miRelationalInserts = ReflectionTools.GetMethodInfo((RelationalTable rt) => rt.RelationalInserts(null, true, null, null));

        void InitializeCollections()
        {
            var paramIdent = Expression.Parameter(typeof(IdentifiableEntity), "ident");
            var paramForbidden = Expression.Parameter(typeof(Forbidden), "forbidden");
            var paramIsNew = Expression.Parameter(typeof(bool), "isNew");

            var entity = Expression.Parameter(Type);

            var castEntity = Expression.Assign(entity, Expression.Convert(paramIdent, Type));  

            var calls = Fields.Values.Where(ef => ef.Field is FieldMList)
                         .Select(ef=>(Expression)Expression.Call(Expression.Constant(((FieldMList)ef.Field).RelationalTable), miRelationalInserts,
                             Expression.Field(entity, ef.FieldInfo), paramIsNew, paramIdent, paramForbidden)).ToList();

            if (calls.IsEmpty())
                saveCollections = null;
            else
            {
                var exp = Expression.Lambda<Action<IdentifiableEntity, Forbidden, bool>>(Expression.Block(new[] { entity },
                    calls.PreAnd(castEntity)), paramIdent, paramForbidden, paramIsNew);

                saveCollections = exp.Compile();
            }
        }

        internal void Save(IdentifiableEntity ident, Forbidden forbidden)
        {
            using (HeavyProfiler.Log(role: "Table"))
            {
                bool isNew = ident.IsNew;

                if (isNew)
                {
                    insert(ident, forbidden);
                    ident.IsNew = false;
                }
                else
                {
                    update(ident, forbidden);
                }

                if (forbidden.Count == 0)
                    ident.Modified = null;

                if (saveCollections != null)
                    saveCollections(ident, forbidden, isNew);
            }
        }

        static readonly Forbidden NullForbidden = new Forbidden();

        public SqlPreCommand InsertSqlSync(IdentifiableEntity ident, string comment = null)
        {
            bool dirty = false; 
            ident.PreSaving(ref dirty);

            return new SqlPreCommandSimple(AddComment(sqlInsert, comment), insertParameters(ident, NullForbidden)); 
        }

        static string AddComment(string sql, string comment)
        {
            if (string.IsNullOrEmpty(comment))
                return sql;

            int index = sql.IndexOf("\r\n");
            if (index == -1)
                return sql + " -- " + comment;
            else
                return sql.Insert(index, " -- " + comment); 
        }

        public SqlPreCommand UpdateSqlSync(IdentifiableEntity ident, string comment = null)
        {   
            if(comment == null)
                comment = ident.ToStr;

            bool dirty = false;
            ident.PreSaving(ref dirty);

            if (!ident.SelfModified)
                return null;

            return new SqlPreCommandSimple(AddComment(sqlUpdate, comment), 
                updateParameters(ident, (ident as Entity).TryCS(a => a.Ticks) ?? -1, NullForbidden));
        }

        public class Trio
        {
            public Trio(IColumn column, Expression value)
            {
                this.SourceColumn = column.Name;
                this.ParameterName = SqlParameterBuilder.GetParameterName(column.Name);
                this.ParameterBuilder = SqlParameterBuilder.ParameterFactory(this.ParameterName, column.SqlDbType, column.Nullable, value);
            }

            public string SourceColumn;
            public string ParameterName;
            public MemberInitExpression ParameterBuilder; //Expression<SqlParameter>

            public override string ToString()
            {
                return "{0} {1} {2}".Formato(SourceColumn, ParameterName, ParameterBuilder.NiceToString());
            }
        }

        static ConstructorInfo ciNewList = ReflectionTools.GetConstuctorInfo(() => new List<SqlParameter>(1));

        public static Expression CreateBlock(IEnumerable<Expression> parameters, IEnumerable<BinaryExpression> assigments)
        {
            return Expression.Block(assigments.Select(a => (ParameterExpression)a.Left),
                assigments.Cast<Expression>().And(
                Expression.ListInit(Expression.New(ciNewList, Expression.Constant(parameters.Count())),
                parameters)));
        }
    }

    public partial class RelationalTable
    {
        string sqlDelete;
        string sqlInsert;
        Func<IdentifiableEntity, object, Forbidden, List<SqlParameter>> ParameterBuilder;

        void InitializeSaveSql()
        {
            DeleteSql();

            InsertSql();
        }

        private void DeleteSql()
        {
            sqlDelete = "DELETE {0} WHERE {1} = @{1}".Formato(Name.SqlScape(), BackReference.Name);
        }

        private void InsertSql()
        {
            var trios = new List<Table.Trio>();
            var assigments = new List<BinaryExpression>();
            var ident = Expression.Parameter(typeof(IdentifiableEntity),"ident");
            var item = Expression.Parameter(typeof(object),"item");
            var forbidden = Expression.Parameter(typeof(Forbidden),"forbidden");

            BackReference.CreateParameter(trios, assigments, ident, forbidden);
            Field.CreateParameter(trios, assigments, item, forbidden);

            sqlInsert = "INSERT {0} ({1})\r\n VALUES ({2})".Formato(Name.SqlScape(),
                trios.ToString(p => p.SourceColumn.SqlScape(), ", "),
                trios.ToString(p => p.ParameterName, ", "));

            var expr = Expression.Lambda<Func<IdentifiableEntity, object, Forbidden, List<SqlParameter>>>(
                Table.CreateBlock(trios.Select(a => a.ParameterBuilder), assigments), ident, item, forbidden);

            ParameterBuilder = expr.Compile();
        }

        internal static SqlPreCommandSimple InsertIdentity(string table, List<SqlParameter> parameters)
        {
            return new SqlPreCommandSimple("INSERT {0} ({1})\r\n VALUES ({2}); SELECT CONVERT(Int,SCOPE_IDENTITY()) AS [newID]".Formato(table,
                    parameters.ToString(p => p.SourceColumn.SqlScape(), ", "),
                    parameters.ToString(p => p.ParameterName, ", ")), parameters);
        }

        internal void RelationalInserts(Modifiable collection, bool newEntity, IdentifiableEntity ident, Forbidden forbidden)
        {
            if (collection == null)
            {
                if (!newEntity)
                    new SqlPreCommandSimple(sqlDelete,
                        new List<SqlParameter> { SqlParameterBuilder.CreateReferenceParameter(BackReference.Name, false, ident.Id) }).ExecuteNonQuery();
            }
            else
            {
                if (collection.Modified == false) // no es modificado ??
                    return;

                if (forbidden.Count == 0)
                    collection.Modified = null;

                if (!newEntity)
                    new SqlPreCommandSimple(sqlDelete,
                        new List<SqlParameter> { SqlParameterBuilder.CreateReferenceParameter(BackReference.Name, false, ident.Id) }).ExecuteNonQuery();

                foreach (object item in (IEnumerable)collection)
                {
                    new SqlPreCommandSimple(sqlInsert,
                        ParameterBuilder(ident, item, forbidden)).ExecuteNonQuery();
                }
            }
        }
    }


    public abstract partial class Field
    {
        protected internal virtual void CreateParameter(List<Table.Trio> trios, List<BinaryExpression> assigments, Expression value, Expression forbidden) { }
    }

    public partial class FieldPrimaryKey
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<BinaryExpression> assigments, Expression value, Expression forbidden)
        {
            trios.Add(new Table.Trio(this, value));
        }
    }

    public partial class FieldValue 
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<BinaryExpression> assigments, Expression value, Expression forbidden)
        {
            trios.Add(new Table.Trio(this, value));
        }
    }

    public static partial class FieldReferenceExtensions
    {
        static MethodInfo miGetIdForLite = ReflectionTools.GetMethodInfo(() => GetIdForLite(null, null));
        static MethodInfo miGetIdForEntity = ReflectionTools.GetMethodInfo(() => GetIdForEntity(null, null));

        public static Expression GetIdFactory(this IFieldReference fr, Expression value, Expression forbidden)
        {
            return Expression.Call(fr.IsLite ? miGetIdForLite : miGetIdForEntity, value, forbidden); 
        }

        static int? GetIdForLite(object value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            Lite l = (Lite)value;
            return l.UntypedEntityOrNull == null ? l.Id :
                   forbidden.Contains(l.UntypedEntityOrNull) ? (int?)null :
                   l.RefreshId();
        }

        static int? GetIdForEntity(object value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            IdentifiableEntity ie = (IdentifiableEntity)value;
            return forbidden.Contains(ie) ? (int?)null : ie.Id;
        }

        static MethodInfo miGetTypeForLite = ReflectionTools.GetMethodInfo(() => GetTypeForLite(null, null));
        static MethodInfo miGetTypeForEntity = ReflectionTools.GetMethodInfo(() => GetTypeForEntity(null, null));

        public static Expression GetTypeFactory(this IFieldReference fr, Expression value, Expression forbidden)
        {
            return Expression.Call(fr.IsLite ? miGetTypeForLite : miGetTypeForEntity, value, forbidden);
        }

        static Type GetTypeForLite(object value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            Lite l = (Lite)value;
            return l.UntypedEntityOrNull == null ? l.RuntimeType :
                 forbidden.Contains(l.UntypedEntityOrNull) ? null :
                 l.RuntimeType;
        }

        static Type GetTypeForEntity(object value, Forbidden forbidden)
        {
            if (value == null)
                return null;

            IdentifiableEntity ie = (IdentifiableEntity)value;
            return forbidden.Contains(ie) ? null : ie.GetType();
        }
    }

    public partial class FieldReference
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<BinaryExpression> assigments, Expression value, Expression forbidden)
        {
            trios.Add(new Table.Trio(this, this.GetIdFactory(value, forbidden)));
        }
    }

    public partial class FieldEnum
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<BinaryExpression> assigments, Expression value, Expression forbidden)
        {
            trios.Add(new Table.Trio(this, Expression.Convert(value, Nullable ? typeof(int?) : typeof(int))));
        }
    }

    public partial class FieldMList
    {
    }

    public partial class FieldEmbedded
    {    
        protected internal override void CreateParameter(List<Table.Trio> trios, List<BinaryExpression> assigments, Expression value, Expression forbidden)
        {
            ParameterExpression embedded = Expression.Parameter(this.FieldType, "embedded");

            if (HasValue != null)
            {
                trios.Add(new Table.Trio(HasValue, Expression.NotEqual(value, Expression.Constant(null, FieldType))));

                assigments.Add(Expression.Assign(embedded, Expression.Convert(value, this.FieldType)));

                foreach (var ef in EmbeddedFields.Values)
                {
                    ef.Field.CreateParameter(trios, assigments,
                        Expression.Condition(
                            Expression.Equal(embedded, Expression.Constant(null, this.FieldType)),
                            Expression.Constant(null, ef.FieldInfo.FieldType.Nullify()),
                            Expression.Field(embedded, ef.FieldInfo).Nullify()), forbidden);
                }
            }
            else
            {
                assigments.Add(Expression.Assign(embedded, Expression.Convert(value.NodeType == ExpressionType.Conditional? value: Expression.Call(Expression.Constant(this), miCheckNull, value), this.FieldType)));

                foreach (var ef in EmbeddedFields.Values)
                {
                    ef.Field.CreateParameter(trios, assigments,
                        Expression.Field(embedded, ef.FieldInfo), forbidden);
                }

            }
        }

        static MethodInfo miCheckNull = ReflectionTools.GetMethodInfo((FieldEmbedded fe) => fe.CheckNull(null));
        object CheckNull(object obj)
        {
            if(obj == null)
                throw new InvalidOperationException("Impossible to save 'null' on the not-nullable embedded field of type '{0}'".Formato(this.FieldType.Name));

            return obj;
        }
    }

    public partial class FieldImplementedBy
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<BinaryExpression> assigments, Expression value, Expression forbidden)
        {
            ParameterExpression ibType = Expression.Parameter(typeof(Type), "ibType");
            ParameterExpression ibId = Expression.Parameter(typeof(int?), "ibId");

            assigments.Add(Expression.Assign(ibType, Expression.Call(Expression.Constant(this), miCheckType, this.GetTypeFactory(value, forbidden))));
            assigments.Add(Expression.Assign(ibId, this.GetIdFactory(value, forbidden))); 

            var nullId = Expression.Constant(null, typeof(int?));

            foreach (var imp in ImplementationColumns)
            {
                trios.Add(new Table.Trio(imp.Value,
                    Expression.Condition(Expression.Equal(ibType, Expression.Constant(imp.Key)), ibId, Expression.Constant(null, typeof(int?)))
                    ));
            }
        }

        static MethodInfo miCheckType = ReflectionTools.GetMethodInfo((FieldImplementedBy fe) => fe.CheckType(null));
        
        Type CheckType(Type type)
        {
            if(type != null && !ImplementationColumns.ContainsKey(type))
                throw new InvalidOperationException("Type {0} is not a mapped type ({1})".Formato(type.Name, ImplementationColumns.Keys.ToString(k => k.Name, ", ")));

            return type;
        }
    }

    public partial class ImplementationColumn
    {

    }

    public partial class FieldImplementedByAll
    {
        protected internal override void CreateParameter(List<Table.Trio> trios, List<BinaryExpression> assigments, Expression value, Expression forbidden)
        {
            trios.Add(new Table.Trio(Column, this.GetIdFactory(value, forbidden)));
            trios.Add(new Table.Trio(ColumnTypes, Expression.Call(Expression.Constant(this), miConvertType, this.GetTypeFactory(value, forbidden))));
        }

        static MethodInfo miConvertType = ReflectionTools.GetMethodInfo((FieldImplementedByAll fe) => fe.ConvertType(null));

        int? ConvertType(Type type)
        {
            if (type == null)
                return null;

            return Schema.Current.TypeToId.GetOrThrow(type, "{0} not registered in the schema"); 
        }
    }

    
}
