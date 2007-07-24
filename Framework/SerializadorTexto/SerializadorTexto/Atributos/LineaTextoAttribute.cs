using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.Atributos
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class LineaTextoAttribute : Attribute
    {
        int _tamanoTotal;


        public LineaTextoAttribute(int tamanoTotal)
        {
            this._tamanoTotal = tamanoTotal; 
        }      

        public int TamanoTotal
        {
            get { return this._tamanoTotal; }
        }
    }
}
