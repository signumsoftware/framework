Public Class OperadorLNC

#Region "Atributos"
    Protected mSession As Hashtable
#End Region

#Region "Constructores"
    Public Sub New(ByVal pDatosMarco As Hashtable)
        mSession = pDatosMarco
    End Sub
#End Region

#Region "Métodos"

    Public Sub GuardarOperador(ByVal operador As AmvDocumentosDN.OperadorDN)
        Dim miAS As New ClienteAdminAS.OperadorAS()
        miAS.GuardarOperador(operador)
    End Sub

    Public Function RecuperarListaOperador() As IList(Of AmvDocumentosDN.OperadorDN)
        Dim miAS As New ClienteAdminAS.OperadorAS()
        Return miAS.RecuperarListaOperador()
    End Function

    Public Function RecuperarOperador(ByVal id As String) As AmvDocumentosDN.OperadorDN
        Dim miAS As New ClienteAdminAS.OperadorAS()
        Return miAS.RecuperarOperador(id)
    End Function

    Public Sub BajaOperador(ByVal idOperador As String)
        Dim miAS As New ClienteAdminAS.OperadorAS()
        miAS.BajaOperador(idOperador)
    End Sub

    Public Sub ReactivarOperador(ByVal idOperador As String)
        Dim miAS As New ClienteAdminAS.OperadorAS()
        miAS.ReactivarOperador(idOperador)
    End Sub

#End Region

End Class
