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
        Prueba_1, 
        MyPrueba2,
        [System.ComponentModel.Description("Prueba!")]
        Prueba3,
        [LocDescription]
        Prueba4,
        [LocDescription(typeof(Resources), "Prueba5")]
        Prueba5
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
            Assert.AreEqual(EnumPruebas.Prueba_1.NiceToString(), "Prueba 1");
            Assert.AreEqual(EnumPruebas.MyPrueba2.NiceToString(), "My Prueba2");
            Assert.AreEqual(EnumPruebas.Prueba3.NiceToString(), "Prueba!");
            Assert.AreEqual(EnumPruebas.Prueba4.NiceToString(), "Test 4");
            Assert.AreEqual(EnumPruebas.Prueba5.NiceToString(), "Custom Test 5");
        }

        [TestMethod]
        public void ExpressionToStr()
        {
            string str = Expression.Add(Expression.Constant(2), new MyExpression()).NiceToString();
            Assert.IsTrue(str.Contains("$$"));
        }

        class MyExpression: Expression
        {
            public MyExpression() : 
                base((ExpressionType)101, typeof(int)) 
            { }

            public override string ToString()
            {
                return "$$";
            }
        }
    }
}
