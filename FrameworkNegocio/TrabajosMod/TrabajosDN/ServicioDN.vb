Imports Framework.DatosNegocio

<Serializable()> _
Public Class ServicioDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mTipoServicio As TipoServicioDN

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mTipoServicio")> _
    Public Property TipoServicio() As TipoServicioDN
        Get
            Return mTipoServicio
        End Get
        Set(ByVal value As TipoServicioDN)
            CambiarValorRef(Of TipoServicioDN)(value, mTipoServicio)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Overrides Function ToString() As String
        Dim cadena As String = ""

        If mTipoServicio IsNot Nothing Then
            cadena = mTipoServicio.ToString()
        End If

        cadena = cadena & ", " & mNombre

        Return cadena
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mTipoServicio Is Nothing Then
            pMensaje = "El servicio debe tener un tipo de servicio válido"
            Return EstadoIntegridadDN.Inconsistente
        End If

        Me.mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class


<Serializable()> _
Public Class ColServicioDN
    Inherits ArrayListValidable(Of ServicioDN)

End Class