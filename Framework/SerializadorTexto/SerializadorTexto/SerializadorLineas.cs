using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using SerializadorTexto.Atributos;

namespace SerializadorTexto
{
    internal class SerializadorLineas
    {

        public static object DeserializarLinea(string s, Type type, ArchivoTextoAttribute ata)
        {
            FieldListInfoCache lineaInfo = ReflectorLineas.GetLineaInfoCache(type, ata);

            if (lineaInfo.TamanoLinea != s.Length)
                throw new ArgumentException("El argumento s deberia ser de " + lineaInfo.TamanoLinea + " y es de " + s.Length);

            object result = Activator.CreateInstance(type);

            int position = 0;
            try
            {
                foreach (FieldInfoCache fa in lineaInfo.Fields)
                {
                    Type fieldType = fa.FieldInfo.FieldType;
                    string subString = s.Substring(position, fa.TamanoFijoTotal);

                    object fieldValue;
                    if (!fa.UsarSpecialToStringAndParse || !((ISpecialToStringAndParse)result).ParseEvent(fa.FieldInfo.Name, subString, out fieldValue))
                    {
                        string unfixed = UnfixText(subString, fa.Alineamiento, fa.RellenarConCeros, fa.TipoCampo);
                        fieldValue = DeserializarCampo(unfixed, fieldType, fa.Format, ata);
                    }

                    fa.FieldInfo.SetValue(result, fieldValue);
                    position += fa.TamanoFijoTotal;
                }
            }
            catch (Exception e)
            {
                if (e.Data["position"] == null)
                    e.Data["position"] = position;
                else
                    e.Data["position"] = (int)e.Data["position"] + position;

                throw;
            }

            return result;
        }

        private static string UnfixText(string fixedText, Alineamiento alineamiento, bool rellenarConCeros, TipoCampo tipoCampo)
        {
            if (tipoCampo == TipoCampo.linea || rellenarConCeros)
                return fixedText;

            if (alineamiento == Alineamiento.Derecha)
                return fixedText.TrimStart(' ');
            else
                return fixedText.TrimEnd(' ');

        }

        private static object DeserializarCampo(string s, Type fieldType, string format, ArchivoTextoAttribute ata)
        {
            if (fieldType.IsClass && Reflector.DameAtributoUnico<LineaTextoAttribute>(fieldType) != null)
            {
                if (s.Trim().Length == 0)
                    return null;
                else
                    return DeserializarLinea(s, fieldType, ata);
            }

            CultureInfo ci = ata.CultureInfo;

            if (fieldType == typeof(string)) return s == "" ? null : s;

            if (fieldType == typeof(DateTime))
            {
                //int val;
                //if (int.TryParse(s, out val) && val == 0)//esto es debido al fichero de fiva haciendo cosas raras
                //    return null;

                if (format == null)
                    return DateTime.Parse(s, ci);
                else
                    return DateTime.ParseExact(s, format, ci);
            }

            if (fieldType == typeof(decimal)) return decimal.Parse(InsertarPunto(s, format, ci), ci);
            if (fieldType == typeof(double)) return double.Parse(InsertarPunto(s, format, ci), ci);
            if (fieldType == typeof(float)) return float.Parse(InsertarPunto(s, format, ci), ci);

            if (fieldType == typeof(long)) return long.Parse(s, ci);
            if (fieldType == typeof(int)) return int.Parse(s, ci);
            if (fieldType == typeof(short)) return short.Parse(s, ci);
            if (fieldType == typeof(byte)) return byte.Parse(s, ci);

            if (fieldType == typeof(ulong)) return ulong.Parse(s, ci);
            if (fieldType == typeof(uint)) return uint.Parse(s, ci);
            if (fieldType == typeof(ushort)) return ushort.Parse(s, ci);
            if (fieldType == typeof(sbyte)) return sbyte.Parse(s, ci);

            if (fieldType == typeof(bool)) return Convert.ToBoolean(int.Parse(s, ci));

            Type baseType = Nullable.GetUnderlyingType(fieldType);
            if (baseType != null)
            {
                if (s.Length == 0) return null;
                else return DeserializarCampo(s, baseType, format, ata);
            }

            if (fieldType.IsEnum) return DeserializarEnum(s, fieldType);

            throw new InvalidOperationException("Tipo no soportado " + fieldType.ToString());
        }

