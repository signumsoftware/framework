Imports Framework.DatosNegocio
<Serializable()> Public Class TipoEntidadPruebaDN
    Inherits Framework.DatosNegocio.EntidadDN


#Region "Validaciones"

    Private Function ValidarNombreTipo(ByRef mensaje As String, ByVal nombreTipo As String) As Boolean
        If String.IsNullOrEmpty(nombreTipo) Then
            mensaje = "El nombre del tipo de entidad de origen no puede ser nulo"
            Return False
        End If

        Return True
    End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarNombreTipo(pMensaje, mNombre) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region
End Class




