using System;
using System.Collections.Generic;
using System.Text;
using Framework.Colecciones;

namespace Framework.GestorSalida.DN
{
    [Serializable]
    public class ConfiguracionImpresionDocumentoSalidaDN : ConfiguracionDocumentoSalidaBaseDN
    {
        protected FuncionImpresora mFuncionImpresora;

        public FuncionImpresora FuncionImpresora
        {
            get { return mFuncionImpresora; }
            set { this.CambiarValorRef<FuncionImpresora>(value, ref mFuncionImpresora); }
        }
    }


    [Serializable]
    public class FuncionImpresora : Framework.DatosNegocio.EntidadDN
    {
        protected string mDescripcion;

        public string Descripcion
        {
            get { return mDescripcion; }
            set { CambiarValorVal<string>(value, ref mDescripcion); }
        }
    }


    [Serializable]
    public class ContenedorDescriptorImpresoraDN : Framework.DatosNegocio.EntidadDN, IComparable<ContenedorDescriptorImpresoraDN>
    {
        protected string mNombreImpresora;
        protected string mNombreDriver;
        protected bool mImpresoraPorDefecto;
        protected Framework.AgenteImpresion.GestorImpresora.TipoImpresora mTipoImpresora;
        private int mTrabajosEnCola;
        private int mErrores;

        public delegate void TrabajosEnColaChangedEventHandler(ContenedorDescriptorImpresoraDN impresora, int Diferencia);
        public event TrabajosEnColaChangedEventHandler TrabajosEnColaChanged;

        public delegate void ErroresChangedEventhandler(ContenedorDescriptorImpresoraDN impresora);
        public event ErroresChangedEventhandler ErroresChanged;

        /// <summary>
        /// Determina el número de errores consecutivos de la impresora en esta sesión
        /// </summary>
        public int Errores
        {
            get { return this.mErrores; }
            set
            {
                if (value < 0) return;
                mErrores = value;
                if (ErroresChanged != null) ErroresChanged(this);
            }
        }

        /// <summary>
        /// Atención: Este constructor sólo debe ser llamado por el Motor de Acceso a Datos
        /// </summary>
        [Obsolete("Este constructor sólo debe ser llamado por el Motor de Acceso a Datos", true)]
        public ContenedorDescriptorImpresoraDN()
        { }

        public ContenedorDescriptorImpresoraDN(AgenteImpresion.GestorImpresora.DescriptorImpresora DescriptorimpresoraAI)
        {
            RellenarCampos(DescriptorimpresoraAI);
        }

        private void RellenarCampos(AgenteImpresion.GestorImpresora.DescriptorImpresora DescriptorimpresoraAI)
        {
            this.mNombreImpresora = DescriptorimpresoraAI.Nombre;
            this.mNombreDriver = DescriptorimpresoraAI.NombreDriver;
            this.mImpresoraPorDefecto = DescriptorimpresoraAI.ImpresoraPorDefecto;
            this.mTipoImpresora = DescriptorimpresoraAI.TipoImpresora;
        }

        /// <summary>
        /// Especifica el número de trabajos que tiene asignados la impresora.
        /// Esta valor se guarda en memoria y no tiene persistencia en Base de Datos.
        /// </summary>
        public int TrabajosEnCola
        {
            get { return mTrabajosEnCola; }
            set
            {
                int diferencia = (mTrabajosEnCola - value);
                mTrabajosEnCola = value;
                if (TrabajosEnColaChanged != null) TrabajosEnColaChanged(this, diferencia);
            }
        }

        public Framework.AgenteImpresion.GestorImpresora.DescriptorImpresora DescriptorImpresoraAI
        {
            get { return new Framework.AgenteImpresion.GestorImpresora.DescriptorImpresora(mNombreImpresora, mNombreDriver, mImpresoraPorDefecto, mTipoImpresora); }
            set { RellenarCampos(value); }
        }

        public string NombreImpresora
        { get { return DescriptorImpresoraAI.Nombre; } }

        public override string ToString()
        {
            return DescriptorImpresoraAI.ToString();
        }

        #region IComparable<ContenedorDescriptorImpresoraDN> Members

        int IComparable<ContenedorDescriptorImpresoraDN>.CompareTo(ContenedorDescriptorImpresoraDN other)
        {
            //primero ordenamos por errores (el que menos tenga, primero), y después por trabajos en cola (el q menos tenga, primero)
            return (mErrores.CompareTo(other.mErrores) * 2) + mTrabajosEnCola.CompareTo(other.mTrabajosEnCola);
        }

        #endregion
    }


