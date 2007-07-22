#Region "Importaciones"

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.LN

#End Region


Public Class UsuarioFS
    Inherits Framework.FachadaLogica.BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "Métodos"

    Public Function IniciarSesion(ByVal di As DatosIdentidadDN, ByVal idSesion As String) As PrincipalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()
        Dim actor As PrincipalDN = Nothing

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario: En este caso no se verifica, pues todavía
            '   no hay un principal logado

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            actor = miLN.ObtenerPrincipal(di)
            IniciarSesion = actor
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try

    End Function

    Public Function ObtenerPrincipal(ByVal di As DatosIdentidadDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As PrincipalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            'actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            ObtenerPrincipal = miLN.ObtenerPrincipal(di)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try

    End Function

    Public Function ObtenerPrincipal(ByVal id As String, ByVal actor As PrincipalDN, ByVal idSesion As String) As PrincipalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            ObtenerPrincipal = miLN.ObtenerPrincipal(id)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try

    End Function

    Public Function RecuperarListadoUsuarios(ByVal actor As PrincipalDN, ByVal idSesion As String) As DataSet
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            RecuperarListadoUsuarios = miLN.RecuperarListado()
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try

    End Function


    Public Function GuardarPrincipal(ByVal principal As PrincipalDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As PrincipalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            GuardarPrincipal = miLN.GuardarPrincipal(principal)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function


    Public Function GuardarPrincipal(ByVal principal As PrincipalDN, ByVal di As DatosIdentidadDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As PrincipalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            GuardarPrincipal = miLN.GuardarPrincipal(principal, di)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function

    Public Function BajaPrincipal(ByVal principal As PrincipalDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As PrincipalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            BajaPrincipal = miLN.BajaPrincipal(principal)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function

    Public Function AltaPrincipal(ByVal principal As PrincipalDN, ByVal datosIdentidad As DatosIdentidadDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As PrincipalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try

            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            AltaPrincipal = miLN.AltaPrincipal(principal, datosIdentidad)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function

    Public Function RecuperarPrincipalxEntidadUser(ByVal actor As PrincipalDN, ByVal idSesion As String, ByVal tipoEnt As System.Type, ByVal idEntidad As String) As PrincipalDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As UsuariosLN
            miLN = New UsuariosLN(mTL, mRec)
            RecuperarPrincipalxEntidadUser = miLN.RecuperarPrincipalxEntidadUser(tipoEnt, idEntidad)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function

#End Region

End Class
