
<Serializable()> Public Class PlantillaCartaDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

    Protected mHuellaRTF As HuellaContenedorRTFDN

#End Region


#Region "Propiedades"

    Public Property HuellaRTF() As HuellaContenedorRTFDN
        Get
            Return Me.mHuellaRTF
        End Get
        Set(ByVal value As HuellaContenedorRTFDN)
            Me.CambiarValorRef(value, Me.mHuellaRTF)
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValidarNombrePlantillas(ByRef mensaje As String, ByVal nombrePlantilla As String) As Boolean
        If String.IsNullOrEmpty(nombrePlantilla) Then
            mensaje = "El nombre de la plantilla no puede ser nulo"
            Return False
        End If

        Return True
    End Function

    'Private Function ValidarTextoRTF(ByRef mensaje As String, ByVal textoRTF As String) As Boolean
    '    If String.IsNullOrEmpty(textoRTF) Then
    '        mensaje = "El texto de la plantilla no puede ser nulo"
    '        Return False
    '    End If

    '    Return True
    'End Function

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not ValidarNombrePlantillas(pMensaje, mNombre) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        'If Not ValidarTextoRTF(pMensaje, mTextoRTF) Then
        '    Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        'End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class
