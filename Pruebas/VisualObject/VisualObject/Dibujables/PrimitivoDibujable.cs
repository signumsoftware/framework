using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace VisualObject
{
    public class PrimitivoDibujable : TipoDibujable
    {

        public PrimitivoDibujable(Type tipo, string valor) : base(tipo, valor) { }

        internal override int AlturaCampo()
        {
            return reca.alturaTexto; 
        }

        internal override void DibujarCampo(Graphics gr, int x, ref int y, int maxWidth, Variable var)
        {
            this.DibujarVariable(gr, var, "= " + Valor, x, ref y, maxWidth);
        }

        internal override void FuerzaMuelles(TipoRefDibujable padre, int x, ref int y, int maxWidth)
        {
            y += reca.alturaTexto;             
        }
}

   
}
