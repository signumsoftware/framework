using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Linq.Expressions;
using Signum.Utilities;
using Signum.Entities;
using Signum.Entities.Reflection;
using Signum.Utilities.Reflection;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;
using Signum.Entities.Basics;
using Signum.Entities.Internal;
using Signum.Utilities.DataStructures;

namespace Signum.Engine.Linq
{
    internal static class TranslatorBuilder
    {
        internal static ITranslateResult Build(ProjectionExpression proj)
        {
            Type type = proj.UniqueFunction == null ? proj.Type.ElementType()! : proj.Type;

            return miBuildPrivate.GetInvoker(type)(proj);
        }

        static GenericInvoker<Func<ProjectionExpression, ITranslateResult>> miBuildPrivate = new GenericInvoker<Func<ProjectionExpression, ITranslateResult>>(pe => BuildPrivate<int>(pe));

        static TranslateResult<T> BuildPrivate<T>(ProjectionExpression proj)
        {
            var eagerChildProjections = EagerChildProjectionGatherer.Gatherer(proj).Select(cp => BuildChild(cp)).ToList();
            var lazyChildProjections = LazyChildProjectionGatherer.Gatherer(proj).Select(cp => BuildChild(cp)).ToList();

            Scope scope = new Scope(
                alias :proj.Select.Alias,
                positions: proj.Select.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name!, p => p.i) /*CSBUG*/
            );

            Expression<Func<IProjectionRow, T>> lambda = ProjectionBuilder.Build<T>(proj.Projector, scope);

            var command = QueryFormatter.Format(proj.Select);

            var result = new TranslateResult<T>(

                eagerProjections: eagerChildProjections,
                lazyChildProjections: lazyChildProjections,

                mainCommand: command,
                projectorExpression: lambda,
                unique: proj.UniqueFunction
            );

            return result;
        }

        static IChildProjection BuildChild(ChildProjectionExpression childProj)
        {
            var proj = childProj.Projection;

            Type type = proj.UniqueFunction == null ? proj.Type.ElementType()! : proj.Type;

            if(!type.IsInstantiationOf(typeof(KeyValuePair<,>)))
                throw new InvalidOperationException("All child projections should create KeyValuePairs");

            Scope scope = new Scope(
                alias: proj.Select.Alias,
                positions: proj.Select.Columns.Select((c, i) => new { c.Name, i }).ToDictionary(p => p.Name!, p => p.i) /*CSBUG*/
            );


            var types = type.GetGenericArguments();

            var command = QueryFormatter.Format(proj.Select);

            if (childProj.IsLazyMList)
            {
                types[1] = types[1].GetGenericArguments()[0];
                return giLazyChild.GetInvoker(types)(proj.Projector, scope, childProj.Token, command);
            }
            else
            {
                return giEagerChild.GetInvoker(types)(proj.Projector, scope, childProj.Token, command);
            }
        }

        static readonly GenericInvoker<Func<Expression, Scope, LookupToken, SqlPreCommandSimple, IChildProjection>> giLazyChild =
            new GenericInvoker<Func<Expression, Scope, LookupToken, SqlPreCommandSimple, IChildProjection>>((proj, scope, token, sql) => LazyChild<int, bool>(proj, scope, token, sql));
        static IChildProjection LazyChild<K, V>(Expression projector, Scope scope, LookupToken token, SqlPreCommandSimple command)
            where K : notnull
        {
            var proj = ProjectionBuilder.Build<KeyValuePair<K, MList<V>.RowIdElement>>(projector, scope);
            return new LazyChildProjection<K, V>(token, command, proj);
        }

