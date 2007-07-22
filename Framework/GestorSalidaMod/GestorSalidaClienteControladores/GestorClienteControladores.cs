using System;
using System.Collections.Generic;
using System.Text;
using Framework.GestorSalida.DN;
using Framework.GestorSalida.AS;
using System.Collections;
using System.IO;
using Framework.DatosNegocio;
using Framework.Configuracion;

namespace Framework.GestorSalida.Cliente.Controladores
{
    public class ControladorGestorSalida
    {
        private static bool mCacheHabilitada;
        private static string mDirectorioTemporal;
        private static List<FuncionImpresora> mCacheFuncionImpresora;
        private static bool mCargadaConfiguracion = false;


        ////constructor estático para cargar los datos de configuración
        //public ControladorGestorSalida()
        //{
        //    if (!mCargadaConfiguracion)
        //    {
        //        try
        //        {
        //            Dictionary<string, string> config = LectorConfiguracionXML.LeerConfiguracion("GestorSalidaClienteControladores.xml");
        //            mCacheHabilitada = bool.Parse(config["CacheCliente"]);
        //            mDirectorioTemporal = config["DirectorioTemporal"];
        //            mCargadaConfiguracion = true;
        //        }
        //        catch (Exception) { throw; }
        //    }
        //}


        public string EnviarDocumentoSalida(DocumentoSalida pDocumentoSalida)
        {
            Framework.GestorSalida.AS.GestorSalidaAS mias = new Framework.GestorSalida.AS.GestorSalidaAS();
            return mias.EnviarDocumentoSalida(pDocumentoSalida);
        }


        public DocumentoSalida RecuperarDocumentoSalidaPorTicket(string ticket)
        {
            Framework.GestorSalida.AS.GestorSalidaAS mias = new Framework.GestorSalida.AS.GestorSalidaAS();
            return mias.RecuperarDocumentoSalidaPorTicket(ticket);
        }


        public Framework.GestorSalida.DN.EstadoEnvio RecuperarEstadoEnvioPorTicket(string ticket)
        {
            Framework.GestorSalida.AS.GestorSalidaAS mias = new Framework.GestorSalida.AS.GestorSalidaAS();
            return mias.RecuperarEstadoEnvioPorTicket(ticket);
        }


        public List<FuncionImpresora> RecuperarTodasFuncionesImpresora()
        {
            List<FuncionImpresora> lista = new List<FuncionImpresora>();
            if (!mCacheHabilitada || mCacheFuncionImpresora == null)
            {
                Framework.AS.DatosBasicosAS mias = new Framework.AS.DatosBasicosAS();
                foreach (FuncionImpresora fim in mias.RecuperarListaTipos(typeof(FuncionImpresora)))
                {
                    lista.Add(fim);
                }
                if (mCacheHabilitada) { mCacheFuncionImpresora = lista; }
            }
            else
            {
                lista = mCacheFuncionImpresora;
            }
            return lista;
        }


        public byte[] ComprimirArchivos(List<System.IO.FileInfo> listaFicheros)
        {
            if (listaFicheros == null || listaFicheros.Count == 0)
            {
                throw new ApplicationException("No hay ningún fichero en la lista de ficheros");
            }

            List<GestorSalida.Utilidades.Documento> listaDocs = new List<Utilidades.Documento>();
            foreach (FileInfo fi in listaFicheros)
            {
                GestorSalida.Utilidades.Documento doc = new Utilidades.Documento();
                doc.NombreArchivo = fi.Name;
                FileStream fs = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
                doc.DocumentoCargado = new byte[fs.Length];
                fs.Read(doc.DocumentoCargado, 0, doc.DocumentoCargado.Length);
                fs.Close();
                listaDocs.Add(doc);
            }

            return GestorSalida.Utilidades.Zipear(listaDocs, Utilidades.NivelCompresion.Medio);
        }

        public void GuardarFuncionImpresora(FuncionImpresora fi)
        {
            Framework.AS.DatosBasicosAS mias = new Framework.AS.DatosBasicosAS();
            mias.GuardarDNGenerico(fi);
        }

        public void BajaFuncionImpresora(FuncionImpresora fi)
        {
            Framework.AS.DatosBasicosAS mias = new Framework.AS.DatosBasicosAS();
            mias.BajaGenericoDN(fi);
        }

        public void ReactivarFuncionImpresora(FuncionImpresora fi)
        {
            Framework.AS.DatosBasicosAS mias = new Framework.AS.DatosBasicosAS();
            mias.ReactivarGenericoDN(fi);
        }

        public List<CategoriaImpresoras> RecuperarTodasCategorias()
        {
            List<CategoriaImpresoras> respuesta = new List<CategoriaImpresoras>();
            IList lista = new Framework.AS.DatosBasicosAS().RecuperarListaTipos(typeof(CategoriaImpresoras));
            for (int i = 0; i < lista.Count; i++)
            {
                respuesta.Add((CategoriaImpresoras)lista[i]);
            }
            return respuesta;
        }

        public void GuardarCategoriaImpresoras(CategoriaImpresoras categoriaImpresoras)
        {
            new Framework.AS.DatosBasicosAS().GuardarDNGenerico(categoriaImpresoras);
        }

        public void BajaCategoriaImpresoras(CategoriaImpresoras c)
        {
            new Framework.AS.DatosBasicosAS().BajaGenericoDN(c);
        }

        public void ReactivarCategoriaImpresoras(CategoriaImpresoras c)
        { new Framework.AS.DatosBasicosAS().ReactivarGenericoDN(c); }

        public List<ContenedorDescriptorImpresoraDN> RecuperarTodasImpresoras()
        {
            List<ContenedorDescriptorImpresoraDN> lista = new List<ContenedorDescriptorImpresoraDN>();
            IList list = new Framework.AS.DatosBasicosAS().RecuperarListaTipos(typeof(ContenedorDescriptorImpresoraDN));
            for (int i = 0; i < list.Count; i++)
            {
                lista.Add((ContenedorDescriptorImpresoraDN)list[i]);
            }
            return lista;
        }


        public void BajaContenedorImpresora(ContenedorDescriptorImpresoraDN cImp)
        {
            cImp = new AS.GestorSalidaAS().BajaContenedorDescriptorImpresora(cImp);
        }

        public void ReactivarImpresora(ContenedorDescriptorImpresoraDN impresora)
        {
            new Framework.AS.DatosBasicosAS().ReactivarGenericoDN(impresora);
        }

        public ContenedorDescriptorImpresoraDN AltaContenedorDescriptorImpresora(ContenedorDescriptorImpresoraDN impresora)
        { return new GestorSalida.AS.GestorSalidaAS().AltaContenedorDescriptorImpresora(impresora); }
    }
}
