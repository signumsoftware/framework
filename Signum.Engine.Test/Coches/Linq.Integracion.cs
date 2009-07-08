using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum;
using Signum.Entities;
using Signum.Utilities;


namespace Signum.Engine.Coches
{
    [TestClass()]
    public class LinqIntegracion
    {
        [TestInitialize()]
        public void SetUp()
        {
            Starter.Start();
        }

        [TestMethod]
        public void FillData()
        {
            Administrator.Initialize();
            Starter.Fill();
        }

        [TestMethod()]
        public void Query()
        {
            var query = from m in Database.Query<Modelo>()
                        select m.Nombre;
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0); 
        }

        [TestMethod()]
        public void QueryWhere()
        {
            var query = from m in Database.Query<Modelo>()
                        where m.Nombre.Like("M%")
                        select m.Nombre;
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryRecuperar()
        {
            var query = from m in Database.Query<Modelo>()
                        select m;
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
            Assert.IsTrue(list.Select(m => m.Marca).Distinct().Count() < list.Count);
        }

        [TestMethod()]
        public void QueryRecuperarLazy()
        {
            var query = from p in Database.Query<Modelo>()
                        select p.ToLazy();
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryImplicitJoin()
        {
            var query = from p in Database.Query<Modelo>()
                        select new { p.Nombre, Colegio = p.Marca.Nombre };
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryImplicitJoinImplementedBy()
        {
            var query = from c in Database.Query<Coche>()
                        select new { c.Matricula, Motor = c.Motor is MotorElectrico ? 
                            (c.Motor as MotorElectrico).Potencia : 
                            (c.Motor as MotorCombustion).Potencia };
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryImplicitJoinImplementedByAll()
        {
            var query = from c in Database.Query<Nota>()
                        where c.Objetivo is MotorCombustion
                        select new { c.Texto, (c.Objetivo as MotorCombustion).Potencia };
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryPolimorphicLazy()
        {
            var query = from c in Database.Query<Coche>()
                        select new {c.Matricula, Motor = c.Motor.ToLazy()};
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryPolimorphicLazy2()
        {
            var query = from p in Database.Query<Nota>()
                        select new { p.Texto, Objetivo = p.Objetivo.ToLazy() };
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryMListCompleta()
        {
            var query = from p in Database.Query<Coche>()
                        select p.Comentarios;
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryMListSelect()
        {
            var query = from p in Database.Query<Coche>()
                        select p.Comentarios.Select(c => c.Valoracion.ToEnum()).ToList();
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

        [TestMethod()]
        public void QueryMListSelectMany()
        {
            var query = from p in Database.Query<Coche>()
                        from c in p.Comentarios
                        select c.Valoracion.ToEnum();
            var list = query.ToList();
            Assert.IsTrue(list.Count > 0);
        }

    }
}
