using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Data;
using Signum.Entities;
using Signum;
using System;
using Signum.Engine;
using Signum.Utilities;
using System.Linq;
using System.Data.SqlClient;

namespace Signum.Engine.Test
{   
    /// <summary>
    ///This is a test class for AADTest and is intended
    ///to contain all AADTest Unit Tests
    ///</summary>
    [TestClass()]
    public class SqlPreCommandTest
    {

        [TestMethod()]
        public void SqlPreCommandSplit()
        {
            SqlParameter param = new SqlParameter();

            var query = SqlPreCommand.Combine(Spacing.Triple,
                            SqlPreCommand.Combine(Spacing.Double,
                                SqlPreCommand.Combine(Spacing.Simple,
                                    new SqlPreCommandSimple("A", 0.To(5).Select(i => param).ToList()),
                                    new SqlPreCommandSimple("B", 0.To(3).Select(i => param).ToList())),
                                new SqlPreCommandSimple("C", 0.To(1).Select(i => param).ToList())),
                            SqlPreCommand.Combine(Spacing.Double,
                                new SqlPreCommandSimple("D", 0.To(2).Select(i => param).ToList()),
                                new SqlPreCommandSimple("E", 0.To(6).Select(i => param).ToList())));

            Assert.IsTrue(query.Splits(6).ToList().Map(l => l.Count == 3 && l.Sum(a => a.NumParameters) == 17));
            Assert.IsTrue(query.Splits(7).ToList().Map(l => l.Count == 3 && l.Sum(a => a.NumParameters) == 17));
            Assert.IsTrue(query.Splits(8).ToList().Map(l => l.Count == 3 && l.Sum(a => a.NumParameters) == 17));
            Assert.IsTrue(query.Splits(9).ToList().Map(l => l.Count == 2 && l.Sum(a => a.NumParameters) == 17));
        }

    }
}
