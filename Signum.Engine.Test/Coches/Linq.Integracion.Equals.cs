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
    public class LinqIntegracionEquals
    {
        [TestInitialize()]
        public void SetUp()
        {
            Starter.Start();
        }

        [TestMethod]
        public void SelectMany()
        {
            var query = from ma in Database.Query<Marca>()
                        from mo in Database.Query<Modelo>()
                        where mo.Marca == ma
                        select new
                        {
                            Marca = ma.Nombre,
                            Modelo = mo.Nombre
                        };

            var lista = query.ToList();
            Assert.IsTrue(lista.Count > 0);
        }


        [TestMethod]
        public void JoinEntity()
        {
            var query = from m in Database.Query<Marca>()
                        join mod in Database.Query<Modelo>() on m equals mod.Marca
                        select new { m.Nombre, Modelo = mod.Nombre };

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
        }

    }
}
