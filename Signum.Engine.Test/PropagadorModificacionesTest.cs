using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Signum.Entities;
using Signum.Engine;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Entities.Reflection;

namespace Signum.Engine
{
    /// <summary>
    /// Summary description for UnitTest1
    /// </summary>
    [TestClass]
    public class PropagadorTest
    {
        [TestMethod]
        public void Propagador()
        {
            var darth = new Persona("Darth Vader", 2, 2, 1);

            Assert.IsTrue(darth.Modified);
            Assert.AreEqual(0, darth.Id);

            long ticks = DBMock.Save(darth);

            Assert.IsFalse(darth.Modified);
            Assert.IsNotNull(darth.Id);

            Assert.AreEqual(darth.Ticks, ticks);
            Assert.AreEqual(darth.Cabeza.Ticks, ticks);

            darth.Cabeza.Diametro = 4;
            Assert.IsTrue(darth.Cabeza.Modified);
            ticks = DBMock.Save(darth);

            Assert.AreEqual(darth.Ticks, ticks);
            Assert.AreEqual(darth.Cabeza.Ticks, ticks);

            var luke = new Persona("Luke Skywalker", 2, 2, 1) { Padre = darth };
            var solo = new Persona("Han Solo", 2, 2, 1);
            var chewaka = new Persona("Chewaka", 2, 2, 1) { Amigos = new MList<Persona> { luke, solo } };

            Thread.Sleep(10);

            ticks = DBMock.Save(chewaka);
            Assert.AreEqual(chewaka.Ticks, ticks);
            Assert.AreEqual(solo.Ticks, ticks);
            Assert.AreEqual(luke.Ticks, ticks);
            Assert.AreNotEqual(darth.Ticks, ticks);

            luke.Cabeza.Diametro = 10;
            solo.Cabeza.Diametro = 20;
        }

    }

    public class Persona : Entity
    {
        public Persona() { }

        public Persona(string nombre, double longIzdo, double longDcho, double diametro)
        {
            this.nombre = nombre;
            brazos = new MList<Brazos>
            {
                new Brazos{ Longitud = longIzdo},
                new Brazos{ Longitud = longDcho}
            };

            cabeza = new Cabeza() { Diametro= diametro}; 
        }

        string nombre;
        Persona padre;
        MList<Persona> amigos;
        MList<Brazos> brazos;
        Cabeza cabeza;


        public string Nombre
        {
            get { return nombre; }
            set { Set(ref nombre, value, "Nombre"); }
        }

        public Persona Padre
        {
            get { return padre; }
            set { Set(ref padre, value, "Padre"); }
        }

        public MList<Persona> Amigos
        {
            get { return amigos; }
            set { Set(ref amigos, value, "Amigos"); }
        }

        public MList<Brazos> Brazos
        {
            get { return brazos; }
            set { Set(ref brazos, value, "Brazos"); }
        }

        public Cabeza Cabeza
        {
            get { return cabeza; }
            set { Set(ref cabeza, value, "Cabeza"); }
        }
    }



    public class Brazos : Entity
    {
        double longitud;
        public double Longitud
        {
            get { return longitud; }
            set { Set(ref longitud, value, "Longitud"); }
        } 
    }

    public class Cabeza : Entity
    {
        double diametro;
        public double Diametro
        {
            get { return diametro; }
            set { Set(ref diametro, value, "Diametro"); }
        } 
    }

    public static class DBMock
    {
        static int bla;

        public static long Save(IdentifiableEntity obj)
        {
            long ticks = DateTime.Now.Ticks;
            DirectedGraph<Modifiable> g = GraphExplorer.FromRoot(obj);

            DirectedGraph<Modifiable> gi = g.Inverse();
            GraphExplorer.PropagateModifications(gi);

            foreach (var item in gi)
                if (item.Modified)
                    InsertOrUpdate(item, ticks);

            return ticks;
        }

        static void InsertOrUpdate(Modifiable obj, long ticks)
        {
            obj.Modified = false; // para colecciones y contenidos
            (obj as Entity).TryDoC(e => e.Ticks = ticks);
            (obj as IdentifiableEntity).TryDoC(ei =>
            {
                if (ei.IsNew)
                    ei.Id = bla++;
            });
        }
    }
   
}