        static readonly GenericInvoker<Func<Expression, Scope, LookupToken, SqlPreCommandSimple, IChildProjection>> giEagerChild =
            new GenericInvoker<Func<Expression, Scope, LookupToken, SqlPreCommandSimple, IChildProjection>>((proj, scope, token, sql) => EagerChild<int, bool>(proj, scope, token, sql));
        static IChildProjection EagerChild<K, V>(Expression projector, Scope scope, LookupToken token, SqlPreCommandSimple command)
        {
            var proj = ProjectionBuilder.Build<KeyValuePair<K, V>>(projector, scope);
            return new EagerChildProjection<K, V>(token, command, proj);
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
            static readonly ParameterExpression row = Expression.Parameter(typeof(IProjectionRow), "row");

            static readonly PropertyInfo piRetriever = ReflectionTools.GetPropertyInfo((IProjectionRow r) => r.Retriever);
            static readonly MemberExpression retriever = Expression.Property(row, piRetriever);

            static readonly MethodInfo miCached = ReflectionTools.GetMethodInfo((IRetriever r) => r.Complete<TypeEntity>(null, null!)).GetGenericMethodDefinition();
            static readonly MethodInfo miRequest = ReflectionTools.GetMethodInfo((IRetriever r) => r.Request<TypeEntity>(null)).GetGenericMethodDefinition();
            static readonly MethodInfo miRequestIBA = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestIBA<TypeEntity>(null, null)).GetGenericMethodDefinition();
            static readonly MethodInfo miRequestLite = ReflectionTools.GetMethodInfo((IRetriever r) => r.RequestLite<TypeEntity>(null)).GetGenericMethodDefinition();
            static readonly MethodInfo miModifiablePostRetrieving = ReflectionTools.GetMethodInfo((IRetriever r) => r.ModifiablePostRetrieving<EmbeddedEntity>(null)).GetGenericMethodDefinition();

            Scope scope;

            public ProjectionBuilder(Scope scope)
            {
                this.scope = scope;
            }

            static internal Expression<Func<IProjectionRow, T>> Build<T>(Expression expression, Scope scope)
            {
                ProjectionBuilder pb = new ProjectionBuilder(scope);
                Expression body = pb.Visit(expression);
                return Expression.Lambda<Func<IProjectionRow, T>>(body, row);
            }

            static Expression NullifyColumn(Expression exp)
            {
                if (exp is not ColumnExpression ce)
                    return exp;

                if (ce.Type.IsNullable() || ce.Type.IsClass)
                    return ce;

                return new ColumnExpression(ce.Type.Nullify(), ce.Alias, ce.Name);
            }

            protected override Expression VisitUnary(UnaryExpression u)
            {
                var col = GetInnerColumn(u);

                if(col != null)
                    return scope.GetColumnExpression(row, col.Alias, col.Name!, u.Type);

                return base.VisitUnary(u);
            }

            public ColumnExpression? GetInnerColumn(UnaryExpression u)
            {
                if(u.NodeType == ExpressionType.Convert && DiffersInNullability(u.Type, u.Operand.Type))
                {
                    if (u.Operand is ColumnExpression c)
                        return c;

                    if (u.Operand is UnaryExpression u2)
                        return GetInnerColumn(u2);
                }

                return null;
            }

            static bool DiffersInNullability(Type a, Type b)
            {
                if (a.IsValueType && a.Nullify() == b ||
                    b.IsValueType && b.Nullify() == a)
                    return true;

                if (a == typeof(Date) && b == typeof(DateTime) ||
                    a == typeof(DateTime) && b == typeof(Date))
                    return true;

                return false;
            }

            protected internal override Expression VisitColumn(ColumnExpression column)
            {
                return scope.GetColumnExpression(row, column.Alias, column.Name!, column.Type);
            }

            protected internal override Expression VisitChildProjection(ChildProjectionExpression child)
            {
                Expression outer = Visit(child.OuterKey);

                if (outer != child.OuterKey)
                    child = new ChildProjectionExpression(child.Projection, outer, child.IsLazyMList, child.Type, child.Token);

                return Scope.LookupEager(row, child);
            }

