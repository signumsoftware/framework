using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using System.Diagnostics;
using Signum.Utilities.DataStructures;
using Signum.Entities;
using Signum.Engine;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using System.Collections;
using Signum.Engine.Maps;
using System.Data.Common;
using Signum.Entities.Basics;
using System.Collections.Concurrent;
using Signum.Entities.Internal;

namespace Signum.Engine.Linq
{  
    internal static class TranslatorBuilder
    {
        internal static ITranslateResult Build(ProjectionExpression proj)
        {
            Type type = proj.UniqueFunction == null ? proj.Type.ElementType() : proj.Type;

            return miBuildPrivate.GetInvoker(type)(proj);
        }

        static GenericInvoker<Func<ProjectionExpression, ITranslateResult>> miBuildPrivate = new GenericInvoker<Func<ProjectionExpression, ITranslateResult>>(pe => BuildPrivate<int>(pe));

        static TranslateResult<T> BuildPrivate<T>(ProjectionExpression proj)
        {
            var eagerChildProjections = EagerChildProjectionGatherer.Gatherer(proj).Select(cp => BuildChild(cp)).ToList();
            var lazyChildProjections = LazyChildProjectionGatherer.Gatherer(proj).Select(cp => BuildChild(cp)).ToList();

            Scope scope = new Scope
            {
                Alias = proj.Select.Alias,
                Positions = proj.Select.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name, p => p.i),
            };

            Expression<Func<IProjectionRow, T>> lambda = ProjectionBuilder.Build<T>(proj.Projector, scope);

            var command = QueryFormatter.Format(proj.Select);

            var result = new TranslateResult<T>
            {
                EagerProjections = eagerChildProjections,
                LazyChildProjections = lazyChildProjections,

                MainCommand = command,

                ProjectorExpression = lambda,

                Unique = proj.UniqueFunction,
            };

            return result;
        }

        static IChildProjection BuildChild(ChildProjectionExpression childProj)
        {
            var proj = childProj.Projection;

            Type type = proj.UniqueFunction == null ? proj.Type.ElementType() : proj.Type;

            if(!type.IsInstantiationOf(typeof(KeyValuePair<,>)))
                throw new InvalidOperationException("All child projections should create KeyValuePairs");

            Scope scope = new Scope
            {
                Alias = proj.Select.Alias,
                Positions = proj.Select.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name, p => p.i),
            };


            var types = type.GetGenericArguments();

            IChildProjection result;
            if (childProj.IsLazyMList)
            {
                types[1] = types[1].GetGenericArguments()[0];

                result = giLazyChild.GetInvoker(types)(proj.Projector, scope); 
            }
            else
            {
                result = giEagerChild.GetInvoker(types)(proj.Projector, scope); 
            }

            result.Token = childProj.Token; 
            result.Command = QueryFormatter.Format(proj.Select);

