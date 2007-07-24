using System;
using System.Collections.Generic;
using System.Text;
using SerializadorTexto.Atributos;
using System.Reflection;

namespace SerializadorTexto
{
    internal static class ReflectorLineas
    {
        #region Cache Linea
        private static Dictionary<Type, FieldListInfoCache> cacheLineaInfo = new Dictionary<Type, FieldListInfoCache>();

        public static FieldListInfoCache GetLineaInfoCache(Type lineaType, ArchivoTextoAttribute ata)
        {
            FieldListInfoCache result;
            
            if (!cacheLineaInfo.TryGetValue(lineaType, out result))
            {
                LineaTextoAttribute cta = Reflector.DameAtributoUnico<LineaTextoAttribute>(lineaType);
                if (cta == null)
                    throw new ArgumentException("El tipo " + lineaType + "no contiene un LineaTextoAttribute");

                result = new FieldListInfoCache();
                result.TamanoLinea = cta.TamanoTotal;


                //dios salve a linq
                result.Fields = new List<FieldInfoCache>();
                foreach (FieldInfo fi in lineaType.GetFields(Reflector.flags))
                {
                    CampoTextoAttribute ca = Reflector.DameAtributoUnico<CampoTextoAttribute>(fi);
                    TipoCampo tipoCampo = GetTipoCampo(fi.FieldType); 
                    bool rellenarConCeros = GetRellenarConCeros(ca.RellenarConCerosNullable, ata.RellenarConCeros, tipoCampo);
                    Alineamiento alineamiento = GetAlinamiento(ca.Align, ata.AlinearDerecha, tipoCampo);
                    result.Fields.Add(new FieldInfoCache(fi, ca.Format, tipoCampo, ca.TamanoCampo, alineamiento, rellenarConCeros, ca.UsarSpecialToStringAndParse, ca.Orden));
                }

                result.Fields.Sort(delegate(FieldInfoCache a, FieldInfoCache b) { return a.Orden.CompareTo(b.Orden); });

                //result.Fields = (from fi in lineaType.GetFields(Reflector.flags)
                //                 let ca = Reflector.DameAtributoUnico<CampoTextoAttribute>(fi)
                //                 where ca != null
                //                 orderby ca.Orden
                //                 let tipoCampo = GetTipoCampo(fi.FieldType)
                //                 let rellenarConCeros = GetRellenarConCeros(ca.RellenarConCerosNullable, ata.RellenarConCeros, tipoCampo)
                //                 let alineamiento = GetAlinamiento(ca.Align, ata.AlinearDerecha, tipoCampo)
                //                 select new FieldInfoCache(fi, fi.FieldType, ca.Format, tipoCampo, ca.TamanoCampo, alineamiento, rellenarConCeros, ca.UsarSpecialToStringAndParse)).ToList();

                cacheLineaInfo.Add(lineaType, result);
            }
            return result;
        }

        static readonly Type[] numeros = new Type[] { typeof(long), typeof(int), typeof(short), typeof(byte), typeof(ulong), typeof(uint), typeof(ushort), typeof(sbyte) };
        static readonly Type[] reales = new Type[] { typeof(decimal), typeof(double), typeof(float) };

        private static TipoCampo GetTipoCampo(Type fieldType)
        {
            
            fieldType = Nullable.GetUnderlyingType(fieldType) ?? fieldType;

            if (fieldType == typeof(string)) return TipoCampo.@string;
            if (fieldType == typeof(bool)) return TipoCampo.@bool;
            if (fieldType == typeof(string)) return TipoCampo.@string; if (fieldType == typeof(DateTime)) return TipoCampo.datetime;
            if (Array.IndexOf(numeros, fieldType) != -1) return TipoCampo.numero;
            if (Array.IndexOf(reales, fieldType) != -1) return TipoCampo.real;
            if (fieldType.IsEnum) return TipoCampo.@enum;
            if (Reflector.DameAtributoUnico<LineaTextoAttribute>(fieldType) != null) return TipoCampo.linea;

            throw new InvalidOperationException("Tipo no soportado " + fieldType.ToString());
        }

        private static bool GetRellenarConCeros(bool? rellenarConCerosCampo, RellenarConCeros rellenarConCeros, TipoCampo tipoCampo)
        {
            if (rellenarConCerosCampo.HasValue) return rellenarConCerosCampo.Value;
            switch (rellenarConCeros)
            {
                case RellenarConCeros.Todos: return true;
                case RellenarConCeros.SoloNumeros: return (tipoCampo == TipoCampo.numero || tipoCampo == TipoCampo.real);
                default: return false;
            }
        }

        private static Alineamiento GetAlinamiento(Alineamiento? alinamientoCampo, AlinearDerecha alinearDerecha, TipoCampo tipoCampo)
        {
            if (alinamientoCampo.HasValue) return alinamientoCampo.Value;
            switch (alinearDerecha)
            {
                case AlinearDerecha.Todos: return Alineamiento.Derecha;
                case AlinearDerecha.SoloNumeros: return (tipoCampo == TipoCampo.numero || tipoCampo == TipoCampo.real) ? Alineamiento.Derecha : Alineamiento.Izquierda;
                default: return Alineamiento.Izquierda;
            }
        }

        #endregion
    }

    internal enum TipoCampo
    {
        @string,
        numero,
        real,
        datetime,
        @enum,
        linea,
        @bool, 
    }

    internal class FieldListInfoCache
    {
        public List<FieldInfoCache> Fields;
        public int TamanoLinea;
    }

    internal class FieldInfoCache
    {
        public readonly FieldInfo FieldInfo;
        public readonly string Format;
        public readonly TipoCampo TipoCampo;
        public readonly int TamanoFijoTotal;
        public readonly Alineamiento Alineamiento;
        public readonly bool RellenarConCeros;
        public readonly bool UsarSpecialToStringAndParse;
        public readonly int Orden; 

        public FieldInfoCache(FieldInfo fieldInfo, string format, TipoCampo tipoCampo, int tamano, Alineamiento alineamiento, bool rellenarConCeros, bool usarSpecialToStringAndParse, int orden)
        {
            this.FieldInfo = fieldInfo;
            this.Format = format;
            this.TipoCampo = tipoCampo;
            this.TamanoFijoTotal = tamano;
            this.Alineamiento = alineamiento;
            this.RellenarConCeros = rellenarConCeros;
            this.UsarSpecialToStringAndParse = usarSpecialToStringAndParse;
            this.Orden = orden;

        }

        public override string ToString()
        {
            return FieldInfo.ToString();
        }
    }

}
