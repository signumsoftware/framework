using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.SerializadorAPila.LenguajeIncontextual
{
    internal class TablaPrediccion : Dictionary<ParStrings, Regla>
    {  

    }

    internal struct ParStrings
    {
        string Simbolo;
        string Terminal;

        public ParStrings(string simbolo, string terminal)
        {
            this.Simbolo = simbolo;
            this.Terminal = terminal; 
        }

        public override bool Equals(object obj)
        {
            if (obj is ParStrings)
            {
                ParStrings otro = (ParStrings)obj;
                return otro.Terminal == Terminal && otro.Simbolo == Simbolo;
            }
            return false; 
        }

        public override int GetHashCode()
        {
            return Simbolo.GetHashCode() ^ Terminal.GetHashCode(); 
        }

        public override string ToString()
        {
            return "{" + Simbolo + "," + Terminal + "}";
        }
    }
}
