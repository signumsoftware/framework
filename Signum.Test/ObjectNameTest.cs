using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using System.Globalization;
using System.Threading;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;
using Signum.Engine.Maps;

namespace Signum.Test
{
  
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class ObjectNameTest
    {
        [TestMethod]
        public void ParseDbo()
        {
            var simple = ObjectName.Parse("MyTable");
            Assert.AreEqual("MyTable", simple.Name);
            Assert.AreEqual("dbo", simple.Schema.ToString());
        }

        [TestMethod]
        public void ParseSchema()
        {
            var simple = ObjectName.Parse("MySchema.MyTable");
            Assert.AreEqual("MyTable", simple.Name);
            Assert.AreEqual("MySchema", simple.Schema.ToString());
        }

        [TestMethod]
        public void ParseNameEscaped()
        {
            var simple = ObjectName.Parse("MySchema.[Select]");
            Assert.AreEqual("Select", simple.Name);
            Assert.AreEqual("MySchema", simple.Schema.ToString());
            Assert.AreEqual("MySchema.[Select]", simple.ToString());
        }

        [TestMethod]
        public void ParseSchemaNameEscaped()
        {
            var simple = ObjectName.Parse("[Select].MyTable");
            Assert.AreEqual("MyTable", simple.Name);
            Assert.AreEqual("Select", simple.Schema.Name);
            Assert.AreEqual("[Select].MyTable", simple.ToString());
        }

        [TestMethod]
        public void ParseServerName()
        {
            var simple = ObjectName.Parse("[FROM].[SELECT].[WHERE].[TOP]");
            Assert.AreEqual("TOP", simple.Name);
            Assert.AreEqual("WHERE", simple.Schema.Name);
            Assert.AreEqual("SELECT", simple.Schema.Database.Name);
            Assert.AreEqual("FROM", simple.Schema.Database.Server.Name);
        }


        [TestMethod]
        public void ParseServerNameSuperComplex()
        {
            var simple = ObjectName.Parse("[FROM].[SELECT].[WHERE].[TOP.DISTINCT]");
            Assert.AreEqual("TOP.DISTINCT", simple.Name);
            Assert.AreEqual("WHERE", simple.Schema.Name);
            Assert.AreEqual("SELECT", simple.Schema.Database.Name);
            Assert.AreEqual("FROM", simple.Schema.Database.Server.Name);
        }
    }
}
