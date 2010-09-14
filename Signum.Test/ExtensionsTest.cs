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
    }
}
