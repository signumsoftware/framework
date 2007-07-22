using System;
using System.Collections.Generic;
using System.Text;

using Framework.DatosNegocio;

namespace Framework.Mensajeria.GestorMails.DN
{
    [Serializable]
    public class CanalMailDN : EntidadTipoDN
    {

        #region Contructores

        public CanalMailDN() : base() { }

        public CanalMailDN(string nombre) : base(nombre) { }

        #endregion

    }
}
