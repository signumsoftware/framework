using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace SerializadorTexto.Atributos
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public sealed class CampoTextoAttribute : OrdenAttribute
    {
        int tamanoCampo;
        string format;
        Alineamiento? align;
        bool? rellenarConCeros = null;
        bool usarSpecialToStringAndParse = false;       

        public CampoTextoAttribute(int orden, int tamanoCampo)
            : base(orden)
        {
            this.tamanoCampo = tamanoCampo;
        }

        public int TamanoCampo
        {
            get { return this.tamanoCampo; }
        }

        public string Format
        {
            get { return format; }
            set { format = value; }
        }

        public Alineamiento? Align
        {
            get { return align; }
            set { align = value; }
        }

        public bool RellenarConCeros
        {
            set { rellenarConCeros = value; }
            get { return rellenarConCeros.GetValueOrDefault(); }
        }

        public bool? RellenarConCerosNullable
        {
            get { return rellenarConCeros; }
        }

        public bool UsarSpecialToStringAndParse
        {
            get { return usarSpecialToStringAndParse; }
            set { usarSpecialToStringAndParse = value; }
        }
    }

    public enum Alineamiento
    {
        Izquierda,
        Derecha
    }


    public interface ISpecialToStringAndParse
    {
        bool ToStringEvent(string fieldName, object value, out string stringValue);
        bool ParseEvent(string fieldName, string stringValue, out object value);
    }
}