    [Serializable]
    public class ColContenedorDescriptorImpresorasDN : Framework.DatosNegocio.ArrayListValidable<ContenedorDescriptorImpresoraDN>
    { }



    /// <summary>
    /// Reúne las impresoras en categorías en función de la Función de Impresora
    /// </summary>
    [Serializable]
    public class CategoriaImpresoras : Framework.DatosNegocio.EntidadDN, IDisposable, IComparable<CategoriaImpresoras>
    {
        protected FuncionImpresora mFuncionImpresora;
        protected ColContenedorDescriptorImpresorasDN mColImpresoras;
        private int mTrabajosEnCola;
        private int mErrores;
        /// <summary>
        /// Da las impresoras contenidas en la categoria ordenadas en función
        /// de los trabajos que tienen en cola
        /// </summary>
        public PriorityQueue<ContenedorDescriptorImpresoraDN> ImpresorasPorTrabajos = new Framework.Colecciones.PriorityQueue<ContenedorDescriptorImpresoraDN>();
        //public SortedList<int, ContenedorDescriptorImpresoraDN> ImpresorasPorTrabajos = new SortedList<int, ContenedorDescriptorImpresoraDN>();


        public delegate void TrabajosEnColaChangedEventHandler(CategoriaImpresoras categoria);
        public event TrabajosEnColaChangedEventHandler TrabajosEnColaChanged;

        public delegate void ErroresChangedEventhandler(CategoriaImpresoras categoria);
        public event ErroresChangedEventhandler ErroresChanged;


        /// <summary>
        /// La suma de los errores que tienen las impresoras que contiene la categoría.
        /// este valor se guarda en memoria y no tiene persistencia en base de datos
        /// </summary>
        public int Errores
        {
            get { return mErrores; }
            set
            {
                if (value < 0) return;
                mErrores = value;
                if (ErroresChanged != null) ErroresChanged(this);
            }
        }


        /// <summary>
        /// La suma de los trabajos en cola de las impresoras que hay dentro de
        /// esta categoría.
        /// Este valor se guarda en memoria y no tiene persistencia en Base de Datos.
        /// </summary>
        public int TrabajosEnCola
        {
            get { return mTrabajosEnCola; }
            set { mTrabajosEnCola = value; }
        }

        public ColContenedorDescriptorImpresorasDN ColImpresoras
        {
            get { return mColImpresoras; }
            set
            {
                if (mColImpresoras != null)
                {
                    mColImpresoras.ElementoAñadido -= new Framework.DatosNegocio.IColEventos.ElementoAñadidoEventHandler(ImpresoraAñadida);
                }
                this.CambiarValorCol<ColContenedorDescriptorImpresorasDN>(value, ref mColImpresoras);
                if (mColImpresoras != null)
                {
                    mColImpresoras.ElementoAñadido += new Framework.DatosNegocio.IColEventos.ElementoAñadidoEventHandler(ImpresoraAñadida);
                    ActualizarImpresoraPorTrabajos();
                    foreach (ContenedorDescriptorImpresoraDN impresora in mColImpresoras)
                    {
                        impresora.TrabajosEnColaChanged += new ContenedorDescriptorImpresoraDN.TrabajosEnColaChangedEventHandler(TrabajosEnColaImpresoraChanged);
                        impresora.ErroresChanged += new ContenedorDescriptorImpresoraDN.ErroresChangedEventhandler(ErroresImpresoraChanged);
                    }
                }
                else
                {
                    LimpiarImpresorasPorTrabajos();
                }
            }
        }


        private void LimpiarImpresorasPorTrabajos()
        {
            ImpresorasPorTrabajos.Clear();
        }

        private void ActualizarImpresoraPorTrabajos()
        {
            LimpiarImpresorasPorTrabajos();
            foreach (ContenedorDescriptorImpresoraDN ci in mColImpresoras.ToListOFt())
            {
                ImpresorasPorTrabajos.Push(ci);
            }
        }

        private void ImpresoraAñadida(object sender, object elementoAñadido)
        {
            ContenedorDescriptorImpresoraDN impresora = (ContenedorDescriptorImpresoraDN)elementoAñadido;
            impresora.TrabajosEnColaChanged -= TrabajosEnColaImpresoraChanged;
            impresora.TrabajosEnColaChanged += TrabajosEnColaImpresoraChanged;
            impresora.ErroresChanged -= new ContenedorDescriptorImpresoraDN.ErroresChangedEventhandler(ErroresImpresoraChanged);
            impresora.ErroresChanged += new ContenedorDescriptorImpresoraDN.ErroresChangedEventhandler(ErroresImpresoraChanged);
            ImpresorasPorTrabajos.Push(impresora);
        }


