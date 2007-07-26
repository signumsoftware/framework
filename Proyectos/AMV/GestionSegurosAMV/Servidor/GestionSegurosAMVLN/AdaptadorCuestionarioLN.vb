Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales
Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Cuestionario.CuestionarioDN

Imports FN.Seguros.Polizas.DN
Imports System

Public Class AdaptadorCuestionarioLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    Dim mColCaracteristicas As ColCaracteristicaDN

#Region "Métodos"

    Public Function GenerarPresupuestoxCuestionarioRes(ByVal cuestionarioR As CuestionarioResueltoDN) As PresupuestoDN
        Dim presupuesto As PresupuestoDN
        Dim futuroTomador As FuturoTomadorDN
        Dim emisoraP As EmisoraPolizasDN

        Using tr As New Transaccion()
            futuroTomador = GenerarFuturoTomadorxCuestionarioResuelto(cuestionarioR)


            'Datos de contacto del futuro tomador
            Dim dc As New FN.Localizaciones.DN.ContactoDN()
            Dim heFT As New HEDN(futuroTomador)
            dc.ColIHEntidad.Add(heFT)

            Dim tlfAux As String = RecuperarValorxPregunta(cuestionarioR, "Telefono")
            Dim faxAux As String = RecuperarValorxPregunta(cuestionarioR, "Fax")
            Dim mailAux As String = RecuperarValorxPregunta(cuestionarioR, "Email")

            If Not String.IsNullOrEmpty(tlfAux) Then
                Dim tlf As New FN.Localizaciones.DN.TelefonoDN()
                tlf.Valor = tlfAux
                tlf.Comentario = "Teléfono"
                dc.ColDatosContacto.Add(tlf)
            End If

            If Not String.IsNullOrEmpty(faxAux) Then
                Dim fax As New FN.Localizaciones.DN.TelefonoDN()
                fax.Valor = faxAux
                fax.Comentario = "Fax"
                dc.ColDatosContacto.Add(fax)
            End If

            If Not String.IsNullOrEmpty(mailAux) Then
                Dim mail As New FN.Localizaciones.DN.EmailDN()
                mail.Valor = mailAux
                mail.Comentario = "Mail"
                dc.ColDatosContacto.Add(mail)
            End If

            'Periodo validez
            Dim fFinPV As Date
            Dim amd As New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias()
            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item("PeriodoValidezPresupuestoAMD") IsNot Nothing Then
                Dim array As Array = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("PeriodoValidezPresupuestoAMD").ToString().Split("/")
                If array.Length = 3 Then
                    amd.Anyos = array(0)
                    amd.Meses = array(1)
                    amd.Dias = array(2)
                End If
            Else
                amd.Meses = 1
            End If

            'Se completan los datos del presupuesto
            presupuesto = New PresupuestoDN()
            presupuesto.Tarifa = Me.GenerarTarifaxCuestionarioRes(cuestionarioR, amd, futuroTomador, False)
            'presupuesto.Tarifa.DatosTarifa.ValorBonificacion = futuroTomador.ValorBonificacion

            'Se recupera la entidad emisora de la póliza
            emisoraP = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)
            presupuesto.Emisora = emisoraP
            presupuesto.FuturoTomador = futuroTomador

            fFinPV = presupuesto.Tarifa.FEfecto.AddDays(amd.Dias)
            fFinPV = fFinPV.AddMonths(amd.Meses)
            fFinPV = fFinPV.AddYears(amd.Anyos)
            presupuesto.PeridoValidez = New Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN(presupuesto.Tarifa.FEfecto().Date(), fFinPV.Date)
            presupuesto.CodColaborador = RecuperarValorxPregunta(cuestionarioR, "CodigoConcesionario")

            ' crear las condiciones de pago del presupuesto
            'presupuesto.CondicionesPago = New FN.GestionPagos.DN.CondicionesPagoDN

            'presupuesto.n()

            Dim rvLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
            presupuesto = rvLN.TarificarPresupuesto(presupuesto)

            Me.GuardarGenerico(dc)

            tr.Confirmar()

            Return presupuesto

        End Using

    End Function

    Public Function GenerarTarifaxCuestionarioRes(ByVal cuestionarioR As CuestionarioResueltoDN) As TarifaDN
        Dim tiempoTarificado As New AnyosMesesDias
        tiempoTarificado.Anyos = 1
        Return GenerarTarifaxCuestionarioRes(cuestionarioR, tiempoTarificado, Nothing, True)
    End Function


    Public Function GenerarTarifaxCuestionarioRes(ByVal cuestionarioR As CuestionarioResueltoDN, ByVal tiempoTarificado As AnyosMesesDias, ByVal tomador As FuturoTomadorDN, ByVal debeTarificar As Boolean) As TarifaDN
        Dim tarifa As TarifaDN
        Dim riesgo As FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Dim colLineaProductos As ColLineaProductoDN
        Dim datosTarifa As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN
        Dim fechaEfecto As Date

        Using tr As New Transaccion()

            If tomador Is Nothing Then
                tomador = GenerarFuturoTomadorxCuestionarioResuelto(cuestionarioR)
            End If

            'Datos de la tarifa
            riesgo = New FN.RiesgosVehiculos.DN.RiesgoMotorDN()
            riesgo.Cilindrada = RecuperarValorxPregunta(cuestionarioR, "CYLD")
            riesgo.Matriculado = RecuperarValorxPregunta(cuestionarioR, "EstaMatriculado")
            riesgo.FechaMatriculacion = RecuperarValorxPregunta(cuestionarioR, "FechaMatriculacion")
            riesgo.Modelo = CType(RecuperarValorxPregunta(cuestionarioR, "Modelo"), FN.RiesgosVehiculos.DN.ModeloDN)

            fechaEfecto = RecuperarValorxPregunta(cuestionarioR, "FechaEfecto")

            Dim rvLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
            riesgo.ModeloDatos = rvLN.RecuperarModeloDatos(riesgo.Modelo.Nombre, riesgo.Modelo.Marca.Nombre, riesgo.Matriculado, fechaEfecto)


            Dim colProductos As ColProductoDN = rvLN.RecuperarProductosModelo(riesgo.Modelo, riesgo.Matriculado, fechaEfecto)

            'colProductos = RecuperarLista(Of ProductoDN)()
            colLineaProductos = New ColLineaProductoDN()
            For Each prod As ProductoDN In colProductos
                Dim lineaProd As New LineaProductoDN()
                lineaProd.Producto = prod
                lineaProd.Ofertado = True
                colLineaProductos.Add(lineaProd)
            Next

            datosTarifa = New FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN()
            If CType(RecuperarValorxPregunta(cuestionarioR, "ConductoresAdicionalesConCarnet"), Boolean) = True Then
                Dim colCAD As FN.RiesgosVehiculos.DN.ColDatosMCND
                colCAD = RecuperarValorxPregunta(cuestionarioR, "ColConductoresAdicionales")
                datosTarifa.ColConductores = New FN.RiesgosVehiculos.DN.ColConductorDN()

                For Each dca As FN.RiesgosVehiculos.DN.DatosMCND In colCAD
                    Dim conductor As New FN.RiesgosVehiculos.DN.ConductorDN()
                    conductor.Persona = New FN.Personas.DN.PersonaDN(dca.Nombre, dca.Apellido1, dca.Apellido2, dca.FechaNacimiento, dca.NIF)
                    datosTarifa.ColConductores.Add(conductor)
                Next

                'TODO: habría que validar que la respuesta del MCND, EDAD y ANTG sean correctas

            End If
            datosTarifa.HeCuestionarioResuelto = New Framework.Cuestionario.CuestionarioDN.HeCuestionarioResueltoDN(cuestionarioR)

            tarifa = New TarifaDN()
            tarifa.ColLineaProducto = colLineaProductos

            tarifa.Riesgo = riesgo
            tarifa.DatosTarifa = datosTarifa
            tarifa.DatosTarifa.ValorBonificacion = tomador.ValorBonificacion
            tarifa.FEfecto = fechaEfecto
            tarifa.AMD = tiempoTarificado

            If debeTarificar Then
                rvLN = New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN()
                tarifa = rvLN.TarificarTarifa(tarifa, Nothing, Nothing, True, True)

                Me.GuardarGenerico(tarifa)
            End If


            tr.Confirmar()

            Return tarifa

        End Using
    End Function

    Public Function GenerarFuturoTomadorxCuestionarioResuelto(ByVal cuestionarioR As CuestionarioResueltoDN) As FuturoTomadorDN
        Dim futuroTomador As FuturoTomadorDN
        Dim anyosSinSiniestro As Integer

        Using tr As New Transaccion()
            'Datos de la persona - futuro tomador
            futuroTomador = New FuturoTomadorDN()
            futuroTomador.Nombre = RecuperarValorxPregunta(cuestionarioR, "Nombre")
            futuroTomador.Apellido1FuturoTomador = RecuperarValorxPregunta(cuestionarioR, "Apellido1")
            futuroTomador.Apellido2FuturoTomador = RecuperarValorxPregunta(cuestionarioR, "Apellido2")
            futuroTomador.NIFCIFFuturoTomador = RecuperarValorxPregunta(cuestionarioR, "NIF")
            futuroTomador.Periodo.FI = RecuperarValorxPregunta(cuestionarioR, "FechaNacimiento")
            futuroTomador.Direccion = RecuperarValorxPregunta(cuestionarioR, "DireccionEnvio")
            anyosSinSiniestro = RecuperarValorxPregunta(cuestionarioR, "AñosSinSiniestro")
            Dim justificante As FN.RiesgosVehiculos.DN.Justificantes = RecuperarValorxPregunta(cuestionarioR, "Justificantes")
            futuroTomador.ValorBonificacion = 1

            If justificante = FN.RiesgosVehiculos.DN.Justificantes.ninguno Then
                anyosSinSiniestro = 0
            ElseIf justificante = FN.RiesgosVehiculos.DN.Justificantes.certificado_y_recibo Then
                anyosSinSiniestro -= 1
            ElseIf justificante = FN.RiesgosVehiculos.DN.Justificantes.certificado Then
                anyosSinSiniestro -= 2
            End If

            For cont As Integer = 0 To anyosSinSiniestro - 1
                futuroTomador.ValorBonificacion *= 0.95
            Next

            tr.Confirmar()

            Return futuroTomador
        End Using

    End Function

    Public Function GenerarCuestionarioResxPoliza() As CuestionarioResueltoDN
        Throw New NotImplementedException("Este método está pendiente de desarrollar")
    End Function

    Private Function RecuperarValorxPregunta(ByVal cuestionarioR As CuestionarioResueltoDN, ByVal nombrePregunta As String) As Object
        Dim respuesta As RespuestaDN
        Dim valor As Object

        Using tr As New Transaccion()

            If mColCaracteristicas Is Nothing Then
                mColCaracteristicas = New ColCaracteristicaDN()
                mColCaracteristicas.AddRange(RecuperarLista(Of CaracteristicaDN)())
            End If

            respuesta = cuestionarioR.ColRespuestaDN.RecuperarxCaracteristica(mColCaracteristicas.RecuperarPrimeroXNombre(nombrePregunta))

            If respuesta Is Nothing Then
                valor = Nothing
            Else
                valor = respuesta.IValorCaracteristicaDN.Valor
            End If

            tr.Confirmar()

            Return valor

        End Using

    End Function

#End Region

End Class
