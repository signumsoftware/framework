using System;
using System.Collections.Generic;
using System.Text;
using Framework.Cuestionario.CuestionarioDN;
using FN.RiesgosVehiculos.DN;
using Framework.LogicaNegocios.Transacciones;
using System.Collections;
using Framework.AccesoDatos.MotorAD.LN;
using FN.Personas.DN;
using Framework.DatosNegocio.Localizaciones.Temporales;
using FN.Localizaciones.DN;
using Framework.ClaseBaseLN;
using FN.RiesgosVehiculos.LN.RiesgosVehiculosLN;
using FN.Localizaciones.LN;

namespace GSAMV.DF
{


    public static class TraductorCuestionarios
    {
        public static CuestionarioResueltoDN Traducir(LineaWeb linea, CuestionarioDN cuestionario)
        {

            CuestionarioResueltoDN cr = new CuestionarioResueltoDN();
            cr.ColRespuestaDN = new ColRespuestaDN();
            cr.CuestionarioDN = cuestionario;

            //recuperamos la característica y la respuesta correpondiente a cada pregunta 
            //para formar las preguntas
            DateTime now = linea.FechaTarificacion ?? DateTime.Now;

            Responder(cr, "CodigoConcesionario", new ValorTextoCaracteristicaDN(), linea.CodigoConcesionario, now);
            Responder(cr, "FechaEfecto", new ValorCaracteristicaFechaDN(), linea.FechaTarificacion, now);
            Responder(cr, "EsCliente", new ValorBooleanoCaracterisitcaDN(), linea.EsCliente, now);
            Responder(cr, "FechaNacimiento", new ValorCaracteristicaFechaDN(), linea.FechaNacimiento, now);
            Responder(cr, "EDAD", new ValorNumericoCaracteristicaDN(), AnyosMesesDias.CalcularDirAMD(now, linea.FechaNacimiento).Anyos, now);
            Responder(cr, "CYLD", new ValorNumericoCaracteristicaDN(), linea.Cilindrada, now);
            Responder(cr, "EstaMatriculado", new ValorBooleanoCaracterisitcaDN(), linea.Matriculado, now);
            
            if (linea.FechaMatriculacion.HasValue)
            {
                Responder(cr, "FechaMatriculacion", new ValorCaracteristicaFechaDN(), linea.FechaMatriculacion.Value, now);
                Responder(cr, "ANTG", new ValorNumericoCaracteristicaDN(), AnyosMesesDias.CalcularDirAMD(now, linea.FechaMatriculacion.Value).Anyos, now);
            }
            
            Responder(cr, "TieneCarnet", new ValorBooleanoCaracterisitcaDN(), linea.Permiso, now);
            Responder(cr, "FechaFabricacion", new ValorCaracteristicaFechaDN(), linea.FechaFabricacion, now);

            Responder(cr, "Sexo", new ValorSexoCaracteristicaDN(), RecuperarSexo(linea.Sexo), now);
            
            Responder(cr, "ZONA", new ValorNumericoCaracteristicaDN(), linea.CodigoPostal.ToString().Substring(0,2), now);
            
            LocalidadDN localidad = RecuperarLocalidad(linea.CodigoPostal, linea.Localidad);
            Responder(cr, "Circulacion-Localidad", new ValorLocalidadCaracteristicaDN(), localidad, now);

            RiesgosVehiculosLN rln = new RiesgosVehiculosLN();
            ModeloDN modelo = rln.RecuperarModeloDatos(linea.Modelo, linea.Marca, linea.Matriculado, now).Modelo;
            Responder(cr, "Marca", new ValorMarcaCaracterisitcaDN(), modelo.Marca, now);
            Responder(cr, "Modelo", new ValorModeloCaracteristicaDN(), modelo, now);

            InfoPresupuesto pres = linea.InformacionPresupuesto;
            Responder(cr, "TarificacionPrueba", new ValorBooleanoCaracterisitcaDN(), pres == null, now);
            if (pres != null)
            {

                Concesionario conc = pres.Concesionario;
                if (conc != null)
                    Responder(cr, "CodigoVendedor", new ValorTextoCaracteristicaDN(), conc.Vendedor, now);

                Cliente cliente = pres.Cliente;
                if (cliente != null)
                {
                    Responder(cr, "Nombre", new ValorTextoCaracteristicaDN(), cliente.Nombre, now);
                    Responder(cr, "Apellido1", new ValorTextoCaracteristicaDN(), cliente.Apellido1, now);
                    Responder(cr, "Apellido2", new ValorTextoCaracteristicaDN(), cliente.Apellido2, now);
                    Responder(cr, "Telefono", new ValorTextoCaracteristicaDN(), cliente.Telefono, now);
                    Responder(cr, "Fax", new ValorTextoCaracteristicaDN(), cliente.Fax, now);


                    DireccionNoUnicaDN dir = new DireccionNoUnicaDN();
                    dir.Nombre = cliente.Calle;
                    dir.CodPostal = linea.CodigoPostal;
                    Responder(cr, "DireccionEnvio", new ValorDireccionNoUnicaCaracteristicaDN(), dir, now);
                }

                ClienteInfoAdicional clienteInfoAdic = pres.ClienteInfoAdicional;
                if (clienteInfoAdic != null)
                {
                    Responder(cr, "Email", new ValorTextoCaracteristicaDN(), clienteInfoAdic.EMail, now);
                }
            }
         
            if (linea.Permiso)
            {
                Responder(cr, "FechaCarnet", new ValorCaracteristicaFechaDN(), linea.FechaPermiso.Value, now);
                Responder(cr, "TipoCarnet", new ValorNumericoCaracteristicaDN(), (int)linea.TipoPermiso, now);
                Responder(cr, "CARN", new ValorNumericoCaracteristicaDN(), AnyosMesesDias.CalcularDirAMD(now, linea.FechaPermiso.Value).Anyos, now);
            }
            else
            {
                Responder(cr, "CARN", new ValorNumericoCaracteristicaDN(), 0, now);
            }
            ConductoresDesignados condDesignados = linea.ConductoresDesignados;
            bool hayConductoresDesignados = condDesignados != null && condDesignados.NumeroConductoresAdicionales > 0;
            Responder(cr, "ConductoresAdicionalesConCarnet", new ValorBooleanoCaracterisitcaDN(), hayConductoresDesignados, now);
            if (hayConductoresDesignados)
            {
                DateTime? joven;
                ColDatosMCND colDatos = GenerarColDatos(condDesignados, out joven);
                Responder(cr, "MCND", new ValorNumericoCaracteristicaDN(), AnyosMesesDias.CalcularDirAMD(now, joven.Value).Anyos, now);
                Responder(cr, "ColConductoresAdicionales", new ValorMCNDCaracteristicaDN(), colDatos, now);
            }
   
           
            Preguntas p = linea.Preguntas; 
            Respuestas r = linea.Respuestas;

            if (p != null && r != null)
            {
                Responder(cr, "SiniestroResponsable3años", new ValorNumericoCaracteristicaDN(), TestInt(Pregunta.SiniestrosConResponsabilidad, p.Pregunta1, r.Respuesta1), now);
                Responder(cr, "SiniestroSinResponsabilidad3años", new ValorNumericoCaracteristicaDN(), TestInt(Pregunta.SiniestrosSinResponsabilidad, p.Pregunta2, r.Respuesta2), now);
                Responder(cr, "RetiradaCarnet3años", new ValorBooleanoCaracterisitcaDN(), TestBool(Pregunta.InfraccionConRetiradaDeCarnet, p.Pregunta3, r.Respuesta3), now);
                Responder(cr, "ConduccionEbrio3años", new ValorBooleanoCaracterisitcaDN(), TestBool(Pregunta.InfraccionPorConducirEbrio, p.Pregunta4, r.Respuesta4), now);
                Responder(cr, "VehículoTransporteRemunerado", new ValorBooleanoCaracterisitcaDN(), TestBool(Pregunta.UtilizaVehiculoParaTransporteRemunerado, p.Pregunta5, r.Respuesta5), now);
                Responder(cr, "CanceladoSeguro3años", new ValorBooleanoCaracterisitcaDN(), TestBool(Pregunta.SeguroCancelado, p.Pregunta6, r.Respuesta6), now);
                //unico conductor del vehiculo no se usa
                if (p.Pregunta7.HasValue && TraductorPreguntas.TraducirPregunta(p.Pregunta7.Value) == Pregunta.UnicoConductorDelVehiculo)
                {
                    Responder(cr, "PermisoCirculacionEspañol", new ValorBooleanoCaracterisitcaDN(), TestBool(Pregunta.DisponesPermisoCirculacionEspañol, p.Pregunta8, r.Respuesta8), now);
                    Responder(cr, "TitularPermisoCirculación", new ValorBooleanoCaracterisitcaDN(), TestBool(Pregunta.TitularPermisoCirculacion, p.Pregunta9, r.Respuesta9), now);
                }
                else
                {
                    Responder(cr, "PermisoCirculacionEspañol", new ValorBooleanoCaracterisitcaDN(), TestBool(Pregunta.DisponesPermisoCirculacionEspañol, p.Pregunta7, r.Respuesta7), now);
                    Responder(cr, "TitularPermisoCirculación", new ValorBooleanoCaracterisitcaDN(), TestBool(Pregunta.TitularPermisoCirculacion, p.Pregunta7, r.Respuesta7), now);
                }
            }

            Responder(cr, "AseguradoActualmente", new ValorBooleanoCaracterisitcaDN(), linea.SeguroAccidentes, now);
            Responder(cr, "VencimientoSeguroActual", new ValorCaracteristicaFechaDN(), linea.FechaVencimiento, now);
            Responder(cr, "AñosSinSiniestro", new ValorNumericoCaracteristicaDN(), linea.AñosSinSiniestro, now);
            Responder(cr, "Justificantes", new ValorNumericoCaracteristicaDN(), ToJustificante(linea.TipoDocumento.Value), now);

            return cr; 
        }

