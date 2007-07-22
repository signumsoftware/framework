using System.Collections.Generic;
using System.Threading;

namespace Framework.LogicaNegocios.Transacciones
{
    internal static class EstanteriaHilos
    {
        private static Dictionary<Thread, CajonHilo> recursos = new Dictionary<Thread, CajonHilo>();

        public static CajonHilo Current
        {
            get
            {
                lock (recursos)
                {
                    return recursos[Thread.CurrentThread];
                }
            }

            set
            {
                lock (recursos)
                {
                    recursos[Thread.CurrentThread] = value;
                }
            }
        }


        public static bool Add(CajonHilo recurso)
        {
            lock (recursos)
            {
                Thread t = Thread.CurrentThread;
                if (recursos.ContainsKey(t))
                    return false;

                recursos.Add(Thread.CurrentThread, recurso);

                return true;
            }
        }

        public static bool Remove()
        {
            lock (recursos)
            {
                return recursos.Remove(Thread.CurrentThread);
            }
        }
    }
}