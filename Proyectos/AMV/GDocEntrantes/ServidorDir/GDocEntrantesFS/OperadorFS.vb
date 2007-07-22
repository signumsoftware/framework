Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica
Imports Framework.Usuarios.DN

Imports GDocEntrantesLN
Imports AmvDocumentosDN

Public Class OperadorFS
    Inherits BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal tl As ITransaccionLogicaLN, ByVal rec As IRecursoLN)
        MyBase.New(tl, rec)
    End Sub

#End Region

#Region "Métodos"

    Public Sub GuardarOperador(ByVal operador As OperadorDN, ByVal actor As PrincipalDN, ByVal idSesion As String)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As OperadorLN
            miLN = New OperadorLN(mTL, mRec)
            miLN.GuardarOperador(operador)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Sub

    Public Function RecuperarListaOperador(ByVal actor As PrincipalDN, ByVal idSesion As String) As IList(Of OperadorDN)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As OperadorLN
            miLN = New OperadorLN(mTL, mRec)
            RecuperarListaOperador = miLN.RecuperarListaOperador()
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function

    Public Function RecuperarOperador(ByVal id As String, ByVal actor As PrincipalDN, ByVal idSesion As String) As OperadorDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As OperadorLN
            miLN = New OperadorLN(mTL, mRec)
            RecuperarOperador = miLN.RecuperarOperador(id)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function

    Public Sub BajaOperador(ByVal idOperador As String, ByVal actor As PrincipalDN, ByVal idSesion As String)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As OperadorLN
            miLN = New OperadorLN(mTL, mRec)
            miLN.BajaOperador(idOperador)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Sub

    Public Sub ReactivarOperador(ByVal idOperador As String, ByVal actor As PrincipalDN, ByVal idSesion As String)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As OperadorLN
            miLN = New OperadorLN(mTL, mRec)
            miLN.ReactivarOperador(idOperador)
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Sub

#End Region

End Class