            return result;
        }

        static GenericInvoker<Func<Expression, Scope, IChildProjection>> giLazyChild = 
            new GenericInvoker<Func<Expression, Scope, IChildProjection>>((proj, scope) => LazyChild<int, bool>(proj, scope));
        static IChildProjection LazyChild<K, V>(Expression projector, Scope scope)
        {
            return new LazyChildProjection<K, V>
            {
                ProjectorExpression = ProjectionBuilder.Build<KeyValuePair<K, MList<V>.RowIdElement>>(projector, scope),
            };
        }
        
        static GenericInvoker<Func<Expression, Scope, IChildProjection>> giEagerChild =
            new GenericInvoker<Func<Expression, Scope, IChildProjection>>((proj, scope) => EagerChild<int, bool>(proj, scope));
        static IChildProjection EagerChild<K, V>(Expression projector, Scope scope)
        {
            return new EagerChildProjection<K, V>
            {
                ProjectorExpression = ProjectionBuilder.Build<KeyValuePair<K, V>>(projector, scope),
            };
        }

        public static SqlPreCommandSimple BuildCommandResult(CommandExpression command)
        {
            return QueryFormatter.Format(command);
        }

        public class LazyChildProjectionGatherer : DbExpressionVisitor
        {
            List<ChildProjectionExpression> list = new List<ChildProjectionExpression>();

            public static List<ChildProjectionExpression> Gatherer(ProjectionExpression proj)
            {
                LazyChildProjectionGatherer pg = new LazyChildProjectionGatherer();

                pg.Visit(proj);

                return pg.list; 
            }

            protected internal override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                if (child.IsLazyMList)
                    list.Add(child);
                
                var result =  base.VisitChildProjection(child);

                return result;
            }
        }

        public class EagerChildProjectionGatherer : DbExpressionVisitor
        {
            List<ChildProjectionExpression> list = new List<ChildProjectionExpression>();

            public static List<ChildProjectionExpression> Gatherer(ProjectionExpression proj)
            {
                EagerChildProjectionGatherer pg = new EagerChildProjectionGatherer();

                pg.Visit(proj);

                return pg.list;
            }

            protected internal override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                var result = base.VisitChildProjection(child);

                if (!child.IsLazyMList)
                    list.Add(child);

                return result;
            }
        }

        /// <summary>
        /// ProjectionBuilder is a visitor that converts an projector expression
        /// that constructs result objects out of ColumnExpressions into an actual
        /// LambdaExpression that constructs result objects out of accessing fields
        /// of a ProjectionRow
        /// </summary>
        public class ProjectionBuilder : DbExpressionVisitor
        {
            static ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");

            static PropertyInfo piRetriever = ReflectionTools.GetPropertyInfo((IProjectionRow r) => r.Retriever);
            static MemberExpression retriever = Expression.Property(row, piRetriever); 
           
            static FieldInfo fiId = ReflectionTools.GetFieldInfo((Entity i) => i.id);

            static MethodInfo miCached = ReflectionTools.GetMethodInfo((IRetriever r) => r.Complete<TypeEntity>(null, null)).GetGenericMethodDefinition();
            static MethodInfo miRequest = ReflectionTools.GetMethodInfo((IRetriever r) => r.Request<TypeEntity>(null)).GetGenericMethodDefinition();
            static MethodInfo miRequestIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestIBA<TypeEntity>(null, null)).GetGenericMethodDefinition();
            static MethodInfo miRequestLite = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestLite<TypeEntity>(null)).GetGenericMethodDefinition();
            static MethodInfo miModifiablePostRetrieving = ReflectionTools.GetMethodInfo((IRetriever r) => r.ModifiablePostRetrieving<EmbeddedEntity>(null)).GetGenericMethodDefinition();

            Scope scope; 
        

            static internal Expression<Func<IProjectionRow, T>> Build<T>(Expression expression, Scope scope)
            {
                ProjectionBuilder pb = new ProjectionBuilder() { scope = scope };
                Expression body = pb.Visit(expression);
                return Expression.Lambda<Func<IProjectionRow, T>>(body, row);
            }

            Expression NullifyColumn(Expression exp)
            {
                ColumnExpression ce = exp as ColumnExpression;
                if (ce == null)
                    return exp;

                if (ce.Type.IsNullable() || ce.Type.IsClass)
                    return ce;

                return new ColumnExpression(ce.Type.Nullify(), ce.Alias, ce.Name);
            }

            protected override Expression VisitUnary(UnaryExpression u)
            {
                if (u.NodeType == ExpressionType.Convert && u.Operand is ColumnExpression && DiffersInNullability(u.Type, u.Operand.Type))
                {
                    ColumnExpression column = (ColumnExpression)u.Operand;
                    return scope.GetColumnExpression(row, column.Alias, column.Name, u.Type);
                }

                return base.VisitUnary(u);
            }

            bool DiffersInNullability(Type a, Type b)
            {
                return
                    a.IsValueType && a.Nullify() == b ||
                    b.IsValueType && b.Nullify() == a;
            }

            protected internal override Expression VisitColumn(ColumnExpression column)
            {
                return scope.GetColumnExpression(row, column.Alias, column.Name, column.Type);
            }

            protected internal override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                Expression outer = Visit(child.OuterKey);

                if (outer != child.OuterKey)
                    child = new ChildProjectionExpression(child.Projection, outer, child.IsLazyMList, child.Type, child.Token); 

                return scope.LookupEager(row, child);
            }

            protected Expression VisitMListChildProjection(ChildProjectionExpression child, MemberExpression field)
            {
                Expression outer = Visit(child.OuterKey);

                if (outer != child.OuterKey)
                    child = new ChildProjectionExpression(child.Projection, outer, child.IsLazyMList, child.Type, child.Token);

                return scope.LookupMList(row, child, field);
            }

            protected internal override Expression VisitProjection(ProjectionExpression proj)
            {
                throw new InvalidOperationException("No ProjectionExpressions expected at this stage"); 
            }

            protected internal override MixinEntityExpression VisitMixinEntity(MixinEntityExpression me)
            {
                throw new InvalidOperationException("Impossible to retrieve MixinEntity {0} without their main entity".FormatWith(me.Type.Name)); 
            }

            protected internal override Expression VisitEntity(EntityExpression fieldInit)
            {
                Expression id = Visit(NullifyColumn(fieldInit.ExternalId));

                if (fieldInit.TableAlias == null)
                    return Expression.Call(retriever, miRequest.MakeGenericMethod(fieldInit.Type), id);

                ParameterExpression e = Expression.Parameter(fieldInit.Type, fieldInit.Type.Name.ToLower().Substring(0, 1));

                var bindings = 
                    fieldInit.Bindings
                    .Where(a => !ReflectionTools.FieldEquals(EntityExpression.IdField, a.FieldInfo))
                    .Select(b =>
                        {
                            var field = Expression.Field(e, b.FieldInfo);

                            var value = b.Binding is ChildProjectionExpression ? 
                                VisitMListChildProjection((ChildProjectionExpression)b.Binding, field) :
                                Convert(Visit(b.Binding), b.FieldInfo.FieldType);

                            return (Expression)Expression.Assign(field, value);
                        }).ToList();

                if (fieldInit.Mixins != null)
                {
                    var blocks = fieldInit.Mixins.Select(m => AssignMixin(e, m)).ToList();

                    bindings.AddRange(blocks);
                }

                LambdaExpression lambda = Expression.Lambda(typeof(Action<>).MakeGenericType(fieldInit.Type), Expression.Block(bindings), e);

                return Expression.Call(retriever, miCached.MakeGenericMethod(fieldInit.Type), id.Nullify(), lambda);
            }

            BlockExpression AssignMixin(ParameterExpression e, MixinEntityExpression m)
            {
                var mixParam = Expression.Parameter(m.Type);

                var mixAssign = Expression.Assign(mixParam, Expression.Call(e, MixinDeclarations.miMixin.MakeGenericMethod(m.Type)));

                var mixBindings = m.Bindings.Select(b =>
                {
                    var field = Expression.Field(mixParam, b.FieldInfo);

                    var value = b.Binding is ChildProjectionExpression ?
                        VisitMListChildProjection((ChildProjectionExpression)b.Binding, field) :
                        Convert(Visit(b.Binding), b.FieldInfo.FieldType);

                    return (Expression)Expression.Assign(field, value);
                }).ToList();

                mixBindings.Insert(0, mixAssign);

                mixBindings.Add(Expression.Call(retriever, miModifiablePostRetrieving.MakeGenericMethod(m.Type), mixParam));

                return Expression.Block(new[] { mixParam }, mixBindings);
            }

            private Expression Convert(Expression expression, Type type)
            {
                if (expression.Type == type)
                    return expression;

                return Expression.Convert(expression, type); 
            }

            protected internal override Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
            {
                var embeddedParam = Expression.Parameter(eee.Type);

                var embeddedAssign = Expression.Assign(embeddedParam, Expression.New(eee.Type));

                var embeddedBindings = 
                       eee.Bindings.Select(b =>
                       {
                           var field = Expression.Field(embeddedParam, b.FieldInfo);

                           var value = b.Binding is ChildProjectionExpression ?
                               VisitMListChildProjection((ChildProjectionExpression)b.Binding, field) :
                               Convert(Visit(b.Binding), b.FieldInfo.FieldType);

                           return Expression.Assign(field, value);
                       }).ToList<Expression>();

                embeddedBindings.Insert(0, embeddedAssign);

                if (typeof(EmbeddedEntity).IsAssignableFrom(eee.Type))
                {
                    embeddedBindings.Add(Expression.Call(retriever, miModifiablePostRetrieving.MakeGenericMethod(eee.Type), embeddedParam));
                }
                else
                {
                    embeddedBindings.Add(embeddedParam);
                }

                var block = Expression.Block(eee.Type, new[] { embeddedParam }, embeddedBindings);

                return Expression.Condition(Expression.Equal(Visit(eee.HasValue.Nullify()), Expression.Constant(true, typeof(bool?))),
                    block, 
                    Expression.Constant(null, block.Type));
            }

            protected internal override Expression VisitImplementedBy(ImplementedByExpression rb)
            {
                return rb.Implementations.Select(ee => new When(Visit(ee.Value.ExternalId).NotEqualsNulll(), Visit(ee.Value))).ToCondition(rb.Type);
            }

            protected internal override Expression VisitImplementedByAll(ImplementedByAllExpression rba)
            {
                return Expression.Call(retriever, miRequestIBA.MakeGenericMethod(rba.Type),
                    Visit(NullifyColumn(rba.TypeId.TypeColumn)),
                    Visit(NullifyColumn(rba.Id)));
            }

            static readonly ConstantExpression NullType = Expression.Constant(null, typeof(Type));
            static readonly ConstantExpression NullId = Expression.Constant(null, typeof(int?));

            protected internal override Expression VisitTypeEntity(TypeEntityExpression typeFie)
            {
                return Expression.Condition(
                    Expression.NotEqual(Visit(NullifyColumn(typeFie.ExternalId)), NullId),
                    Expression.Constant(typeFie.TypeValue, typeof(Type)),
                    NullType);
            }
     
            protected internal override Expression VisitTypeImplementedBy(TypeImplementedByExpression typeIb)
            {
                return typeIb.TypeImplementations.Reverse().Aggregate((Expression)NullType, (acum, imp) => Expression.Condition(
                    Expression.NotEqual(Visit(NullifyColumn(imp.Value)), NullId),
                    Expression.Constant(imp.Key, typeof(Type)),
                    acum));
            }

            static MethodInfo miGetType = ReflectionTools.GetMethodInfo((Schema s) => s.GetType(1));

            protected internal override Expression VisitTypeImplementedByAll(TypeImplementedByAllExpression typeIba)
            {
                return Expression.Condition(
                    Expression.NotEqual(Visit(NullifyColumn(typeIba.TypeColumn)), NullId),
                    SchemaGetType(typeIba),
                    NullType);
            }

            private MethodCallExpression SchemaGetType(TypeImplementedByAllExpression typeIba)
            {
                return Expression.Call(Expression.Constant(Schema.Current), miGetType, Visit(typeIba.TypeColumn).UnNullify());
            }

            protected internal override Expression VisitLiteReference(LiteReferenceExpression lite)
            {
                var reference = Visit(lite.Reference);

                var toStr = Visit(lite.CustomToStr);

                return Lite.ToLiteFatInternalExpression(reference, toStr ?? Expression.Constant(null, typeof(string)));
            }

            protected internal override Expression VisitLiteValue(LiteValueExpression lite)
            {
                var id = Visit(NullifyColumn(lite.Id));

                if (id == null)
                    return Expression.Constant(null, lite.Type);

                var toStr = Visit(lite.ToStr);
                var typeId = lite.TypeId;

                var toStringOrNull = toStr ?? Expression.Constant(null, typeof(string));

                
                Expression nothing = Expression.Constant(null, lite.Type);
                Expression liteConstructor = null;
                if (typeId is TypeEntityExpression)
                {
                    Type type = ((TypeEntityExpression)typeId).TypeValue;

                    liteConstructor = Expression.Condition(Expression.NotEqual(id, NullId),
                        Expression.Convert(Lite.NewExpression(type, id, toStringOrNull), lite.Type),
                        nothing);
                }
                else if (typeId is TypeImplementedByExpression tib)
                {
                    liteConstructor = tib.TypeImplementations.Aggregate(nothing,
                        (acum, ti) =>
                            {
                                var visitId = Visit(NullifyColumn(ti.Value));
                                return Expression.Condition(Expression.NotEqual(visitId, NullId),
                                    Expression.Convert(Lite.NewExpression(ti.Key, visitId, toStringOrNull), lite.Type), acum);
                            });
                }
                else if (typeId is TypeImplementedByAllExpression tiba)
                {
                    var tid = Visit(NullifyColumn(tiba.TypeColumn));
                    liteConstructor = Expression.Convert(Expression.Call(miLiteCreateParse, Expression.Constant(Schema.Current), tid, id.UnNullify(), toStringOrNull), lite.Type);
                }
                else
                {
                    liteConstructor = Expression.Condition(Expression.NotEqual(id.Nullify(), NullId),
                                       Expression.Convert(Expression.Call(miLiteCreate, Visit(typeId), id.UnNullify(), toStringOrNull), lite.Type),
                                        nothing);
                }

                if (toStr != null)
                    return Expression.Call(retriever, miModifiablePostRetrieving.MakeGenericMethod(typeof(LiteImp)), liteConstructor.TryConvert(typeof(LiteImp))).TryConvert(liteConstructor.Type);
                else
                    return Expression.Call(retriever, miRequestLite.MakeGenericMethod(Lite.Extract(lite.Type)), liteConstructor);
            }

            static MethodInfo miLiteCreateParse = ReflectionTools.GetMethodInfo(() => LiteCreateParse(null, null, null, null));

            static Lite<Entity> LiteCreateParse(Schema schema, PrimaryKey? typeId, string id, string toString)
            {
                if (typeId == null)
                    return null;

                Type type = schema.GetType(typeId.Value);

                return Lite.Create(type, PrimaryKey.Parse(id, type), toString);
            }

            static MethodInfo miLiteCreate = ReflectionTools.GetMethodInfo(() => Lite.Create(null, 0, null));

            protected internal override Expression VisitMListElement(MListElementExpression mle)
            {
                Type type = mle.Type;

                var bindings = new List<MemberAssignment> 
                {
                    Expression.Bind(type.GetProperty("RowId"), Visit(mle.RowId.UnNullify())),
                    Expression.Bind(type.GetProperty("Parent"), Visit(mle.Parent)),
                };

                if (mle.Order != null)
                    bindings.Add(Expression.Bind(type.GetProperty("Order"), Visit(mle.Order)));

                bindings.Add(Expression.Bind(type.GetProperty("Element"), Visit(mle.Element)));

                var init = Expression.MemberInit(Expression.New(type), bindings);

                return Expression.Condition(SmartEqualizer.NotEqualNullable(Visit(mle.RowId.Nullify()), NullId),
                    init,
                    Expression.Constant(null, init.Type));
            }

            private Type ConstantType(Expression typeId)
            {
                if (typeId.NodeType == ExpressionType.Convert)
                    typeId = ((UnaryExpression)typeId).Operand;

                if (typeId.NodeType == ExpressionType.Constant)
                    return (Type)((ConstantExpression)typeId).Value;

                return null;
            }

            protected internal override Expression VisitSqlConstant(SqlConstantExpression sce)
            {
                return Expression.Constant(sce.Value, sce.Type);
            }

            protected internal override Expression VisitPrimaryKey(PrimaryKeyExpression pk)
            {
                var val = Visit(pk.Value);

                return Expression.Call(miWrap, Expression.Convert(val, typeof(IComparable)));
            }

            static readonly MethodInfo miWrap = ReflectionTools.GetMethodInfo(() => PrimaryKey.Wrap(1));

            protected internal override Expression VisitPrimaryKeyString(PrimaryKeyStringExpression pk)
            {
                var id = this.Visit(pk.Id);
                var type = this.Visit(pk.TypeId);

                return Expression.Call(miTryParse, type, id);
            }

            static readonly MethodInfo miTryParse = ReflectionTools.GetMethodInfo(() => TryParse(null, null));

            static PrimaryKey? TryParse(Type type, string id)
            {
                if (type == null)
                    return null;

                return PrimaryKey.Parse(id, type);
            }

            protected override Expression VisitNew(NewExpression node)
            {
                var expressions =  this.Visit(node.Arguments);

                if (node.Members != null)
                {
                    for (int i = 0; i < node.Members.Count; i++)
                    {
                        var m = node.Members[i];
                        var e = expressions[i];
                        if (m is PropertyInfo pi && !pi.PropertyType.IsAssignableFrom(e.Type))
                        {
                            throw  new InvalidOperationException(
                                $"Impossible to assign a '{e.Type.TypeName()}' to the member '{m.Name}' of type '{pi.PropertyType.TypeName()}'." +
                                (e.Type.IsInstantiationOf(typeof(IEnumerable<>)) ? "\nConsider adding '.ToList()' at the end of your sub-query" : null)
                            );
                        }
                    }
                }

                    return (Expression) node.Update(expressions);
            }
        }
    }

    internal class Scope
    {
        public Alias Alias;

        public Dictionary<string, int> Positions;

        static PropertyInfo miReader = ReflectionTools.GetPropertyInfo((IProjectionRow row) => row.Reader);

        public Expression GetColumnExpression(Expression row, Alias alias, string name, Type type)
        {
            if (alias != Alias)
                throw new InvalidOperationException("alias '{0}' not found".FormatWith(alias));

            int position = Positions.GetOrThrow(name, "column name '{0}' not found in alias '" + alias + "'");

            return FieldReader.GetExpression(Expression.Property(row, miReader), position, type);
        }

        static MethodInfo miLookupRequest = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.LookupRequest<int, double>(null, 0, null)).GetGenericMethodDefinition();
        static MethodInfo miLookup = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.Lookup<int, double>(null, 0)).GetGenericMethodDefinition();

        public Expression LookupEager(Expression row, ChildProjectionExpression cProj)
        {
            if (cProj.IsLazyMList)
                throw new InvalidOperationException("IsLazyMList not expected at this stage");

            Type type = cProj.Projection.UniqueFunction == null ? cProj.Type.ElementType() : cProj.Type;

            MethodInfo mi = miLookup.MakeGenericMethod(cProj.OuterKey.Type, type);

            Expression call = Expression.Call(row, mi, Expression.Constant(cProj.Token), cProj.OuterKey);

            if (cProj.Projection.UniqueFunction != null)
                throw new InvalidOperationException("Eager ChildProyection with UniqueFunction '{0}' not expected at this stage".FormatWith(cProj.Projection.UniqueFunction));

            return call;
        }

        public Expression LookupMList(Expression row, ChildProjectionExpression cProj, MemberExpression field)
        {
            if (!cProj.IsLazyMList)
                throw new InvalidOperationException("Not IsLazyMList not expected at this stage");

            if (!cProj.Type.IsMList())
                throw new InvalidOperationException("Lazy ChildProyection of type '{0}' instead of MList".FormatWith(cProj.Type.TypeName()));

            if (cProj.Projection.UniqueFunction != null)
                throw new InvalidOperationException("Lazy ChildProyection with UniqueFunction '{0}'".FormatWith(cProj.Projection.UniqueFunction));

            MethodInfo mi = miLookupRequest.MakeGenericMethod(cProj.OuterKey.Type, cProj.Type.ElementType());

            return Expression.Call(row, mi, Expression.Constant(cProj.Token), cProj.OuterKey, field);
        }
    }
}
