#Region "Importaciones"

Imports Framework.Usuarios.DN

#End Region

Public Class UsuariosAS
    Inherits Framework.AS.BaseAS

#Region "Métodos"

#Region "Principal y usuarios"

    Public Function IniciarSesion(ByVal di As DatosIdentidadDN) As PrincipalDN
        Dim respuesta As PrincipalDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS
        Dim parametro As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        parametro = Framework.Utilidades.Serializador.Serializar(di)
        paqueteRespuesta = servicio.IniciarSesion(parametro)

        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function ObtenerPrincipal(ByVal pID As String) As PrincipalDN
        Dim respuesta As PrincipalDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteRespuesta = servicio.ObtenerPrincipalPorID(pID)
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function ObtenerPrincipal(ByVal di As DatosIdentidadDN) As PrincipalDN
        Dim respuesta As PrincipalDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteRespuesta = servicio.ObtenerPrincipal(Framework.Utilidades.Serializador.Serializar(di))

        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function RecuperarListadoUsuarios() As DataSet
        Dim servicio As UsrWS.UsuariosWS

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return servicio.RecuperarListadoUsuarios()

    End Function

    Public Function GuardarPrincipal(ByVal principal As PrincipalDN, ByVal di As DatosIdentidadDN) As PrincipalDN
        Dim respuesta As PrincipalDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS
        Dim miPrincipal As Byte()
        Dim miDI As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'Empaquetamos los parámetros
        miPrincipal = Framework.Utilidades.Serializador.Serializar(principal)
        miDI = Framework.Utilidades.Serializador.Serializar(di)

        paqueteRespuesta = servicio.GuardarPrincipal(miPrincipal, miDI)
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function AltaPrincipal(ByVal principal As PrincipalDN, ByVal di As DatosIdentidadDN) As PrincipalDN
        Dim respuesta As PrincipalDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS
        Dim miPrincipal As Byte()
        Dim miDI As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'Empaquetamos los parámetros
        miPrincipal = Framework.Utilidades.Serializador.Serializar(principal)
        miDI = Framework.Utilidades.Serializador.Serializar(di)

        paqueteRespuesta = servicio.AltaPrincipal(miPrincipal, miDI)
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function BajaPrincipal(ByVal principal As PrincipalDN) As PrincipalDN
        Dim respuesta As PrincipalDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS
        Dim miPrincipal As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'Empaquetamos los parámetros
        miPrincipal = Framework.Utilidades.Serializador.Serializar(principal)

        paqueteRespuesta = servicio.BajaPrincipal(miPrincipal)
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function GuardarPrincipal(ByVal principal As PrincipalDN) As PrincipalDN
        Dim respuesta As PrincipalDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS
        Dim miPrincipal As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        'Empaquetamos los parámetros
        miPrincipal = Framework.Utilidades.Serializador.Serializar(principal)

        paqueteRespuesta = servicio.GuardarPrincipalSinDI(miPrincipal)
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function RecuperarPrincipalxEntidadUser(ByVal tipoEnt As System.Type, ByVal idEntidad As String) As PrincipalDN
        Dim respuesta As PrincipalDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS
        Dim paqTipoEnt As Byte()

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqTipoEnt = Framework.Utilidades.Serializador.Serializar(tipoEnt)

        paqueteRespuesta = servicio.RecuperarPrincipalxEntidadUser(paqTipoEnt, idEntidad)
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

#End Region

#Region "Roles, casos de uso y métodos de sistema"

    Public Function RecuperarColRol() As ColRolDN
        Dim respuesta As ColRolDN
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteRespuesta = servicio.RecuperarColRoles()
        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function RecuperarListaCasosUso() As IList(Of CasosUsoDN)
        Dim respuesta As IList(Of CasosUsoDN)
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteRespuesta = servicio.RecuperarListaCasosUso()

        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function RecuperarMetodos() As IList(Of MetodoSistemaDN)
        Dim respuesta As IList(Of MetodoSistemaDN)
        Dim paqueteRespuesta As Byte()
        Dim servicio As UsrWS.UsuariosWS

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteRespuesta = servicio.RecuperarMetodos()

        respuesta = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return respuesta

    End Function

    Public Function GuardarRol(ByVal rol As RolDN) As RolDN
        Dim paqueteRespuesta As Byte()
        Dim paqueteParametro As Byte()
        Dim servicio As UsrWS.UsuariosWS

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteParametro = Framework.Utilidades.Serializador.Serializar(rol)
        paqueteRespuesta = servicio.GuardarRol(paqueteParametro)

        rol = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return rol

    End Function

    Public Function GuardarCasoUso(ByVal casoUso As CasosUsoDN) As CasosUsoDN
        Dim paqueteRespuesta As Byte()
        Dim paqueteParametro As Byte()
        Dim servicio As UsrWS.UsuariosWS

        ' crear y redirigir a la url del servicio
        servicio = New UsrWS.UsuariosWS()
        servicio.Url = RedireccionURL(servicio.Url)
        servicio.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        paqueteParametro = Framework.Utilidades.Serializador.Serializar(casoUso)
        paqueteRespuesta = servicio.GuardarCasoUso(paqueteParametro)

        casoUso = Framework.Utilidades.Serializador.DesSerializar(paqueteRespuesta)

        Return casoUso

    End Function

#End Region


#End Region

End Class
