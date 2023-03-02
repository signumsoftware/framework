using Signum.Utilities.Reflection;

namespace Signum.Entities.DynamicQuery;

public class NetPropertyToken : QueryToken
{
    public MemberInfo MemberInfo { get; private set; }
    public Func<string> DisplayName { get; private set; }

    QueryToken parent;
    public override QueryToken? Parent => parent;

    internal NetPropertyToken(QueryToken parent, MemberInfo pi, Func<string> displayName, string? format = null, string ? unit = null)
    {
        this.parent = parent ?? throw new ArgumentNullException(nameof(parent));

        this.DisplayName = displayName ?? throw new ArgumentNullException(nameof(displayName));
        this.MemberInfo = pi ?? throw new ArgumentNullException(nameof(pi));
        this.Format = format;
        this.Unit = unit;
    }

    public override Type Type
    {
        get
        {
            return
                MemberInfo is PropertyInfo pi ? pi.PropertyType.Nullify() :
                MemberInfo is MethodInfo mi ? mi.ReturnType.Nullify() :
                throw new UnexpectedValueException(MemberInfo);
        }
    }

    public override string ToString()
    {
        return DisplayName();
    }

    public override string Key
    {
        get { return MemberInfo.Name; }
    }

    public static MethodInfo miInSql = ReflectionTools.GetMethodInfo(() => (1).InSql()).GetGenericMethodDefinition();

    protected override Expression BuildExpressionInternal(BuildExpressionContext context)
    {
        var result = parent.BuildExpression(context);

        var prop =
            MemberInfo is PropertyInfo pi ? (Expression)Expression.Property(result.UnNullify(), pi) :
            MemberInfo is MethodInfo mi ? (mi.IsStatic ? Expression.Call(null, mi, result.UnNullify()) : Expression.Call(result.UnNullify(), mi)) :
            throw new UnexpectedValueException(MemberInfo);

        return Expression.Call(miInSql.MakeGenericMethod(prop.Type), prop).Nullify();
    }

    protected override List<QueryToken> SubTokensOverride(SubTokensOptions options)
    {
        return SubTokensBase(this.Type, options, GetImplementations());
    }


    public override string? Format { get; }

    public override string? Unit { get; }

    public override Implementations? GetImplementations()
    {
        return null;
    }

    public override string? IsAllowed()
    {
        return parent.IsAllowed();
    }

    public override PropertyRoute? GetPropertyRoute()
    {
        return null;
    }

    public override string NiceName()
    {
        return QueryTokenMessage._0Of1.NiceToString(DisplayName(), parent.ToString());
    }

    public override QueryToken Clone()
    {
        return new NetPropertyToken(parent.Clone(), MemberInfo, DisplayName);
    }
}

