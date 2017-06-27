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
                throw new InvalidOperationException("Expression type is not IQueryable");

            Expression simplified = MetaEvaluator.Clean(expression);

            MetaProjectorExpression meta = new MetadataVisitor().Visit(simplified) as MetaProjectorExpression;

            if (meta == null)
                return null;

            var proj = meta.Projector;

            if (proj.NodeType != ExpressionType.New &&  //anonymous types
                proj.NodeType != ExpressionType.MemberInit && // not-anonymous type
                !(proj is MetaExpression && ((MetaExpression)proj).IsEntity)) // raw-entity!
                return null;

            PropertyInfo[] props = proj.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            return props.ToDictionary(pi => pi.Name, pi =>
            {
                Expression ex = BindMember(proj, pi, pi.PropertyType);
                return (ex as MetaExpression)?.Meta;
            });
        }



        //internal static Expression JustVisit(LambdaExpression expression, PropertyRoute route)
        //{
        //    if (route.Type.IsLite())
        //        route = route.Add("Entity");

        //    return JustVisit(expression, ));
        //}

        internal static Expression JustVisit(LambdaExpression expression, MetaExpression metaExpression)
        {
            var cleaned = MetaEvaluator.Clean(expression);

            var replaced = ExpressionReplacer.Replace(Expression.Invoke(cleaned, metaExpression));

            return new MetadataVisitor().Visit(replaced);
        }

        static MetaExpression MakeCleanMeta(Type type, Expression expression)
        {
            MetaExpression meta = (MetaExpression)expression;

            return new MetaExpression(type, meta.Meta);
        }

        static MetaExpression MakeDirtyMeta(Type type, Implementations? implementations, params Expression[] expression)
        {
            var metas = expression.OfType<MetaExpression>().Select(a => a.Meta).NotNull().ToArray();

            return new MetaExpression(type, new DirtyMeta(implementations, metas));
        }

        static internal Expression MakeVoidMeta(Type type)
        {
            return new MetaExpression(type, new DirtyMeta(null, new Meta[0]));
        }

        protected override Expression VisitMethodCall(MethodCallExpression m)
        {
            if (m.Method.DeclaringType == typeof(Queryable) ||
                m.Method.DeclaringType == typeof(Enumerable) ||
                m.Method.DeclaringType == typeof(EnumerableUniqueExtensions))
            {
                switch (m.Method.Name)
                {
                    case "Where":
                        return this.BindWhere(m.Type, m.GetArgument("source"), m.GetArgument("predicate").StripQuotes());
                    case "Select":
                        return this.BindSelect(m.Type, m.GetArgument("source"), m.GetArgument("selector").StripQuotes());
                    case "SelectMany":
                        if (m.Arguments.Count == 2)
                            return this.BindSelectMany(m.Type, m.GetArgument("source"), m.GetArgument("selector").StripQuotes(), null);
                        else
                            return this.BindSelectMany(m.Type, m.GetArgument("source"), m.GetArgument("collectionSelector").StripQuotes(), m.TryGetArgument("resultSelector").StripQuotes());
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
                        return this.BindAggregate(m.Type, m.Method.Name.ToEnum<AggregateSqlFunction>(),
                            m.GetArgument("source"), m.TryGetArgument("selector").StripQuotes());
                    case "First":
                    case "FirstOrDefault":
                    case "Single":
                    case "SingleOrDefault":
                        return BindUniqueRow(m.Type, m.Method.Name.ToEnum<UniqueFunction>(),
                            m.GetArgument("source"), m.TryGetArgument("predicate").StripQuotes());
                    case "FirstEx":
                    case "SingleEx":
                    case "SingleOrDefaultEx":
                        return BindUniqueRow(m.Type, m.Method.Name.RemoveEnd(2).ToEnum<UniqueFunction>(),
                           m.GetArgument("collection"), m.TryGetArgument("predicate").StripQuotes());
                    case "Distinct":
                        return BindDistinct(m.Type, m.GetArgument("source"));
                    case "Take":
                        return BindTake(m.Type, m.GetArgument("source"), m.GetArgument("count"));
                    case "Skip":
                        return BindSkip(m.Type, m.GetArgument("source"), m.GetArgument("count"));
                }
            }

            if (m.Method.Name == "Mixin" && m.Method.GetParameters().Length == 0)
            {
                var obj = Visit(m.Object);

                if (obj is MetaExpression me && me.Meta is CleanMeta)
                {
                    CleanMeta cm = (CleanMeta)me.Meta;

                    var mixinType = m.Method.GetGenericArguments().Single();

                    return new MetaExpression(mixinType, new CleanMeta(null, cm.PropertyRoutes.Select(a => a.Add(mixinType)).ToArray()));
                }
            }

            if (m.Method.DeclaringType == typeof(LinqHints) || m.Method.DeclaringType == typeof(LinqHintEntities))
                return Visit(m.Arguments[0]);

            if (m.Method.DeclaringType == typeof(Lite) && m.Method.Name == "ToLite")
                return MakeCleanMeta(m.Type, Visit(m.Arguments[0]));

            if (m.Method.DeclaringType == typeof(Math) &&
               (m.Method.Name == "Abs" ||
                m.Method.Name == "Ceiling" ||
                m.Method.Name == "Floor" ||
                m.Method.Name == "Round" ||
                m.Method.Name == "Truncate"))
                return MakeCleanMeta(m.Type, Visit(m.Arguments[0]));

            if (m.Method.Name == "ToString" && m.Object != null && typeof(IEntity).IsAssignableFrom(m.Object.Type))
                return Visit(Expression.Property(m.Object, piToStringProperty));

            if (m.Object != null)
            {
                var a = this.Visit(m.Object);
                var list = this.Visit(m.Arguments);
                return MakeDirtyMeta(m.Type, null, list.PreAnd(a).ToArray());
            }
            else
            {
                var list = this.Visit(m.Arguments);
                return MakeDirtyMeta(m.Type, null, list.ToArray());
            }
        }

        static readonly PropertyInfo piToStringProperty = ReflectionTools.GetPropertyInfo((IEntity ii) => ii.ToStringProperty);


        private Expression MapAndVisit(LambdaExpression lambda, params MetaProjectorExpression[] projs)
        {
            map.SetRange(lambda.Parameters, projs.Select(a => a.Projector));
            var result = Visit(lambda.Body);
            map.RemoveRange(lambda.Parameters);
            return result;
        }

        public static MetaProjectorExpression AsProjection(Expression expression)
        {
            if (expression is MetaProjectorExpression mpe)
                return (MetaProjectorExpression)mpe;

            if (expression.NodeType == ExpressionType.New)
            {
                NewExpression nex = (NewExpression)expression;
                if (nex.Type.IsInstantiationOf(typeof(Grouping<,>)))
                    return (MetaProjectorExpression)nex.Arguments[1];
            }

            Type elementType = expression.Type.ElementType();
            if (elementType != null)
            {
                if (expression is MetaExpression meta && meta.Meta is CleanMeta)
                {
                    PropertyRoute route = ((CleanMeta)meta.Meta).PropertyRoutes.SingleEx(() => "PropertyRoutes for {0}. Metas don't work over polymorphic MLists".FormatWith(meta.Meta)).Add("Item");

                    return new MetaProjectorExpression(expression.Type,
                        new MetaExpression(elementType,
                            new CleanMeta(route.TryGetImplementations(), route)));
                }

                return new MetaProjectorExpression(expression.Type,
                     MakeVoidMeta(elementType));
            }

            throw new InvalidOperationException();
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

        private Expression BindAggregate(Type resultType, AggregateSqlFunction aggregateFunction, Expression source, LambdaExpression selector)
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

        protected virtual Expression BindSelectMany(Type resultType, Expression source, LambdaExpression collectionSelector, LambdaExpression resultSelector)
        {
            MetaProjectorExpression mp = AsProjection(Visit(source));
            MetaProjectorExpression collectionProjector = AsProjection(MapAndVisit(collectionSelector, mp));

            if (resultSelector == null)
                return collectionProjector;

            Expression resultProjection = MapAndVisit(resultSelector, mp, collectionProjector);
            return new MetaProjectorExpression(resultType, resultProjection);
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

        public Type TableType(object value)
        {
            if (value == null)
                return null;

            Type t = value.GetType();
            return typeof(IQueryable).IsAssignableFrom(t) ?
                t.GetGenericArguments()[0] :
                null;
        }

        public override Expression Visit(Expression exp)
        {
            if (exp is MetaExpression)
                return exp;

            return base.Visit(exp);
        }

        protected override Expression VisitConstant(ConstantExpression c)
        {
            Type type = TableType(c.Value);
            if (type != null)
            {
                if (typeof(Entity).IsAssignableFrom(type))
                    return new MetaProjectorExpression(c.Type, new MetaExpression(type, new CleanMeta(Implementations.By(type), PropertyRoute.Root(type))));

                if (type.IsInstantiationOf(typeof(MListElement<,>)))
                {
                    var parentType = type.GetGenericArguments()[0];
                
                    ISignumTable st = (ISignumTable)c.Value;
                    TableMList rt = (TableMList)st.Table;


                    PropertyRoute element = rt.PropertyRoute.Add("Item");

                    return new MetaProjectorExpression(c.Type, new MetaMListExpression(type, 
                        new CleanMeta(Implementations.By(parentType), PropertyRoute.Root(rt.PropertyRoute.RootType)), 
                        new CleanMeta(element.TryGetImplementations(), element)));
                }
            }

            return MakeVoidMeta(c.Type);
        }

        protected override Expression VisitParameter(ParameterExpression p)
        {
            return map.TryGetC(p) ?? p;
        }

        protected override Expression VisitMember(MemberExpression m)
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
                        .SingleEx(a => ReflectionTools.MemeberEquals(a.Member, member)).Expression;
                case ExpressionType.New:
                    NewExpression nex = (NewExpression)source;
                    if (nex.Type.IsInstantiationOf(typeof(Grouping<,>)) && member.Name == "Key")
                    {
                        return nex.Arguments[0];
                    }

                    if (nex.Members != null)
                    {
                        PropertyInfo pi = (PropertyInfo)member;
                        return nex.Members.Zip(nex.Arguments).SingleEx(p => ReflectionTools.PropertyEquals((PropertyInfo)p.first, pi)).second;
                    }
                    break;
            }

            if (source is MetaMListExpression mme)
            {
                var ga = mme.Type.GetGenericArguments();
                if (member.Name == "Parent")
                    return new MetaExpression(ga[0], mme.Parent);

                if (member.Name == "Element")
                    return new MetaExpression(ga[1], mme.Element);

                throw new InvalidOperationException("Property {0} not found on {1}".FormatWith(member.Name, mme.Type.TypeName()));
            }

            if (typeof(ModifiableEntity).IsAssignableFrom(source.Type) || typeof(IEntity).IsAssignableFrom(source.Type))
            {
                var pi = member as PropertyInfo ?? Reflector.TryFindPropertyInfo((FieldInfo)member);

                if (pi == null)
                    return new MetaExpression(memberType, null);

                MetaExpression meta = (MetaExpression)source;

                if (meta.Meta.Implementations != null)
                {
                    var routes = meta.Meta.Implementations.Value.Types.Select(t=> PropertyRoute.Root(t).Add(pi)).ToArray();

                    return new MetaExpression(memberType, new CleanMeta(GetImplementations(routes, memberType), routes)); 
                }

                if (meta.Meta is CleanMeta)
                {
                    PropertyRoute[] routes = ((CleanMeta)meta.Meta).PropertyRoutes.Select(r => r.Add(pi.Name)).ToArray();

                    return new MetaExpression(memberType, new CleanMeta(GetImplementations(routes, memberType), routes));
                }

                if (typeof(Entity).IsAssignableFrom(source.Type) && !source.Type.IsAbstract) //Works for simple entities and also for interface casting
                {
                    var pr = PropertyRoute.Root(source.Type).Add(pi);

                    return new MetaExpression(memberType, new CleanMeta(pr.TryGetImplementations(), pr));
                }
            }

            if (source.Type.IsLite() && member.Name == "Entity")
            {
                MetaExpression meta = (MetaExpression)source;

                if (meta.Meta is CleanMeta)
                {
                    PropertyRoute[] routes = ((CleanMeta)meta.Meta).PropertyRoutes.Select(pr => pr.Add("Entity")).ToArray();

                    return new MetaExpression(memberType, new CleanMeta(meta.Meta.Implementations, routes));
                }
            }

            return MakeDirtyMeta(memberType, null, source);
        }

        internal static Entities.Implementations? GetImplementations(PropertyRoute[] propertyRoutes, Type cleanType)
        {
            if (!cleanType.IsIEntity() && !cleanType.IsLite())
                return (Implementations?)null;

            var only = propertyRoutes.Only();
            if (only != null && only.PropertyRouteType == PropertyRouteType.Root)
                return Signum.Entities.Implementations.By(cleanType);

            var aggregate = AggregateImplementations(propertyRoutes.Select(pr => pr.GetImplementations()));

            return aggregate;
        }

        public static Implementations AggregateImplementations(IEnumerable<Implementations> implementations)
        {
            if (implementations.IsEmpty())
                throw new InvalidOperationException("implementations is Empty");

            if (implementations.Count() == 1)
                return implementations.First();

            if (implementations.Any(a => a.IsByAll))
                return Signum.Entities.Implementations.ByAll;

            var types = implementations
                .SelectMany(ib => ib.Types)
                .Distinct()
                .ToArray();

            return Signum.Entities.Implementations.By(types);
        }

        protected override Expression VisitTypeBinary(TypeBinaryExpression b)
        {
            return MakeDirtyMeta(b.Type, null, Visit(b.Expression));
        }

        protected override Expression VisitUnary(UnaryExpression u)
        {
            var exp = (MetaExpression)Visit(u.Operand);

            if (u.NodeType == ExpressionType.Convert || u.NodeType == ExpressionType.TypeAs)
            {
                var imps = exp.Meta.Implementations?.Let(s => CastImplementations(s, u.Type.CleanType()));

                return new MetaExpression(u.Type, exp.Meta is DirtyMeta ?
                    (Meta)new DirtyMeta(imps, ((DirtyMeta)exp.Meta).CleanMetas.Cast<Meta>().ToArray()) :
                    (Meta)new CleanMeta(imps, ((CleanMeta)exp.Meta).PropertyRoutes));
            }

            return new MetaExpression(u.Type, exp.Meta);
        }

        internal static Implementations CastImplementations(Implementations implementations, Type cleanType)
        {
            if (implementations.IsByAll)
            {
                if (!Schema.Current.Tables.ContainsKey(cleanType))
                    throw new InvalidOperationException("Tye type {0} is not registered in the schema as a concrete table".FormatWith(cleanType));

                return Signum.Entities.Implementations.By(cleanType);
            }

            if (implementations.Types.All(cleanType.IsAssignableFrom))
                return implementations;

            return Signum.Entities.Implementations.By(implementations.Types.Where(cleanType.IsAssignableFrom).ToArray());
        }

        protected override Expression VisitBinary(BinaryExpression b)
        {
            var right = Visit(b.Right);
            var left = Visit(b.Left);

            var mRight = right as MetaExpression;
            var mLeft = left as MetaExpression;

            Implementations? imps =
                mRight != null && mRight.Meta.Implementations != null &&
                mLeft != null && mLeft.Meta.Implementations != null ?
                AggregateImplementations(new[] { 
                    mRight.Meta.Implementations.Value, 
                    mLeft.Meta.Implementations.Value }) :
                (Implementations?)null;

            return MakeDirtyMeta(b.Type, imps, left, right);
        }

        protected override Expression VisitConditional(ConditionalExpression c)
        {
            var ifTrue = Visit(c.IfTrue);
            var ifFalse = Visit(c.IfFalse);

            var mIfTrue = ifTrue as MetaExpression;
            var mIfFalse = ifFalse as MetaExpression;

            Implementations? imps =
                mIfTrue != null && mIfTrue.Meta.Implementations != null && 
                mIfFalse != null && mIfFalse.Meta.Implementations != null ?
                AggregateImplementations(new[] { 
                    mIfTrue.Meta.Implementations.Value, 
                    mIfFalse.Meta.Implementations.Value }) :
                (Implementations?)null;

            return MakeDirtyMeta(c.Type, imps, Visit(c.Test), ifTrue, ifFalse);
        }
    }
}
