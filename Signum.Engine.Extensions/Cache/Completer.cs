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

namespace Signum.Engine.Cache
{
    internal static class Completer
    {
        static ConcurrentDictionary<Type, Delegate> completers = new ConcurrentDictionary<Type, Delegate>();

        public static Action<T, T, IRetriever> GetCompleter<T>()
        {
            return (Action<T, T, IRetriever>)completers.GetOrAdd(typeof(T), ConstructCompleter);
        }

        private static Delegate ConstructCompleter(Type type)
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

            if(typeof(IAfterClone).IsAssignableFrom(type))
            {
                list.Add(Expression.Call(me, miAfterClone, new[] { origin }));
            }

            var lambda = Expression.Lambda(typeof(Action<,,>).MakeGenericType(type, type, typeof(IRetriever)), Expression.Block(list), me, origin, retriever);

            return lambda.Compile();
        }

        static MethodInfo miAfterClone = ReflectionTools.GetMethodInfo((IAfterClone ac) => ac.AfterClone(null));
        static MethodInfo miRequest = ReflectionTools.GetMethodInfo((IRetriever r) => r.Request<IdentifiableEntity>(0)).GetGenericMethodDefinition();
        static MethodInfo miRequestIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestIBA<IdentifiableEntity>(0, typeof(IdentifiableEntity))).GetGenericMethodDefinition();
        static MethodInfo miGetType = ReflectionTools.GetMethodInfo((IdentifiableEntity ie) => ie.GetType());

        static MethodInfo miToString = ReflectionTools.GetMethodInfo((object o) => o.ToString());

        private static Expression Clone(Expression origin, Field field, ParameterExpression retriever)
        {
            if (field is FieldValue || field is FieldEnum)
                return origin;

            if(field is IFieldReference)
            {   
                var nullref  = Expression.Constant(null, field.FieldType); 

                if(((IFieldReference)field).IsLite)
                {
                    var ci = field.FieldType.GetConstructor(new []{typeof(Type), typeof(int), typeof(string)});
                    Expression call = Expression.New(ci,
                        Expression.Property(origin, "EntityType"),
                        Expression.Property(origin, "Id"),
                        Expression.Call(origin, miToString));

                    return Expression.Condition(Expression.Equal(origin, nullref), nullref,  call);
                }
                
                if (field is FieldReference)
                {
                    Expression call = CallComplete(origin, retriever, field.FieldType);

                    return Expression.Condition(Expression.Equal(origin, nullref), nullref,  call);
                }

                if (field is FieldImplementedBy)
                {
                    var ib = (FieldImplementedBy)field;

                    var call = ib.ImplementationColumns.Keys.Aggregate((Expression)nullref, (acum, t) => Expression.Condition(
                        Expression.Equal(Expression.Call(origin, miGetType), Expression.Constant(t)),
                        Expression.Convert(CallComplete(Expression.Convert(origin, t), retriever, t), field.FieldType), 
                        acum));

                    return Expression.Condition(Expression.Equal(origin, nullref), nullref, call);
                }

                if (field is FieldImplementedByAll)
                {
                    throw new InvalidOperationException("You can not cache entities with ImplementedByAll"); 
                    //var call = Expression.Call(retriever, miRequestIBA, Expression.Property(origin, "IdOrNull"), Expression.Call(origin, miGetType));

                    //return Expression.Condition(Expression.Equal(origin, nullref), nullref, call);
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
                var mlistField  = (FieldMList)field;

                var elemField = mlistField.RelationalTable.Field;

                ParameterExpression pElement = Expression.Parameter(elemField.FieldType);

                var body = Clone(pElement, elemField, retriever);

                Expression collection = pElement == body ? origin : Expression.Call(null, miSelectE.MakeGenericMethod(elemField.FieldType, elemField.FieldType), origin, Expression.Lambda(body, pElement));

                var ci = mlistField.FieldType.GetConstructor(new []{typeof(IEnumerable<>).MakeGenericType(elemField.FieldType)});

                return Expression.MemberInit(Expression.New(ci, collection), resetModified);
            }

            throw new InvalidOperationException("Unexpected {0}".Formato(field.GetType().Name)); 
        }

        static MethodInfo miSelectE = ReflectionTools.GetMethodInfo(() => Enumerable.Select((IEnumerable<string>)null, s => s)).GetGenericMethodDefinition();
        static PropertyInfo piModified = ReflectionTools.GetPropertyInfo((Modifiable me) => me.Modified);
        static MemberBinding resetModified = Expression.Bind(piModified, Expression.Constant(null, typeof(bool?)));

        private static Expression CallComplete(Expression origin, ParameterExpression retriever, Type type)
        {
            return Expression.Call(retriever, miRequest.MakeGenericMethod(type), Expression.Property(origin, "IdOrNull"));
        }
    }
}
