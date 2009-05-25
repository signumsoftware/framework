using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Signum.Utilities;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum;


namespace Signum.Engine.Coches
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class LinqBasicos
    {
        [TestInitialize()]
        public void SetUp()
        {
            Starter.Start();
        }

        [TestMethod]
        public void Where()
        {
            var query =
                from c in Database.Query<Marca>()
                where c.Nombre == "Peugeot"
                select c;

            var lista = query.ToList();
            Assert.IsTrue(lista.Count > 0);
        }

        [TestMethod]
        public void JoinSelectWheres()
        {

            var query =
                from ma in Database.Query<Marca>()
                join mo in Database.Query<Modelo>() on ma.Id equals mo.Marca.Id
                let ma2 = ma
                let nom = ma.Nombre
                orderby ma.Nombre
                where ma.Nombre.Length < 100
                select new { ma.Id, ma.Nombre, IDModelo = mo.Id } into x
                where x.IDModelo < 10
                select x;

            var lista = query.ToList();

                Assert.IsTrue(lista.Count > 0);
        }

        [TestMethod]
        public void DoubleJoin()
        {
            var query =
                    from ma in Database.Query<Marca>()
                    join mo in Database.Query<Modelo>() on new { A = ma.Id, B = 2 } equals new { A = mo.Marca.Id, B = 2 }
                    select new { Marca = ma.Nombre, Modelo = mo.Nombre, mo.Id};

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
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
        public void SelectManyManual()
        {
            var query = Database.Query<Marca>().SelectMany(ma => Database.Query<Modelo>()
                .Where(mo => mo.Marca == ma), (ma, mo) => new { Marca = ma.Nombre, mo.Nombre, });

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
        }


        [TestMethod]
        public void NestedResults()
        {
            var query = from m in Database.Query<Marca>()
                        select new
                        {
                            m.Nombre,
                            Modelos = Database.Query<Modelo>().Where(mod => mod.Marca == m).ToList()
                        };

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
        }

        [TestMethod]
        public void OutterCount()
        {
            var num = Database.Query<Marca>().Count();

            num = Database.Query<Marca>().Count(c => c.Nombre.Length > 4);

            Assert.IsTrue(num > 0);
        }

        [TestMethod]
        public void OutterMax()
        {
            var num = Database.Query<Coche>().Max(c => c.Id);

            Assert.IsTrue(num > 0);
        }

        [TestMethod]
        public void InnerCount()
        {
            var query = from m in Database.Query<Marca>()
                        let num = Database.Query<Modelo>().Where(mod => mod.Marca == m).Where(mod => mod.Nombre.Length < 5).Count()
                        where num > 1
                        select new { m.Nombre, num };

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
        }


        [TestMethod]
        public void InnerMax()
        {
            var query = from m in Database.Query<Marca>()
                        let num = Database.Query<Modelo>().Where(mod => mod.Marca == m).Select(mod => mod.Nombre.Length).Max(i => (int?)i)
                        select new { m.Nombre, num };

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
        }

        [TestMethod]
        public void GroupByManual()
        {
            var query1 = Database.Query<Modelo>().GroupBy(m => m.Marca);
            var lista1 = query1.ToList();
            Assert.IsTrue(lista1.Count > 0);

            var query2 = Database.Query<Modelo>().GroupBy(m => m.Marca.Id, m=>m.Nombre);
            var lista2 = query2.ToList();
            Assert.IsTrue(lista2.Count > 0);

            var query3 = Database.Query<Modelo>().GroupBy(m => m.Marca, (m, gm) => new { m.Nombre, Count = gm.Count()});
            var lista3 = query3.ToList();
            Assert.IsTrue(lista3.Count > 0);

            var query4 = Database.Query<Modelo>().GroupBy(o => o.Marca, o => o.Nombre, (m, gm) => new { m, Max = gm.Max(a=>a.Length) });
            var lista4 = query4.ToList();
            Assert.IsTrue(lista4.Count > 0);
        }

        [TestMethod]
        public void GroupByBasic()
        {
            var query = from c in Database.Query<Modelo>()
                        group c.Nombre by c.Marca into g
                        select g.Key;

            var lista = query.ToList();
            Assert.IsTrue(lista.Count > 0);
        }

        [TestMethod]
        public void GroupByList()
        {
            var query = from c in Database.Query<Modelo>()
                        group c.Nombre by c.Marca into g
                        select new { g.Key, Lista = g.ToString(", ") };

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
        }

        [TestMethod]
        public void GroupByMezcla()
        {
            var query = from c in Database.Query<Modelo>()
                        group c by c.Marca into g
                        let n = g.Count()
                        let nC = g.Where(p => p.Id % 2 == 0).Count()
                        let max = g.Max(a=>a.Id)
                        let list = g.ToList()
                        select new { g.Key, n, nC, max, list };

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
        }

        [TestMethod]
        public void GroupGroup()
        {
            var query = from c in Database.Query<Coche>()
                        group c by c.Modelo into g
                        group g by g.Key.Marca into g2
                        select g2;

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);
        }


        [TestMethod]
        public void JoinAndGroup()
        {

            var query =
                from c in Database.Query<Coche>()
                join g in
                    (from o in Database.Query<Modelo>() group o by o.Marca) on c.Modelo.Marca equals g.Key
                select new { c.Matricula , Count = g.Count(), g.Key.Nombre };

            var lista = query.ToList();

            Assert.IsTrue(lista.Count > 0);

        }

        [TestMethod]
        public void GroupJoin()
        {
            
            var query =
                from m in Database.Query<Marca>()
                join mod in Database.Query<Modelo>() on m equals mod.Marca into g
                select new { m.Nombre, Num = g.Count() };

            var lista = query.ToList();

            Assert.IsTrue(lista.Count != 0);
        }

        [TestMethod]
        public void SimpleBack()
        {
            Marca m = Database.Query<Marca>().First();
            var lista = m.Back((Modelo mod) => mod.Marca).ToList();

            Assert.IsTrue(lista.Count != 0);
        }

        [TestMethod]
        public void QueryBack()
        {
            var query = from m in Database.Query<Marca>()
                        select new
                        {
                            Marca = m,
                            Modelos = m.Back((Modelo mod) => mod.Marca).ToList()
                        };
            var lista = query.ToList(); 

            Assert.IsTrue(lista.Count != 0);
        }


        [TestMethod]
        public void SimpleBackMany()
        {
            Comentario coment = Database.Retrieve<Comentario>(23);
            var lista = coment.Back((Coche c) => c.Comentarios).ToList();

            Assert.IsTrue(lista.Count != 0);
        }

        [TestMethod]
        public void QueryBackMany()
        {
            Coche coche = Database.Retrieve<Coche>(35);
            coche.Comentarios.Add(Database.Retrieve<Comentario>(12));
            Database.Save(coche);

            var query = from coment in Database.Query<Comentario>()
                        where coment.Back((Coche c) => c.Comentarios).Count() > 1
                        select coment;

            Comentario comentario = query.Single();

            Assert.AreEqual(comentario.Id, 12); 
        }

      

        [TestMethod]
        public void DoubleNestedProjection()
        {
            var query =
               from m in Database.Query<Marca>()
               select new
               {
                   Marca = m,
                   Modelos = m.Back((Modelo mod) => mod.Marca).Select(mod=> 
                             new
                             {
                                 Modelo = mod,
                                 Coches = mod.Back((Coche c)=>c.Modelo).ToList()
                             }).ToList()
               };
            var lista = query.ToList();

            Assert.IsTrue(lista.Count != 0);
        }


        [TestMethod]
        public void AnyAll()
        {
            bool hayPeugeot = Database.Query<Marca>().Any(m => m.Nombre == "Peugeot");

            bool algunaMarca = Database.Query<Marca>().Any();

            bool algunRojo = Database.Query<Coche>().Any(c => c.Color == Color.Rojo);

            bool todos = Database.Query<Coche>().All(c => c.Motor is MotorCombustion || c.Motor is MotorElectrico);

            var lista = Database.Query<Marca>().Where(m => !m.Back((Modelo mod) => mod.Marca).Any()).ToList();
        }

        [TestMethod]
        public void Contains()
        {
            bool hayPeugeot = Database.Query<Marca>().Select(m => m.Nombre).Contains("Peugeot");

            bool contieneMarca = Database.Query<Marca>().Contains(Database.Query<Marca>().First()); 
        }

    
    }
}