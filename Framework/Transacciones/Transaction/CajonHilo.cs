using System;

namespace Framework.LogicaNegocios.Transacciones
{
    public abstract class CajonHilo : IDisposable
    {
        private bool conseguido;

        public CajonHilo()
        {
            conseguido = EstanteriaHilos.Add(this);
        }

        public void Dispose()
        {
            if (conseguido) EstanteriaHilos.Remove();
        }

        public static CajonHilo Current
        {
            get { return EstanteriaHilos.Current; }
        }
    }
}