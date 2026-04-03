
namespace Signum.Utilities.ExpressionTrees;

public static class Linq
{
    /// <summary>
    /// Utility function for building expression trees for lambda functions
    /// that return C# anonymous type as a result (because you can't declare
    /// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
    /// </summary>
    public static Expression<Func<R>> Expr<R>(Expression<Func<R>> f)
    {
        return f;
    }

	/// <summary>
	/// Utility function for building expression trees for lambda functions
	/// that return C# anonymous type as a result (because you can't declare
	/// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
	/// </summary>
	public static Expression<Func<T, R>> Expr<T, R>(Expression<Func<T, R>> f)
	{
		return f;
	}

	/// <summary>
	/// Utility function for building expression trees for lambda functions
	/// that return C# anonymous type as a result (because you can't declare
	/// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
	/// </summary>
	public static Expression<Func<T0, T1, R>> Expr<T0, T1, R>(Expression<Func<T0, T1, R>> f)
	{
		return f;
	}

	/// <summary>
	/// Utility function for building expression trees for lambda functions
	/// that return C# anonymous type as a result (because you can't declare
	/// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
	/// </summary>
	public static Expression<Func<T0, T1, T2, R>> Expr<T0, T1, T2, R>(Expression<Func<T0, T1, T2, R>> f)
	{
		return f;
	}

	/// <summary>
	/// Utility function for building expression trees for lambda functions
	/// that return C# anonymous type as a result (because you can't declare
	/// it using Expression&lt;Func&lt;...&gt;&gt; syntax)
	/// </summary>
	public static Expression<Func<T0, T1, T2, T3, R>> Expr<T0, T1, T2, T3, R>(Expression<Func<T0, T1, T2, T3, R>> f)
	{
		return f;
	}


    /// <summary>
    /// Utility function for building delegates for lambda functions
    /// that return C# anonymous type as a result (because you can't declare
    /// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
    /// </summary>
    public static Func<R> Func<R>(Func<R> f)
    {
        return f;
    }

	/// <summary>
	/// Utility function for building delegates for lambda functions
	/// that return C# anonymous type as a result (because you can't declare
	/// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
	/// </summary>
	public static Func<T, R> Func<T, R>(Func<T, R> f)
	{
		return f;
	}

	/// <summary>
	/// Utility function for building delegates for lambda functions
	/// that return C# anonymous type as a result (because you can't declare
	/// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
	/// </summary>
	public static Func<T0, T1, R> Func<T0, T1, R>(Func<T0, T1, R> f)
	{
		return f;
	}

	/// <summary>
	/// Utility function for building delegates for lambda functions
	/// that return C# anonymous type as a result (because you can't declare
	/// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
	/// </summary>
	public static Func<T0, T1, T2, R> Func<T0, T1, T2, R>(Func<T0, T1, T2, R> f)
	{
		return f;
	}

	/// <summary>
	/// Utility function for building delegates for lambda functions
	/// that return C# anonymous type as a result (because you can't declare
	/// it using delegates&lt;Func&lt;...&gt;&gt; syntax)
	/// </summary>
	public static Func<T0, T1, T2, T3, R> Func<T0, T1, T2, T3, R>(Func<T0, T1, T2, T3, R> f)
	{
		return f;
	}
}
