using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace SerializadorTexto.Atributos
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public class ArchivoTextoAttribute : Attribute
    {
        bool retornoCarro = true;
        RellenarConCeros rellenarConCeros = RellenarConCeros.Ninguno;
        AlinearDerecha alinearDerecha = AlinearDerecha.SoloNumeros;
        Codificacion codificacion = Codificacion.Default;
        string culture = null;
        int tamanoLinea;

        public ArchivoTextoAttribute(int tamanoLinea)
        {
            this.tamanoLinea = tamanoLinea;
        }

        public int TamanoLinea
        {
            get { return this.tamanoLinea; }
        }

        public bool RetornoCarro
        {
            get { return retornoCarro; }
            set { retornoCarro = value; }
        }

        public RellenarConCeros RellenarConCeros
        {
            get { return rellenarConCeros; }
            set { rellenarConCeros = value; }
        }

        public AlinearDerecha AlinearDerecha
        {
            get { return alinearDerecha; }
            set { alinearDerecha = value; }
        }

        public string Culture
        {
            get { return culture; }
            set { culture = value; }
        }

        public CultureInfo CultureInfo
        {
            get { return ci ?? GenerateCultureInfo(); }
        }

        CultureInfo ci;
        private CultureInfo GenerateCultureInfo()
        {
            if (ci == null)
                ci = culture == null ? CultureInfo.InvariantCulture : CultureInfo.GetCultureInfo(culture);
            return ci;
        }

        public Codificacion Codificacion
        {
            get { return codificacion; }
            set { codificacion = value; }
        }

        public Encoding Encoding
        {
            get
            {
                return Encoding.GetEncoding((int)codificacion);
            }
        }
    }

    public enum AlinearDerecha
    {
        Todos,
        Ninguno,
        SoloNumeros
    }

    public enum RellenarConCeros
    {
        Todos,
        Ninguno,
        SoloNumeros
    }

    public enum Codificacion
    {
        ASCII = 20127,
        BigEndianUnicode = 1201,
        Default = 0,
        Unicode = 1200,
        UTF32 = 12000,
        UTF7 = 65000,
        UTF8 = 65001,
        OEMMultilingualLatinI = 850,
        OEMMultilingualLatinIAndEuro = 858
    }   
}
