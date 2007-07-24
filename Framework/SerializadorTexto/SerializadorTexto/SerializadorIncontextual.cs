using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Globalization;
using System.IO;
using System.Collections;
using SerializadorTexto.Atributos.Incontextual;
using SerializadorTexto.Atributos;
using SerializadorTexto.SerializadorAPila;
using SerializadorTexto.SerializadorAPila.LenguajeIncontextual;

namespace SerializadorTexto
{
    internal static class SerializadorIncontextual
    {

        public static object DeserializarArchivo<T>(StreamReader s)
        {
            Type type = typeof(T);

            ArchivoTextoIncontextualAttribute ata = Reflector.DameAtributoUnico<ArchivoTextoIncontextualAttribute>(type);
            if (ata == null)
                throw new ArgumentException("El tipo " + type + " no contiene un ArchivoTextoIncontextualAttribute");

            bool retornoCarro = ata.RetornoCarro;
            int tam = ata.TamanoLinea;
            CultureInfo ci = ata.CultureInfo;

            Gramatica gr =  GeneradorGramatica.GenerarGramatica(type);
            gr.CalculaTablaPrediccion();

            AutomataDeserializador<T> automata = new AutomataDeserializador<T>(gr, s);

            if (!automata.Procesar())
                throw new ApplicationException("Error en el Automata a Pila: " + automata.ErrorMessage); 

            return automata.Resultado;
        }


        public static void SerializarArchivo(object objArch, Type type, StreamWriter sw)
        {
            ArchivoTextoAttribute ata = Reflector.DameAtributoUnico<ArchivoTextoAttribute>(type);
            if (ata == null)
                throw new ArgumentException("El tipo " + type + " no contiene un ArchivoTextoAttribute");

            bool retornoCarro = ata.RetornoCarro;
            int tam = ata.TamanoLinea;
            CultureInfo ci = ata.CultureInfo;

            SerializarBloque(objArch, type, retornoCarro, tam, ata, sw);
        }

        private static void SerializarBloque(object objArch, Type type, bool retornoCarro, int tam, ArchivoTextoAttribute ata, StreamWriter sw)
        {
            LineListInfoCache llic = ReflectorBloques.GetLineListInfoCache(type);

            foreach (LineInfoCache fa in llic.Fields)
            {
                Type fieldType = fa.FieldInfo.FieldType;

                object value = fa.FieldInfo.GetValue(objArch);

                if (value == null)
                {
                    if (!fa.Optional)
                        throw new NullReferenceException("El campo no opcional " + fa.FieldInfo.Name + " contiene una referencia a Null");
                    else
                        continue;
                }

                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type subType = fieldType.GetGenericArguments()[0];
                    LineaTextoIncontextualAttribute attr = Reflector.DameAtributoUnico<LineaTextoIncontextualAttribute>(subType);

                    IList list = (IList)value;

                    if (attr != null)
                    {
                        foreach (object elem in list)
                        {
                            string s = SerializadorLineas.SerializarLinea(elem, subType, ata);

                            sw.Write(s);
                            if (retornoCarro) sw.WriteLine();
                        }
                    }
                    else
                    {
                        foreach (object elem in list)
                        {
                            SerializarBloque(elem, subType, retornoCarro, tam, ata, sw);
                        }
                    }

                }
                else
                {
                    LineaTextoIncontextualAttribute attr = Reflector.DameAtributoUnico<LineaTextoIncontextualAttribute>(fieldType);
                    if (attr != null)
                    {
                        string s = SerializadorLineas.SerializarLinea(value, fieldType, ata);

                        sw.Write(s);
                        if (retornoCarro) sw.WriteLine();
                    }
                    else
                    {
                        SerializarBloque(value, fieldType, retornoCarro, tam, ata, sw);
                    }
                }

            }

        }
    }

}
