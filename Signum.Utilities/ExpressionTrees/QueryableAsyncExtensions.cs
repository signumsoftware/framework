using System;
using System.Collections.Generic;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;


namespace Signum.Utilities.ExpressionTrees
{

    public interface IQueryProviderAsync : IQueryProvider
    {
        Task<object> ExecuteAsync(Expression expression, CancellationToken token);
    }

    public static class QueryableAsyncExtensions
    {
        public static Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source) => source.ToListAsync(CancellationToken.None);
        public static async Task<List<TSource>> ToListAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
        {
            var provider = (IQueryProviderAsync)source.Provider;

            var value = await provider.ExecuteAsync(source.Expression, token);

            return (List<TSource>)value;
        }

        public static Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source) => source.ToArrayAsync(CancellationToken.None);
        public static async Task<TSource[]> ToArrayAsync<TSource>(this IQueryable<TSource> source, CancellationToken token)
        {
            var provider = (IQueryProviderAsync)source.Provider;

            var value = await provider.ExecuteAsync(source.Expression, token);

            return ((List<TSource>)value).ToArray();
        }

        static async Task<R> Bind<R>(CancellationToken cancellationToken, Expression<Func<R>> bind)
        {
            var mce = (MethodCallExpression)bind.Body;

            IQueryable query = (IQueryable)ExpressionEvaluator.Eval(mce.Arguments.FirstEx());

            List<Expression> otherExpressions = mce.Arguments.Skip(1).Select(a => (Expression)ExpressionEvaluator.Eval(a)).ToList();

            var mc2 = Expression.Call(mce.Method, otherExpressions.PreAnd(query.Expression));

            var provider = (IQueryProviderAsync)query.Provider;

            var value = await provider.ExecuteAsync(mc2, cancellationToken);

            return (R)value;
        }

        static Task<R> BindAsyncWithoutCancellationToken___<R>(Expression<Func<R>> bind)
        {
            return Bind(CancellationToken.None, bind);
        }

        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.FirstEx());
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.FirstEx());
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate) => BindAsyncWithoutCancellationToken___(() => source.FirstEx(predicate));
        public static Task<TSource> FirstAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token) => Bind(token, () => source.FirstEx(predicate));

        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.FirstOrDefault());
        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.FirstOrDefault());
        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate) => BindAsyncWithoutCancellationToken___(() => source.FirstOrDefault(predicate));
        public static Task<TSource> FirstOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token) => Bind(token, () => source.FirstOrDefault(predicate));

        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.SingleEx());
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.SingleEx());
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate) => BindAsyncWithoutCancellationToken___(() => source.SingleEx(predicate));
        public static Task<TSource> SingleAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token) => Bind(token, () => source.SingleEx(predicate));

        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.SingleOrDefaultEx());
        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.SingleOrDefaultEx());
        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate) => BindAsyncWithoutCancellationToken___(() => source.SingleOrDefaultEx(predicate));
        public static Task<TSource> SingleOrDefaultAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token) => Bind(token, () => source.SingleOrDefaultEx(predicate));


        public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item) => BindAsyncWithoutCancellationToken___(() => source.Contains(item));
        public static Task<bool> ContainsAsync<TSource>(this IQueryable<TSource> source, TSource item, CancellationToken token) => Bind(token, () => source.Contains(item));

        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.Any());
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.Any());
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate) => BindAsyncWithoutCancellationToken___(() => source.Any(predicate));
        public static Task<bool> AnyAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token) => Bind(token, () => source.Any(predicate));

        public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate) => BindAsyncWithoutCancellationToken___(() => source.All(predicate));
        public static Task<bool> AllAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token) => Bind(token, () => source.All(predicate));

        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.Count());
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.Count());
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate) => BindAsyncWithoutCancellationToken___(() => source.Count(predicate));
        public static Task<int> CountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token) => Bind(token, () => source.Count(predicate));

        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.LongCount());
        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.LongCount());
        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate) => BindAsyncWithoutCancellationToken___(() => source.LongCount(predicate));
        public static Task<long> LongCountAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, bool>> predicate, CancellationToken token) => Bind(token, () => source.LongCount(predicate));

        public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.Min());
        public static Task<TSource> MinAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.Min());
        public static Task<TResult> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector) => BindAsyncWithoutCancellationToken___(() => source.Min(selector));
        public static Task<TResult> MinAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken token) => Bind(token, () => source.Min(selector));

        public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source) => BindAsyncWithoutCancellationToken___(() => source.Max());
        public static Task<TSource> MaxAsync<TSource>(this IQueryable<TSource> source, CancellationToken token) => Bind(token, () => source.Max());
        public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector) => BindAsyncWithoutCancellationToken___(() => source.Max(selector));
        public static Task<TResult> MaxAsync<TSource, TResult>(this IQueryable<TSource> source, Expression<Func<TSource, TResult>> selector, CancellationToken token) => Bind(token, () => source.Max(selector));

        public static Task<int> SumAsync(this IQueryable<int> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<int> SumAsync(this IQueryable<int> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<int?> SumAsync(this IQueryable<int?> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<int?> SumAsync(this IQueryable<int?> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<long> SumAsync(this IQueryable<long> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<long> SumAsync(this IQueryable<long> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<long?> SumAsync(this IQueryable<long?> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<long?> SumAsync(this IQueryable<long?> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<float> SumAsync(this IQueryable<float> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<float> SumAsync(this IQueryable<float> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<float?> SumAsync(this IQueryable<float?> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<float?> SumAsync(this IQueryable<float?> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<double> SumAsync(this IQueryable<double> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<double> SumAsync(this IQueryable<double> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<double?> SumAsync(this IQueryable<double?> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<double?> SumAsync(this IQueryable<double?> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<decimal> SumAsync(this IQueryable<decimal> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<decimal> SumAsync(this IQueryable<decimal> source, CancellationToken token) => Bind(token, () => source.Sum());
        public static Task<decimal?> SumAsync(this IQueryable<decimal?> source) => BindAsyncWithoutCancellationToken___(() => source.Sum());
        public static Task<decimal?> SumAsync(this IQueryable<decimal?> source, CancellationToken token) => Bind(token, () => source.Sum());


        public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<int> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<int?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<long> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<long?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<float> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<float?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<double> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<double?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<decimal> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));
        public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Sum(selector));
        public static Task<decimal?> SumAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken token) => Bind(token, () => source.Sum(selector));


        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, int?>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, long?>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<float> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<float?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, float?>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<double> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<double?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, double?>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<decimal> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));
        public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector) => BindAsyncWithoutCancellationToken___(() => source.Average(selector));
        public static Task<decimal?> AverageAsync<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, decimal?>> selector, CancellationToken token) => Bind(token, () => source.Average(selector));

    }
}
