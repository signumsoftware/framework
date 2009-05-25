using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data;
using Signum.Entities;

namespace Signum.Engine.Test.Personas
{
    [ImplementedBy(typeof(Persona), typeof(Perro))]
    public interface IAmigable: IIdentifiable
    {

    }

    public enum Sexo
    {
        Hombre,
        Mujer
    }

    public enum EstadoCivil
    {
        Soltero,
        Casado,
        Divorciado
    }

    public class PersonaSimple : Entity
    {
        [MultipleIndex]
        string nombre;
        public string Nombre
        {
            get { return nombre; }
            set { Set(ref nombre, value, "Nombre"); }
        }

        DateTime dob;
        public DateTime Dob
        {
            get { return dob; }
            set { Set(ref dob, value, "Dob"); }
        }

        [Ignore]
        int edad;
        public int Edad
        {
            get { return edad == 0 ? edad = DateTime.Today.Year - dob.Year : edad; }
        }

        Sexo sexo;
        public Sexo Sexo
        {
            get { return sexo; }
            set { Set(ref sexo, value, "Sexo"); }
        }

        EnumProxy<EstadoCivil> estadoCivil;
        public EstadoCivil EstadoCivil
        {
            get { return estadoCivil.ToEnum(); }
            set { Set(ref estadoCivil, EnumProxy<EstadoCivil>.FromEnum(value), "EstadoCivil"); }
        }

        public string EstadoCivilEspecial
        {
            get { return estadoCivil.ToStr; }
        }

        Dni dni;
        public Dni Dni
        {
            get { return dni; }
            set { Set(ref dni, value, "Dni"); }
        }

        Pasaporte pasaporte;
        public Pasaporte Pasaporte
        {
            get { return pasaporte; }
            set { Set(ref pasaporte, value, "Pasaporte"); }
        }
    }

    public class Persona : PersonaSimple, IAmigable
    {

        MList<Coche> coches;

        public MList<Coche> Coches
        {
            get { return coches; }
            set { Set(ref coches, value, "Coches"); }
        }

        MList<DateTime> agenda;

        public MList<DateTime> Agenda
        {
            get { return agenda; }
            set { Set(ref agenda, value, "Agenda"); }
        }

        MList<Sexo> preferenciasSexuales;

        public MList<Sexo> PreferenciasSexuales
        {
            get { return preferenciasSexuales; }
            set { Set(ref preferenciasSexuales, value, "PreferenciasSexuales"); }
        }

        MList<Dni> dnisFalsos;

        public MList<Dni> DnisFalsos
        {
            get { return dnisFalsos; }
            set { Set(ref dnisFalsos, value, "DnisFalsos"); }
        }

        MList<Pasaporte> pasaportesFalsos;

        public MList<Pasaporte> PasaportesFalsos
        {
            get { return pasaportesFalsos; }
            set { Set(ref pasaportesFalsos, value, "PasaportesFalsos"); }
        }

        [ImplementedBy(typeof(Persona), typeof(Perro), typeof(Gato))]
        IAmigable amigable;
        public IAmigable Amigable
        {
            get { return amigable; }
            set { Set(ref amigable, value, "Amigable"); }
        }

        [ImplementedBy(typeof(Gato))]
        MList<IAmigable> amigosSegundarios;

        public MList<IAmigable> AmigosSegundarios
        {
            get { return amigosSegundarios; }
            set { Set(ref amigosSegundarios, value, "AmigosSegundarios"); }
        }
    }

    public class Pasaporte : EmbeddedEntity
    {
        private Dni dni;

        public Dni Dni
        {
            get { return dni; }
            set { Set(ref dni, value, "Dni"); }
        } 
    }

    public class Dni : EmbeddedEntity
    {
        [UniqueIndex,NotNullable]
        public string codigo;

        public string Codigo
        {
            get { return codigo; }
            set { Set(ref codigo, value, "Codigo"); }
        } 
    }

    public class Coche : Entity, IAmigable
    {
        [UniqueIndex]
        public string matricula;

        public string Matricula
        {
            get { return matricula; }
            set { Set(ref matricula, value, "Matricula"); }
        } 
    }

    public class Perro: Entity, IAmigable
    {
        [SqlDbType(SqlDbType=SqlDbType.VarChar)]
        public string nombre;

        public string Nombre
        {
            get { return nombre; }
            set { Set(ref nombre, value, "Nombre"); }
        } 

        public IAmigable amigable;

        public IAmigable Amigable
        {
            get { return amigable; }
            set { Set(ref amigable, value, "Amigable"); }
        } 
    }

    public class Gato : Entity, IAmigable
    {
        [SqlDbType(SqlDbType = SqlDbType.NChar, Size = 50)]
        public string nombre;

        public string Nombre
        {
            get { return nombre; }
            set { Set(ref nombre, value, "Nombre"); }
        } 
    }

}
