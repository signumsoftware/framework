using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Utilities;

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
        public void Order()
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

    }
}
