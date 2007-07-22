Imports Framework.DatosNegocio

<Serializable()> _
Public Class MotivoAnulacionDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mTipoAnulacion As TipoAnulacionDN
    Protected mDescripcion As String

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mTipoAnulacion")> _
    Public Property TipoAnulacion() As TipoAnulacionDN
        Get
            Return mTipoAnulacion
        End Get
        Set(ByVal value As TipoAnulacionDN)
            CambiarValorRef(Of TipoAnulacionDN)(value, mTipoAnulacion)
        End Set
    End Property

    Public Property Descripcion() As String
        Get
            Return mDescripcion
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mDescripcion)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String = String.Empty

        If mTipoAnulacion IsNot Nothing Then
            cadena = mTipoAnulacion.ToString()
        End If

        cadena &= " - " & mDescripcion

        Return cadena
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mTipoAnulacion Is Nothing Then
            pMensaje = "El tipo de anulación no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class


<Serializable()> _
Public Class TipoAnulacionDN
    Inherits EntidadDN

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If String.IsNullOrEmpty(mNombre) Then
            pMensaje = "El nombre del tipo de anulación no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

End Class