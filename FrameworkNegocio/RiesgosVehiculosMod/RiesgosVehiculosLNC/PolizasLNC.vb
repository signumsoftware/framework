Public Class RVPolizasLNC
    'Public Function AltaDePolizaDesdePresupuesto(ByVal presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN, ByVal pCifNifTomador As String, ByVal pMatriculaRiesgo As String) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN


    '    '' hay que solicitarlo a un as
    '    '' recuperar o crear el tomador
    '    'Dim toma As FN.Seguros.Polizas.DN.TomadorDN ' de mometo lo pone el usuario 1º busca por identificador fiscla y sino crea uno nuevo

    '    ''''''''''''''''''''''''''''''''''''''''''
    '    ' '' Vinculación del tomador
    '    '''''''''''''''''''''''''''''''''''''''''


    '    'Dim polas As New FN.Seguros.Polizas.AS.PolizasAS.PolizasAS
    '    'Dim tomador As FN.Seguros.Polizas.DN.TomadorDN = polas.RecuperarCrearTomador(pCifNifTomador)

    '    'If String.IsNullOrEmpty(tomador.ID) Then

    '    '    Dim mensaje As String
    '    '    If FN.Localizaciones.DN.NifDN.ValidaNif(pCifNifTomador, mensaje) Then

    '    '        Dim pef As FN.Personas.DN.PersonaFiscalDN = tomador.EntidadFiscalGenerica.IentidadFiscal
    '    '        Dim dt As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = presupuesto.Tarifa.DatosTarifa
    '    '        pef.Persona.Nombre = dt.HeCuestionarioResuelto.EntidadReferidaTipada.ColRespuestaDN.RecuperarRespuestaaxPregunta("Nombre").IValorCaracteristicaDN.Valor
    '    '        pef.Persona.Apellido = dt.HeCuestionarioResuelto.EntidadReferidaTipada.ColRespuestaDN.RecuperarRespuestaaxPregunta("Apellido").IValorCaracteristicaDN.Valor
    '    '        pef.Persona.Apellido2 = dt.HeCuestionarioResuelto.EntidadReferidaTipada.ColRespuestaDN.RecuperarRespuestaaxPregunta("Apellido2").IValorCaracteristicaDN.Valor
    '    '        pef.DomicilioFiscal = dt.HeCuestionarioResuelto.EntidadReferidaTipada.ColRespuestaDN.RecuperarRespuestaaxPregunta("DireccionEnvio").IValorCaracteristicaDN.Valor
    '    '        pef.Persona.FechaNacimiento = dt.HeCuestionarioResuelto.EntidadReferidaTipada.ColRespuestaDN.RecuperarRespuestaaxPregunta("FechaNacimiento").IValorCaracteristicaDN.Valor

    '    '    ElseIf FN.Localizaciones.DN.CifDN.ValidaCif(pCifNifTomador, mensaje) Then

    '    '        Throw New NotImplementedException

    '    '    End If

    '    'End If





    '    Dim polLnc As New FN.Seguros.Polizas.PolizasLNC.PolizasLNC
    '    ' inicialmente ponemos la fecha de inico del perido de validez del presupuesto
    '    AltaDePolizaDesdePresupuesto = polLnc.AltaDePoliza(tomador, presupuesto.Emisora, presupuesto.Tarifa, presupuesto.PeridoValidez.FInicio)

    '    'verificacion

    '    ' la fecha de alta del perido de renivación debe estar contenida en el perido de validad del presupuesto
    '    If Not presupuesto.PeridoValidez.Contiene(AltaDePolizaDesdePresupuesto.PeridoCoberturaActivo.FI) Then
    '        Throw New ApplicationException("la fecha de inicio del perido de renovación debe estar contenida en el perido de validez del presupuesto")
    '    End If



    'End Function


    Public Function VerificarDatosPresupuesto(ByVal presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.PresupuestoDN

        Dim mias As New RiesgosVehiculos.AS.RiesgosVehículosAS

        Return mias.VerificarDatosPresupuesto(presupuesto)

    End Function

    Public Function ClonarCuestionarioResuelto(ByVal datosT As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN) As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim cuestionarioActivo As Framework.Cuestionario.CuestionarioDN.CuestionarioDN

        Dim miAS As New Framework.AS.DatosBasicosAS()

        If CType(datosT, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN).HeCuestionarioResuelto.EntidadReferida Is Nothing Then
            cr = miAS.RecuperarGenerico(CType(datosT, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN).HeCuestionarioResuelto.IdEntidadReferida, GetType(Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN))
        Else
            cr = CType(datosT, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN).HeCuestionarioResuelto.EntidadReferida
        End If

        If cr Is Nothing Then
            Throw New ApplicationException("No se ha podido recuperar el cuestionario resuelto")
        End If

        Dim colCues As New Framework.Cuestionario.CuestionarioDN.ColCuestionarioDN()
        colCues.AddRangeObject(miAS.RecuperarListaTipos(GetType(Framework.Cuestionario.CuestionarioDN.CuestionarioDN)))
        cuestionarioActivo = colCues.RecuperarCuestionarioxFecha(Now())
        If cuestionarioActivo Is Nothing Then
            Throw New ApplicationException("No se ha podido recuperar el cuestionario activo actual")
        End If

        Return cr.ClonarCuestionarioRxC(colCues.RecuperarCuestionarioxFecha(Now()))

    End Function



End Class
