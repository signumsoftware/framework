using System;
using System.Collections.Generic;
using System.Text;
using SerializadorTexto.Atributos;

namespace FicherosWebAD
{
    [ArchivoTexto(1871, RetornoCarro = true, AlinearDerecha = AlinearDerecha.Ninguno)]
    public class FicheroWeb
    {
        [Orden(0)]
        public List<LineaWeb> Lineas;
    }

    [LineaTexto(1871)]
    public class LineaWeb
    {
        [CampoTexto(0, 10)]
        public int ID;

        [CampoTexto(1, 4)]
        public TipoProyecto TipoProyecto;

        [CampoTexto(2, 5)]
        public int? CodigoConcesionario;

        [CampoTexto(3, 1)]
        public bool EsCliente;

        [CampoTexto(4, 20)]
        public string NumeroPoliza;

        [CampoTexto(5, 1)]
        public Sexo Sexo;

        [CampoTexto(6, 10, Format = "dd/MM/yyyy")]
        public DateTime FechaNacimiento;

        [CampoTexto(7, 50)]
        public string Localidad;

        [CampoTexto(8, 10)]
        public string CodigoPostal;

        [CampoTexto(9, 80)]
        public string Marca;

        [CampoTexto(10, 80)]
        public string Modelo;

        [CampoTexto(11, 2)]
        public Categoria Categoria;

        [CampoTexto(12, 8)]
        public int Cilindrada;

        [CampoTexto(13, 1)]
        public bool Matriculado;

        [CampoTexto(14, 1)]
        public bool PermisoCirculacion;

        [CampoTexto(15, 10, Format = "dd/MM/yyyy")]
        public DateTime? FechaMatriculacion;

        [CampoTexto(16, 10, Format = "dd/MM/yyyy")]
        public DateTime? FechaFabricacion;

        [CampoTexto(17, 1)]
        public bool Permiso;

        [CampoTexto(18, 2)]
        public TipoPermiso TipoPermiso;

        [CampoTexto(19, 10, Format = "dd/MM/yyyy")]
        public DateTime? FechaPermiso;

        [CampoTexto(20, 3)]
        public int? NivelBonificacion;

        [CampoTexto(21, 4)]
        public double ValorBonificacion;

        [CampoTexto(22, 1)]
        public bool SeguroAccidentes;

        [CampoTexto(23, 10, Format = "dd/MM/yyyy")]
        public DateTime? FechaVencimiento;

        [CampoTexto(24, 1)]
        public TipoDocumento? TipoDocumento;

        [CampoTexto(25, 2)]
        public int? AñosSinSiniestro;

        [CampoTexto(26, 2)]
        public int AñosSinSiniestroConCulpa;

        [CampoTexto(27, 2)]
        public int AñosSinSiniestroSinCulpa;

        [CampoTexto(28, 40)]
        public Preguntas Preguntas;

        [CampoTexto(29, 20)]
        public Respuestas Respuestas;

        [CampoTexto(30, 10)]
        public string Modalidad;

        [CampoTexto(31, 50)]
        public string Coberturas;

        /// <summary>
        /// Pertenece a InformacionPresupuesto.InformaicionFraccionamietno
        /// </summary>
        [CampoTexto(32, 1)]
        public int? Fraccionamientos;

        [CampoTexto(33, 10, Format = "dd/MM/yyyy")]
        public DateTime? FechaTarificacion;

        [CampoTexto(34, 9)]
        public double TotalBasica;

        [CampoTexto(35, 672)]
        public InfoPresupuesto InformacionPresupuesto;

        [CampoTexto(36, 1)]
        public string OffOff;

        [CampoTexto(37, 717)]
        public ConductoresDesignados ConductoresDesignados;

    }

    [LineaTexto(40)]
    public class Preguntas
    {
        [CampoTexto(1, 4)]
        public int? Pregunta1;

        [CampoTexto(2, 4)]
        public int? Pregunta2;

        [CampoTexto(3, 4)]
        public int? Pregunta3;

        [CampoTexto(4, 4)]
        public int? Pregunta4;

        [CampoTexto(5, 4)]
        public int? Pregunta5;

        [CampoTexto(6, 4)]
        public int? Pregunta6;

        [CampoTexto(7, 4)]
        public int? Pregunta7;

        [CampoTexto(8, 4)]
        public int? Pregunta8;

        [CampoTexto(9, 4)]
        public int? Pregunta9;

        [CampoTexto(10, 4)]
        public int? Pregunta10;
    }


    [LineaTexto(20)]
    public class Respuestas
    {
        [CampoTexto(1, 2)]
        public int? Respuesta1;

        [CampoTexto(2, 2)]
        public int? Respuesta2;

        [CampoTexto(3, 2)]
        public int? Respuesta3;

        [CampoTexto(4, 2)]
        public int? Respuesta4;

        [CampoTexto(5, 2)]
        public int? Respuesta5;

        [CampoTexto(6, 2)]
        public int? Respuesta6;

        [CampoTexto(7, 2)]
        public int? Respuesta7;

        [CampoTexto(8, 2)]
        public int? Respuesta8;

        [CampoTexto(9, 2)]
        public int? Respuesta9;

        [CampoTexto(10, 2)]
        public int? Respuesta10;
    }

