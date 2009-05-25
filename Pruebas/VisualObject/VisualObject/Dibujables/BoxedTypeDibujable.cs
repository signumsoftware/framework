using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace VisualObject
{
    public class BoxedValueType : TipoRefDibujable
    {
        TipoDibujable elemento;

        public TipoDibujable Elemento
        {
            get { return elemento; }
            set { elemento = value; }
        }

        public BoxedValueType(Type tipo, string valor, TipoDibujable elemento)
            : base(tipo, valor)
        {
            this.elemento = elemento; 
        }

        protected override int RecalcularAltura()
        {
            int height = 0;

            height += reca.margenSuperior;
            height += reca.margenTextoVertical;

            height += elemento.AlturaCampo();
            height += reca.margenTextoVertical;

            height += reca.margenInferior;

            return height;
        }

        public override void Dibujar(Graphics gr)
        {
            DibujarCuadro(gr, "Boxed " + Variable.TypeName(Tipo) + ": " + this.Valor);

            int x, y, maxWidth;
            CoordenadasCuerpo(out x, out y, out maxWidth);

            elemento.DibujarCampo(gr, x, ref y, maxWidth, new Variable(elemento.Tipo, "value"));
        }
        
        public override void CalcularFuerzaMuelles()
        {
            int x, y, maxWidth;
            CoordenadasCuerpo(out x, out y, out maxWidth);

            elemento.FuerzaMuelles(this, x, ref y, maxWidth); 
        }
    }
}
