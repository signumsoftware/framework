using System;
using System.Collections.Generic;
using System.Text;

namespace SerializadorTexto.SerializadorAPila.LenguajeIncontextual
{
    public class Regla
    {
        public readonly string Cabeza;
        public string[] Cola;

        public Regla(string cabeza, params string[] cola)
        {
            this.Cabeza = cabeza;
            this.Cola = cola;
        }

        public override string ToString()
        {
            return Cabeza + "->" + string.Join(" ",Cola);
        }

        public virtual void ComienzoRegla(object param, ref object result)
        {
        }

        public virtual void ProcesarTerminal(int i, ref object result, object member)
        {
        }

        public virtual void ProcesarSimbolo(int i, ref object result, object member)
        {
        }
    }
}
