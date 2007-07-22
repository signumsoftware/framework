#Region "Importaciones"
Imports Framework.DatosNegocio
#End Region

''' <summary>
''' FALTA: Validación de los números de teléfono
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class TelefonoDN
    Inherits EntidadDN
    Implements IDatoContactoDN

#Region "Atributos"

    Protected mNumTelefono As String
    Protected mComentario As String

#End Region

#Region "Propiedades"

    Public Property tipo() As String Implements IDatoContactoDN.Tipo
        Get
            Return Me.GetType().ToString()
        End Get
        Set(ByVal value As String)
            Throw New NotImplementedException()
        End Set
    End Property

    Public Property Valor() As String Implements IDatoContactoDN.Valor
        Get
            Return mNumTelefono
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mNumTelefono)
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


#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mNumTelefono = String.Empty Then
            pMensaje = ""
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class

<Serializable()> _
Public Class ColTelefonosDN
    Inherits ArrayListValidable(Of TelefonoDN)

    ' metodos de coleccion
    '
End Class
