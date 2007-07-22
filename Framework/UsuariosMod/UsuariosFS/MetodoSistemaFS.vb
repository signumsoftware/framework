Imports Framework.LogicaNegocios.Transacciones
Imports Framework.FachadaLogica

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.LN

Public Class MetodoSistemaFS
    Inherits Framework.FachadaLogica.BaseFachadaFL

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As Framework.LogicaNegocios.Transacciones.IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "Métodos"

    Public Function RecuperarMetodos(ByVal actor As PrincipalDN, ByVal idSesion As String) As IList(Of MetodoSistemaDN)
        Dim mfh As MetodoFachadaHelper = New MetodoFachadaHelper()

        Using New CajonHiloLN(mRec)
            Try
                '1º guardar log de inicio
                mfh.EntradaMetodo(idSesion, actor, mRec)

                '2º verificacion de permisos por rol de usuario
                actor.Autorizado()

                '-----------------------------------------------------------------------------
                '3º creacion de la ln y ejecucion del metodo
                Dim miLN As MetodoSistemaLN
                miLN = New MetodoSistemaLN()
                RecuperarMetodos = miLN.RecuperarMetodos()
                '-----------------------------------------------------------------------------

                '4º guardar log de fin de metodo , con salidas excepcionales incluidas
                mfh.SalidaMetodo(idSesion, actor, mRec)
            Catch ex As Exception
                mfh.SalidaMetodoExcepcional(idSesion, actor, ex, "", mRec)
                Throw
            End Try

        End Using

    End Function

#End Region

End Class
