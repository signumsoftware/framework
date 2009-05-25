using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace VisualObject
{
    public class StructDibujable : TipoDibujable
    {

        public StructDibujable(Type tipo, string valor)
            : base(tipo, valor)
        {
            elementos = new Dictionary<Variable, TipoDibujable>();
        }

        private Dictionary<Variable, TipoDibujable> elementos;

        public Dictionary<Variable, TipoDibujable> Elementos
        {
            get { return elementos; }
        }

        internal override int AlturaCampo()
        {
            int altura =  reca.alturaTexto + reca.margenTextoVertical;
            altura += AlturaBloque();
            return altura; 

        }

        private int AlturaBloque()
        {
            int altura = reca.margenTextoVertical;
            foreach (Variable var in elementos.Keys)
            {
                altura += elementos[var].AlturaCampo();
                altura += reca.margenTextoVertical;
            }            
            return altura;
        }

        internal override void DibujarCampo(System.Drawing.Graphics gr, int x, ref int y, int maxWidth, Variable var)
        {

            DibujarVariable(gr, var, "= " + this.Valor, x, ref y, maxWidth);
            y += reca.margenTextoVertical;

            Rectangle rect = new Rectangle(x, y, maxWidth, this.AlturaBloque());

            gr.DrawRectangle(reca.penGeneral, rect);

            int nmaxWidth = maxWidth - reca.margenDerecho - reca.margenIzquierdo;
            x += reca.margenIzquierdo;
            y += reca.margenTextoVertical; 
            foreach (Variable nvar in elementos.Keys)
            {
                elementos[nvar].DibujarCampo(gr, x, ref y, nmaxWidth, nvar);
                y += reca.margenTextoVertical;
            }
            x -= reca.margenIzquierdo;

        }

        internal override void FuerzaMuelles(TipoRefDibujable padre, int x, ref int y, int maxWidth)
        {
            y += reca.alturaTexto;
            y += reca.margenTextoVertical;

            int nmaxWidth = maxWidth - reca.margenDerecho - reca.margenIzquierdo;
            x += reca.margenIzquierdo;
            y += reca.margenTextoVertical;
            foreach (Variable nvar in elementos.Keys)
            {
                elementos[nvar].FuerzaMuelles(padre, x, ref y, nmaxWidth);
                y += reca.margenTextoVertical;
            }
            x -= reca.margenIzquierdo;
        }
}
}
