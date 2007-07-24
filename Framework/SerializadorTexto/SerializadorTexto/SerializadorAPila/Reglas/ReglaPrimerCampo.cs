using System;
using System.Collections.Generic;
using System.Text;
using SerializadorTexto.SerializadorAPila.LenguajeIncontextual;

namespace SerializadorTexto.SerializadorAPila.Reglas
{
    internal class ReglaPrimerCampo : Regla
    {
        public ReglaPrimerCampo(string cabeza, params string[] cola)
            : base(cabeza, cola)
        {

        }

        public override void ProcesarSimbolo(int i, ref object result, object member)
        {
            if(result == null)
                result = member;
        }

        public override void ProcesarTerminal(int i, ref object result, object member)
        {
            if (result == null)
                result = member;
        }
    }
}