        private static TipoSexo RecuperarSexo(Sexo sexo)
        {
            BaseTransaccionConcretaLN btc = new BaseTransaccionConcretaLN();
            IList list = btc.RecuperarLista(typeof(TipoSexo));
            foreach (TipoSexo ts in list)
            {
                if (ts.Nombre == sexo.ToString())
                    return ts;
            }
            return null; 
        }

        static LocalidadDN RecuperarLocalidad(string codigoPostal, string nombre)
        {
            LocalizacionesLN loc = new LocalizacionesLN();
            ColLocalidadDN col = loc.RecuperarLocalidadporCodigoPostal(codigoPostal);
            return col.RecuperarPrimeroXNombre(nombre);
        }

        static int? TestInt(Pregunta preguntaSupuesta, int? pregunta, int? respuesta)
        {
            if(pregunta == null)
                return null;

            Pregunta preguntaReal = TraductorPreguntas.TraducirPregunta(pregunta.Value);
            if (preguntaReal != preguntaSupuesta)
                throw new InvalidOperationException("Se esperaba la pregunta " + preguntaReal + "  y se encontró " + preguntaSupuesta);

            return respuesta; 
        }

        static bool? TestBool(Pregunta preguntaSupuesta, int? pregunta, int? respuesta)
        {
            int? result =  TestInt(preguntaSupuesta, pregunta, respuesta);
            return result == null ? null : (bool?)Convert.ToBoolean(result.Value); 
        }

