using System;
using System.Collections.Generic;
using System.Text;
using Framework.LogicaNegocios.Transacciones;
using Framework.GestorInformes.ContenedorPlantilla.DN;
using Framework.GestorInformes.ContenedorPlantilla.AD;
using System.Collections;

namespace Framework.GestorInformes.ContenedorPlantilla.LN
{
    public class ContenedorPlantillaLN
    {

        public List<ContenedorPlantillaDN> RecuperarPlantilla(string pNombrePlantilla, TipoPlantilla pTipoPlantilla)
        {
            List<ContenedorPlantillaDN> lista;
            using (Transaccion tr = new Framework.LogicaNegocios.Transacciones.Transaccion())
            {
                ContenedorPlantillaAD ad = new ContenedorPlantillaAD();
                lista = ad.RecuperarPlantilla(pNombrePlantilla, pTipoPlantilla);
                tr.Confirmar();
            }
            return lista;
        }

        public List<ContenedorPlantillaDN> RecuperarPlantilla(string pNombrePlantilla)
        {
            return RecuperarPlantilla(pNombrePlantilla, null);
        }

        public List<TipoPlantilla> RecuperarTipoPlantilla(string nombre)
        {
            List<TipoPlantilla> lista;
            using (Transaccion tr = new Framework.LogicaNegocios.Transacciones.Transaccion())
            {
                lista = new ContenedorPlantillaAD().RecuperarTipoPlantilla(nombre);
                tr.Confirmar();
            }
            return lista;
        }
    }
}

