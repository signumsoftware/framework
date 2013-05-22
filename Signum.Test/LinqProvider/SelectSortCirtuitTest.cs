using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Engine;
using Signum.Entities;
using System.Diagnostics;
using System.IO;
using Signum.Engine.Linq;
using Signum.Utilities;
using System.Linq.Expressions;
using System.Data.SqlTypes;
using System.Reflection;
using Signum.Test.Environment;

namespace Signum.Test.LinqProvider
{
    /// <summary>
    /// Summary description for LinqProvider
    /// </summary>
    [TestClass]
    public class SelectSortCircuitTest
    {
        [ClassInitialize()]
        public static void MyClassInitialize(TestContext testContext)
        {
            Starter.StartAndLoad();
        }

        [TestInitialize]
        public void Initialize()
        {
            Connector.CurrentLogger = new DebugTextWriter();
        }      
     

        [TestMethod]
        public void SortCircuitCoallesce()
        {
            var list = Database.Query<AlbumDN>().Where(a => ("Hola" ?? Throw<string>()) == null).Select(a => a.Year).ToList();
        }

        [TestMethod]
        public void SortCircuitCoallesceNullable()
        {
            var list = Database.Query<AlbumDN>().Where(a => (((DateTime?)DateTime.Now) ?? Throw<DateTime>()) == DateTime.Today).Select(a => a.Year).ToList();
        }


        [TestMethod]
        public void SortCircuitConditionalIf()
        {
            var list = Database.Query<AlbumDN>().Where(a => "Hola" == "Hola" ? true : Throw<bool>()).Select(a => a.Year).ToList();
        }

        [TestMethod]
        public void SortCircuitOr()
        {
            var list = Database.Query<AlbumDN>().Where(a => true | Throw<bool>()).Select(a => a.Year).ToList();
        }

        [TestMethod]
        public void SortCircuitOrElse()
        {
            var list = Database.Query<AlbumDN>().Where(a => true || Throw<bool>() ).Select(a => a.Year).ToList();
        }

        [TestMethod]
        public void SortCircuitAnd()
        {
            var list = Database.Query<AlbumDN>().Where(a => false & Throw<bool>()).Select(a => a.Year).ToList();
        }

        [TestMethod]
        public void SortCircuitAndAlso()
        {
            var list = Database.Query<AlbumDN>().Where(a => false && Throw<bool>()).Select(a => a.Year).ToList();
        }

        [TestMethod]
        public void SortEqualsTrue()
        {
            var list = Database.Query<AlbumDN>().Where(a => true == (a.Year == 1900)).Select(a => a.Year).ToList();
        }

        [TestMethod]
        public void SortEqualsFalse()
        {
            var list = Database.Query<AlbumDN>().Where(a => false == (a.Year == 1900)).Select(a => a.Year).ToList();
        }

        public T Throw<T>()
        {
            throw new InvalidOperationException("This {0} should not be evaluated".Formato(typeof(T).Name));
        }
    }
}
