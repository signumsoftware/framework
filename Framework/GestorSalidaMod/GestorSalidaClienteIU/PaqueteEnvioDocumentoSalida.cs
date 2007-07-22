using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorSalida.DN;

namespace Framework.GestorSalida.ClienteIU
{
    public class PaqueteEnvioDocumentoSalida : MotorIU.PaqueteIU
    {
        public List<System.IO.FileInfo> ListaFicheros;
        public CanalSalida CanalSalida = CanalSalida.indefinido;
        public int NumeroCopias = 1;
        public bool Peristente = false;
        public int Prioridad = 5;
        public FuncionImpresora FuncionImpresora;
        public bool MostrarTicket;
        public string Ticket;
    }
}
