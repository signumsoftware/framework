using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities.Reflection;
using Signum.Utilities;
using System.ComponentModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Test.Properties;
using System.Reflection;
using Signum.Entities;
using Signum.Entities.DynamicQuery;
using System.Globalization;
using System.Threading;
using System.Linq.Expressions;
using Signum.Utilities.ExpressionTrees;

namespace Signum.Test
{
    public enum EnumPruebas
    {
        Test,
        [System.ComponentModel.Description("Test!")]
        Test2,
        MyTest, 
        My_Test,
        TEST,
        YouAreFromONU,
    }



    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class NiceToStringTest
    {
        [TestMethod]
        public void EnumToStr()
        {
            Assert.AreEqual(EnumPruebas.Test.NiceToString(), "Test");
            Assert.AreEqual(EnumPruebas.Test2.NiceToString(), "Test!");
            Assert.AreEqual(EnumPruebas.MyTest.NiceToString(), "My test");
            Assert.AreEqual(EnumPruebas.My_Test.NiceToString(), "My Test");
            Assert.AreEqual(EnumPruebas.TEST.NiceToString(), "TEST");
            Assert.AreEqual(EnumPruebas.YouAreFromONU.NiceToString(), "You are from ONU");
        }

        [TestMethod]
        public void ExpressionToStr()
        {
            string str = Expression.Add(Expression.Constant(2), new MyExpression()).NiceToString();
            Assert.IsTrue(str.Contains("$$"));
        }

        class MyExpression: Expression
        {
            public override Type Type { get{ return typeof(int);} }
            public override ExpressionType NodeType { get{ return (ExpressionType)101;} }

            public override string ToString()
            {
                return "$$";
            }
        }
    }
}
