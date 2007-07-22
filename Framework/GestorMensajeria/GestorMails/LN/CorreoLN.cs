using System;
using System.Collections.Generic;
using System.Text;

using Framework.LogicaNegocios;
using Framework.LogicaNegocios.Transacciones;
using Framework.ClaseBaseLN;

using Framework.Mensajeria.GestorMensajeriaDN;
using Framework.Mensajeria.GestorMails.DN;
using Framework.Mensajeria.GestorMails.AD;
using Framework.Mensajeria.GestorMails.Properties;

namespace Framework.Mensajeria.GestorMails.LN
{
    public class CorreoLN : BaseGenericLN
    {
        public CorreoLN(ITransaccionLogicaLN pTL, IRecursoLN pRec) : base(pTL, pRec) { }


        public void Enviar(SobreDN sobre)
        {
            if (sobre.Enviado)
                throw new ApplicationExceptionLN(Resources.NoEnviarYaEnviado);
            if (sobre.Descartado)
                throw new ApplicationExceptionLN(Resources.NoEnviarDescartado);

            DateTime dt = DateTime.Now;
            sobre.FechaEncolado = dt;
            sobre.FechaReintento = dt;
            Guardar(sobre);
        }


        public void Guardar(SobreDN sobre)
        {
            sobre.Cerrar();
            base.Guardar<SobreDN>(sobre);
        }

        public SobreDN Recuperar(string id)
        {
            SobreDN sobre = base.Recuperar<SobreDN>(id);
            sobre.Abrir();
            return sobre;

        }

        public SobreDN RecuperarSiguienteAEnviar()
        {
            List<SobreDN> lst = base.RecuperarListaCondicional<SobreDN>(new ConstructorTodosVistaAD("vwSiguienteCorreo"));
            if (lst.Count >= 1)
            {
                SobreDN result = lst[0];
                result.Abrir();
                return result;
            }
            else
                return null;
        }
    }
}
