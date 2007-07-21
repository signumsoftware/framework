using System;
using System.Collections.Generic;
using System.Text;

using SerializadorTexto;
using SerializadorTexto.Atributos;
using SerializadorTexto.SerializadorAPila;
using SerializadorTexto.Atributos.Incontextual;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

#pragma warning disable 0169

namespace BancosDF.Domiciliaciones
{
    solo corrigiendo unchecked bug

    [ArchivoTextoIncontextual(162, TipoLinea = typeof(Linea), Culture = "es-ES", Codificacion = Codificacion.OEMMultilingualLatinIAndEuro, RellenarConCeros=RellenarConCeros.SoloNumeros)]
    public class Fichero
    {
        [Orden(0)]
        public CabeceraPresentador CabeceraPresentador;

        [Orden(1)]
        public List<Ordenante> Ordenantes;

        [Orden(2)]
        public TotalGeneral TotalGeneral;
    }

    public class Ordenante
    {
        [Orden(0)]
        public CabeceraOrdenante Cabercera;

        [Orden(1)]
        public List<Domiciliacion> Domiciliacion;

        [Orden(2)]
        public TotalOrdenante Total;
    }

    public class Domiciliacion
    {
        [Orden(0)]
        public IndividualObligatorio Obligatorio;

        [Orden(1), Opcional]
        public Opcional1 Opcional1;

        [Orden(2), Opcional]
        public Opcional2 Opcional2;

        [Orden(3), Opcional]
        public Opcional3 Opcional3;

        [Orden(4), Opcional]
        public Opcional4 Opcional4;

        [Orden(5), Opcional]
        public Opcional5 Opcional5;

        [Orden(6), Opcional]
        public Opcional6 Opcional6;
    }

    [LineaTextoIncontextual(162, ID = "51-80")]
    public class CabeceraPresentador : Linea
    {
        [CampoTexto(0, 12)]
        public CodigoIdentificacion CodigoPresentador;

        [CampoTexto(1, 6, Format = "ddMMyy")]
        public DateTime FechaConfeccionSoporte;

        [CampoTexto(2, 6)]
        string _Libre;

        [CampoTexto(3, 40)]
        public string NombreClientePresentador;

        [CampoTexto(4, 20)]
        string _Libre2;

        [CampoTexto(5, 4)]
        public int EntidadReceptora;

        [CampoTexto(6, 4)]
        public int Oficina;

        [CampoTexto(7, 12)]
        string _Libre3;

        [CampoTexto(8, 40)]
        string _Libre4;

        [CampoTexto(9, 14)]
        string _Libre5;

    }


    [LineaTextoIncontextual(162, ID = "53-80")]
    public class CabeceraOrdenante : Linea
    {
        [CampoTexto(0, 12)]
        public CodigoIdentificacion CodigoOrdenante;

        [CampoTexto(1, 6, Format = "ddMMyy")]
        public DateTime FechaConfeccionSoporte;

        [CampoTexto(2, 6, Format = "ddMMyy")]
        public DateTime FechaDeCargo;

        [CampoTexto(3, 40)]
        public string NombreClienteOrdenante;

        [CampoTexto(4, 20)]
        public CodigoCuentaCliente CodigoCuentaAbono;

        [CampoTexto(5, 8)]
        string _Libre;

        [CampoTexto(6, 2)]
        public int Procedimiento;

        [CampoTexto(7, 10)]
        string _Libre2;

        [CampoTexto(8, 40)]
        string _Libre3;

        [CampoTexto(9, 14)]
        string _Libre4;
    }

    [LineaTextoIncontextual(162, ID = "56-80")]
    public class IndividualObligatorio : Linea
    {
        [CampoTexto(0, 12)]
        public CodigoIdentificacion CodigoOrdenante;

        [CampoTexto(1, 12)]
        public string CodigoReferencia;

        [CampoTexto(2, 40)]
        public string NombreTitularDomiciliacion;

        [CampoTexto(3, 20)]
        public CodigoCuentaCliente CodigoCuentaAdeudo;

        [CampoTexto(4, 10, Format = "0!00")]
        public decimal Importe;

        [CampoTexto(5, 6)]
        public string CodigoParaDevoluciones;

        [CampoTexto(6, 10)]
        public string CodigoReferenciaInterna;

        [CampoTexto(7, 40)]
        public string PrimerCampoDeConcepto;

        [CampoTexto(8, 8)]
        string _Libre;
    }

    [LineaTextoIncontextual(162, ID = "56-81")]
    public class Opcional1 : Opcional { }

    [LineaTextoIncontextual(162, ID = "56-82")]
    public class Opcional2 : Opcional { }

    [LineaTextoIncontextual(162, ID = "56-83")]
    public class Opcional3 : Opcional { }

