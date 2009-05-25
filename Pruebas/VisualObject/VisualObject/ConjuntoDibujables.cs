using System;
using System.Collections.Generic;
using System.Text;
using PFC.Timers;
using Sharp3D.Math.Core;

namespace VisualObject
{
    public class ConjuntoDibujables
    {

        #region Ktes Staticas
        static float ktePixelsPorUnidad = 50f;
        static float kteViento = 0f;
        static float kteElectromagnetica = 0f;
        static float kteRozamiento = 0f;
        static float kteGravedad = 0f;
        static float kteMuelles = 0f;
        static float kteDistMuelles = 0f;

        public static float KtePixelsPorUnidad
        {
            get { return ConjuntoDibujables.ktePixelsPorUnidad; }
            set { ConjuntoDibujables.ktePixelsPorUnidad = value; }
        }  
        public static float KteViento
        {
            get { return ConjuntoDibujables.kteViento; }
            set { ConjuntoDibujables.kteViento = value; }
        }
        public static float KteElectromagnetica
        {
            get { return ConjuntoDibujables.kteElectromagnetica; }
            set { ConjuntoDibujables.kteElectromagnetica = value; }
        }

        public static float KteRozamiento
        {
            get { return ConjuntoDibujables.kteRozamiento; }
            set { ConjuntoDibujables.kteRozamiento = value; }
        }

        public static float KteGravedad
        {
            get { return ConjuntoDibujables.kteGravedad; }
            set { ConjuntoDibujables.kteGravedad = value; }
        }

        public static float KteMuelles
        {
            get { return ConjuntoDibujables.kteMuelles; }
            set { ConjuntoDibujables.kteMuelles = value; }
        }

        public static float KteDistMuelles
        {
            get { return ConjuntoDibujables.kteDistMuelles; }
            set { ConjuntoDibujables.kteDistMuelles = value; }
        }   

        #endregion
            

        long lastMovement;
        List<TipoRefDibujable> objetos;

        public List<TipoRefDibujable> Objetos
        {
            get { return objetos; }
        }

        public ConjuntoDibujables(int capacidad)
        {
            objetos = new List<TipoRefDibujable>(capacidad);
            lastMovement = FastTimer.Now;
        }

        long currentSecond;
        int currentFPS;
        int lastFPS;


        public int FPS
        {
            get { return lastFPS; }
            set { lastFPS = value; }
        } 


        public void MoverTodas()
        {                  
            long act = FastTimer.Now;
            float timeSpan = (float)(FastTimer.TickToMiliseconds(act - lastMovement) / 1000.0);
            if (timeSpan > 1) timeSpan = 0;
            lastMovement = act;

            // cosas de ps
            long reallycurrentSecond = FastTimer.TickToMilisecondsLong(act)/1000;
            if (currentSecond == reallycurrentSecond) currentFPS++;
            else
            {
                lastFPS = currentFPS;
                currentFPS = 0;
                currentSecond = reallycurrentSecond;              
            }

            MoverTodas(timeSpan);
        }

        public void MoverTodas(float timeSpan)
        {
            foreach (TipoRefDibujable objeto in objetos)
            {
                objeto.ResetearFuerzas();
                objeto.Fuerza[(int)TiposFueza.Viento] = new Vector2F(kteViento, 0); 
            }

            for (int i = 0; i < objetos.Count; i++)
            {
                for (int j = i+1; j < objetos.Count; j++)
                {
                    FuerzaElectromagnetica(objetos[i], objetos[j]);                    
                }                
            }

            foreach (TipoRefDibujable objeto in objetos)
            {
                objeto.CalcularFuerzaMuelles(); 
            }

            foreach (TipoRefDibujable objeto in objetos)
            {
                objeto.Mover(timeSpan); 
            }
        }

        public void FuerzaElectromagnetica(TipoRefDibujable uno, TipoRefDibujable dos)
        {
            Vector2F vect = TipoRefDibujable.VectorDist(uno, dos);
            float distCuad = vect.GetLengthSquared() / (ConjuntoDibujables.ktePixelsPorUnidad * ConjuntoDibujables.ktePixelsPorUnidad);
            if (distCuad < 0.2f) distCuad = 0.2f; 
            if (vect.TryNormalize())
            {
                Vector2F fparcial = vect * (1 / distCuad) *ConjuntoDibujables.KteElectromagnetica;
                uno.Fuerza[(int)TiposFueza.Repulsion] -= fparcial;
                dos.Fuerza[(int)TiposFueza.Repulsion] += fparcial;
            }
        }
    }
}
