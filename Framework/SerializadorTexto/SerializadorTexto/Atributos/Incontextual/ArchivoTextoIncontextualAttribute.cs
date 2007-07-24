using System;
using System.Collections.Generic;
using System.Text;
using SerializadorTexto.Atributos;

namespace SerializadorTexto.Atributos.Incontextual
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class ArchivoTextoIncontextualAttribute : ArchivoTextoAttribute
    {
        Type tipoLinea = null;
        /// <summary>
        /// Utilizado para la deserializacion a pila
        /// </summary>
        public Type TipoLinea
        {
            get { return tipoLinea; }
            set { tipoLinea = value; }
        }

        public ArchivoTextoIncontextualAttribute(int tamano)
            : base(tamano)
        {
        }

    }

}