    [LineaTextoIncontextual(162, ID = "56-84")]
    public class Opcional4 : Opcional { }

    [LineaTextoIncontextual(162, ID = "56-85")]
    public class Opcional5 : Opcional { }

    public abstract class Opcional : Linea
    {
        [CampoTexto(0, 12)]
        public CodigoIdentificacion CodigoOrdenante;

        [CampoTexto(1, 12)]
        public string CodigoReferencia;

        [CampoTexto(2, 40)]
        public string Concepto1;

        [CampoTexto(3, 40)]
        public string Concepto2;

        [CampoTexto(4, 40)]
        public string Concepto3;

        [CampoTexto(5, 14)]
        string _Libre1;
    }

    [LineaTextoIncontextual(162, ID = "56-86")]
    public class Opcional6 : Linea
    {
        [CampoTexto(0, 12)]
        public CodigoIdentificacion CodigoOrdenante;

        [CampoTexto(1, 12)]
        public string CodigoReferencia;

        [CampoTexto(2, 40)]
        public string NombreDelTitular;

        [CampoTexto(3, 40)]
        public string DomicilioDelTitular;

        [CampoTexto(4, 35)]
        public string PlazaDelDomicilioDelTitular;

        [CampoTexto(5, 5)]
        public string CodigoPostalDelDomicilioDelTitular;

        [CampoTexto(6, 14)]
        string _Libre;
    }

    [LineaTextoIncontextual(162, ID = "58-80")]
    public class TotalOrdenante : Linea
    {
        [CampoTexto(0, 12)]
        public CodigoIdentificacion CodigoOrdenante;

        [CampoTexto(1, 12)]
        string _Libre;

        [CampoTexto(2, 40)]
        string _Libre2;

        [CampoTexto(3, 20)]
        string _Libre3;

        [CampoTexto(4, 10, Format="0!00")]
        public decimal SumaDeImporte;

        [CampoTexto(5, 6)]
        string _Libre4;

        [CampoTexto(6, 10)]
        public int NumeroDomiciliaciones;

        [CampoTexto(7, 10)]
        public int NumeroRegistrosOrdenante;

        [CampoTexto(8, 20)]
        string _Libre5;

        [CampoTexto(9, 18)]
        string _Libre6;
    }

    [LineaTextoIncontextual(162, ID = "59-80")]
    public class TotalGeneral : Linea
    {
        [CampoTexto(0, 12)]
        public CodigoIdentificacion CodigoPresentador;

        [CampoTexto(1, 12)]
        string _Libre;

        [CampoTexto(2, 40)]
        string _Libre2;

        [CampoTexto(3, 4)]
        public int NumeroOrdenantes;

        [CampoTexto(4, 16)]
        string _Libre3;

        [CampoTexto(5, 10, Format = "0!00")]
        public decimal SumaDeImporte;

        [CampoTexto(6, 6)]
        string _Libre4;

        [CampoTexto(7, 10)]
        public int NumeroTotalDomiciliaciones;

        [CampoTexto(8, 10)]
        public int NumeroTotalRegistros;

        [CampoTexto(9, 20)]
        string _Libre5;

        [CampoTexto(10, 18)]
        string _Libre6;
    }

    [LineaTexto(12)]
    public class CodigoIdentificacion
    {
        [CampoTexto(0, 9, RellenarConCeros=true)]
        public string NIF;

        [CampoTexto(1, 3, RellenarConCeros=true)]
        public string Sufijo;
    }

    [LineaTexto(20)]
    public class CodigoCuentaCliente: ISpecialToStringAndParse
    {
        [CampoTexto(0, 4)]
        public int Entidad;

        [CampoTexto(1, 4)]
        public int Oficina;

        [CampoTexto(2, 2, UsarSpecialToStringAndParse=true)]
        public int? DigitosControl;

        [CampoTexto(3, 10)]
        public long NumeroCuenta;

        public bool ToStringEvent(string fieldName, object value, out string stringValue)
        {
            stringValue = "**";
            return value == null;
        }

        public bool ParseEvent(string fieldName, string stringValue, out object value)
        {
            value = null;
            return stringValue == "**";
        }
    }

    [LineaTexto(162)]
    public class Linea : IIDProvider
    {
        [CampoTexto(-2, 2)]
        public int CodigoRegisto;

        [CampoTexto(-1, 2)]
        public int CodigoDato;

        public Linea()
        {
            Type t = GetType();
            if (t != typeof(Linea))
            {
                string id = DictionarioIdentidadFila<Fichero>.DameID(t);
                string[] ids = id.Split('-');
                CodigoRegisto = int.Parse(ids[0]);
                CodigoDato = int.Parse(ids[1]);
            }
        }

        public string ID
        {
            get { return string.Format("{0:00}-{1:00}", CodigoRegisto, CodigoDato); }
        }
    }
}
