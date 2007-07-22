Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales
Imports FN.RiesgosVehiculos.DN

Public Class CuestionarioAS
    Inherits Framework.AS.BaseAS

    Public Function GenerarPresupuestoxCuestionarioRes(ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN) As FN.Seguros.Polizas.DN.PresupuestoDN
        Dim servicio As New GSAMVWS.GSAMVWS()
        Dim paquete As Byte()
        Dim respuesta As Byte()
        Dim pr As FN.Seguros.Polizas.DN.PresupuestoDN

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paquete = Framework.Utilidades.Serializador.Serializar(cuestionarioR)
        respuesta = servicio.GenerarPresupuestoxCuestionarioRes(paquete)

        pr = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return pr

    End Function

    Public Function GenerarTarifaxCuestionarioRes(ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal tiempoTarificado As AnyosMesesDias) As FN.Seguros.Polizas.DN.TarifaDN
        Dim servicio As New GSAMVWS.GSAMVWS()
        Dim paqueteP As Byte()
        Dim paqueteT As Byte()
        Dim respuesta As Byte()
        Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteP = Framework.Utilidades.Serializador.Serializar(cuestionarioR)
        paqueteT = Framework.Utilidades.Serializador.Serializar(tiempoTarificado)

        respuesta = servicio.GenerarTarifaxCuestionarioRes(paqueteP, paqueteT)

        tarifa = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return tarifa

    End Function

    Public Sub RecuperarRequisitosTarificar(ByVal marca As String, ByVal modelo As String, ByVal cilindrada As Integer, ByRef minEdad As Integer, ByRef minCarnet As Integer, ByRef admiteNoMatric As Boolean, ByRef tiposAnyosCarnet As IList(Of TipoCarnet))

    End Sub

    Public Function AdmiteMCND(ByVal marca As String, ByVal modelo As String, ByVal cilindrada As String, ByVal edad As Integer, ByVal anyosCarnet As Integer, ByVal matriculada As Boolean, ByVal tipoCarnet As TipoCarnet) As Boolean

    End Function

End Class
