using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using System.Collections.Concurrent;
using Signum.Engine.Maps;
using Signum.Utilities.Reflection;
using Signum.Engine.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Signum.Utilities;
using Signum.Entities.Cache;
using System.Data.SqlClient;
using Signum.Engine.Basics;

namespace Signum.Engine.Cache
{
    abstract class CachedTableBase 
    {
        Dictionary<Field, CachedTableBase> subTables;

        protected Type tupleType; 
        protected Func<FieldReader, object> reader;
        protected SqlPreCommandSimple query;
        protected List<IColumn> columns;


        Expression Clone(Expression origin, ParameterExpression retriever, Field field, out CachedTableBase table)
        {
            if (field is FieldValue)
                return TupleReflection.TupleChainProperty(origin, columns.IndexOf((IColumn)field));

            if (field is FieldEnum)
                return Expression.Convert(TupleReflection.TupleChainProperty(origin, columns.IndexOf((IColumn)field)), field.FieldType);

            if (field is IFieldReference)
            {
                var nullref = Expression.Constant(null, field.FieldType);

                if (((IFieldReference)field).IsLite)
                {
                    Expression call = Expression.Call(origin, field.FieldType.GetMethod("Clone"));

                    return Expression.Condition(Expression.Equal(origin, nullref), nullref, call);
                }

                if (field is FieldReference)
                {
                    Expression call = Expression.Call(miCallComplete.MakeGenericMethod(field.FieldType), origin, retriever);

                    return Expression.Condition(Expression.Equal(origin, nullref), nullref, call);
                }

                if (field is FieldImplementedBy)
                {
                    var ib = (FieldImplementedBy)field;

                    var call = ib.ImplementationColumns.Keys.Aggregate((Expression)nullref, (acum, t) => Expression.Condition(
                        Expression.Equal(Expression.Call(origin, miGetType), Expression.Constant(t)),
                        Expression.Convert(Expression.Call(miCallComplete.MakeGenericMethod(t), Expression.Convert(origin, t), retriever), field.FieldType),
                        acum));

                    return Expression.Condition(Expression.Equal(origin, nullref), nullref, call);
                }

                if (field is FieldImplementedByAll)
                {
                    throw new InvalidOperationException("You can not cache entities with ImplementedByAll");
                }
            }

            if (field is FieldEmbedded)
            {
                var nullref = Expression.Constant(null, field.FieldType);

                var fe = (FieldEmbedded)field;

                Expression ctor = Expression.MemberInit(Expression.New(fe.FieldType),
                    fe.EmbeddedFields.Values.Select(f => Expression.Bind(f.FieldInfo, Clone(Expression.Field(origin, f.FieldInfo), f.Field, retriever))).And(resetModified));

                return Expression.Condition(Expression.Equal(origin, nullref), nullref, ctor);
            }

            if (field is FieldMList)
            {
                var mlistField = (FieldMList)field;

                var elemField = mlistField.RelationalTable.Field;

                ParameterExpression pElement = Expression.Parameter(elemField.FieldType);

                var body = Clone(pElement, elemField, retriever);

                Expression collection = pElement == body ? origin : Expression.Call(null, miSelectE.MakeGenericMethod(elemField.FieldType, elemField.FieldType), origin, Expression.Lambda(body, pElement));

                var ci = mlistField.FieldType.GetConstructor(new[] { typeof(IEnumerable<>).MakeGenericType(elemField.FieldType) });

                return Expression.MemberInit(Expression.New(ci, collection), resetModified);
            }

            throw new InvalidOperationException("Unexpected {0}".Formato(field.GetType().Name));
        }


      
        static MethodInfo miCallComplete = ReflectionTools.GetMethodInfo(() => CallComplete<IdentifiableEntity>(null, null)).GetGenericMethodDefinition();

        protected Type GetColumnType(IColumn column)
        {
            if (column is FieldValue || column is FieldEnum)
                return ((Field)column).FieldType;

            if (column is FieldEmbedded.EmbeddedHasValueColumn)
                return typeof(bool);

            return column.Nullable ? typeof(int?) : typeof(int);
        }

        static MethodInfo miGetType = ReflectionTools.GetMethodInfo((IdentifiableEntity ie) => ie.GetType());
        static MethodInfo miTypeId = ReflectionTools.GetMethodInfo(() => TypeId(null));