        private void ImpresoraEliminada(object sender, object elementoEliminado)
        {
            ContenedorDescriptorImpresoraDN impresora = (ContenedorDescriptorImpresoraDN)elementoEliminado;
            impresora.TrabajosEnColaChanged -= TrabajosEnColaImpresoraChanged;
            ActualizarImpresoraPorTrabajos();
        }

        private void TrabajosEnColaImpresoraChanged(ContenedorDescriptorImpresoraDN impresora, int diferencia)
        {
            this.mTrabajosEnCola += diferencia;
            ImpresorasPorTrabajos.Update(impresora);
            //lanzamos el evento indicando que se ha actualizado el nº de trabajos en cola
            if (TrabajosEnColaChanged != null)
            {
                TrabajosEnColaChanged(this);
            }
        }

        private void ErroresImpresoraChanged(ContenedorDescriptorImpresoraDN impresora)
        {
            ImpresorasPorTrabajos.Update(impresora);
        }

        public FuncionImpresora FuncionImpresora
        {
            get { return mFuncionImpresora; }
            set { this.CambiarValorVal<FuncionImpresora>(value, ref mFuncionImpresora); }
        }

        public bool ContieneImpresora(string NombreImpresora)
        {
            bool contenida = false;
            if (mColImpresoras != null || mColImpresoras.Count != 0)
            {
                foreach (ContenedorDescriptorImpresoraDN cimp in mColImpresoras)
                {
                    if (cimp.DescriptorImpresoraAI.Nombre == NombreImpresora)
                    {
                        contenida = true;
                        break;
                    }
                }
            }
            return contenida;
        }

        public bool ContieneImpresora(ContenedorDescriptorImpresoraDN Impresora)
        {
            bool contenida = false;
            if (mColImpresoras != null) { contenida = mColImpresoras.Contains(Impresora); }
            return contenida;
        }


        #region IDisposable Members

        public void Dispose()
        {
            //eliminamos los delegados a los eventos de las impresoras y la col dei mpresoras
            if (mColImpresoras != null)
            {
                foreach (ContenedorDescriptorImpresoraDN impresora in mColImpresoras)
                {
                    impresora.TrabajosEnColaChanged -= TrabajosEnColaImpresoraChanged;
                }
                mColImpresoras.ElementoAñadido -= new Framework.DatosNegocio.IColEventos.ElementoAñadidoEventHandler(ImpresoraAñadida);
            }
        }

        #endregion

        #region IComparable<CategoriaImpresoras> Members

        int IComparable<CategoriaImpresoras>.CompareTo(CategoriaImpresoras other)
        {
            return (mErrores.CompareTo(other.mErrores) * 2) + mTrabajosEnCola.CompareTo(other.mTrabajosEnCola);
        }

        #endregion
    }



    /// <summary>
    /// Un Dictionary de funcionimpresora-pq of categorias especializado, que atiende
    /// a los eventos para organizar la prioridad de las categorías
    /// </summary>
    [Serializable]
    public class CategoriasImpresorasPorFuncion : Dictionary<FuncionImpresora, PriorityQueue<CategoriaImpresoras>>
    {
        public void AddItems(List<CategoriaImpresoras> categorias)
        {
            foreach (CategoriaImpresoras c in categorias)
            {
                AddCategoria(c);
            }
        }


        public void AddCategoria(CategoriaImpresoras categoria)
        {
            if (this.ContainsKey(categoria.FuncionImpresora))
            {
                this[categoria.FuncionImpresora].Push(categoria);
            }
            else
            {
                PriorityQueue<CategoriaImpresoras> pq = new PriorityQueue<CategoriaImpresoras>();
                pq.Push(categoria);
                this.Add(categoria.FuncionImpresora, pq);
            }
            categoria.TrabajosEnColaChanged += new CategoriaImpresoras.TrabajosEnColaChangedEventHandler(c_TrabajosEnColaChanged);
            categoria.ErroresChanged += new CategoriaImpresoras.ErroresChangedEventhandler(categoria_ErroresChanged);
        }

        void categoria_ErroresChanged(CategoriaImpresoras categoria)
        {
            ActualizarCategoria(categoria);
        }


        void c_TrabajosEnColaChanged(CategoriaImpresoras categoria)
        {
            ActualizarCategoria(categoria);
        }


        private void ActualizarCategoria(CategoriaImpresoras categoria)
        {
            foreach (KeyValuePair<FuncionImpresora, PriorityQueue<CategoriaImpresoras>> kvp in this)
            {
                if (kvp.Value.Contains(categoria)) kvp.Value.Update(categoria);
            }
        }

    }

}
