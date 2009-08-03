using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Diagnostics;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using Signum.Entities;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using Signum.Engine.Maps;
using Signum.Entities.Reflection;
using Signum.Engine.Properties;
using System.Collections.ObjectModel;
using Signum.Engine.DynamicQuery;
using Signum.Entities.DynamicQuery;

namespace Signum.Engine.Linq
{
    /// <summary>
    /// QueryBinder is a visitor that converts method calls to LINQ operations into 
    /// custom DbExpression nodes and references to class members into references to columns
    /// </summary>
    internal class MetadataVisitor : ExpressionVisitor
    {
        Dictionary<ParameterExpression, Expression> map = new Dictionary<ParameterExpression, Expression>();

        private MetadataVisitor() { }

        static internal Dictionary<string, Meta> GatherMetadata(Expression expression)
        {
            if (expression == null)
                throw new ArgumentException("expression");

            if (!typeof(IQueryable).IsAssignableFrom(expression.Type))
                throw new InvalidOperationException("expression Type is not IQueryable");

            Expression expand = ExpressionExpander.ExpandUntyped(expression);
            //Expression partialEval = ExpressionEvaluator.PartialEval(expand);
            Expression simplified = OverloadingSimplifier.Simplify(expand);

            MetaProjectorExpression meta = new MetadataVisitor().Visit(simplified) as MetaProjectorExpression;

            if (meta == null)
                return null;

            var proj = meta.Projector;

            if (proj.NodeType != ExpressionType.New &&  //anonymous types
                proj.NodeType != ExpressionType.MemberInit && // not-anonymous type
                !(proj.NodeType == (ExpressionType)MetaExpressionType.MetaExpression && ((MetaExpression)proj).IsEntity)) // raw-entity!
                return null;

            PropertyInfo[] props = proj.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            return props.ToDictionary(pi => pi.Name, pi =>
            {
                Expression ex = BindMember(proj, pi, pi.PropertyType);
                return (ex as MetaExpression).TryCC(c => c.Meta);
            });
        }

        static MetaExpression MakeCleanMeta(Type type, Expression expression)
        {
            MetaExpression meta = expression as MetaExpression;
            if (meta != null)
            {
                return new MetaExpression(type, meta.Meta); 
            }

            return new MetaExpression(type, null); 
        }

        static MetaExpression MakeDirtyMeta(Type type, params Expression[] expression)
        {
            var metas = expression.OfType<MetaExpression>().Select(a => a.Meta).NotNull().ToArray();
            if (metas.Length != expression.Length)
            {
                metas = metas;
            }

            return new MetaExpression(type, new DirtyMeta(metas));
        }

        Expression MakeVoidMeta(Type type)
        {
            return new MetaExpression(type, new DirtyMeta(new Meta[0]));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) ||
                m.Method.DeclaringType == typeof(Enumerable))
            {
                switch (m.Method.Name)
                {
                    case "Where":
                        return this.BindWhere(m.Type, m.GetArgument("source"), m.GetArgument("predicate").StripQuotes());
                    case "Select":
                        return this.BindSelect(m.Type, m.GetArgument("source"), m.GetArgument("selector").StripQuotes());
                    case "SelectMany":
                        return this.BindSelectMany(m.Type, m.GetArgument("source"), m.GetArgument("selector").StripQuotes());
                    case "Join":
                        return this.BindJoin(
                            m.Type, m.GetArgument("outer"), m.GetArgument("inner"),
                            m.GetArgument("outerKeySelector").StripQuotes(),
                            m.GetArgument("innerKeySelector").StripQuotes(),
                            m.GetArgument("resultSelector").StripQuotes());
                    case "OrderBy":
                        return this.BindOrderBy(m.Type, m.GetArgument("source"), m.GetArgument("keySelector").StripQuotes(), OrderType.Ascending);
                    case "OrderByDescending":
                        return this.BindOrderBy(m.Type, m.GetArgument("source"), m.GetArgument("keySelector").StripQuotes(), OrderType.Descending);
                    case "ThenBy":
                        return this.BindThenBy(m.GetArgument("source"), m.GetArgument("keySelector").StripQuotes(), OrderType.Ascending);
                    case "ThenByDescending":
                        return this.BindThenBy(m.GetArgument("source"), m.GetArgument("keySelector").StripQuotes(), OrderType.Descending);
                    case "GroupBy":
                        return this.BindGroupBy(m.Type, m.GetArgument("source"),
                            m.GetArgument("keySelector").StripQuotes(),
                            m.GetArgument("elementSelector").StripQuotes());
                    case "Count":
                        return this.BindCount(m.Type, m.GetArgument("source"));
                    case "DefaultIfEmpty":
                       return Visit(m.GetArgument("source"));
                    case "Any":
                        return this.BindAny(m.Type, m.GetArgument("source"));
                    case "All":
                        return this.BindAll(m.Type, m.GetArgument("source"), m.GetArgument("predicate").StripQuotes());
                    case "Contains":
                        return this.BindContains(m.Type, m.GetArgument("source"), m.TryGetArgument("item") ?? m.GetArgument("value"));
                    case "Sum":
                    case "Min":
                    case "Max":
                    case "Average":
                        return this.BindAggregate(m.Type, m.Method.Name.ToEnum<AggregateFunction>(),
                            m.GetArgument("source"), m.TryGetArgument("selector").StripQuotes());
                    case "First":
                    case "FirstOrDefault":
                    case "Single":
                    case "SingleOrDefault":
                        return BindUniqueRow(m.Type, m.Method.Name.ToEnum<UniqueFunction>(),
                            m.GetArgument("source"), m.TryGetArgument("predicate").StripQuotes());
                    case "Distinct":
                        return BindDistinct(m.Type, m.GetArgument("source"));
                    case "Take":
                        return BindTake(m.Type, m.GetArgument("source"), m.GetArgument("count"));
                    case "Skip":
                        return BindSkip(m.Type, m.GetArgument("source"), m.GetArgument("count"));
                    case "Union":
                    case "Concat":
                    case "Except":
                    case "Intersect":
                        return BindSetOperation(m.Type, m.Method.Name.ToEnum<SetOperation>(),
                            m.TryGetArgument("first") ?? m.GetArgument("source1"),
                            m.TryGetArgument("second") ?? m.GetArgument("source2"));
                }
            }


