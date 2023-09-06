using Signum.Engine.Linq;
using Signum.Utilities.DataStructures;
using Signum.Utilities.Reflection;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using static System.Net.WebRequestMethods;

namespace Signum.Authorization.Rules;

public class QueryAuditorVisitor : ExpressionVisitor
{
    public ConstantExpression BaseQuery { get; private set; }

    public static FilterAuditorProjectorExpression FilterAuditor(Expression fullQuery, ConstantExpression baseQuery)
    {
        var visitor = new QueryAuditorVisitor { BaseQuery = baseQuery };

        var result = visitor.Visit(fullQuery);

        return (FilterAuditorProjectorExpression)result;
    }

    [return: NotNullIfNotNull("node")]
    public override Expression? Visit(Expression? node)
    {
        return base.Visit(node);
    }
    protected override Expression VisitConstant(ConstantExpression node)
    {
        if (node == BaseQuery)
        {
            var queryable = ((IQueryable)node.Value!);
            var paramName = Reflector.CleanTypeName(queryable.ElementType).Where(char.IsUpper).ToString("").ToLower();
            var param = Expression.Parameter(queryable.ElementType, paramName);
            return new FilterAuditorProjectorExpression(node.Type, param, projector: param, FilterAuditorProjectorExpression.EmptyFilters);
        }

        return base.VisitConstant(node);
    }

    protected override Expression VisitMethodCall(MethodCallExpression node)
    {
        if (node.Method.DeclaringType == typeof(Enumerable) ||
            node.Method.DeclaringType == typeof(Queryable) || 
            node.Method.DeclaringType == typeof(LinqHints))
        {
            var source = node.TryGetArgument("source");

            if (source != null)
            {
                var proj = Visit(source);

                if (proj is FilterAuditorProjectorExpression fap)
                {
                    if(fap.Param == null)
                        return new FilterAuditorProjectorExpression(node.Type);

                    if (fap.Projector == null)
                        return new FilterAuditorProjectorExpression(node.Type, fap.Param, null, fap.Filters);

                    if (node.Method.Name == nameof(Queryable.Select))
                    {
                        var selector = node.GetArgument("selector").StripQuotes();

                        var replaced = ExpressionReplacer.Replace(selector.Body, new Dictionary<ParameterExpression, Expression>
                        {
                            { selector.Parameters[0], fap.Projector }
                        });

                        var binded = MemberBinderVisitor.Bind(replaced);

                        return new FilterAuditorProjectorExpression(node.Type, fap.Param, binded, fap.Filters);
                    }

                    if (node.Method.Name == nameof(Queryable.Where))
                    {
                        var predicate = node.GetArgument("predicate").StripQuotes();

                        var replaced = ExpressionReplacer.Replace(predicate.Body, new Dictionary<ParameterExpression, Expression>
                        {
                            { predicate.Parameters[0], fap.Projector }
                        });

                        var binded = MemberBinderVisitor.Bind(replaced);

                        var splitted = ConditionSplitter.SplitAnds(binded);

                        return new FilterAuditorProjectorExpression(node.Type, fap.Param, fap.Projector, new ReadOnlyCollection<Expression>(fap.Filters.Concat(splitted).ToList()));
                    }

                    if (node.Method.Name is  
                        nameof(Queryable.Distinct) or
                        nameof(Queryable.DistinctBy) or
                        nameof(Queryable.Skip) or 
                        nameof(Queryable.SkipLast) or 
                        nameof(Queryable.SkipWhile) or 
                        nameof(Queryable.Take)  or
                        nameof(Queryable.TakeLast) or 
                        nameof(Queryable.TakeWhile) or 
                        nameof(Queryable.Order) or
                        nameof(Queryable.OrderBy) or
                        nameof(Queryable.OrderByDescending) or
                        nameof(Queryable.ThenBy) or
                        nameof(Queryable.ThenByDescending) or
                        nameof(LinqHints.DisableQueryFilter) or 
                        nameof(LinqHints.OrderAlsoByKeys)
                        )
                        return new FilterAuditorProjectorExpression(node.Type, fap.Param, fap.Projector, fap.Filters);

                    return new FilterAuditorProjectorExpression(node.Type, fap.Param, null, fap.Filters);
                }
            }
        }
        return new FilterAuditorProjectorExpression(node.Type);
    }

