using System.Collections.Generic;

namespace Framework.LogicaNegocios.Transacciones
{
    public class CajonHiloLN : CajonHilo
    {
        public Stack<ITransaccionLogicaLN> Transacciones;
        public Stack<IRecursoLN> Recursos;

        public CajonHiloLN(IRecursoLN recurso)
        {
            Recursos = new Stack<IRecursoLN>(5);
            Transacciones = new Stack<ITransaccionLogicaLN>(5);
            Recursos.Push(recurso);
        }

        public new static CajonHiloLN Current
        {
            get { return (CajonHiloLN) CajonHilo.Current; }
        }
    }
}