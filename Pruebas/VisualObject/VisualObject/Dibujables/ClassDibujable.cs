using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace VisualObject
{
    public class ClassDibujable : TipoRefDibujable
    {
        Dictionary<Variable, TipoDibujable> elementos;
        
               
        public Dictionary<Variable, TipoDibujable> Elementos
        {
            get { return elementos; }
        }


        public ClassDibujable(Type tipo, string valor)
            : base(tipo, valor)
        {
            elementos = new Dictionary<Variable, TipoDibujable>();
        }

        protected override int RecalcularAltura()
        {
            int height = 0;

            height += reca.margenSuperior;
            height += reca.margenTextoVertical;

            foreach (Variable nvar in elementos.Keys)
            {
                height += elementos[nvar].AlturaCampo();
                height += reca.margenTextoVertical;
            }

            height += reca.margenInferior;

            return height;
        }




        public override void Dibujar(Graphics gr)
        {
            DibujarCuadro(gr, Variable.TypeName(Tipo) + ": " + this.Valor);

            int x, y, maxWidth;
            CoordenadasCuerpo(out x, out y, out maxWidth);

            foreach (Variable nvar in elementos.Keys)
            {
                elementos[nvar].DibujarCampo(gr, x, ref y, maxWidth, nvar);
                y += reca.margenTextoVertical;
            }

        }

        public override void CalcularFuerzaMuelles()
        {
            int x, y, maxWidth;
            CoordenadasCuerpo(out x, out y, out maxWidth);

            foreach (Variable nvar in elementos.Keys)
            {
                elementos[nvar].FuerzaMuelles(this, x, ref y, maxWidth);
                y += reca.margenTextoVertical;
            }
        }
}

   

}
