Imports Framework.LogicaNegocios.Transacciones

Imports AmvDocumentosDN

Public Class OperadorLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Constructores"

    Public Sub New(ByVal tl As ITransaccionLogicaLN, ByVal rec As IRecursoLN)
        MyBase.New(tl, rec)
    End Sub

#End Region

#Region "Métodos"

    Public Sub GuardarOperador(ByVal operador As OperadorDN)
        MyBase.Guardar(Of OperadorDN)(operador)
    End Sub

    Public Function RecuperarListaOperador() As IList(Of OperadorDN)
        Return MyBase.RecuperarLista(Of OperadorDN)()
    End Function

    Public Function RecuperarOperador(ByVal id As String) As OperadorDN
        Return MyBase.Recuperar(Of OperadorDN)(id)
    End Function

    Public Sub BajaOperador(ByVal idOperador As String)
        Dim tlproc As Framework.LogicaNegocios.Transacciones.ITransaccionLogicaLN = Nothing

        Try
            tlproc = ObtenerTransaccionDeProceso()

            MyBase.Baja(Of OperadorDN)(idOperador)

            'Hay que dar de baja a todos los usuarios que refieran al operador
            Dim lnUsr As New Framework.Usuarios.LN.UsuariosLN(mTL, mRec)
            lnUsr.BajaxIdEntidadUser(idOperador)

            tlproc.Confirmar()

        Catch ex As Exception
            tlproc.Cancelar()
            Throw ex
        End Try

    End Sub

    Public Sub ReactivarOperador(ByVal idOperador As String)
        MyBase.Reactivar(Of OperadorDN)(idOperador)
    End Sub

#End Region

End Class
