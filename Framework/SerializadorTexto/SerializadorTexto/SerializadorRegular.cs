using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Collections;
using SerializadorTexto.Atributos;
using System.Globalization;


namespace SerializadorTexto
{
    internal static class SerializadorRegular
    {
        public static object DeserializarArchivo(StreamReader s, Type type)
        {
            ArchivoTextoAttribute ata = Reflector.DameAtributoUnico<ArchivoTextoAttribute>(type);
            if (ata == null)
                throw new ArgumentException("El tipo " + type + " no contiene un ArchivoTextoAttribute");

            bool retornoCarro = ata.RetornoCarro;

            CultureInfo ci = ata.CultureInfo;

            LineListInfoCache llic = ReflectorBloques.GetLineListInfoCache(type);

            object result = Activator.CreateInstance(type);
            int linePos = 0; 
            bool hayLista = false;
            try
            {
                foreach (LineInfoCache fa in llic.Fields)
                {
                    if (hayLista)
                        throw new InvalidOperationException("Solo se admite un campo de tipo List, y ha de ser el último");

                    Type fieldType = fa.FieldInfo.FieldType;
                    if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        hayLista = true;
                        Type subType = fieldType.GetGenericArguments()[0];
                        LineaTextoAttribute attr = Reflector.DameAtributoUnico<LineaTextoAttribute>(subType);
                        if (ata == null)
                            throw new ArgumentException("El tipo " + subType + " no contiene un LineaTextoAttribute");


                        IList list = (IList)Activator.CreateInstance(fieldType);

                        string line;
                        while ((line = SerializadorLineas.GetLine(s, attr.TamanoTotal, retornoCarro)) != null)
                        {
                            object obj = SerializadorLineas.DeserializarLinea(line, subType, ata);
                            list.Add(obj);

                            linePos++;
                        }

                        fa.FieldInfo.SetValue(result, list);
                    }
                    else
                    {
                        LineaTextoAttribute attr = Reflector.DameAtributoUnico<LineaTextoAttribute>(fieldType);

                        string line = SerializadorLineas.GetLine(s, attr.TamanoTotal, retornoCarro);

                        object obj = SerializadorLineas.DeserializarLinea(line, fieldType, ata);

                        fa.FieldInfo.SetValue(result, obj);

                        linePos++;
                    }
                }

            }
            catch (Exception ex)
            {
                ex.Data["linea"] = linePos;
                throw; 
            }

            return result;
        }

        public static void SerializarArchivo(object objArch, Type type, StreamWriter sw)
        {
            ArchivoTextoAttribute ata = Reflector.DameAtributoUnico<ArchivoTextoAttribute>(type);
            if (ata == null)
                throw new ArgumentException("El tipo " + type + " no contiene un ArchivoTextoAttribute");

            bool retornoCarro = ata.RetornoCarro;
            CultureInfo ci = ata.CultureInfo;

            LineListInfoCache llic =  ReflectorBloques.GetLineListInfoCache(type);

            bool hayLista = false;
            foreach (LineInfoCache fa in llic.Fields)
            {
                if (hayLista)
                    throw new InvalidOperationException("Solo se admite un campo de tipo List, y ha de ser el último");

                Type fieldType = fa.FieldInfo.FieldType;
                if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    hayLista = true;
                    Type subType = fieldType.GetGenericArguments()[0];
                    LineaTextoAttribute attr = Reflector.DameAtributoUnico<LineaTextoAttribute>(subType);

                    if (ata == null)
                        throw new ArgumentException("El tipo " + subType + " no contiene un LineaTextoAttribute");

                    IList list = (IList)fa.FieldInfo.GetValue(objArch);

                    foreach (object elem in list)
                    {
                        string s = SerializadorLineas.SerializarLinea(elem, subType, ata);

                        sw.Write(s);
                        if (retornoCarro) sw.WriteLine();
                    }
                }
                else
                {
                    LineaTextoAttribute attr = Reflector.DameAtributoUnico<LineaTextoAttribute>(fieldType);

                    object elem = fa.FieldInfo.GetValue(objArch);

                    string s = SerializadorLineas.SerializarLinea(elem, fieldType, ata);

                    sw.Write(s);
                    if (retornoCarro) sw.WriteLine();
                }
            }
        }
    }
}
