using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Collections;
using SerializadorTexto.Atributos;

namespace SerializadorTexto
{
    internal static class Reflector
    {
        internal static readonly BindingFlags flags = BindingFlags.GetProperty | BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        
        #region Atributos
        private static Dictionary<ICustomAttributeProvider, Attribute[]> cacheAttributos = new Dictionary<ICustomAttributeProvider, Attribute[]>();
        // este metodo no es thread safe para ser rapido
        public static T DameAtributoUnico<T>(ICustomAttributeProvider provider) where T : Attribute
        {
            Attribute[] result;

            if (!cacheAttributos.TryGetValue(provider, out result))
            {
                object[] arr = provider.GetCustomAttributes(true);
                result = Array.ConvertAll<object,Attribute>(arr, delegate(object o){return (Attribute)o;});

                cacheAttributos.Add(provider, result);
            }

            foreach (Attribute atr in result)
            {
                T val = atr as T;
                if (val != null) return val;
            }
            return null; 

        } 
        #endregion


       
    }
}
