using System;

namespace Framework.LogicaNegocios.Transacciones
{
    /// <summary>
    /// Administra los IRecursos de la pila de recursos, crear en un using
    /// </summary>
    public class Recurso : IDisposable
    {
        public Recurso(IRecursoLN recurso)
        {
            CajonHiloLN.Current.Recursos.Push(recurso);
        }

        public void Dispose()
        {
            CajonHiloLN.Current.Recursos.Pop();
        }

        public static IRecursoLN Actual
        {
            get { return CajonHiloLN.Current.Recursos.Peek(); }
        }
    }
}