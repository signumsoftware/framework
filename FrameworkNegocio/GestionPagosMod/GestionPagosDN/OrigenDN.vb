
<Serializable()> Public Class OrigenDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "Atributos"

    Protected mComentario As String
    Protected mIDEntidad As String
    Protected mTipoEntidadOrigen As TipoEntidadOrigenDN

#End Region

#Region "Constructores"

    'Public Sub New()
    '    MyBase.New()
    'End Sub

#End Region

#Region "Propiedades"

    Public ReadOnly Property TipoMasID() As String
        Get
            Dim salida As String = String.Empty

            If Not Me.mTipoEntidadOrigen Is Nothing Then
                salida = Me.mTipoEntidadOrigen.Nombre
            End If

            Return salida & " " & Me.mIDEntidad
        End Get
    End Property

    Public Property TipoEntidadOrigen() As TipoEntidadOrigenDN
        Get
            Return mTipoEntidadOrigen
        End Get
        Set(ByVal value As TipoEntidadOrigenDN)
            CambiarValorRef(Of TipoEntidadOrigenDN)(value, mTipoEntidadOrigen)
        End Set
    End Property

    Public Property IDEntidad() As String
        Get
            Return mIDEntidad
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mIDEntidad)
        End Set
    End Property

    Public Property Comentario() As String
        Get
            Return mComentario
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mComentario)
        End Set
    End Property

#End Region

#Region "Validaciones"

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Me.mTipoEntidadOrigen Is Nothing Then
            pMensaje = "El Tipo de Origen no puede estar vacío"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        mToSt = Me.ToString()

        Return MyBase.EstadoIntegridad(pMensaje)

    End Function

    Public Overrides Function ToString() As String
        Dim cadena As String = ""

        If mTipoEntidadOrigen IsNot Nothing Then
            cadena = mTipoEntidadOrigen.ToString() & " - "
        End If

        Return cadena & mIDEntidad

    End Function

#End Region

End Class

<Serializable()> Public Class ColOrigenDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of OrigenDN)

End Class
