using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using VisualObject.Properties;

namespace VisualObject
{
    public abstract class TipoDibujable
    { 
        internal static Recursos reca = new Recursos(); 
        private Type tipo;
        private string valor;

        public Type Tipo
        {

            get { return tipo; }
            set { tipo = value; }
        }

        public string Valor
        {
            get { return valor; }
            set { valor = value; }
        }
        public TipoDibujable(Type tipo, string valor){
            this.tipo = tipo;
            this.valor = valor;
        }



        internal abstract void DibujarCampo(Graphics gr, int x, ref int y,int maxWidth, Variable var);
        internal abstract int AlturaCampo();
        internal abstract void FuerzaMuelles(TipoRefDibujable padre, int x, ref int y, int maxWidth);


        Bitmap bmpRef = Resources.porreferencia;
        Bitmap bmpVal = Resources.porvalor;

       

        protected void DibujarVariable(Graphics gr,Variable var, string restoTexto, int x, ref int y, int maxWidth)
        {
            Bitmap bmp = var.Tipo.IsValueType ? bmpVal : bmpRef;

            Point p = new Point(x + (reca.alturaTexto - bmp.Width) / 2, y + (reca.alturaTexto - bmp.Height) / 2);
            gr.DrawImageUnscaled(bmp, p);

            Rectangle rect = new Rectangle(x + reca.alturaTexto, y,maxWidth  - reca.alturaTexto, reca.alturaTexto);
            gr.DrawString(var.ToString() + " " + restoTexto, reca.fnt, Brushes.Black, rect, reca.strFrmt);

            y+= reca.alturaTexto; 
        }


    }
}
