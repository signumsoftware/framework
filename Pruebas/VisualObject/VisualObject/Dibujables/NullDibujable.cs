using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace VisualObject
{

    public class NullDibujable : TipoDibujable
    {

        public NullDibujable() : base(null, "") { }


        internal override void DibujarCampo(Graphics gr, int x, ref int y, int maxWidth, Variable var)
        {
            DibujarVariableConPunto(gr, var, "= null", x, ref y, maxWidth); 
        }

        protected void DibujarVariableConPunto(Graphics gr, Variable var, string restoTexto, int x, ref int y, int maxWidth)
        {
            Point origen = new Point(x + maxWidth - reca.alturaTexto / 2, y + reca.alturaTexto / 2);

            int dot = reca.anchoCirculo;
            gr.FillEllipse(Brushes.Black, new Rectangle(origen.X - dot / 2, origen.Y - dot / 2, dot, dot));

            DibujarVariable(gr, var, restoTexto, x, ref  y, maxWidth - reca.alturaTexto);
        }


        internal override int AlturaCampo()
        {
            return reca.alturaTexto;
        }

        internal override void FuerzaMuelles(TipoRefDibujable padre, int x, ref int y, int maxWidth)
        {
            y += reca.alturaTexto;
        }
    }

}
