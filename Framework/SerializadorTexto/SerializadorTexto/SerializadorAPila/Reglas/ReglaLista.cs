using System;
using System.Collections.Generic;
using System.Text;
using System.Collections;
using SerializadorTexto.SerializadorAPila.LenguajeIncontextual;

namespace SerializadorTexto.SerializadorAPila.Reglas
{
    internal class ReglaLista : Regla
    {
        Type listType; 
        public ReglaLista(Type listType, string cabeza, params string[] cola):base(cabeza,  cola)
        {
            this.listType = listType; 
        }

        public override void ComienzoRegla(object param, ref object result)
        {
            if (param != null && listType.IsInstanceOfType(param))
                result = param;
            else
                result = Activator.CreateInstance(listType);
        }

        public override void ProcesarSimbolo(int i, ref object result, object member)
        {
            if (!listType.IsInstanceOfType(member))
            {
                ((IList)result).Add(member);
            }
        }

        public override void ProcesarTerminal(int i, ref object result, object member)
        {
            ((IList)result).Add(member); 
        }
    }
}