            if (m.Method.DeclaringType == typeof(LazyUtils) && m.Method.Name == "ToLazy")
                return MakeCleanMeta(m.Type, Visit(m.Arguments[0]));

            if (m.Method.DeclaringType == typeof(Math) &&
               (m.Method.Name == "Abs" ||
                m.Method.Name == "Ceiling" ||
                m.Method.Name == "Floor" ||
                m.Method.Name == "Round" ||
                m.Method.Name == "Truncate"))
                return MakeCleanMeta(m.Type, Visit(m.Arguments[0]));

            if (m.Object != null)
            {
                var a = this.Visit(m.Object);
                var list = this.VisitExpressionList(m.Arguments);
                return MakeDirtyMeta(m.Type, list.PreAnd(a).ToArray());
            }
            else
            {
                var list = this.VisitExpressionList(m.Arguments);
                return MakeDirtyMeta(m.Type, list.ToArray());
            }
        }

        private Expression MapAndVisit(LambdaExpression lambda, params MetaProjectorExpression[] projs)
        {
            map.SetRange(lambda.Parameters, projs.Select(a => a.Projector));
            var result =  Visit(lambda.Body);
            map.RemoveRange(lambda.Parameters);
            return result;
        }

        private MetaProjectorExpression AsProjection(Expression expression)
        {
            MetaProjectorExpression mpe = expression as MetaProjectorExpression;
            if (mpe != null)
                return (MetaProjectorExpression)mpe;

            if (expression.NodeType == ExpressionType.New)
            {
                NewExpression nex = (NewExpression)expression;
                if (nex.Type.IsInstantiationOf(typeof(Grouping<,>)))
                    return (MetaProjectorExpression)nex.Arguments[1]; 
            }

            Type elementType = ReflectionTools.CollectionType(expression.Type);
            if (elementType != null)
            {
                MetaExpression meta = expression as MetaExpression;
                if (meta != null && meta.Meta is CleanMeta)
                    return new MetaProjectorExpression(expression.Type,
                        new MetaExpression(elementType,
                            new CleanMeta(elementType, expression.Type.GetProperty("Item"), (CleanMeta)meta.Meta)));

                return new MetaProjectorExpression(expression.Type,
                     MakeVoidMeta(elementType)); 
            }

            throw new InvalidOperationException(); 
        }

        private Expression BindSetOperation(Type resultType, SetOperation setOperation, Expression left, Expression right)
        {
            return AsProjection(Visit(left));
        }

        private Expression BindTake(Type resultType, Expression source, Expression count)
        {
            return AsProjection(Visit(source));
        }

        private Expression BindSkip(Type resultType, Expression source, Expression count)
        {
            return AsProjection(Visit(source));
        }

        private Expression BindUniqueRow(Type resultType, UniqueFunction function, Expression source, LambdaExpression predicate)
        {
            return AsProjection(Visit(source)).Projector;
        }

        private Expression BindDistinct(Type resultType, Expression source)
        {
            return AsProjection(Visit(source));
        }

        private Expression BindCount(Type resultType, Expression source)
        {
            return MakeVoidMeta(resultType);
        }

        private Expression BindAll(Type resultType, Expression source, LambdaExpression predicate)
        {
            return MakeVoidMeta(resultType);
        }

        private Expression BindAny(Type resultType, Expression source)
        {
            return MakeVoidMeta(resultType);
        }

        private Expression BindContains(Type resultType, Expression source, Expression item)
        {
            return MakeVoidMeta(resultType);
        }

        private Expression BindAggregate(Type resultType, AggregateFunction aggregateFunction, Expression source, LambdaExpression selector)
        {
            MetaProjectorExpression mp = AsProjection(Visit(source));
            if (selector == null)
                return mp.Projector;

            Expression projector = MapAndVisit(selector, mp);
            return projector; 
        }

        private Expression BindWhere(Type resultType, Expression source, LambdaExpression predicate)
        {
            return AsProjection(Visit(source)); 
        }

        private Expression BindSelect(Type resultType, Expression source, LambdaExpression selector)
        {
            MetaProjectorExpression mp = AsProjection(Visit(source));
            Expression projector = MapAndVisit(selector, mp);
            return new MetaProjectorExpression(resultType, projector); 
        }

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector)
        {
            MetaProjectorExpression mp = AsProjection(Visit(source));
            return AsProjection(MapAndVisit(collectionSelector, mp));
        }

        protected virtual Expression BindJoin(Type resultType, Expression outerSource, Expression innerSource, LambdaExpression outerKey, LambdaExpression innerKey, LambdaExpression resultSelector)
        {
            MetaProjectorExpression mpOuter = AsProjection(Visit(outerSource));
            MetaProjectorExpression mpInner = AsProjection(Visit(innerSource));
            Expression projector = MapAndVisit(resultSelector, mpOuter, mpInner);
            return new MetaProjectorExpression(resultType, projector); 
        }

        private Expression BindGroupBy(Type resultType, Expression source, LambdaExpression keySelector, LambdaExpression elementSelector)
        {
            MetaProjectorExpression mp = AsProjection(Visit(source));
            Expression key = MapAndVisit(keySelector, mp);
            Expression element = MapAndVisit(elementSelector, mp);

            Type colType = typeof(IEnumerable<>).MakeGenericType(element.Type);
            Type groupType = typeof(Grouping<,>).MakeGenericType(key.Type, element.Type);

            return new MetaProjectorExpression(resultType,
                Expression.New(groupType.GetConstructor(new Type[] { key.Type, colType }),
                key, new MetaProjectorExpression(colType, element)));
        }
   
        protected virtual Expression BindOrderBy(Type resultType, Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            return AsProjection(Visit(source)); 
        }

        protected virtual Expression BindThenBy(Expression source, LambdaExpression orderSelector, OrderType orderType)
        {
            return AsProjection(Visit(source)); 
        }

        private Type TableType(object value)
        {
            if (value == null)
                return null;

            Type t = value.GetType();
            return t.IsInstantiationOf(typeof(Query<>)) ?
                t.GetGenericArguments()[0] :
                null;
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            Type type = TableType(c.Value);
            if (type != null)
                return new MetaProjectorExpression(c.Type, new MetaExpression(type, null));

            return MakeVoidMeta(c.Type);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return map.TryGetC(p) ?? p;
        }

        protected override Expression VisitMemberAccess(MemberExpression m)
        {
            Expression source = Visit(m.Expression);

            return BindMember(source, m.Member, m.Type);
        }

        static Expression BindMember(Expression source, MemberInfo member, Type memberType)
        {
            switch (source.NodeType)
            {
                case ExpressionType.MemberInit:
                    return ((MemberInitExpression)source).Bindings
                        .OfType<MemberAssignment>()
                        .Single(a => ReflectionTools.MemeberEquals(a.Member, member)).Expression;
                case ExpressionType.New:
                    NewExpression nex = (NewExpression)source;
                    if (nex.Type.IsInstantiationOf(typeof(Grouping<,>)) && member.Name == "Key")
                    {
                        return nex.Arguments[0];
                    }
                    if (nex.Members != null)
                    {
                        MethodInfo mi = ((PropertyInfo)member).GetGetMethod();
                        return nex.Members.Zip(nex.Arguments).Single(p => ReflectionTools.MethodEqual((MethodInfo)p.First, mi)).Second;
                    }

                    break;
            }

            if (typeof(ModifiableEntity).IsAssignableFrom(source.Type))
            {
                MetaExpression meta = source as MetaExpression;
                if (typeof(EmbeddedEntity).IsAssignableFrom(source.Type) && meta != null && meta.Meta is CleanMeta)
                {
                    return new MetaExpression(memberType, new CleanMeta(meta.Type, member, ((CleanMeta)meta.Meta)));
                }

                return new MetaExpression(memberType, new CleanMeta(source.Type, member, null));
            }

            return MakeDirtyMeta(memberType, source);
        }


        protected override Expression VisitTypeIs(TypeBinaryExpression b)
        {
            return MakeDirtyMeta(b.Type, Visit(b.Expression)); 
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            return MakeCleanMeta(u.Type, Visit(u.Operand));
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            return MakeDirtyMeta(b.Type, Visit(b.Right), Visit(b.Left)); 
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            return MakeDirtyMeta(c.Type, Visit(c.Test), Visit(c.IfTrue), Visit(c.IfFalse));
        }
    }
}
