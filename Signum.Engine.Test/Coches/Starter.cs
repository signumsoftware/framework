using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Entities;
using Signum;
using System.IO;
using Signum.Utilities;
using Signum.Utilities.DataStructures;
using Signum.Engine.Maps;

namespace Signum.Engine.Coches
{
    static class Starter
    {
        public static void Start()
        {
            SchemaBuilder sb = new SchemaBuilder();

            TypeLogic.Start(sb, false); 
            sb.Include<Coche>();
            sb.Include<Comentario>();
            sb.Include<Nota>();

            ConnectionScope.Default = new Connection("Data Source=INSPIRON;Initial Catalog=LINQ3Capas;Integrated Security=True", sb.Schema);

            Administrator.Initialize();
 

            Connection.CurrentLog = new StringWriter();
        }

     
        public static void Fill()
        {
            Dictionary<string, HashSet<string>> marcaModelos = new Dictionary<string, HashSet<string>>()
            {
                {"Renault" , new HashSet<string>{ "Mégane", "Clio"}},
                {"Citroën" , new HashSet<string>{ "Xsara", "C3"}},
                {"Ford" , new HashSet<string>{ "Focus"}},
                {"Seat" , new HashSet<string>{ "Ibiza", "Leon"}},
                {"Opel" , new HashSet<string>{ "Astra"}},
                {"Peugeot" , new HashSet<string>{ "206", "306"}},
                {"Dacia" , new HashSet<string>()}
            };

            var marcas = marcaModelos.SelectDictionary(k => k, (k, hs) => new Marca { Nombre = k });

            Database.SaveParams(marcas.Values.ToArray());

            var modelos = marcaModelos.SelectMany(a => a.Value.Select(m=>new Modelo{ Marca = marcas[a.Key], Nombre =m})).ToArray();

            Database.SaveParams(modelos);

            Random r = MyRandom.Current;

            Coche[] coches = 0.To(100).Select(i =>
                new Coche
                {
                    Color = (Color)r.Next(4),
                    Intervalo = Intervalo.Random(),
                    Motor = MyRandom.Current.Next(5) == 0 ?
                        (Motor)new MotorElectrico 
                        {
                            Potencia = r.Next(60, 100),
                            Corriente = (float)r.NextDouble() * 10 
                        }:
                        (Motor)new MotorCombustion
                        {
                            NumeroCilindros = r.Next(2, 5) * 2,
                            Potencia = r.Next(60, 180),
                            TipoCombustion  = r.NextBool()?TipoCombustion.Diesel: TipoCombustion.Gasolina
                        },
                    Matricula = "{0:0000}{1}".Formato(r.Next(10000), r.NextUppercaseString(3)),
                    Modelo = modelos[r.Next(modelos.Length)]
                }.Do(c=>c.Comentarios = new MList<Comentario>(0.To(r.Next(4)).Select(j=>new Comentario
                        {
                            Valoracion = EnumProxy<Valoracion>.FromEnum(r.NextElement(Valoracion.Negativa, Valoracion.Positiva, Valoracion.Sugerencia)),
                            Autor = r.NextElement("Antonio", "Pedro", "Juan", "Aurora", "Sofia"),
                            Coche = c.ToLazy(),
                        })))
                ).ToArray();

            Database.SaveParams(coches);

            Nota[] notas = 0.To(250).Select(i => new Nota
            {
                Objetivo = r.NextElement(coches).Map(c => new Switch<int, IdentifiableEntity>(r.Next(4))
                    .Case(0, c)
                    .Case(1, c.Modelo)
                    .Case(2, c.Modelo.Marca)
                    .Case(3, c.Motor)
                    .NoDefault()),
                Texto = "{0} es {1}".Formato(r.NextElement("El Volante", "El Motor", "El parabrisas", "El starter", "el carburante"),
                  r.NextElement("sólido", "bonito", "endeble", "sensible", "suave"))
            }).ToArray(); 

            Database.SaveParams(notas); 
            
        }

    }
}
