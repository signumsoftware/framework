using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Data;
using Signum.Entities;
using Signum;
using System;
using Signum.Engine;
using Signum.Engine.Maps;

namespace Signum.Engine.Test.Personas
{   
    /// <summary>
    ///This is a test class for AADTest and is intended
    ///to contain all AADTest Unit Tests
    ///</summary>
    [TestClass()]
    public class CreateTableTest
    {

        [TestInitialize()]
        public void SetUp()
        {
            SchemaBuilder generador = new SchemaBuilder(); 
            
            generador.Include<TypeDN>();
            generador.Include<Persona>();
            generador.Include<PersonaSimple>();

            ConnectionScope.Default = new Connection("Data Source=INSPIRON;Initial Catalog=LINQ3Capas;Integrated Security=True", generador.Schema);


            Administrator.TotalGeneration();
            Administrator.Initialize();
        }

        [TestMethod()]
        public void GuardarPersonaSimple()
        {

            PersonaSimple p = new PersonaSimple()
            {
                 Dob = new DateTime(1983, 11,10),
                 Nombre = "Olmo",
                 EstadoCivil = EstadoCivil.Soltero,
                 Dni = new Dni(){ Codigo = "12345" },
                 Pasaporte = new Pasaporte(){ Dni = new Dni(){ Codigo = "6789087"}}
            };

            Assert.IsTrue(p.IsNew);
            Assert.IsTrue(p.Modified); 

            Database.Save(p);

            Assert.IsFalse(p.IsNew);
            Assert.IsFalse(p.Modified); 

            p.Dob = new DateTime(1985, 10, 12);
            p.Nombre = "Olmita";
            p.Sexo = Sexo.Mujer;
            p.EstadoCivil = EstadoCivil.Divorciado; 

            Assert.IsTrue(p.Modified);

            Database.Save(p);

            Assert.IsFalse(p.Modified);

            PersonaSimple ps = Database.Retrieve<PersonaSimple>(1);
            Assert.AreNotSame(ps, p);
            Assert.AreEqual(ps.Dni.Codigo, p.Dni.Codigo);
            Assert.AreEqual(ps.Dob, p.Dob);
            Assert.AreEqual(ps.Edad, p.Edad);
            Assert.AreEqual(ps.EstadoCivil, p.EstadoCivil);
            Assert.AreEqual(ps.EstadoCivilEspecial, p.EstadoCivilEspecial);
            Assert.AreEqual(ps.Sexo, p.Sexo);
            Assert.AreEqual(ps.Pasaporte.Dni.Codigo, p.Pasaporte.Dni.Codigo); 
        }

        [TestMethod()]
        public void GuardarPersona()
        {
            Persona p = new Persona()
            {
                Dob = new DateTime(1983, 11, 10),
                Nombre = "Olmo",
                Dni = new Dni { Codigo = "1234567" },
                Pasaporte = new Pasaporte { Dni = new Dni { Codigo = "6798765" } },
                EstadoCivil = EstadoCivil.Soltero,
                Agenda = new MList<DateTime> { DateTime.Now },
                Coches = new MList<Coche> { new Coche() { Matricula = "2456GFH" } },
                PasaportesFalsos = new MList<Pasaporte> { new Pasaporte { Dni = new Dni { Codigo = "1234" } } },
                PreferenciasSexuales = new MList<Sexo> { Sexo.Hombre, Sexo.Mujer },
                DnisFalsos = new MList<Dni> { new Dni { Codigo = "123434G" } },
                Amigable = new Perro { Nombre = "Boby" },
                AmigosSegundarios = new MList<IAmigable>
                {
                    new Gato{ Nombre =  "Minino" },
                    new Gato{Nombre = "Dartañan" }
                },
            };

            Assert.IsTrue(p.IsNew);

            Database.Save(p);

            Assert.IsFalse(p.IsNew);

            p.Agenda.Add(DateTime.Now.AddDays(1));
            Assert.IsTrue(p.Agenda.SelfModified);

            Database.Save(p);

            Assert.IsFalse(p.Agenda.SelfModified);

            Persona p2 = Database.Retrieve<Persona>(1); 


        }

        [TestMethod()]
        public void GuardarPersonaCiclo()
        {
            Persona p = new Persona()
            {
                Dob = new DateTime(1983, 11, 10),
                Nombre = "Olmo",
                EstadoCivil = EstadoCivil.Soltero,
                Dni = new Dni { Codigo = "1234567" },
                Pasaporte = new Pasaporte { Dni = new Dni { Codigo = "6798765" } },
                Amigable = new Perro { Nombre = "boby" }
            };

            ((Perro)p.Amigable).Amigable = p; 

            Database.Save(p);

            Persona p2 = Database.Retrieve<Persona>(1);

            Assert.AreNotSame(p, p2);
            Assert.AreNotSame(p.Amigable, p2.Amigable);

            Assert.AreEqual(p.Dob, p2.Dob);
            Assert.AreEqual(p.Nombre, p2.Nombre);
            Assert.AreEqual(p.EstadoCivil, p2.EstadoCivil);
            Assert.AreEqual(p.Dni.Codigo, p2.Dni.Codigo);
            Assert.AreEqual(p.Pasaporte.Dni.Codigo, p2.Pasaporte.Dni.Codigo);
            Assert.AreSame(((Perro)(p2.Amigable)).Amigable, p2); 
        }

        [TestMethod()]
        public void EnumProxy()
        {
            var hp = EnumProxy<Sexo>.FromEnum(Sexo.Hombre);
            hp.PreSaving();
            Assert.AreEqual(hp.Id, (int)Sexo.Hombre);
            Assert.AreEqual(hp.ToStr, Sexo.Hombre.ToString());
            Assert.AreEqual(hp.ToEnum(), Sexo.Hombre);
        }
    }
}
