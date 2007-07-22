Imports Framework.Cuestionario.CuestionarioDN

Imports FN.Seguros.Polizas.DN
Imports FN.RiesgosVehiculos.DN

Public Class RiesgosVehículosAS
    Inherits Framework.AS.BaseAS

    Public Sub DesCargarGrafoTarificacion()
        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        servicio.DesCargarGrafoTarificacion()

    End Sub

    Public Sub CargarGrafoTarificacion()
        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        servicio.CargarGrafoTarificacion()

    End Sub

    Public Function RecuperarModelosPorMarca(ByVal pMarca As FN.RiesgosVehiculos.DN.MarcaDN) As List(Of FN.RiesgosVehiculos.DN.ModeloDN)
        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim respuesta As Byte() = servicio.RecuperarModelosPorMarca(Framework.Utilidades.Serializador.Serializar(pMarca))
        Return Framework.Utilidades.Serializador.DesSerializar(respuesta)
    End Function

    Public Function ExisteModeloDatos(ByVal nombreModelo As String, ByVal nombreMarca As String, ByVal estadoMatriculacion As Boolean, ByVal fecha As Date) As Boolean
        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return servicio.ExisteModeloDatos(nombreModelo, nombreMarca, estadoMatriculacion, fecha)
    End Function

    Public Function TarificarPresupuesto(ByVal presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.PresupuestoDN
        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        Dim paquete As Byte()
        Dim respuesta As Byte()
        Dim presResp As FN.Seguros.Polizas.DN.PresupuestoDN

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paquete = Framework.Utilidades.Serializador.Serializar(presupuesto)
        respuesta = servicio.TarificarPresupuesto(paquete)

        presResp = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return presResp

    End Function

    Public Function TarificarTarifa(ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN) As FN.Seguros.Polizas.DN.TarifaDN
        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        Dim paquete As Byte()
        Dim respuesta As Byte()
        Dim presResp As FN.Seguros.Polizas.DN.TarifaDN

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paquete = Framework.Utilidades.Serializador.Serializar(pTarifa)
        respuesta = servicio.TarificarTarifa(paquete)

        presResp = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return presResp

    End Function

    Public Function VerificarDatosPresupuesto(ByVal presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.PresupuestoDN
        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        Dim paquete As Byte()
        Dim respuesta As Byte()
        Dim presResp As FN.Seguros.Polizas.DN.PresupuestoDN

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paquete = Framework.Utilidades.Serializador.Serializar(presupuesto)
        respuesta = servicio.VerificarDatosPresupuesto(paquete)

        presResp = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return presResp

    End Function

    Public Function RecuperarRiesgoMotor(ByVal pMatricula As String, ByVal pNumeroBastidor As String) As FN.RiesgosVehiculos.DN.RiesgoMotorDN

        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        '  Dim paquete As Byte()
        Dim respuesta As Byte()
        Dim presResp As FN.RiesgosVehiculos.DN.RiesgoMotorDN

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        '  paquete = Framework.Utilidades.Serializador.Serializar(presupuesto)
        respuesta = servicio.RecuperarRiesgoMotor(pMatricula, pNumeroBastidor)

        presResp = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return presResp

    End Function

    Public Sub ModificarPoliza(ByVal periodoR As PeriodoRenovacionPolizaDN, ByVal tarifa As TarifaDN, ByVal cuestionarioR As CuestionarioResueltoDN, ByVal fechaInicioPC As Date)

        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        Dim periodoRP As Byte()
        Dim tarifaP As Byte()
        Dim cuestionarioRP As Byte()

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        periodoRP = Framework.Utilidades.Serializador.Serializar(periodoR)
        tarifaP = Framework.Utilidades.Serializador.Serializar(tarifa)
        cuestionarioRP = Framework.Utilidades.Serializador.Serializar(cuestionarioR)

        servicio.ModificarPoliza(periodoRP, tarifaP, cuestionarioRP, fechaInicioPC)

    End Sub

    Public Function RecuperarModeloDatos(ByVal nombreModelo As String, ByVal nombreMarca As String, ByVal matriculado As Boolean, ByVal fecha As Date) As ModeloDatosDN

        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        Dim respuesta As Byte()
        Dim modeloDatos As ModeloDatosDN

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        respuesta = servicio.RecuperarModeloDatos(nombreModelo, nombreMarca, matriculado, fecha)
        modeloDatos = Framework.Utilidades.Serializador.DesSerializar(respuesta)

        Return modeloDatos

    End Function

    Public Function RecuperarProductosModelo(ByVal modelo As ModeloDN, ByVal matriculado As Boolean, ByVal fecha As Date) As ColProductoDN
        Dim servicio As New RiesgosVehiculosWS.RiesgosVehiculosWS()
        Dim respuesta As Byte()
        Dim modeloByte As Byte()

        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        modeloByte = Framework.Utilidades.Serializador.Serializar(modelo)

        respuesta = servicio.RecuperarProductosModelo(modeloByte, matriculado, fecha)
        RecuperarProductosModelo = Framework.Utilidades.Serializador.DesSerializar(respuesta)

    End Function

End Class