        private static string InsertarPunto(string s, string format, CultureInfo ci)
        {
            int pos;
            if (format == null || (pos = format.IndexOf("!")) == -1)
                return s;
            else
            {
                int lastPos = (format.Length - pos - 1);
                string result = s.Insert(s.Length - lastPos, ci.NumberFormat.CurrencyDecimalSeparator);
                return result;
            }
        }

        private static object DeserializarEnum(string s, Type fieldType)
        {
            if (Reflector.DameAtributoUnico<SerializarComoStringAttribute>(fieldType) == null)
                return Enum.ToObject(fieldType, int.Parse(s));
            else
                return ReflectorEnums.DameEnum(s, fieldType);
        }

        public static string SerializarLinea(object o, Type type, ArchivoTextoAttribute ata)
        {
            FieldListInfoCache lineaInfo = ReflectorLineas.GetLineaInfoCache(type, ata);

            if (o == null)
                return "".PadRight(lineaInfo.TamanoLinea);

            StringBuilder sb = new StringBuilder(lineaInfo.TamanoLinea);

            foreach (FieldInfoCache fa in lineaInfo.Fields)
            {
                object fieldValue = fa.FieldInfo.GetValue(o);

                string fieldText;
                if (!fa.UsarSpecialToStringAndParse || !((ISpecialToStringAndParse)o).ToStringEvent(fa.FieldInfo.Name, fieldValue, out fieldText))
                {
                    string text = SerializarCampo(fieldValue, fa.FieldInfo.FieldType, fa.Format, fa.TipoCampo, ata);
                    fieldText = FixText(text, fa.TamanoFijoTotal, fa.Alineamiento, fa.RellenarConCeros, fa.TipoCampo);
                }

                sb.Append(fieldText);
            }

            sb.Append(' ', lineaInfo.TamanoLinea - sb.Length);

            return sb.ToString();

        }

        private static string SerializarCampo(object o, Type fieldType, string format, TipoCampo tipoCampo, ArchivoTextoAttribute ata)
        {
            if (o == null && tipoCampo != TipoCampo.linea) // serializar una linea siempre requiere devolver un string del tamaño correcto
                return "";

            CultureInfo ci = ata.CultureInfo;

            switch (tipoCampo)
            {
                case TipoCampo.@string: return (string)o;
                case TipoCampo.@bool: return ((bool)o) ? "1" : "0";
                case TipoCampo.numero: return ((IFormattable)o).ToString(format, ci);
                case TipoCampo.real: return ToStringReal((IFormattable)o, format, ci);
                case TipoCampo.datetime: return ((DateTime)o).ToString(format, ci);
                case TipoCampo.@enum: return SerializarEnum(o, fieldType, format);
                case TipoCampo.linea: return SerializarLinea(o, fieldType, ata);
                default:
                    throw new InvalidOperationException("Tipo no soportado " + tipoCampo.ToString());
            }
        }

        private static string ToStringReal(IFormattable o, string format, CultureInfo ci)
        {
            int pos = -1;
            if (format != null && (pos = format.IndexOf('!')) != -1)
            {
                format = format.Replace('!', '.');
                pos = format.Length - pos - 1;
            }

            string result = o.ToString(format, ci);

            return pos != -1 ? result.Remove(result.Length - pos - 1, 1) : result;
        }

        private static string SerializarEnum(object o, Type fieldType, string format)
        {
            if (Reflector.DameAtributoUnico<SerializarComoStringAttribute>(fieldType) == null)
                return Convert.ToInt32(o).ToString(format);
            else
                return ReflectorEnums.DameString((Enum)o, fieldType);
        }

        private static string FixText(string fieldText, int size, Alineamiento alineamiento, bool rellenarConCeros, TipoCampo tipoCampo)
        {
            if (fieldText.Length > size)
                throw new OverflowException("La cadena " + fieldText + " es demasiado larga. Máximo " + size);

            if (tipoCampo == TipoCampo.linea)
                return fieldText;

            char padChar = rellenarConCeros ? '0' : ' ';

            if (alineamiento == Alineamiento.Derecha)
                return fieldText.PadLeft(size, padChar);
            else
                return fieldText.PadRight(size, padChar);
        }

        public static string GetLine(StreamReader s, int size, bool retornoCarro)
        {
            char[] line = new char[size];
            int r = s.Read(line, 0, size);

            if (r == 0)
                return null;

            if (retornoCarro)
            {
                int a = s.Read();
                if (a != -1 && (char)a == '\r') a = s.Read();

                if (a != -1 && (char)a != '\n')
                    throw new ArgumentException("Se esperaba encontrar carácter de nueva linea");
            }

            return new string(line, 0, r);
        }
    }
}
