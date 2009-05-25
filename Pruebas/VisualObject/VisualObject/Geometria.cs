using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using Sharp3D.Math.Core;

namespace VisualObject
{
    public static class Geometria
    {
        public static Point PuntoMasCercano2(Point orig, Rectangle destino)
        {
            Point dif = new Point(orig.X - destino.Location.X,
                                  orig.Y - destino.Location.Y);

            if (dif.X < 0) dif.X = 0;
            if (dif.X > destino.Width) dif.X = destino.Width;

            if (dif.Y < 0) dif.Y = 0;
            if (dif.Y > destino.Height) dif.Y = destino.Height;

            return new Point(destino.Location.X + dif.X, 
                             destino.Location.Y + dif.Y); 

        }

        public static Point PuntoMasCercano(Point orig, Rectangle destino)
        {
            int hWidth = destino.Width / 2;
            int hHeight = destino.Height / 2;
            

            Vector2F centro = new Vector2F(destino.X + hWidth,
                                           destino.Y + hHeight);

            if (hHeight == 0 || hWidth == 0) return (Point)centro; 

            Vector2F dist = (Vector2F)orig - centro;

           

            float ratioX = Math.Abs((float)dist.X / hWidth); 
            float ratioY = Math.Abs((float)dist.Y / hHeight);

            if (ratioY < 1 && ratioX < 1) return (Point)centro;

            if (ratioX > ratioY)
                dist /= ratioX;
            else
                dist /= ratioY;

            return (Point)(dist + centro); 
        }
    }
}
