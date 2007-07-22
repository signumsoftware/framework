Imports Framework.AS
Imports FN.Empresas.DN

Public Class EmpresaAS
    Inherits Framework.AS.BaseAS

#Region "Métodos"

    Public Function GuardarEmpleadoYPuestosR(ByVal control As Object, ByVal empPuestoR As EmpleadoYPuestosRDN, ByVal vincMetodo As Object) As EmpleadoYPuestosRDN
        Dim servicio As EmpresasWS.EmpresasWS
        Dim paqueteRespuesta As Byte()
        Dim paqueteEntrada As Byte()
        Dim respuesta As EmpleadoYPuestosRDN


        ' verifiacar estado intefridad
        Dim mensaje As String = ""
        If empPuestoR.EstadoIntegridad(mensaje) <> Framework.DatosNegocio.EstadoIntegridadDN.Consistente Then
            Throw New ApplicationException(mensaje)

        End If





        servicio = New EmpresasWS.EmpresasWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteEntrada = Framework.Utilidades.Serializador.Serializar(empPuestoR)
        paqueteRespuesta = servicio.GuardarEmpleadoYPuestosR(paqueteEntrada)

        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function RecuperarSedePrincipalxCIFEmpresa(ByVal cifNifEmpresa As String) As SedeEmpresaDN
        Dim servicio As EmpresasWS.EmpresasWS
        Dim paqueteRespuesta As Byte()
        Dim respuesta As SedeEmpresaDN

        servicio = New EmpresasWS.EmpresasWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteRespuesta = servicio.RecuperarSedePrincipalxCIFEmpresa(cifNifEmpresa)

        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

#End Region

End Class
