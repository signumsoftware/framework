Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports Framework.DatosNegocio
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Cuestionario.CuestionarioDN

Imports GestionSegurosAMV.AD

<TestClass()> Public Class utPresupuestos

    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

    <TestMethod()> Public Sub CrearPresupuesto()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            CrearPresupuestoP()
        End Using


    End Sub


    <TestMethod()> Public Sub CrearPagosTarifa()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            Dim rvLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
            rvLN.CargarGrafoTarificacion()

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Framework.Configuracion.AppConfiguracion.DatosConfig.Item("PeriodoValidezPresupuestoAMD") = "0/1/0"
            CrearPagosTarifaP()
        End Using


    End Sub



    <TestMethod()> Public Sub CrearPolizaEnGrafoDesdePresupuesto()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CrearPolizaEnGrafoDesdePresupuestop()
        End Using


    End Sub

    <TestMethod()> Public Sub EfectuaryLiquidarSiguientePago()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            EfectuaryLiquidarSiguientePagop()
        End Using


    End Sub


    Private Function EfectuaryLiquidarSiguientePagop() As FN.GestionPagos.DN.ColLiquidacionPagoDN




        Using tr As New Transaccion


            Dim bln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim colpagos As New FN.GestionPagos.DN.ColPagoDN


            colpagos.AddRangeObject(bln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))

            For Each pago As FN.GestionPagos.DN.PagoDN In colpagos

                If pago.Efectuable(Nothing) Then

                    Dim ln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.LiquidadorConcretoRVLN
                    EfectuaryLiquidarSiguientePagop = ln.EfectuarYLiquidar(pago)
                    tr.Confirmar()
                    Exit Function
                End If
            Next




        End Using





        Return Nothing

    End Function



    Private Function CrearPolizaEnGrafoDesdePresupuestop() As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN





        Using tr As New Transaccion

            Dim presu As FN.Seguros.Polizas.DN.PresupuestoDN = CrearPresupuestoP()
            presu.FuturoTomador.NIFCIFFuturoTomador = FN.Localizaciones.DN.NifDN.RecuperarNifAleatorio

            Dim matricula As FN.RiesgosVehiculos.DN.MatriculaDN = FN.RiesgosVehiculos.DN.MatriculaDN.GenerarMatriculaAleatoriaDelTipo(FN.RiesgosVehiculos.DN.TipoMatricula.NormalTM)
            CType(presu.Tarifa.Riesgo, FN.RiesgosVehiculos.DN.RiesgoMotorDN).Matricula = matricula

            presu.FechaAltaSolicitada = presu.FI.AddDays(1)
            presu.CondicionesPago = New FN.GestionPagos.DN.CondicionesPagoDN
            presu.CondicionesPago.ModalidadDePago = FN.GestionPagos.DN.ModalidadPago.IngresoEnCuenta
            presu.CondicionesPago.NumeroRecibos = 3

            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolAplicacion") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolAplicacion", "RolServidorGSAMV")
            End If
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("nombreRolCliente") Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("nombreRolCliente", "RolClienteGSAMV")
            End If

            '1º se crea el objeto póliza completo
            Dim bln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()


            Dim polprhe As New Framework.DatosNegocio.HEDN(presu)

            '2º se guarda la operación que inicia el flujo de operaciones para el grafo de la póliza
            Dim opPOrg As New Framework.Procesos.ProcesosDN.OperacionDN()
            Dim opPDes As New Framework.Procesos.ProcesosDN.OperacionDN()

            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            Dim colOps As New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOps.AddRangeObject(bln.RecuperarLista(GetType(Framework.Procesos.ProcesosDN.OperacionDN)))
            opPOrg = colOps.RecuperarPrimeroXNombre("Gestión Pólizas")
            opPDes = colOps.RecuperarPrimeroXNombre("Alta Póliza desde Presupuesto")

            Dim oprOrg As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN()
            Dim oprDes As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN()
            oprOrg.Operacion = opPOrg
            oprDes.Operacion = opPOrg

            Dim prin As Framework.Usuarios.DN.PrincipalDN
            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            prin = bln.RecuperarGenerico("1", GetType(Framework.Usuarios.DN.PrincipalDN))

            oprOrg.SujetoOperacion = prin
            oprOrg.ObjetoIndirectoOperacion = presu
            oprOrg.ObjetoDirectoOperacion = presu

            oprDes.SujetoOperacion = prin
            oprDes.ObjetoIndirectoOperacion = presu
            oprDes.ObjetoDirectoOperacion = presu

            Dim pr As New Framework.Procesos.ProcesosLN.OperacionesLN()
            Dim colTRA As New Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN()
            colTRA = pr.RecuperarTransicionesAutorizadasSobre(prin, polprhe, Framework.Procesos.ProcesosDN.TipoTransicionDN.InicioDesde)

            Dim trr As Framework.Procesos.ProcesosDN.TransicionRealizadaDN
            trr = colTRA.Item(0)

            ' trr.OperacionRealizadaOrigen = oprDes
            trr.OperacionRealizadaOrigen = oprOrg
            trr.OperacionRealizadaOrigen.ObjetoIndirectoOperacion = presu


            Dim ejOPR As New Framework.Procesos.ProcesosLN.GestorOPRLN()
            ejOPR.EjecutarOperacion(presu, Nothing, prin, trr)




            tr.Confirmar()



            CrearPolizaEnGrafoDesdePresupuestop = trr.OperacionRealizadaOrigen.ObjetoIndirectoOperacion


        End Using





    End Function

    Public Function CrearPresupuestoP() As FN.Seguros.Polizas.DN.PresupuestoDN
        Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN

        Using tr As New Transaccion()
            Dim rvLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
            rvLN.CargarGrafoTarificacion()

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Framework.Configuracion.AppConfiguracion.DatosConfig.Item("PeriodoValidezPresupuestoAMD") = "0/1/0"

            Dim adaptadorLN As New GSAMV.LN.AdaptadorCuestionarioLN()
            presupuesto = adaptadorLN.GenerarPresupuestoxCuestionarioRes(CrearCuestionarioRes)

            tr.Confirmar()

            Return presupuesto
        End Using

    End Function

    Private Function CrearCuestionarioRes() As CuestionarioResueltoDN
        Dim cuestionarioRes As CuestionarioResueltoDN
        Dim colCaracteristicas As ColCaracteristicaDN
        Dim bln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN



        Using tr As New Transaccion()

            cuestionarioRes = New CuestionarioResueltoDN

            If cuestionarioRes.ColRespuestaDN Is Nothing Then
                cuestionarioRes.ColRespuestaDN = New ColRespuestaDN()
            End If

            If cuestionarioRes.CuestionarioDN Is Nothing Then
                bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                cuestionarioRes.CuestionarioDN = CType(bln.Recuperar(GetType(CuestionarioDN), "1"), CuestionarioDN)
            End If

            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colCaracteristicas = New ColCaracteristicaDN()
            Dim listaC As System.Collections.IList = bln.RecuperarLista(GetType(CaracteristicaDN))
            For Each caract As CaracteristicaDN In listaC
                colCaracteristicas.Add(caract)
            Next

            'recuperamos la característica y la respuesta correpondiente a cada pregunta 
            'para formar las preguntas

            Dim fEfecto As Date = Now()
            responder(cuestionarioRes, colCaracteristicas, "FechaEfecto", New ValorCaracteristicaFechaDN(), fEfecto, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "CodigoConcesionario", New ValorTextoCaracteristicaDN(), 0, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "CodigoVendedor", New ValorTextoCaracteristicaDN(), 0, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "EsCliente", New ValorBooleanoCaracterisitcaDN(), False, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "IDCliente", New ValorTextoCaracteristicaDN(), 0, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "Nombre", New ValorTextoCaracteristicaDN(), "Rodrigo", fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "Apellido1", New ValorTextoCaracteristicaDN(), "Vallejo", fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "Apellido2", New ValorTextoCaracteristicaDN(), "Cifuentes", fEfecto)
            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            responder(cuestionarioRes, colCaracteristicas, "Sexo", New FN.RiesgosVehiculos.DN.ValorSexoCaracteristicaDN(), bln.Recuperar(GetType(FN.Personas.DN.TipoSexo), "2"), fEfecto)
            Dim fNac As Date = New Date(1981, 10, 26)
            responder(cuestionarioRes, colCaracteristicas, "FechaNacimiento", New ValorCaracteristicaFechaDN(), fNac, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "EDAD", New ValorNumericoCaracteristicaDN(), Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias.CalcularDirAMD(Now(), fNac).Anyos, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "Telefono", New ValorTextoCaracteristicaDN(), "915690068", fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "Fax", New ValorTextoCaracteristicaDN(), "", fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "Email", New ValorTextoCaracteristicaDN(), "rvallejo@empresa.com", fEfecto)
            Dim dir As New FN.Localizaciones.DN.DireccionNoUnicaDN()
            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            dir.TipoVia = CType(bln.Recuperar(GetType(FN.Localizaciones.DN.TipoViaDN), "1"), FN.Localizaciones.DN.TipoViaDN)
            dir.Localidad = CType(bln.Recuperar(GetType(FN.Localizaciones.DN.LocalidadDN), "1"), FN.Localizaciones.DN.LocalidadDN)
            dir.Via = "Via Mayor"
            dir.CodPostal = dir.Localidad.ColCodigoPostal.Item(0).Nombre
            responder(cuestionarioRes, colCaracteristicas, "DireccionEnvio", New FN.RiesgosVehiculos.DN.ValorDireccionNoUnicaCaracteristicaDN(), dir, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "ZONA", New ValorNumericoCaracteristicaDN(), dir.Localidad.ColCodigoPostal.Item(0).Nombre, fEfecto)
            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            responder(cuestionarioRes, colCaracteristicas, "Circulacion-Localidad", New FN.RiesgosVehiculos.DN.ValorLocalidadCaracteristicaDN, dir.Localidad, fEfecto)
            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim modelo As FN.RiesgosVehiculos.DN.ModeloDN
            modelo = CType(bln.Recuperar(GetType(FN.RiesgosVehiculos.DN.ModeloDN), "1"), FN.RiesgosVehiculos.DN.ModeloDN)
            responder(cuestionarioRes, colCaracteristicas, "Modelo", New FN.RiesgosVehiculos.DN.ValorModeloCaracteristicaDN(), modelo, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "Marca", New FN.RiesgosVehiculos.DN.ValorMarcaCaracterisitcaDN(), modelo.Marca, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "CYLD", New ValorNumericoCaracteristicaDN(), 150, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "EstaMatriculado", New ValorBooleanoCaracterisitcaDN(), True, fEfecto)
            Dim fMat As New Date(2007, 1, 1)
            responder(cuestionarioRes, colCaracteristicas, "FechaMatriculacion", New ValorCaracteristicaFechaDN(), fMat, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "ANTG", New ValorNumericoCaracteristicaDN(), Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias.CalcularDirAMD(Now(), fMat).Anyos, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "FechaFabricacion", New ValorCaracteristicaFechaDN(), New Date(2007, 1, 1), fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "TieneCarnet", New ValorBooleanoCaracterisitcaDN(), True, fEfecto)
            Dim fCarn As New Date(2000, 1, 1)
            responder(cuestionarioRes, colCaracteristicas, "FechaCarnet", New ValorCaracteristicaFechaDN, fCarn, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "CARN", New ValorNumericoCaracteristicaDN, Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias.CalcularDirAMD(Now(), fCarn).Anyos, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "TipoCarnet", New ValorNumericoCaracteristicaDN, FN.RiesgosVehiculos.DN.TipoCarnet.A, fEfecto)

            'Para crear MCND hay que descomentar esta parte
            'Dim valConductoresAdC As New FN.RiesgosVehiculos.DN.ValorMCNDCaracteristicaDN()
            'responder(cuestionarioRes, colCaracteristicas, "MCND", New ValorNumericoCaracteristicaDN(), 0)
            'responder(cuestionarioRes, colCaracteristicas, "coleccionMCND", New FN.RiesgosVehiculos.DN.ValorMCNDCaracteristicaDN(), Nothing)
            'responder(cuestionarioRes, colCaracteristicas, "ConductoresAdicionalesConCarnet", New ValorBooleanoCaracterisitcaDN(), False)

            responder(cuestionarioRes, colCaracteristicas, "SiniestroResponsable3años", New ValorNumericoCaracteristicaDN(), 0, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "SiniestroSinResponsabilidad3años", New ValorNumericoCaracteristicaDN(), 0, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "RetiradaCarnet3años", New ValorBooleanoCaracterisitcaDN(), False, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "ConduccionEbrio3años", New ValorBooleanoCaracterisitcaDN(), False, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "VehículoTransporteRemunerado", New ValorBooleanoCaracterisitcaDN(), False, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "CanceladoSeguro3años", New ValorBooleanoCaracterisitcaDN(), False, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "PermisoCirculacionEspañol", New ValorBooleanoCaracterisitcaDN(), True, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "TitularPermisoCirculación", New ValorBooleanoCaracterisitcaDN(), True, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "AseguradoActualmente", New ValorBooleanoCaracterisitcaDN(), False, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "VencimientoSeguroActual", New ValorCaracteristicaFechaDN(), Now(), fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "AñosSinSiniestro", New ValorNumericoCaracteristicaDN, 2, fEfecto)
            responder(cuestionarioRes, colCaracteristicas, "Justificantes", New ValorNumericoCaracteristicaDN, FN.RiesgosVehiculos.DN.Justificantes.ninguno, fEfecto)

            tr.Confirmar()

            Return cuestionarioRes

        End Using

    End Function

    Private Sub CrearPagosTarifaP()
        Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN
        Dim bln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using tr As New Transaccion()
            CrearPresupuestoP()

            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim lista As System.Collections.IList
            lista = bln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PresupuestoDN))
            If lista Is Nothing Then
                Throw New ApplicationException("No se ha podido generar el presupuesto con su tarifa")
            End If
            presupuesto = CType(lista.Item(lista.Count - 1), FN.Seguros.Polizas.DN.PresupuestoDN)

            bln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim listaTF As System.Collections.IList
            listaTF = bln.RecuperarLista(GetType(FN.GestionPagos.DN.FraccionamientoDN))

            Dim gf As New FN.GestionPagos.DN.GrupoFraccionamientosDN()
            Dim gpf As FN.GestionPagos.DN.GrupoPagosFraccionadosDN
            Dim pf As FN.GestionPagos.DN.PagoFraccionadoDN
            For Each tf As FN.GestionPagos.DN.FraccionamientoDN In listaTF
                gpf = New FN.GestionPagos.DN.GrupoPagosFraccionadosDN()
                gpf.TipoFraccionamiento = tf

                For i As Integer = 1 To tf.NumeroPagos
                    pf = New FN.GestionPagos.DN.PagoFraccionadoDN()
                    pf.Importe = presupuesto.Tarifa.Importe / tf.NumeroPagos
                    pf.NumOrdenPago = i + 1

                    gpf.ColPagoFraccionadoDN.Add(pf)
                Next

                gf.ColGrupoPagosF.Add(gpf)
            Next

            presupuesto.Tarifa.GrupoFraccionamientos = gf

            bln.GuardarGenerico(presupuesto)


            Dim presupuestoP As FN.Seguros.Polizas.DN.PresupuestoDN
            presupuestoP = bln.RecuperarGenerico(presupuesto.ID, GetType(FN.Seguros.Polizas.DN.PresupuestoDN))

            Dim gfR As FN.GestionPagos.DN.GrupoFraccionamientosDN
            gfR = presupuestoP.Tarifa.GrupoFraccionamientos()

            tr.Confirmar()

        End Using
    End Sub

    Public Sub responder(ByVal cuestionarioRes As CuestionarioResueltoDN, ByVal colCaract As ColCaracteristicaDN, ByVal nombre As String, ByVal valor As IValorCaracteristicaDN, ByVal valorAsignar As Object, ByVal fechaEfecto As Date)
        Dim pregunta As PreguntaDN = Nothing
        Dim respuesta As RespuestaDN = Nothing
        Dim caracteristica As CaracteristicaDN = colCaract.RecuperarPrimeroXNombre(nombre)
        If Not cuestionarioRes.ColRespuestaDN Is Nothing Then
            respuesta = cuestionarioRes.ColRespuestaDN.RecuperarxCaracteristica(caracteristica)
        End If
        If respuesta Is Nothing Then
            respuesta = New RespuestaDN()
            pregunta = cuestionarioRes.CuestionarioDN.ColPreguntaDN.RecuperarPrimeroXNombre(nombre)
            respuesta.PreguntaDN = pregunta
        End If
        If respuesta.IValorCaracteristicaDN Is Nothing Then
            respuesta.IValorCaracteristicaDN = valor
        End If
        respuesta.IValorCaracteristicaDN.Valor = valorAsignar
        respuesta.IValorCaracteristicaDN.Caracteristica = caracteristica
        respuesta.IValorCaracteristicaDN.FechaEfectoValor = fechaEfecto
        If Not cuestionarioRes.ColRespuestaDN.Contains(respuesta) Then
            cuestionarioRes.ColRespuestaDN.Add(respuesta)
        End If
    End Sub

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GSAMV.AD.GestorMapPersistenciaCamposGSAMV()

    End Sub

End Class


Public Class prueba

    Public a As Integer
    Public b As String

End Class