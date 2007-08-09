using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace VisualObject
{
    class Recursos:IDisposable
    {
        public int margenSuperior = 18; 
        public int margenInferior = 8;
        public int alturaTexto = 12; 
        public int margenIzquierdo = 5;
        public int margenDerecho = 5;
        public int margenTextoVertical = 4;
        public int anchoClaseDefecto = 150;
        public int anchoCirculo = 4; 
        public Color deg1 = Color.FromArgb(213,221,240);
        public Color deg2 = Color.White;
        public Font fnt = new Font("Microsoft Sans Serif", 8);
        public Font fntTitulos = new Font("Microsoft Sans Serif", 8, FontStyle.Bold);
        public StringFormat strFrmt; 
        public Pen penGeneral = new Pen(Color.FromArgb(113,110,100));
        public Point alasFlechaArriba = new Point(-8, 4);
        public Point alasFlechaAbajo  = new Point(-8,-4);
        


        public Recursos(){

            strFrmt = new StringFormat();
            strFrmt.Trimming = StringTrimming.EllipsisCharacter;
            strFrmt.Alignment = StringAlignment.Near;
            strFrmt.LineAlignment = StringAlignment.Center;
            strFrmt.FormatFlags = StringFormatFlags.NoWrap;
        }





        public void Dispose()
        {
            fnt.Dispose();
            fntTitulos.Dispose();
            strFrmt.Dispose();
            penGeneral.Dispose();
        }
}
}