            protected Expression VisitMListChildProjection(ChildProjectionExpression child, MemberExpression field)
            {
                Expression outer = Visit(child.OuterKey);

                if (outer != child.OuterKey)
                    child = new ChildProjectionExpression(child.Projection, outer, child.IsLazyMList, child.Type, child.Token);

                return Scope.LookupMList(row, child, field);
            }

            protected internal override Expression VisitProjection(ProjectionExpression proj)
            {
                throw new InvalidOperationException("No ProjectionExpressions expected at this stage");
            }

            protected internal override MixinEntityExpression VisitMixinEntity(MixinEntityExpression me)
            {
                throw new InvalidOperationException("Impossible to retrieve MixinEntity {0} without their main entity".FormatWith(me.Type.Name));
            }

            protected internal override Expression VisitEntity(EntityExpression entityExpr)
            {
                Expression id = Visit(NullifyColumn(entityExpr.ExternalId));

                if (entityExpr.TableAlias == null)
                    return Expression.Call(retriever, miRequest.MakeGenericMethod(entityExpr.Type), id);

                ParameterExpression e = Expression.Parameter(entityExpr.Type, entityExpr.Type.Name.ToLower().Substring(0, 1));

                var bindings =
                    entityExpr.Bindings!
                    .Where(a => !ReflectionTools.FieldEquals(EntityExpression.IdField, a.FieldInfo))
                    .Select(b =>
                        {
                            var field = Expression.Field(e, b.FieldInfo);

                            var value = b.Binding is ChildProjectionExpression cpe ?
                                VisitMListChildProjection(cpe, field) :
                                Convert(Visit(b.Binding), b.FieldInfo.FieldType);

                            return (Expression)Expression.Assign(field, value);
                        }).ToList();

                if (entityExpr.Mixins != null)
                {
                    var blocks = entityExpr.Mixins.Select(m => AssignMixin(e, m)).ToList();

                    bindings.AddRange(blocks);
                }

                LambdaExpression lambda = Expression.Lambda(typeof(Action<>).MakeGenericType(entityExpr.Type), Expression.Block(bindings), e);

                return Expression.Call(retriever, miCached.MakeGenericMethod(entityExpr.Type), id.Nullify(), lambda);
            }

            BlockExpression AssignMixin(ParameterExpression e, MixinEntityExpression m)
            {
                var mixParam = Expression.Parameter(m.Type);

                var mixBindings = new List<Expression>
                {
                    Expression.Assign(mixParam, Expression.Call(e, MixinDeclarations.miMixin.MakeGenericMethod(m.Type))),
                    Expression.Call(retriever, miModifiablePostRetrieving.MakeGenericMethod(m.Type), mixParam)
                };
                mixBindings.AddRange(m.Bindings.Select(b =>
                {
                    var field = Expression.Field(mixParam, b.FieldInfo);

                    var value = b.Binding is ChildProjectionExpression cpe ? VisitMListChildProjection(cpe, field) :
                        Convert(Visit(b.Binding), b.FieldInfo.FieldType);

                    return (Expression)Expression.Assign(field, value);
                }));

                return Expression.Block(new[] { mixParam }, mixBindings);
            }

            private static Expression Convert(Expression expression, Type type)
            {
                if (expression.Type == type)
                    return expression;

                return Expression.Convert(expression, type);
            }

