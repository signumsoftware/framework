using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.Atributos.Incontextual
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class LineaTextoIncontextualAttribute : LineaTextoAttribute
    {
        private string _id;

        public string ID
        {
            get { return _id; }
            set { _id = value; }
        }

        public LineaTextoIncontextualAttribute(int tamanoTotal)
            : base(tamanoTotal)
        {

        }
    }
}
