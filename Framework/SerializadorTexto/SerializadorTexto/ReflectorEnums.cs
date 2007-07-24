using System;
using System.Collections.Generic;
using System.Text;
using SerializadorTexto.Atributos;
using System.Reflection;

namespace SerializadorTexto
{
    public static class ReflectorEnums
    {
        #region String - Enum mapping
        static Dictionary<Type, Dictionary<Enum, string>> enumsAString = new Dictionary<Type, Dictionary<Enum, string>>();
        static Dictionary<Type, Dictionary<string, Enum>> stringsAEnum = new Dictionary<Type, Dictionary<string, Enum>>();

        public static Dictionary<string, Enum> DameStringToEnum(Type tipo)
        {
            Dictionary<string, Enum> dic;
            if (!stringsAEnum.TryGetValue(tipo, out dic))
            {
                GeneraDiccionarios(tipo);
                dic = stringsAEnum[tipo];
            }
            return dic;
        }

        public static T DameEnum<T>(string s)
        {
            Type tipo = typeof(T);

            return (T)(object)DameEnum(s, tipo);
        }

        public static Enum DameEnum(string s, Type tipo)
        {
            Dictionary<string, Enum> dic;
            if (!stringsAEnum.TryGetValue(tipo, out dic))
            {
                GeneraDiccionarios(tipo);
                dic = stringsAEnum[tipo];
            }

            if (dic == null)
                throw new InvalidOperationException("El tipo " + tipo.ToString() + " no contiene atributos ValorString");

            Enum en;
            if (!dic.TryGetValue(s, out en))
                throw new InvalidOperationException("No existe ningun atributo ValorString con el valor " + s + " en el tipo " + tipo.ToString());

            return en;
        }

        public static string DameString<T>(T en)
        {
            Type tipo = typeof(T);

            return DameString((Enum)(object)en, tipo);
        }

        public static string DameString(Enum en, Type tipo)
        {
            Dictionary<Enum, string> dic;
            if (!enumsAString.TryGetValue(tipo, out dic))
            {
                GeneraDiccionarios(tipo);
                dic = enumsAString[tipo];
            }

            if (dic == null)
                throw new InvalidOperationException("El tipo " + tipo.ToString() + " no contiene atributos ValorString");

            string s;
            if (!dic.TryGetValue((Enum)(object)en, out s))
                throw new InvalidOperationException("No existe ningún elemento en la enumeración " + tipo.ToString() + " con el valor " + en.ToString() + " y con un atributo ValorString");

            return s;
        }

        public static Dictionary<Enum, string> DameEnumsAString(Type tipo)
        {
            Dictionary<Enum, string> dic;
            if (!enumsAString.TryGetValue(tipo, out dic))
            {
                GeneraDiccionarios(tipo);
                dic = enumsAString[tipo];
            }
            return dic;
        }

        private static void GeneraDiccionarios(Type type)
        {
            if (Reflector.DameAtributoUnico<SerializarComoStringAttribute>(type) != null)
            {
                Dictionary<string, Enum> se = new Dictionary<string, Enum>();
                Dictionary<Enum, string> es = new Dictionary<Enum, string>();

                bool primero = true; 
                foreach (FieldInfo fi in type.GetFields()) // el primer elemento es el único campo real de tipo el tipo subyacente del enum
                {
                    if (primero) { primero = false; continue;  } 
                    ValorStringAttribute vs = Reflector.DameAtributoUnico<ValorStringAttribute>(fi);
                    if (vs == null)
                        throw new InvalidCastException("El campo " + fi.Name + " debe tener el atributo ValorStringAttribute");


                    se.Add(vs.ValorString, (Enum)fi.GetValue(null));
                    if (vs.ValorStringAlternativo != null)
                        se.Add(vs.ValorStringAlternativo, (Enum)fi.GetValue(null));

                    es.Add((Enum)fi.GetValue(null), vs.ValorString);
                }

                stringsAEnum.Add(type, se);
                enumsAString.Add(type, es);
            }
        }
        #endregion

    }
}
