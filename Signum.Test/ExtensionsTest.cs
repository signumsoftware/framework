using Xunit;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Test
{
    public class ExtensionsTest
    {
        [Fact]
        public void CartesianProduct()
        {
            var result1 = new[] { "ab", "xy", "01" }.CartesianProduct().ToString(a => a.ToString(""), " ");
        }

        [Fact]
        public void DelegateOrder()
        {
            Action action = null!;
            action += A;
            action += B;

            action();
            Assert.Equal(2, a);

            Assert.Equal("AB", action.GetInvocationList().ToString(d => d.Method.Name, ""));

            Assert.True(action.GetInvocationList().Zip(action.GetInvocationList()).All(p => p.Item1 == p.Item2));
        }
        int a = 0;

        internal void A() { a++; }

        internal void B() { a *= 2; }

        [Fact]
        public void CompareExpression()
        {
            Expression<Func<int, int>> f1 = a => a;
            Expression<Func<int, int>> f2 = a => a;
            Expression<Func<int, int>> f3 = b => b;

            Assert.True(ExpressionComparer.AreEqual(f1, f2, checkParameterNames: true));
            Assert.True(ExpressionComparer.AreEqual(f1, f2, checkParameterNames: false));
            Assert.False(ExpressionComparer.AreEqual(f1, f3, checkParameterNames: true));
            Assert.True(ExpressionComparer.AreEqual(f1, f3, checkParameterNames: false));

            f1.Evaluate(1);

            var e = Assert.Throws<InvalidOperationException>(() => f2.Evaluate(2));
            Assert.Contains("cache", e.Message);
        }



        [Fact]
        public void StdDev()
        {
            Assert.Null(new int[] { }.StdDev());
            Assert.Null(new int?[] { }.StdDev());
            Assert.Null(new int?[] { 1 }.StdDev());
            Assert.Equal(0, new int?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new int?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), new int?[] { 1, null, 3 }.StdDev());

            Assert.Null(new long[] { }.StdDev());
            Assert.Null(new long?[] { }.StdDev());
            Assert.Null(new long?[] { 1 }.StdDev());
            Assert.Equal(0, new long?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new long?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), new long?[] { 1, null, 3 }.StdDev());

            Assert.Null(new double[] { }.StdDev());
            Assert.Null(new double?[] { }.StdDev());
            Assert.Null(new double?[] { 1 }.StdDev());
            Assert.Equal(0, new double?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new double?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), new double?[] { 1, null, 3 }.StdDev());

            Assert.Null(new float[] { }.StdDev());
            Assert.Null(new float?[] { }.StdDev());
            Assert.Null(new float?[] { 1 }.StdDev());
            Assert.Equal(0, new float?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new float?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), new float?[] { 1, null, 3 }.StdDev());

            Assert.Null(new decimal[] { }.StdDev());
            Assert.Null(new decimal?[] { }.StdDev());
            Assert.Null(new decimal?[] { 1 }.StdDev());
            Assert.Equal(0, new decimal?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new decimal?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), (double?)new decimal?[] { 1, null, 3 }.StdDev());
        }

        internal static void AssertSimilar(double? a, double? b, double epsilon = 0.0001)
        {
            if (a == null && b == null)
                return;

            if (b == null || a == null || Math.Abs(a.Value - b.Value) > epsilon)
                Assert.True(false, "Values {0} and {1} are too different".FormatWith(a, b));
        }

        [Fact]
        public void StdDevP()
        {
            Assert.Null(new int[] { }.StdDevP());
            Assert.Null(new int?[] { }.StdDevP());
            Assert.Equal(0, new int?[] { 1 }.StdDevP());
            Assert.Equal(0, new int?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new int?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new int?[] { 1, null, 3 }.StdDevP());

            Assert.Null(new long[] { }.StdDevP());
            Assert.Null(new long?[] { }.StdDevP());
            Assert.Equal(0, new long?[] { 1 }.StdDevP());
            Assert.Equal(0, new long?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new long?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new long?[] { 1, null, 3 }.StdDevP());

            Assert.Null(new double[] { }.StdDevP());
            Assert.Null(new double?[] { }.StdDevP());
            Assert.Equal(0, new double?[] { 1 }.StdDevP());
            Assert.Equal(0, new double?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new double?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new double?[] { 1, null, 3 }.StdDevP());

            Assert.Null(new float[] { }.StdDevP());
            Assert.Null(new float?[] { }.StdDevP());
            Assert.Equal(0, new float?[] { 1 }.StdDevP());
            Assert.Equal(0, new float?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new float?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new float?[] { 1, null, 3 }.StdDevP());

            Assert.Null(new decimal[] { }.StdDevP());
            Assert.Null(new decimal?[] { }.StdDevP());
            Assert.Equal(0, new decimal?[] { 1 }.StdDevP());
            Assert.Equal(0, new decimal?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new decimal?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new decimal?[] { 1, null, 3 }.StdDevP());
        }
    }
}
