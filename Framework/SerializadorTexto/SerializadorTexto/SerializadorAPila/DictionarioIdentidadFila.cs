using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using SerializadorTexto;
using SerializadorTexto.Atributos;
using SerializadorTexto.Atributos.Incontextual;

namespace SerializadorTexto.SerializadorAPila
{
    public class DictionarioIdentidadFila<F>
    {
        public static Dictionary<string, Type> diccionarioIDTipo;
        public static Dictionary<Type, string> diccionarioTipoId;

        public static Type DameTipo(string id)
        {
            Type result; 
            if(!diccionarioIDTipo.TryGetValue(id, out result))
            {
                throw new ApplicationException("No existe ningun tipo asociado para el ID " + id + " en " + typeof(DictionarioIdentidadFila<F>).FullName);  
            }
            return result; 
        }

        public static string DameID(Type type)
        {
            string result;
            if (!diccionarioTipoId.TryGetValue(type, out result))
            {
                throw new ApplicationException("No existe ningun ID asociado para el tipo " + type.FullName + " en " + typeof(DictionarioIdentidadFila<F>).FullName);
            }
            return result;
        }

        static DictionarioIdentidadFila()
        {
            diccionarioIDTipo = new Dictionary<string, Type>();
            diccionarioTipoId = new Dictionary<Type, string>();

            ExplorarTipo(typeof(F));
        }

        private static void ExplorarTipo(Type type)
        {
            LineListInfoCache llic = ReflectorBloques.GetLineListInfoCache(type);

            foreach (LineInfoCache fa in llic.Fields)
            {
                Type ft = fa.FieldInfo.FieldType;
                if (ft.IsGenericType && ft.GetGenericTypeDefinition() == typeof(List<>))
                {
                    ft = ft.GetGenericArguments()[0]; 
                }

                LineaTextoIncontextualAttribute lta = Reflector.DameAtributoUnico<LineaTextoIncontextualAttribute>(ft);
                if (lta == null)
                {
                    ExplorarTipo(ft);
                }
                else
                {
                    string id = lta.ID;
                    diccionarioIDTipo[id] = ft;
                    diccionarioTipoId[ft] = id;
                }
            }
        }

    }
}
