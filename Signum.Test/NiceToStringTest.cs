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

namespace Signum.Test
{
    [DescriptionOptions(DescriptionOptions.Members)]
    public enum EnumPruebas
    {
        Test,
        [System.ComponentModel.Description("Test!")]
        Test2,
        MyTest, 
        My_Test,
        TEST,
        YouAreFromONU,
        ILoveYou,
        YoYTu,
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
            Assert.AreEqual("Test",         EnumPruebas.Test.NiceToString());
            Assert.AreEqual("Test!",        EnumPruebas.Test2.NiceToString());
            Assert.AreEqual("My test",      EnumPruebas.MyTest.NiceToString());
            Assert.AreEqual("My Test",      EnumPruebas.My_Test.NiceToString());
            Assert.AreEqual("TEST",         EnumPruebas.TEST.NiceToString());
            Assert.AreEqual("You are from ONU", EnumPruebas.YouAreFromONU.NiceToString());
            Assert.AreEqual("I love you", EnumPruebas.ILoveYou.NiceToString());
            Assert.AreEqual("Yo y tu", EnumPruebas.YoYTu.NiceToString());
        }

        [TestMethod]
        public void ExpressionToStr()
        {
            string str = Expression.Add(Expression.Constant(2), new MyExpression()).ToString();
            Assert.IsTrue(str.Contains("$$"));
        }

        class MyExpression: Expression
        {
            public override Type Type { get{ return typeof(int);} }
            public override ExpressionType NodeType { get { return ExpressionType.Extension; } }

            public override string ToString()
            {
                return "$$";
            }
        }


        [TestMethod]
        public void ForNumber()
        {
            var cats = "{0} Cat[s]";

            Assert.AreEqual("0 Cats", cats.ForGenderAndNumber(number: 0).FormatWith(0));
            Assert.AreEqual("1 Cat", cats.ForGenderAndNumber(number: 1).FormatWith(1));
            Assert.AreEqual("2 Cats", cats.ForGenderAndNumber(number: 2).FormatWith(2));

            Assert.AreEqual("0 Cats", cats.ForGenderAndNumber(number: 0).FormatWith(0));
            Assert.AreEqual("1 Cat", cats.ForGenderAndNumber(number: 1).FormatWith(1));
            Assert.AreEqual("2 Cats", cats.ForGenderAndNumber(number: 2).FormatWith(2));
        }

        [TestMethod]
        public void ForGenderAndNumber()
        {
            var manWoman = "{0} [1m:Man|m:Men|1f:Woman|f:Women]";

            Assert.AreEqual("0 Men", manWoman.ForGenderAndNumber(gender: 'm', number: 0).FormatWith(0));
            Assert.AreEqual("1 Man", manWoman.ForGenderAndNumber(gender: 'm', number: 1).FormatWith(1));
            Assert.AreEqual("2 Men", manWoman.ForGenderAndNumber(gender: 'm', number: 2).FormatWith(2));

            Assert.AreEqual("0 Women", manWoman.ForGenderAndNumber(gender: 'f', number: 0).FormatWith(0));
            Assert.AreEqual("1 Woman", manWoman.ForGenderAndNumber(gender: 'f', number: 1).FormatWith(1));
            Assert.AreEqual("2 Women", manWoman.ForGenderAndNumber(gender: 'f', number: 2).FormatWith(2));
        }
    }
}
