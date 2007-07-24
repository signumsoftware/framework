using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.Atributos
{
    [AttributeUsage(AttributeTargets.Field, Inherited = false)]
    public class ValorStringAttribute : Attribute
    {
        string _valorString;
        string _valorStringAlternativo; 

        public ValorStringAttribute(string valorString)
        {
            this._valorString = valorString;
        }

        public ValorStringAttribute(string valorString, string valorStringAlternativo)
        {
            this._valorString = valorString;
            this._valorStringAlternativo = valorStringAlternativo; 
        }

        public string ValorString
        {
            get { return this._valorString; }
        }

        //Odio los ficheros ochenteros de FIVA!
        public string ValorStringAlternativo
        {
            get { return this._valorStringAlternativo; }
        }
    }
}
