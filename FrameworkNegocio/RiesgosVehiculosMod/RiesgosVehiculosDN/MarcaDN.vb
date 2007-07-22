Imports Framework.DatosNegocio

<Serializable()> _
Public Class MarcaDN
    Inherits EntidadDN

    Public Overrides Function ToString() As String
        Return Me.Nombre
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mNombre = String.Empty Then
            pMensaje = "El nombre de la marca no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

End Class




<Serializable()> _
Public Class ColMarcaDN
    Inherits ArrayListValidable(Of MarcaDN)

End Class




