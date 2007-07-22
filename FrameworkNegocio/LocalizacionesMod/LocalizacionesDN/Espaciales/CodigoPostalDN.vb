Imports Framework.DatosNegocio

<Serializable()> _
Public Class CodigoPostalDN
    Inherits EntidadDN

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal codPostal As String)
        CambiarValorVal(Of String)(codPostal, mNombre)
        modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Validaciones"

    Private Function ValCodPostal(ByRef mensaje As String, ByVal codPostal As String) As Boolean
        If String.IsNullOrEmpty(codPostal) Then
            mensaje = "El código postal no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValCodPostal(pMensaje, mNombre) Then
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class


<Serializable()> _
Public Class ColCodigoPostalDN
    Inherits ArrayListValidable(Of CodigoPostalDN)

End Class