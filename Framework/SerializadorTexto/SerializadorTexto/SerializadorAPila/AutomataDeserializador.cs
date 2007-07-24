using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.IO;
using SerializadorTexto;
using SerializadorTexto.SerializadorAPila.LenguajeIncontextual;
using SerializadorTexto.Atributos.Incontextual;
using System.Globalization;

namespace SerializadorTexto.SerializadorAPila
{
    internal class AutomataDeserializador<F> : Automata
    {
        ArchivoTextoIncontextualAttribute atia;

        StreamReader sr;
        Type baselineType;

        private object lastObject;
        private string lastString;

        public AutomataDeserializador(Gramatica gr, StreamReader sr)
            : base(gr)
        {
            atia = Reflector.DameAtributoUnico<ArchivoTextoIncontextualAttribute>(typeof(F));
            if (atia == null)
                throw new ApplicationException("El tipo " + typeof(F) + " no contiene un atributo ArchivoTextoIncontextual");

            if (atia.TipoLinea == null || atia.TipoLinea.IsAssignableFrom(typeof(IIDProvider)))
                throw new ApplicationException("TipoLinea no está especificado o el valor asignado no implementa IIDProvider");

            baselineType = atia.TipoLinea;

            this.sr = sr;
        }


        protected override void NextToken()
        {
            string line = SerializadorLineas.GetLine(sr, atia.TamanoLinea, atia.RetornoCarro);
            if (line == null)
            {
                lastObject = null;
                lastString = Gramatica.End; 
            }
            else
            {
                IIDProvider idp = (IIDProvider)SerializadorLineas.DeserializarLinea(line, baselineType, atia);
                Type tipoLinea = DictionarioIdentidadFila<F>.DameTipo(idp.ID);
                lastObject = SerializadorLineas.DeserializarLinea(line, tipoLinea, atia);
                lastString = GeneradorNombres.GenerarNombre(tipoLinea.Name, false, false, true);
            }
        }

        protected override string 
            PeekToken(ref object token)
        {
            token = lastObject;
            return lastString; 
        }
    }
}
