using System;
using System.Collections.Generic;

namespace Framework.LogicaNegocios.Transacciones
{
    /// <summary>
    /// Administra las ITransaccionesLogicas de la pila de transacciones, crear en un using
    /// </summary>
    public class Transaccion : IDisposable
    {
        private bool confirmado = false;

        public Transaccion() : this(false)
        {
        }

        public Transaccion(bool forzarNuevaTransaccion)
        {
            CTDLN ctl = new CTDLN();
            ITransaccionLogicaLN padre;
            Stack<ITransaccionLogicaLN> pila = CajonHiloLN.Current.Transacciones;

            if (forzarNuevaTransaccion || pila.Count == 0)
                padre = null;
            else
                padre = pila.Peek();

            ITransaccionLogicaLN nueva = null;

            ctl.IniciarTransaccion(ref padre, ref nueva);

            pila.Push(nueva);
        }


        /// <summary>
        /// Método para que codigo viejo llame a codigo nuevo
        /// </summary>
        /// <param name="padre"></param>
        public Transaccion(ITransaccionLogicaLN padre)
        {
            CTDLN ctl = new CTDLN();

            ITransaccionLogicaLN nueva = null;

            ctl.IniciarTransaccion(ref padre, ref nueva);

            Stack<ITransaccionLogicaLN> pila = CajonHiloLN.Current.Transacciones;

            pila.Push(nueva);
        }

        public void Dispose()
        {
            Stack<ITransaccionLogicaLN> pila = CajonHiloLN.Current.Transacciones;
            if (!confirmado)
            {
                pila.Peek().Cancelar();
            }
            pila.Pop();
        }

        public void Confirmar()
        {
            Stack<ITransaccionLogicaLN> pila = CajonHiloLN.Current.Transacciones;
            pila.Peek().Confirmar();
            confirmado = true;
        }

        /// <summary>
        /// Casi que ni lo llames
        /// </summary>
        public void Cancelar()
        {
            Stack<ITransaccionLogicaLN> pila = CajonHiloLN.Current.Transacciones;
            pila.Peek().Cancelar();
        }

        public static ITransaccionLogicaLN Actual
        {
            get { return CajonHiloLN.Current.Transacciones.Peek(); }
        }
    }
}