        static ColDatosMCND GenerarColDatos(ConductoresDesignados condDesignados, out DateTime? max)
        {
            max = null;

            ColDatosMCND result = new ColDatosMCND();
            for (int i = 1; i <= condDesignados.NumeroConductoresAdicionales; i++)
            {
                ConductorDesignado cond = null;
                switch (i)
                {
                    case 1: cond = condDesignados.Conductor1; break;
                    case 2: cond = condDesignados.Conductor2; break;
                    case 3: cond = condDesignados.Conductor3; break;
                    case 4: cond = condDesignados.Conductor4; break;
                }

                if (cond == null)
                    throw new InvalidOperationException("Se esperaban " + condDesignados.NumeroConductoresAdicionales + " pero el numero " + i + " no existe");

                DatosMCND datos = new DatosMCND();
                datos.Nombre = cond.Nombre;
                datos.Apellido1 = cond.Apellido1;
                datos.Apellido2 = cond.Apellido2;
                datos.FechaNacimiento = cond.FechaNacimiento;
                datos.Parentesco = ToParentescoConductorAdicional(cond.Parentesco);
                datos.NIF = new NifDN(cond.Nif);
                result.Add(datos);

                max = max == null || max < cond.FechaNacimiento ? cond.FechaNacimiento : max;

            }

            return result; 
        }

        static ParentescoConductorAdicional ToParentescoConductorAdicional(Parentesco parentesco)
        {
            switch (parentesco)
            {
                case Parentesco.Esposo:
                case Parentesco.Esposa: return ParentescoConductorAdicional.Conyuge;
                
                case Parentesco.Hermano:
                case Parentesco.Hermana: return ParentescoConductorAdicional.Hermano;
                
                case Parentesco.Hijo:
                case Parentesco.Hija: return ParentescoConductorAdicional.Hijo;

                case Parentesco.Padre:
                case Parentesco.Madre: return ParentescoConductorAdicional.Padre;

                default: throw new InvalidOperationException("Valor " + parentesco + " no admitido");
            }
        }

        static Justificantes ToJustificante(TipoDocumento tipoDocumento)
        {
            switch (tipoDocumento)
            {
                case TipoDocumento.Ninguno: return Justificantes.ninguno;
                case TipoDocumento.CertificadoNoSiniestralidad: return Justificantes.certificado;
                case TipoDocumento.CertificadoNoSiniestralidadYReciboAñoEnCurso: return Justificantes.certificado_y_recibo;
                default: throw new InvalidOperationException(); 
            }
        }

        static void Responder(CuestionarioResueltoDN cuestionarioRes, string nombre, IValorCaracteristicaDN valor, object valorAsignar, System.DateTime fechaEfecto)
        {
            if (valorAsignar == null)
                return; 

            PreguntaDN pregunta = cuestionarioRes.CuestionarioDN.ColPreguntaDN.RecuperarPrimeroXNombre(nombre);
            CaracteristicaDN caracteristica = pregunta.CaracteristicaDN;

            valor.Valor = valorAsignar;
            valor.Caracteristica = caracteristica;
            valor.FechaEfectoValor = fechaEfecto;

            RespuestaDN respuesta = new RespuestaDN();
            respuesta.PreguntaDN = pregunta;
            respuesta.IValorCaracteristicaDN = valor;

            cuestionarioRes.ColRespuestaDN.Add(respuesta);
        }

    }
}
