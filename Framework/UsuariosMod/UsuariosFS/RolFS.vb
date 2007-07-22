#Region "Importaciones"

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.LN

#End Region

Public Class RolFS
    Inherits Framework.FachadaLogica.BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "Métodos"

    Public Function RecuperarColRoles(ByVal actor As PrincipalDN, ByVal idSesion As String) As ColRolDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As RolLN
            miLN = New RolLN(mTL, mRec)
            RecuperarColRoles = miLN.RecuperarColRoles()
            '-----------------------------------------------------------------------------

            '4º guardar log de fin de metodo , con salidas excepcionales incluidas
            mfh.SalidaMetodo(idSesion, actor, mRec)

        Catch ex As Exception

            mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
            Throw ex

        End Try
    End Function

    Public Function GuardarRol(ByVal rol As RolDN, ByVal actor As PrincipalDN, ByVal idSesion As String) As RolDN
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Try
            '1º guardar log de inicio
            mfh.EntradaMetodo(idSesion, actor, mRec)

            '2º verificacion de permisos por rol de usuario
            actor.Autorizado()

            '-----------------------------------------------------------------------------
            '3º creacion de la ln y ejecucion del metodo

            Dim miLN As RolLN
            miLN = New RolLN(mTL, mRec)
            GuardarRol = miLN.GuardarRol(rol)
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
