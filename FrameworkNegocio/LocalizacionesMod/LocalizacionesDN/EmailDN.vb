Imports Framework.DatosNegocio

<Serializable()> _
Public Class EmailDN
    Inherits EntidadDN
    Implements IDatoContactoDN

#Region "Atributos"
    Protected mValorMail As String
    Protected mComentario As String
#End Region

#Region "Propiedades"

    Public Property tipo() As String Implements IDatoContactoDN.Tipo
        Get
            Return Me.GetType.ToString()
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Property Valor() As String Implements IDatoContactoDN.Valor
        Get
            Return mValorMail
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mValorMail)
        End Set
    End Property

    Public Property Comentario() As String Implements IDatoContactoDN.Comentario
        Get
            Return mComentario
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mComentario)
        End Set
    End Property

#End Region

#Region "Métodos de validación"

    Private Function ValidarValor(ByRef mensaje As String, ByVal valorMail As String) As Boolean
        If Not IsMail(valorMail) Then
            mensaje = "Correo no válido"
            Return False
        End If

        Return True

    End Function

#End Region

#Region "Métodos"

    Public Shared Function IsMail(ByVal address As String) As Boolean
        Dim strRegex As String

        If String.IsNullOrEmpty(address) Then
            Return False
        End If

        strRegex = "^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}" & _
                  "\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\" & _
                  ".)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"

        Dim re As New System.Text.RegularExpressions.Regex(strRegex)

        If re.IsMatch(address) Then
            Return True
        Else
            Return False
        End If

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarValor(pMensaje, mValorMail) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region


    
End Class

<Serializable()> _
Public Class ColEmailDN
    Inherits ArrayListValidable(Of EmailDN)

End Class