    internal static bool IsEqualsConstant(Expression replaced, Expression condition, [NotNullWhen(true)]out ConstantExpression? constant)
    {
        constant = default;

        if(condition is BinaryExpression be && be.NodeType == ExpressionType.Equal)
        {
            if(be.Left is ConstantExpression ceLeft && CleanEquals(be.Right, replaced))
            {
                constant = ceLeft;
                return true;
            }

            if (be.Right is ConstantExpression ceRight && CleanEquals(be.Left, replaced))
            {
                constant = ceRight;
                return true;
            }
        }

        if(condition is MethodCallExpression mce && mce.Method.Name == nameof(Lite.Is) && mce.Method.DeclaringType == typeof(Lite))
        {
            var left = mce.Arguments[0];
            var right = mce.Arguments[1];

            if (left is ConstantExpression ceLeft && CleanEquals(right, replaced))
            {
                constant = ceLeft;
                return true;
            }

            if (left is ConstantExpression ceRight && CleanEquals(right, replaced))
            {
                constant = ceRight;
                return true;
            }
        }

        return false;
    }

    internal static bool CleanEquals(Expression a, Expression b)
    {
        if (a.Type == b.Type)
            return ExpressionComparer.AreEqual(a, b);

        var aClean = Clean(a);
        var bClean = Clean(b);

        return ExpressionComparer.AreEqual(aClean, bClean);
    }

    private static Expression Clean(Expression e)
    {
        if (e is MemberExpression me && me.Expression != null && me.Expression.Type.IsLite()
            && me.Member.Name is nameof(Lite<Entity>.Entity) or nameof(Lite<Entity>.EntityOrNull))
            return Clean(me.Expression);

        if (e is MethodCallExpression mc && mc.Method.DeclaringType == typeof(Lite) && mc.Method.Name is nameof(Lite.ToLite) or nameof(Lite.ToLiteFat))
            return Clean(mc.Arguments[0]);

        if (e is UnaryExpression ue && ue.NodeType == ExpressionType.Convert)
            return Clean(ue.Operand);

        return e;
    }
}

//Database.Query<PersonEntity>().Select(p => p.Country).Where(c => c.Name == "Germany"); 

public class FilterAuditorProjectorExpression : Expression
{
    public override Type Type { get; }

    public override ExpressionType NodeType { get; }

    public ParameterExpression? Param { get; }
    public Expression? Projector { get; }

    public ReadOnlyCollection<Expression> Filters { get; }

    public static readonly ReadOnlyCollection<Expression> EmptyFilters = new ReadOnlyCollection<Expression>(new List<Expression>());

    public FilterAuditorProjectorExpression(Type type)
    {
        this.Type = type;
        this.NodeType = (ExpressionType)FilterAuditorExpressionType.FilterAuditorProjecto;
        this.Filters = EmptyFilters;
    }

    public FilterAuditorProjectorExpression(Type type, ParameterExpression param, Expression? projector, ReadOnlyCollection<Expression> filters)
    {
        this.Type = type;
        this.NodeType = (ExpressionType)FilterAuditorExpressionType.FilterAuditorProjecto;
        this.Param = param;
        this.Projector = projector;
        this.Filters = filters;
    }
}

internal enum FilterAuditorExpressionType
{
    FilterAuditorProjecto = 3000,
}


public class MemberBinderVisitor : ExpressionVisitor
{
    public static Expression Bind(Expression body)
    {
        var visitor = new MemberBinderVisitor();

        var result = visitor.Visit(body);

        return result;
    }

