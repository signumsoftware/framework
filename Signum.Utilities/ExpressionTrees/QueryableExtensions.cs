using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Utilities.ExpressionTrees
{
	/// <summary>
	/// Extension methods for IQueryable
	/// </summary>
	public static class QueryableExtensions
	{
		/// <summary>
		/// Returns wrapper that automatically expands expressions in LINQ queries
		/// </summary>
		public static IQueryable<T> ToExpandable<T>(this IQueryable<T> q)
		{
			return new ExpandableWrapper<T>(q);
		}
	}


	/// <summary>
	/// Wrapper for IQueryable that calls Expand
	/// </summary>
	internal class ExpandableWrapper<T> : IQueryable<T>, IQueryProvider
	{
		IQueryable<T> _item;

		public ExpandableWrapper(IQueryable<T> item)
		{
			_item = item;
		}

		public IQueryable CreateQuery(Expression expression)
		{
			return _item.Provider.CreateQuery(expression);
		}

		public object Execute(Expression expression)
		{
			return _item.Provider.Execute(expression.ExpandUntyped());
		}

		public IQueryable<S> CreateQuery<S>(Expression expression)
		{
			Expression res = expression.ExpandUntyped();
			return new ExpandableWrapper<S>(_item.Provider.CreateQuery<S>(res));
		}

		public S Execute<S>(Expression expression)
		{
			return _item.Provider.Execute<S>(expression);
		}

		IEnumerator<T> IEnumerable<T>.GetEnumerator()
		{
			return _item.GetEnumerator();
		}

		public Type ElementType
		{
			get { return _item.ElementType; }
		}

		public Expression Expression
		{
			get { return _item.Expression; }
		}

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return _item.GetEnumerator();
		}

		public IQueryProvider Provider
		{
			get { return this; }
		}
	}
}
