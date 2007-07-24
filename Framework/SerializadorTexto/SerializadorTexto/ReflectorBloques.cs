using System;
using System.Collections.Generic;
using System.Text;
using SerializadorTexto.Atributos;
using System.Reflection;
using SerializadorTexto.Atributos.Incontextual;

namespace SerializadorTexto
{
    internal static class ReflectorBloques
    {
        #region Cache Linea
        private static Dictionary<Type, LineListInfoCache> cacheLineaInfo = new Dictionary<Type, LineListInfoCache>();

        public static LineListInfoCache GetLineListInfoCache(Type lineType)
        {
            LineListInfoCache result;

            if (!cacheLineaInfo.TryGetValue(lineType, out result))
            {
                ArchivoTextoAttribute cta = Reflector.DameAtributoUnico<ArchivoTextoAttribute>(lineType);
                if (cta == null)
                    throw new ArgumentException("El tipo " + lineType + " no contiene un ArchivoTextoAttribute");

                result = new LineListInfoCache();


                //Dios salve a linq
                result.Fields = new List<LineInfoCache>();
                foreach (FieldInfo fi in lineType.GetFields(Reflector.flags))
                {
                    OrdenAttribute la = Reflector.DameAtributoUnico<OrdenAttribute>(fi);
                    bool opc = Reflector.DameAtributoUnico<OpcionalAttribute>(fi) != null;
                    result.Fields.Add(new LineInfoCache(fi, opc, la.Orden));
                }

                result.Fields.Sort(delegate(LineInfoCache a, LineInfoCache b) { return a.Orden.CompareTo(b.Orden); });

                //result.Fields = (from fi in lineType.GetFields(Reflector.flags)
                //                 let la = Reflector.DameAtributoUnico<OrdenAttribute>(fi)
                //                 let opc = Reflector.DameAtributoUnico<OpcionalAttribute>(fi) != null
                //                 where la != null
                //                 orderby la.Orden
                //                 select new LineInfoCache(fi, opc)).ToList();

                cacheLineaInfo.Add(lineType, result);
            }
            return result;
        }
        #endregion
    }

    internal class LineListInfoCache
    {
        public List<LineInfoCache> Fields;
    }

    internal class LineInfoCache
    {
        public readonly FieldInfo FieldInfo;
        public readonly bool Optional;
        public readonly int Orden; 
       
        public LineInfoCache(FieldInfo fieldInfo,bool optional, int orden)
        {
            this.FieldInfo = fieldInfo;
            this.Optional = optional;
            this.Orden = orden; 
        }

        public override string ToString()
        {
            return FieldInfo.ToString();
        }
    }

}
