using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Utilities;
using Signum.Utilities.ExpressionTrees;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace Signum.Test
{
    [TestClass]
    public class ExtensionsTest
    {
        [TestMethod]
        public void CartesianProduct()
        {
            var result1 = new[] { "ab", "xy", "01" }.CartesianProduct().ToString(a => a.ToString(""), " ");
        }

        [TestMethod]
        public void DelegateOrder()
        {
            Action action = null;
            action += A;
            action += B;

            action();
            Assert.AreEqual(2, a);

            Assert.AreEqual("AB", action.GetInvocationList().ToString(d => d.Method.Name, ""));

            Assert.IsTrue(action.GetInvocationList().Zip(action.GetInvocationList()).All(p => p.Item1 == p.Item2));
        }
        int a = 0;

        public void A() { a++; }
        public void B() { a *= 2; }


        [TestMethod]
        public void CompareExpression()
        {
            Expression<Func<int, int>> f1 = a => a;
            Expression<Func<int, int>> f2 = a => a;
            Expression<Func<int, int>> f3 = b => b;

            Assert.IsTrue(ExpressionComparer.AreEqual(f1, f2, checkParameterNames: true));
            Assert.IsTrue(ExpressionComparer.AreEqual(f1, f2, checkParameterNames: false));
            Assert.IsFalse(ExpressionComparer.AreEqual(f1, f3, checkParameterNames: true));
            Assert.IsTrue(ExpressionComparer.AreEqual(f1, f3, checkParameterNames: false));

            f1.Evaluate(1);
            Assert2.Throws<InvalidOperationException>("cache", () => f2.Evaluate(2));
        }



        [TestMethod]
        public void StdDev()
        {
            Assert.AreEqual(null, new int[] { }.StdDev());
            Assert.AreEqual(null, new int?[] { }.StdDev());
            Assert.AreEqual(null, new int?[] { 1 }.StdDev());
            Assert.AreEqual(0, new int?[] { 1, 1, 1 }.StdDev());
            Assert.AreEqual(0, new int?[] { 1, null, 1 }.StdDev());
            Assert2.AreSimilar(Math.Sqrt(2), new int?[] { 1, null, 3 }.StdDev());

            Assert.AreEqual(null, new long[] { }.StdDev());
            Assert.AreEqual(null, new long?[] { }.StdDev());
            Assert.AreEqual(null, new long?[] { 1 }.StdDev());
            Assert.AreEqual(0, new long?[] { 1, 1, 1 }.StdDev());
            Assert.AreEqual(0, new long?[] { 1, null, 1 }.StdDev());
            Assert2.AreSimilar(Math.Sqrt(2), new long?[] { 1, null, 3 }.StdDev());

            Assert.AreEqual(null, new double[] { }.StdDev());
            Assert.AreEqual(null, new double?[] { }.StdDev());
            Assert.AreEqual(null, new double?[] { 1 }.StdDev());
            Assert.AreEqual(0, new double?[] { 1, 1, 1 }.StdDev());
            Assert.AreEqual(0, new double?[] { 1, null, 1 }.StdDev());
            Assert2.AreSimilar(Math.Sqrt(2), new double?[] { 1, null, 3 }.StdDev());

            Assert.AreEqual(null, new float[] { }.StdDev());
            Assert.AreEqual(null, new float?[] { }.StdDev());
            Assert.AreEqual(null, new float?[] { 1 }.StdDev());
            Assert.AreEqual(0, new float?[] { 1, 1, 1 }.StdDev());
            Assert.AreEqual(0, new float?[] { 1, null, 1 }.StdDev());
            Assert2.AreSimilar(Math.Sqrt(2), new float?[] { 1, null, 3 }.StdDev());

            Assert.AreEqual(null, new decimal[] { }.StdDev());
            Assert.AreEqual(null, new decimal?[] { }.StdDev());
            Assert.AreEqual(null, new decimal?[] { 1 }.StdDev());
            Assert.AreEqual(0, new decimal?[] { 1, 1, 1 }.StdDev());
            Assert.AreEqual(0, new decimal?[] { 1, null, 1 }.StdDev());
            Assert2.AreSimilar(Math.Sqrt(2), (double?)new decimal?[] { 1, null, 3 }.StdDev());
        }

        [TestMethod]
        public void StdDevP()
        {
            Assert.AreEqual(null, new int[] { }.StdDevP());
            Assert.AreEqual(null, new int?[] { }.StdDevP());
            Assert.AreEqual(0, new int?[] { 1 }.StdDevP());
            Assert.AreEqual(0, new int?[] { 1, 1, 1 }.StdDevP());
            Assert.AreEqual(0, new int?[] { 1, null, 1 }.StdDevP());
            Assert.AreEqual(1, new int?[] { 1, null, 3 }.StdDevP());

            Assert.AreEqual(null, new long[] { }.StdDevP());
            Assert.AreEqual(null, new long?[] { }.StdDevP());
            Assert.AreEqual(0, new long?[] { 1 }.StdDevP());
            Assert.AreEqual(0, new long?[] { 1, 1, 1 }.StdDevP());
            Assert.AreEqual(0, new long?[] { 1, null, 1 }.StdDevP());
            Assert.AreEqual(1, new long?[] { 1, null, 3 }.StdDevP());

            Assert.AreEqual(null, new double[] { }.StdDevP());
            Assert.AreEqual(null, new double?[] { }.StdDevP());
            Assert.AreEqual(0, new double?[] { 1 }.StdDevP());
            Assert.AreEqual(0, new double?[] { 1, 1, 1 }.StdDevP());
            Assert.AreEqual(0, new double?[] { 1, null, 1 }.StdDevP());
            Assert.AreEqual(1, new double?[] { 1, null, 3 }.StdDevP());

            Assert.AreEqual(null, new float[] { }.StdDevP());
            Assert.AreEqual(null, new float?[] { }.StdDevP());
            Assert.AreEqual(0, new float?[] { 1 }.StdDevP());
            Assert.AreEqual(0, new float?[] { 1, 1, 1 }.StdDevP());
            Assert.AreEqual(0, new float?[] { 1, null, 1 }.StdDevP());
            Assert.AreEqual(1, new float?[] { 1, null, 3 }.StdDevP());

            Assert.AreEqual(null, new decimal[] { }.StdDevP());
            Assert.AreEqual(null, new decimal?[] { }.StdDevP());
            Assert.AreEqual(0, new decimal?[] { 1 }.StdDevP());
            Assert.AreEqual(0, new decimal?[] { 1, 1, 1 }.StdDevP());
            Assert.AreEqual(0, new decimal?[] { 1, null, 1 }.StdDevP());
            Assert.AreEqual(1, new decimal?[] { 1, null, 3 }.StdDevP());
        }
    }
}