            protected internal override Expression VisitEmbeddedEntity(EmbeddedEntityExpression eee)
            {
                var embeddedParam = Expression.Parameter(eee.Type);

                var embeddedBindings = new List<Expression>
                {
                    Expression.Assign(embeddedParam, Expression.New(eee.Type))
                };

                if (typeof(EmbeddedEntity).IsAssignableFrom(eee.Type))
                    embeddedBindings.Add(Expression.Call(retriever, miModifiablePostRetrieving.MakeGenericMethod(eee.Type), embeddedParam));

                embeddedBindings.AddRange(eee.Bindings.Select(b =>
                {
                    var field = Expression.Field(embeddedParam, b.FieldInfo);

                    var value = b.Binding is ChildProjectionExpression cpe ? VisitMListChildProjection(cpe, field) :
                        Convert(Visit(b.Binding), b.FieldInfo.FieldType);

                    return Expression.Assign(field, value);
                }));

                if (eee.Mixins != null)
                {
                    var blocks = eee.Mixins.Select(m => AssignMixin(embeddedParam, m)).ToList();
                    embeddedBindings.AddRange(blocks);
                }

                embeddedBindings.Add(embeddedParam);
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
                Expression liteConstructor;
                if (typeId is TypeEntityExpression tee)
                {
                    Type type = tee.TypeValue;

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
                    return Expression.Call(retriever, miRequestLite.MakeGenericMethod(Lite.Extract(lite.Type)!), liteConstructor);
            }

            static readonly MethodInfo miLiteCreateParse = ReflectionTools.GetMethodInfo(() => LiteCreateParse(null!, null, null!, null!));

            static Lite<Entity>? LiteCreateParse(Schema schema, PrimaryKey? typeId, string id, string toString)
            {
                if (typeId == null)
                    return null;

                Type type = schema.GetType(typeId.Value);

                return Lite.Create(type, PrimaryKey.Parse(id, type), toString);
            }

            static MethodInfo miLiteCreate = ReflectionTools.GetMethodInfo(() => Lite.Create(null!, 0, null));

            protected internal override Expression VisitMListElement(MListElementExpression mle)
            {
                Type type = mle.Type;

                var bindings = new List<MemberAssignment>
                {
                    Expression.Bind(type.GetProperty("RowId")!, Visit(mle.RowId.UnNullify())),
                    Expression.Bind(type.GetProperty("Parent")!, Visit(mle.Parent)),
                };

                if (mle.Order != null)
                    bindings.Add(Expression.Bind(type.GetProperty("Order")!, Visit(mle.Order)));

                bindings.Add(Expression.Bind(type.GetProperty("Element")!, Visit(mle.Element)));

                var init = Expression.MemberInit(Expression.New(type), bindings);

                return Expression.Condition(SmartEqualizer.NotEqualNullable(Visit(mle.RowId.Nullify()), NullId),
                    init,
                    Expression.Constant(null, init.Type));
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

            static readonly MethodInfo miTryParse = ReflectionTools.GetMethodInfo(() => TryParse(null!, null!));

            

            static PrimaryKey? TryParse(Type type, string id)
            {
                if (type == null)
                    return null;

                return PrimaryKey.Parse(id, type);
            }

            protected internal override Expression VisitToDayOfWeek(ToDayOfWeekExpression toDayOfWeek)
            {
                var result = this.Visit(toDayOfWeek.Expression);

                if (Schema.Current.Settings.IsPostgres)
                {
                    return Expression.Call(ToDayOfWeekExpression.miToDayOfWeekPostgres, result);
                }
                else
                {
                    var dateFirst = ((SqlServerConnector)Connector.Current).DateFirst;
                    return Expression.Call(ToDayOfWeekExpression.miToDayOfWeekSql, result, Expression.Constant(dateFirst, typeof(byte)));
                }
            }

            static MethodInfo miToInterval = ReflectionTools.GetMethodInfo(() => ToInterval<int>(new NpgsqlTypes.NpgsqlRange<int>())).GetGenericMethodDefinition();
            static NullableInterval<T> ToInterval<T>(NpgsqlTypes.NpgsqlRange<T> range) where T : struct, IComparable<T>, IEquatable<T>
                => new NullableInterval<T>(
                    range.LowerBoundInfinite ? null : range.LowerBound,
                    range.UpperBoundInfinite ? null : range.UpperBound);

            protected internal override Expression VisitInterval(IntervalExpression interval)
            {
                var intervalType = interval.Type.GetGenericArguments()[0];
                if (Schema.Current.Settings.IsPostgres)
                {
                    return Expression.Call(miToInterval.MakeGenericMethod(intervalType), Visit(interval.PostgresRange!));
                }
                else
                {
                    return Expression.New(typeof(NullableInterval<>).MakeGenericType(intervalType).GetConstructor(new[] { intervalType, intervalType })!, Visit(interval.Min!), Visit(interval.Max!));
                }
            }

            protected override Expression VisitNew(NewExpression node)
            {
                var expressions = this.Visit(node.Arguments);

                if (node.Members != null)
                {
                    for (int i = 0; i < node.Members.Count; i++)
                    {
                        var m = node.Members[i];
                        var e = expressions[i];
                        if (m is PropertyInfo pi && !pi.PropertyType.IsAssignableFrom(e.Type))
                        {
                            throw new InvalidOperationException(
                                $"Impossible to assign a '{e.Type.TypeName()}' to the member '{m.Name}' of type '{pi.PropertyType.TypeName()}'." +
                                (e.Type.IsInstantiationOf(typeof(IEnumerable<>)) ? "\nConsider adding '.ToList()' at the end of your sub-query" : null)
                            );
                        }
                    }
                }

                return (Expression)node.Update(expressions);
            }
        }
    }

