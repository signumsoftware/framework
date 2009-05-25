using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace VisualObject
{
    public class CollectionDibujable : TipoRefDibujable
    {
        private TipoDibujable[] elementos;  

        public TipoDibujable[] Elementos
        {
            get { return elementos; }
            set { elementos = value; }
        }

        private Type elementType;

        public Type ElementType
        {
            get { return elementType; }
            set { elementType = value; }
        }

        public CollectionDibujable(int tam, Type elementType, Type tipo, string valor)
            : base(tipo, valor)
        {
            elementos = new TipoDibujable[tam];
            this.elementType = elementType; 
        }




        public override void Dibujar(Graphics gr)
        {
            DibujarCuadro(gr, Variable.TypeName(Tipo) + ": " + this.Valor);

            int x, y, maxWidth;
            CoordenadasCuerpo(out x, out y, out maxWidth);

            for (int i = 0; i < elementos.Length; i++)
            {
                elementos[i].DibujarCampo(gr, x, ref y, maxWidth, new Variable(elementType, "[" + i + "]"));
                y += reca.margenTextoVertical;
            }
        }

        public override void CalcularFuerzaMuelles()
        {
            int x, y, maxWidth;
            CoordenadasCuerpo(out x, out y, out maxWidth);

            for (int i = 0; i < elementos.Length; i++)
            {
                elementos[i].FuerzaMuelles(this, x, ref y, maxWidth);
                y += reca.margenTextoVertical;
            }
        }

        protected override int RecalcularAltura()
        {
            int height = 0;

            height += reca.margenSuperior;
            height += reca.margenTextoVertical;

            if (elementos.Length != 0)
                height += (elementos[0].AlturaCampo() + reca.margenTextoVertical) * elementos.Length;

            height += reca.margenInferior;

            return height;
        }
    }
}
