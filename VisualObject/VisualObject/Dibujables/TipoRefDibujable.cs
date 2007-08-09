using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.Drawing.Drawing2D;
using Sharp3D.Math.Core;

namespace VisualObject
{    
    public abstract class TipoRefDibujable : TipoDibujable
    {
        bool procesarMovimiento=true;

        public bool ProcesarMovimiento
        {
            get { return procesarMovimiento; }
            set { procesarMovimiento = value;}
        }
        protected Rectangle rectangulo;

        public Rectangle Rectangulo
        {
            get { return rectangulo; }
        }
        
        public TipoRefDibujable(Type tipo, string valor)
            : base(tipo, valor)
        {
        }

        #region Métodos que Implementa de TipoDibujable

        internal override int AlturaCampo()
        {
            return reca.alturaTexto;
        }

        internal override void FuerzaMuelles(TipoRefDibujable padre, int x, ref int y, int maxWidth)
        {
            Point punto1 = PuntoReferencia(x, y, maxWidth);
            Point punto2 = Geometria.PuntoMasCercano(punto1, this.rectangulo);
            Vector2F vect = (Vector2F)punto2 - (Vector2F)punto1;
            float dist = vect.GetLength();
            float distNorm = dist /= ConjuntoDibujables.KtePixelsPorUnidad*ConjuntoDibujables.KteDistMuelles;
            if (distNorm > 10.0f)
                distNorm = 10.0f; 
            if (distNorm > 1.0f )
            {
                vect /= dist;                
                vect *= (distNorm - 1.0f)*ConjuntoDibujables.KteMuelles; 
                padre.fuerza[(int)TiposFueza.Muelle] += vect;
                this.fuerza[(int)TiposFueza.Muelle] -= vect;
            }
        }

        internal override void DibujarCampo(Graphics gr, int x, ref int y, int maxWidth, Variable var)
        {
            Point orig;
            DibujarVariable(gr, var, "", x, ref y, maxWidth, out orig);
            DibujarFlecha(gr, orig, Geometria.PuntoMasCercano(orig, rectangulo));
        }
        #endregion

        #region Métodos que Delega
        protected abstract int RecalcularAltura();

        public abstract void Dibujar(Graphics gr);

        public abstract void CalcularFuerzaMuelles(); 
        #endregion
                
        public void GenerarSize(Size sizeTotal)
        {
            Random r = VisualObjectEngine.r;
            int width = reca.anchoClaseDefecto;
            int height = RecalcularAltura(); ;
            rectangulo.Width = width;
            rectangulo.Height = height;

            Posicion = new Vector2F(r.Next(width / 2, sizeTotal.Width - rectangulo.Width / 2),
                                    r.Next(height  / 2, sizeTotal.Height - height/2));
        }

        #region Recursos dibujado
        protected void DibujarCuadro(Graphics gr, string titulo)
        {
            gr.FillRectangle(Brushes.White, rectangulo);
            gr.DrawRectangle(reca.penGeneral, rectangulo);

            Rectangle titulos = new Rectangle(rectangulo.Location, new Size(rectangulo.Width, reca.margenSuperior));

            Brush b = new LinearGradientBrush(titulos, reca.deg1, reca.deg2, 0.0f);
            gr.FillRectangle(b, titulos);
            gr.DrawString(titulo, reca.fntTitulos, Brushes.Black, titulos, reca.strFrmt);

            gr.DrawRectangle(reca.penGeneral, titulos);
        }

        protected void DibujarFlecha(Graphics gr, Point origen, Point destino)
        {
            gr.DrawLine(reca.penGeneral, origen, destino);
            Vector2F dest = (Vector2F)destino;
            Vector2F orig = (Vector2F)origen;


            Vector2F x = (Vector2F)dest - (Vector2F)orig;
            if (x.TryNormalize())
            {
                Vector2F y = x.Perp();

                Vector2F ala1 = (Vector2F)dest + reca.alasFlechaAbajo.X * x + reca.alasFlechaAbajo.Y * y;
                Vector2F ala2 = (Vector2F)dest + reca.alasFlechaArriba.X * x + reca.alasFlechaArriba.Y * y;

                gr.DrawLine(reca.penGeneral, destino, (Point)ala1);
                gr.DrawLine(reca.penGeneral, destino, (Point)ala2);
            }
        }

        protected void DibujarVariable(Graphics gr, Variable var, string restoTexto, int x, ref int y, int maxWidth, out Point origen)
        {
            origen = PuntoReferencia(x, y, maxWidth);

            int dot = reca.anchoCirculo;
            gr.DrawEllipse(Pens.Black, new Rectangle(origen.X - dot / 2, origen.Y - dot / 2, dot, dot));

            DibujarVariable(gr, var, restoTexto, x, ref  y, maxWidth - reca.alturaTexto);
        }

        private static Point PuntoReferencia(int x, int y, int maxWidth)
        {
            return new Point(x + maxWidth - reca.alturaTexto / 2, y + reca.alturaTexto / 2);
        }

        protected void CoordenadasCuerpo(out int x, out int y, out int maxWidth)
        {
            x = rectangulo.X + reca.margenIzquierdo;
            y = rectangulo.Y + reca.margenSuperior + reca.margenTextoVertical;
            maxWidth = rectangulo.Width - reca.margenDerecho - reca.margenIzquierdo;
        }

        #endregion

        #region fisica
       
        Vector2F[] fuerza = new Vector2F[5];

        public Vector2F[] Fuerza
        {
            get { return fuerza; }
            set { fuerza = value; }
        }


        Vector2F velocidad = Vector2F.Zero;

        public Vector2F Velocidad
        {
            get { return velocidad; }
            set { velocidad = value; }
        }
        Vector2F posicion = Vector2F.Zero;

        public Vector2F Posicion
        {
            get { return posicion; }
            set
            {
                posicion = value;
                rectangulo.X = (int)value.X - rectangulo.Width / 2;
                rectangulo.Y = (int)value.Y - rectangulo.Height / 2;
            }
        }

        public float Masa
        {
            get { return (rectangulo.Height * rectangulo.Width) / 100000.0f; }
        }

        public void ResetearFuerzas()
        {
            fuerza[(int)TiposFueza.Total] = Vector2F.Zero;
            fuerza[(int)TiposFueza.Viento] = Vector2F.Zero;
            fuerza[(int)TiposFueza.Gravedad] = Vector2F.Zero;
            fuerza[(int)TiposFueza.Repulsion] = Vector2F.Zero;
            fuerza[(int)TiposFueza.Muelle] = Vector2F.Zero;
        }

        public void Mover(float timeSpam ){
            if (procesarMovimiento)
            {
                fuerza[(int)TiposFueza.Total] = fuerza[(int)TiposFueza.Viento] +
                                                fuerza[(int)TiposFueza.Gravedad] +
                                                fuerza[(int)TiposFueza.Repulsion] +
                                                fuerza[(int)TiposFueza.Muelle];

                Vector2F aceleracion = fuerza[0] / Masa;
                velocidad += aceleracion * timeSpam;
                velocidad *= 1 - ConjuntoDibujables.KteRozamiento * timeSpam;
                Posicion += velocidad * timeSpam;
            }
        }

        public static Vector2F VectorDist(TipoRefDibujable uno, TipoRefDibujable dos)
        {           
              return dos.posicion - uno.posicion; 
          
        }

        public void PosicionInicio(int altura)
        {
            Posicion =new Vector2F(reca.anchoClaseDefecto/2, altura/2); 
        }    
        #endregion

    }
    public enum TiposFueza { Total = 0, Viento = 1, Gravedad = 2, Repulsion = 3, Muelle = 4 };

}