        static int TypeId(Type type)
        {
            return TypeLogic.TypeToDN[type].Id;
        }
    }

    class CachedTable<T> : CachedTableBase where T : IdentifiableEntity
    {
        ResetLazy<Dictionary<int, object>> rows;
        Action<object, IRetriever, T> CompleterFactory;

        public CachedTable(AliasGenerator aliasGenerator, string columnToJoin, string joins)
        {
            Table table = Schema.Current.Table(typeof(T));

            columns = table.Columns.Values.ToList();

            tupleType = TupleReflection.TupleChainType(columns.Select(GetColumnType));

            Alias alias = aliasGenerator.NextTableAlias(table.Name.Name);

            string select = "SELECT {0} FROM {1} {2}".Formato(columns.Select(c=>alias.Name.SqlScape() + "." + alias.Name.SqlScape()));

            

            query = new SqlPreCommandSimple(

            ParameterExpression pe = Expression.Parameter(typeof(T));


            var tupleConstructor = TupleReflection.TupleChainConstructor(expressions);

            IQueryable query = Database.Query<T>().Select(a=> 



            query = new SqlPreCommandSimple("SELECT {0} FROM {1}".Formato(columns.ToString( + joins)); 
        }

     
       

        public void Complete(T entity, IRetriever retriever)
        {
            CompleterFactory(rows.Value[entity.Id], retriever, entity);
        }
    }

    class CachedRelationalTable<T> : CachedTableBase
    {
        ResetLazy<Dictionary<int, List<object>>> relationalRows;

        Func<object, T> activator;

        public MList<T> Fill(int id, IRetriever retriever)
        {
            MList<T> result;
            var list = relationalRows.Value.TryGetC(id);
            if (list == null)
                result = new MList<T>();
            else
            {
                result = new MList<T>(list.Count);
                foreach (var obj in list)
                {
                    result.Add(activator(obj));
                }
            }

            return result;
        }
    }

    class CachedSemiLite<T> : CachedTableBase where T:IdentifiableEntity
    {
        ResetLazy<Dictionary<int, string>> lite;

        public Lite<T> GetLite(int id)
        {
            return Lite.Create<T>(id, lite[id]);
        }
    }

    internal static class Completer
    {
        static ConcurrentDictionary<Type, Delegate> completers = new ConcurrentDictionary<Type, Delegate>();

        public static Action<T, T, IRetriever> GetCompleter<T>() where T : IdentifiableEntity
        {
            return (Action<T, T, IRetriever>)completers.GetOrAdd(typeof(T), ConstructCompleter);
        }

        static Delegate GetCompleter(Type type)
        {
            return completers.GetOrAdd(type, ConstructCompleter);
        }

        static Delegate ConstructCompleter(Type type)
        {
            Table table = Schema.Current.Table(type);

            ParameterExpression me = Expression.Parameter(type, "me");
            ParameterExpression origin = Expression.Parameter(type, "origin");
            ParameterExpression retriever = Expression.Parameter(typeof(IRetriever), "retriever");

            var list = table.Fields.Values
                .Where(f => !(f.Field is FieldPrimaryKey))
                .Select(f => Expression.Assign(
                    Expression.Field(me, f.FieldInfo),
                    Clone(Expression.Field(origin, f.FieldInfo), f.Field, retriever))).ToList<Expression>();

            var lambda = Expression.Lambda(typeof(Action<,,>).MakeGenericType(type, type, typeof(IRetriever)), Expression.Block(list), me, origin, retriever);

            return lambda.Compile();
        }



        static MethodInfo miSelectE = ReflectionTools.GetMethodInfo(() => Enumerable.Select((IEnumerable<string>)null, s => s)).GetGenericMethodDefinition();
        static PropertyInfo piModified = ReflectionTools.GetPropertyInfo((Modifiable me) => me.Modified);
        static MemberBinding resetModified = Expression.Bind(piModified, Expression.Constant(null, typeof(bool?)));

       

        static T CallComplete<T>(T original, IRetriever r) where T : IdentifiableEntity
        {
            return r.Complete<T>(original.Id, e => GetCompleter<T>()(e, original, r));
        }
    }
}
