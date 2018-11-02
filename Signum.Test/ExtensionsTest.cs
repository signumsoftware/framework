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
            Action action = null;
            action += A;
            action += B;

            action();
            Assert.Equal(2, a);

            Assert.Equal("AB", action.GetInvocationList().ToString(d => d.Method.Name, ""));

            Assert.True(action.GetInvocationList().Zip(action.GetInvocationList()).All(p => p.Item1 == p.Item2));
        }
        int a = 0;

        public void A() { a++; }
        public void B() { a *= 2; }


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
            Assert.Equal(null, new int[] { }.StdDev());
            Assert.Equal(null, new int?[] { }.StdDev());
            Assert.Equal(null, new int?[] { 1 }.StdDev());
            Assert.Equal(0, new int?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new int?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), new int?[] { 1, null, 3 }.StdDev());

            Assert.Equal(null, new long[] { }.StdDev());
            Assert.Equal(null, new long?[] { }.StdDev());
            Assert.Equal(null, new long?[] { 1 }.StdDev());
            Assert.Equal(0, new long?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new long?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), new long?[] { 1, null, 3 }.StdDev());

            Assert.Equal(null, new double[] { }.StdDev());
            Assert.Equal(null, new double?[] { }.StdDev());
            Assert.Equal(null, new double?[] { 1 }.StdDev());
            Assert.Equal(0, new double?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new double?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), new double?[] { 1, null, 3 }.StdDev());

            Assert.Equal(null, new float[] { }.StdDev());
            Assert.Equal(null, new float?[] { }.StdDev());
            Assert.Equal(null, new float?[] { 1 }.StdDev());
            Assert.Equal(0, new float?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new float?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), new float?[] { 1, null, 3 }.StdDev());

            Assert.Equal(null, new decimal[] { }.StdDev());
            Assert.Equal(null, new decimal?[] { }.StdDev());
            Assert.Equal(null, new decimal?[] { 1 }.StdDev());
            Assert.Equal(0, new decimal?[] { 1, 1, 1 }.StdDev());
            Assert.Equal(0, new decimal?[] { 1, null, 1 }.StdDev());
            AssertSimilar(Math.Sqrt(2), (double?)new decimal?[] { 1, null, 3 }.StdDev());
        }

        public static void AssertSimilar(double? a, double? b, double epsilon = 0.0001) 
        { 
            if (a == null && b == null) 
                return; 
 
            if (b == null || Math.Abs(a.Value - b.Value) > epsilon) 
                Assert.True(false, "Values {0} and {1} are too different".FormatWith(a, b)); 
        } 

        [Fact]
        public void StdDevP()
        {
            Assert.Equal(null, new int[] { }.StdDevP());
            Assert.Equal(null, new int?[] { }.StdDevP());
            Assert.Equal(0, new int?[] { 1 }.StdDevP());
            Assert.Equal(0, new int?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new int?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new int?[] { 1, null, 3 }.StdDevP());

            Assert.Equal(null, new long[] { }.StdDevP());
            Assert.Equal(null, new long?[] { }.StdDevP());
            Assert.Equal(0, new long?[] { 1 }.StdDevP());
            Assert.Equal(0, new long?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new long?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new long?[] { 1, null, 3 }.StdDevP());

            Assert.Equal(null, new double[] { }.StdDevP());
            Assert.Equal(null, new double?[] { }.StdDevP());
            Assert.Equal(0, new double?[] { 1 }.StdDevP());
            Assert.Equal(0, new double?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new double?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new double?[] { 1, null, 3 }.StdDevP());

            Assert.Equal(null, new float[] { }.StdDevP());
            Assert.Equal(null, new float?[] { }.StdDevP());
            Assert.Equal(0, new float?[] { 1 }.StdDevP());
            Assert.Equal(0, new float?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new float?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new float?[] { 1, null, 3 }.StdDevP());

            Assert.Equal(null, new decimal[] { }.StdDevP());
            Assert.Equal(null, new decimal?[] { }.StdDevP());
            Assert.Equal(0, new decimal?[] { 1 }.StdDevP());
            Assert.Equal(0, new decimal?[] { 1, 1, 1 }.StdDevP());
            Assert.Equal(0, new decimal?[] { 1, null, 1 }.StdDevP());
            Assert.Equal(1, new decimal?[] { 1, null, 3 }.StdDevP());
        }
    }
}
