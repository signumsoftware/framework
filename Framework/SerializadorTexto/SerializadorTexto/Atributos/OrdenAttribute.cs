using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.Atributos
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public class OrdenAttribute : Attribute
    {
        int _orden;

        public OrdenAttribute(int orden)
        {
            this._orden = orden;
        }

        public int Orden
        {
            get { return this._orden; }
        }
    }
}