    internal class Scope
    {
        public Alias Alias;

        public Dictionary<string, int> Positions;
        
        public Scope(Alias alias, Dictionary<string, int> positions)
        {
            Alias = alias;
            Positions = positions;
        }

        static readonly PropertyInfo miReader = ReflectionTools.GetPropertyInfo((IProjectionRow row) => row.Reader);

        public Expression GetColumnExpression(Expression row, Alias alias, string name, Type type)
        {
            if (alias != Alias)
                throw new InvalidOperationException("alias '{0}' not found".FormatWith(alias));

            int position = Positions.GetOrThrow(name, "column name '{0}' not found in alias '" + alias + "'");

            return FieldReader.GetExpression(Expression.Property(row, miReader), position, type);
        }

        static readonly MethodInfo miLookupRequest = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.LookupRequest<int, double>(null!, 0, null!)).GetGenericMethodDefinition();
        static readonly MethodInfo miLookup = ReflectionTools.GetMethodInfo((IProjectionRow row) => row.Lookup<int, double>(null!, 0)).GetGenericMethodDefinition();

        public static Expression LookupEager(Expression row, ChildProjectionExpression cProj)
        {
            if (cProj.IsLazyMList)
                throw new InvalidOperationException("IsLazyMList not expected at this stage");

            Type type = cProj.Projection.UniqueFunction == null ? cProj.Type.ElementType()! : cProj.Type;

            MethodInfo mi = miLookup.MakeGenericMethod(cProj.OuterKey.Type, type);

            Expression call = Expression.Call(row, mi, Expression.Constant(cProj.Token), cProj.OuterKey);

            if (cProj.Projection.UniqueFunction != null)
                throw new InvalidOperationException("Eager ChildProyection with UniqueFunction '{0}' not expected at this stage".FormatWith(cProj.Projection.UniqueFunction));

            return call;
        }

        public static Expression LookupMList(Expression row, ChildProjectionExpression cProj, MemberExpression field)
        {
            if (!cProj.IsLazyMList)
                throw new InvalidOperationException("Not IsLazyMList not expected at this stage");

            if (!cProj.Type.IsMList())
                throw new InvalidOperationException("Lazy ChildProyection of type '{0}' instead of MList".FormatWith(cProj.Type.TypeName()));

            if (cProj.Projection.UniqueFunction != null)
                throw new InvalidOperationException("Lazy ChildProyection with UniqueFunction '{0}'".FormatWith(cProj.Projection.UniqueFunction));

            MethodInfo mi = miLookupRequest.MakeGenericMethod(cProj.OuterKey.Type, cProj.Type.ElementType()!);

            return Expression.Call(row, mi, Expression.Constant(cProj.Token), cProj.OuterKey, field);
        }
    }
}