    protected override Expression VisitMember(MemberExpression m)
    {
        if (m.Expression != null)
        {
            var expr = Visit(m.Expression);

            if (expr is MemberInitExpression n)
            {
                var binding = n.Bindings.SingleOrDefault(a => ReflectionTools.Equals(a.Member, m.Member));

                if (binding is MemberAssignment ma)
                    return ma.Expression;
            }
            else if (expr is NewExpression nex)
            {
                if (nex.Type.IsInstantiationOf(typeof(Grouping<,>)))
                {
                    if (m.Member.Name == "Key")
                        return nex.Arguments[0];
                }
                else if (TupleReflection.IsTuple(nex.Type))
                {
                    int index = TupleReflection.TupleIndex((PropertyInfo)m.Member);
                    return nex.Arguments[index];
                }
                else if (ValueTupleReflection.IsValueTuple(nex.Type))
                {
                    int index = ValueTupleReflection.TupleIndex((FieldInfo)m.Member);
                    return nex.Arguments[index];
                }
                else
                {
                    if (nex.Members == null)
                    {
                        int index = nex.Constructor!.GetParameters().IndexOf(p => p.Name!.Equals(m.Member.Name, StringComparison.InvariantCultureIgnoreCase));

                        if (index == -1)
                            throw new InvalidOperationException("Impossible to bind '{0}' on '{1}'".FormatWith(m.Member.Name, nex.Constructor.ConstructorSignature()));

                        return nex.Arguments[index].TryConvert(m.Member.ReturningType());
                    }

                    PropertyInfo pi = (PropertyInfo)m.Member;
                    return nex.Members.Zip(nex.Arguments).SingleEx(p => ReflectionTools.PropertyEquals((PropertyInfo)p.First, pi)).Second;
                }
            }
            else if (expr is MethodCallExpression mce)
            {

                if (mce.Method.DeclaringType == typeof(Tuple) && mce.Method.Name == "Create")
                {
                    int index = TupleReflection.TupleIndex((PropertyInfo)m.Member);
                    return mce.Arguments[index];
                }
                else if (mce.Method.DeclaringType == typeof(ValueTuple) && mce.Method.Name == "Create")
                {
                    int index = ValueTupleReflection.TupleIndex((FieldInfo)m.Member);
                    return mce.Arguments[index];
                }
                else if (mce.Method.DeclaringType == typeof(KeyValuePair) && mce.Method.Name == "Create")
                {
                    if (m.Member.Name == "Key")
                        return mce.Arguments[0];
                    else if (m.Member.Name == "Value")
                        return mce.Arguments[1];
                }
                //else if (mce.Method.IsInstantiationOf(miSetReadonly))
                //{
                //    var pi = ReflectionTools.BasePropertyInfo(mce.Arguments[1].StripQuotes());
                //    if (m.Member is PropertyInfo piMember && ReflectionTools.PropertyEquals(pi, piMember))
                //        return mce.Arguments[2];
                //    else
                //        return BindMemberAccess(
                //            m.Member is PropertyInfo pi1 ? Expression.Property(mce.Arguments[0], pi1) :
                //            m.Member is FieldInfo fi1 ? Expression.Field(mce.Arguments[0], fi1) :
                //            throw new InvalidOperationException(nameof(m.Member))
                //            );
                //}
            }
        }

        return base.VisitMember(m);
    }
}

public static class ConditionSplitter
{
    public static List<Expression> SplitAnds(Expression expression)
    {
        List<Expression> result = new List<Expression>();
        SplitAndsPrivate(result, expression);
        return result;
    }

    private static void SplitAndsPrivate(List<Expression> result, Expression expression)
    {
        if(expression is BinaryExpression be &&  be.NodeType is ExpressionType.AndAlso or ExpressionType.And)
        {
            SplitAndsPrivate(result, be.Left);
            SplitAndsPrivate(result, be.Right);
        }
        else
        {
            result.Add(expression);
        }
    }
}

