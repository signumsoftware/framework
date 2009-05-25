using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Signum.Utilities;
using Signum;
using Signum.Entities; 

namespace Signum.Engine.Coches
{
    public class Marca : Entity
    {
        string nombre;
        public string Nombre
        {
            get { return nombre; }
            set { Set(ref nombre, value, "Nombre"); }
        }

        public override string ToString()
        {
            return nombre;
        }
    }

    public class Modelo : Entity
    {
        string nombre;
        public string Nombre
        {
            get { return nombre; }
            set { Set(ref nombre, value, "Nombre"); }
        }

        Marca marca;
        public Marca Marca
        {
            get { return marca; }
            set { Set(ref marca, value, "Marca"); }
        }

        public override string ToString()
        {
            return marca.Nombre + " " + nombre;
        }
    }

    public class Coche : Entity
    {
        Modelo modelo;
        public Modelo Modelo
        {
            get { return modelo; }
            set { Set(ref modelo, value, "Modelo"); }
        }

        Intervalo intervalo;
        public Intervalo Intervalo
        {
            get { return intervalo; }
            set { Set(ref intervalo, value, "Intervalo"); }
        }

        string matricula;
        public string Matricula
        {
            get { return matricula; }
            set { Set(ref matricula, value, "Matricula"); }
        }

        Color color;
        public Color Color
        {
            get { return color; }
            set { Set(ref color, value, "Color"); }
        }

        [ImplementedBy(typeof(MotorCombustion), typeof(MotorElectrico))]
        Motor motor;
        public Motor Motor
        {
            get { return motor; }
            set { Set(ref motor, value, "Motor"); }
        }

        MList<Comentario> comentarios;
        public MList<Comentario> Comentarios
        {
            get { return comentarios; }
            set { Set(ref comentarios, value, "Comentarios"); }
        }

        public override string ToString()
        {
            return "{0} {1} [{2}]".Formato(modelo, color, matricula);
        }
    }

    public class Intervalo : EmbeddedEntity
    {
        DateTime inicio;
        public DateTime Inicio
        {
            get { return inicio; }
            set { Set(ref inicio, value, "Inicio"); }
        }

        DateTime? fin;
        public DateTime? Fin
        {
            get { return fin; }
            set { Set(ref fin, value, "Fin"); }
        }

        public Intervalo() { }
        public Intervalo(DateTime inicio, DateTime? fin) 
        {
            this.inicio = inicio;
            this.fin = fin; 
        }

        public static Intervalo Random()
        {
            DateTime min = MyRandom.Current.NextDateTime(new DateTime(1990, 1, 1), DateTime.Now);

            return new Intervalo(min, MyRandom.Current.NextBool() ? (DateTime?)null : MyRandom.Current.NextDateTime(min, DateTime.Now)); 
        }

        public override string ToString()
        {
            return "{0} - {1}".Formato(inicio, fin);
        }

    }

    public abstract class Motor : Entity
    {
        int potencia;
        public int Potencia
        {
            get { return potencia; }
            set { Set(ref potencia, value, "Potencia"); }
        }
    }

    public class MotorCombustion : Motor
    {
        TipoCombustion tipoCombustion;
        public TipoCombustion TipoCombustion
        {
            get { return tipoCombustion; }
            set { Set(ref tipoCombustion, value, "TipoCombustion"); }
        }

        int numeroCilindros;
        public int NumeroCilindros
        {
            get { return numeroCilindros; }
            set { Set(ref numeroCilindros, value, "NumeroCilindros"); }
        }

        public override string ToString()
        {
            return "Motor Combustión {0} de {1} cilindros ({2} CV)".Formato(tipoCombustion, numeroCilindros, Potencia);
        }
    }

    public class MotorElectrico : Motor
    {
        float corriente;
        public float Corriente
        {
            get { return corriente; }
            set { Set(ref corriente, value, "Corriente"); }
        }

        public override string ToString()
        {
            return "Motor Electrico de {0} amperios ({1} CV)".Formato(corriente, Potencia);
        }
    }

    public enum TipoCombustion
    {
        Gasolina,
        Diesel
    }

    public enum Color
    {
        Rojo, 
        Verde, 
        Azul,
        Amarillo,
    }

    public class Comentario : Entity
    {
        Lazy<Coche> coche;
        public Lazy<Coche> Coche
        {
            get { return coche; }
            set { Set(ref coche, value, "Coche"); }
        }

        string autor;
        public string Autor
        {
            get { return autor; }
            set { Set(ref autor, value, "Autor"); }
        }

        EnumProxy<Valoracion> valoracion;
        public EnumProxy<Valoracion> Valoracion
        {
            get { return valoracion; }
            set { Set(ref valoracion, value, "Valoracion"); }
        }
    }

    public enum Valoracion
    {
        Positiva,
        Negativa,
        Sugerencia
    }

    public class Nota: IdentifiableEntity
    {
        [ImplementedByAll]
        IdentifiableEntity objetivo;
        public IdentifiableEntity Objetivo
        {
            get { return objetivo; }
            set { Set(ref objetivo, value, "Objetivo"); }
        }

        string texto;
        public string Texto
        {
            get { return texto; }
            set { Set(ref texto, value, "Texto"); }
        }

        public override string ToString()
        {
            return texto.Left(10) + "...";
        }
    }
}