    [LineaTexto(672)]
    public class InfoPresupuesto
    {
        [CampoTexto(1, 59)]
        public InfoFraccionamiento InfoFraccionamiento;

        [CampoTexto(2, 275)]
        public Cliente Cliente;

        [CampoTexto(3, 165)]
        public Titular Titular;

        [CampoTexto(4, 35)]
        public InfoContratacion InfoContratacion;

        [CampoTexto(5, 18)]
        public Concesionario Concesionario;

        [CampoTexto(6, 3)]
        public int? Conocimiento;

        [CampoTexto(7, 115)]
        public ClienteInfoAdicional ClienteInfoAdicional;

        [CampoTexto(8, 1)]
        public TipoNif? ClienteTipoNif;

        [CampoTexto(9, 1)]
        public TipoNif? TitularTipoNif;
    }

    [LineaTexto(59)]
    public class InfoFraccionamiento
    {
        [CampoTexto(1, 9)]
        public double PrimaNetaTotalAnual;

        [CampoTexto(2, 20, Format = "dd/MM/yyyyHH:mm:ss")]
        public DateTime? FechaYHoraEfecto;

        [CampoTexto(3, 10)]
        public string Matricula;

        [CampoTexto(4, 20)]
        public string Bastidor;
    }

    [LineaTexto(35)]
    public class InfoContratacion
    {
        [CampoTexto(4, 26)]
        public string DomiciliacionBancaria;

        [CampoTexto(5, 9)]
        public string NumeroContrato;
    }

    [LineaTexto(275)]
    public class Cliente
    {
        [CampoTexto(1, 50)]
        public string Nombre;

        [CampoTexto(2, 50)]
        public string Apellido1;

        [CampoTexto(3, 50)]
        public string Apellido2;

        [CampoTexto(4, 80)]
        public string Calle;

        [CampoTexto(5, 15)]
        public string Telefono;

        [CampoTexto(6, 15)]
        public string Fax;

        [CampoTexto(7, 15)]
        public string Nif;
    }

    [LineaTexto(165)]
    public class Titular
    {
        [CampoTexto(1, 15)]
        public string Nif;

        [CampoTexto(2, 50)]
        public string Nombre;

        [CampoTexto(3, 50)]
        public string Apellido1;

        [CampoTexto(4, 50)]
        public string Apellido2;
    }

    [LineaTexto(18)]
    public class Concesionario
    {
        [CampoTexto(1, 15)]
        public string Telefono;

        [CampoTexto(2, 3)]
        public string Vendedor;
    }

    [LineaTexto(115)]
    public class ClienteInfoAdicional
    {
        [CampoTexto(1, 100)]
        public string EMail;

        [CampoTexto(2, 15)]
        public string Telefono2;
    }

    [LineaTexto(717)]
    public class ConductoresDesignados
    {
        [CampoTexto(1, 1)]
        public int NumeroConductoresAdicionales;

        [CampoTexto(2, 179)]
        public ConductorDesignado Conductor1;

        [CampoTexto(3, 179)]
        public ConductorDesignado Conductor2;

        [CampoTexto(4, 179)]
        public ConductorDesignado Conductor3;

        [CampoTexto(5, 179)]
        public ConductorDesignado Conductor4;
    }

    [LineaTexto(179)]
    public class ConductorDesignado
    {
        [CampoTexto(1, 1)]
        public Sexo Sexo;

        [CampoTexto(2, 2)]
        public Parentesco Parentesco;

        [CampoTexto(3, 50)]
        public string Nombre;

        [CampoTexto(4, 50)]
        public string Apellido1;

        [CampoTexto(5, 50)]
        public string Apellido2;

        [CampoTexto(6, 10, Format = "dd/MM/yyyy")]
        public DateTime FechaNacimiento;

        [CampoTexto(7, 15)]
        public string Nif;

        [CampoTexto(8, 1)]
        public int? TipoNif;
    }

    public enum TipoPermiso
    {
        No = 0,
        A = 1, 
        A1 = 2, 
        B = 3
    }

    public enum TipoDocumento
    {
        Ninguno = 0, 
        CertificadoNoSiniestralidad = 1,
        CertificadoNoSiniestralidadYReciboAñoEnCurso = 2
    }

    public enum TipoNif
    {
        Nif = 1,
        Nie = 2, 
        Pasaporte = 3
    }

    public enum Sexo
    {
        Hombre = 1,
        Mujer = 2
    }

    public enum Categoria
    {
        Basic = 1,
        Roadster = 2,
        Turismos = 3,
        Cross = 4,
        Quad = 5,
        Ciclomotor = 6,
        Enduro = 7,
        Trial = 8,
        Scooter = 9,
        Trail = 10,
        Custom = 11,
        Sport = 12,
        QuadMatriculado = 13
    }

    [SerializarComoString]
    public enum Parentesco
    {
        [ValorString("CM")]
        Esposo,
        [ValorString("CF")]
        Esposa,
        [ValorString("FM")]
        Hermano,
        [ValorString("FF")]
        Hermana,
        [ValorString("EM")]
        Hijo,
        [ValorString("EF")]
        Hija,
        [ValorString("PM")]
        Padre,
        [ValorString("PF")]
        Madre
    }

    public enum TipoProyecto
    {
        Tarificado = 90,
        Presupuestado = 5,
        Contratado = 4, 
    }
}
