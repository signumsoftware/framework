<Serializable()> _
Public Class ConfiguracionImpresionTalonDN
    Inherits Framework.DatosNegocio.EntidadDN

#Region "atributos"
    Protected mGeneralX As Single
    Protected mGeneralY As Single
    Protected mCantidadX As Single
    Protected mCantidadY As Single
    Protected mDestinatarioX As Single
    Protected mDestinatarioY As Single
    Protected mCantidadLetrasX As Single
    Protected mCantidadLetrasY As Single
    Protected mFechaX As Single
    Protected mFechaY As Single
    Protected mConfigPagina As System.Drawing.Printing.PageSettings
    Protected mFuente As System.Drawing.Font






    Protected mColImagenes As ColContenedorImagenDN

#End Region

#Region "constructor"
    Public Sub New()

    End Sub

    Public Sub New(ByVal pFuente As System.Drawing.Font, ByVal pGeneralX As Single, ByVal pGeneralY As Single, ByVal pCantidadX As Single, ByVal pCantidadY As Single, ByVal pDestinatarioX As Single, ByVal pDestinatarioY As Single, ByVal pCantidadLetrasX As Single, ByVal pCantidadLetrasY As Single, ByVal pFechaX As Single, ByVal pFechaY As Single, ByVal pConfigPagina As System.Drawing.Printing.PageSettings)
        Me.CambiarValorVal(pGeneralX, mGeneralX)
        Me.CambiarValorVal(pGeneralY, mGeneralY)
        Me.CambiarValorVal(pCantidadX, mCantidadX)
        Me.CambiarValorVal(pCantidadY, mCantidadY)
        Me.CambiarValorVal(pDestinatarioX, mDestinatarioX)
        Me.CambiarValorVal(pDestinatarioY, mDestinatarioY)
        Me.CambiarValorVal(pCantidadLetrasX, mCantidadLetrasX)
        Me.CambiarValorVal(pCantidadLetrasY, mCantidadLetrasY)
        Me.CambiarValorVal(pFechaX, mFechaX)
        Me.CambiarValorVal(pFechaY, mFechaY)
        Me.CambiarValorVal(pConfigPagina, mConfigPagina)
        Me.CambiarValorRef(pFuente, mFuente)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Modificado
    End Sub

#End Region

#Region "propiedades"

    Public Property ColImagenes() As ColContenedorImagenDN
        Get
            Return Me.mColImagenes
        End Get
        Set(ByVal value As ColContenedorImagenDN)
            Me.CambiarValorRef(value, Me.mColImagenes)
        End Set
    End Property

    Public Property Fuente() As System.Drawing.Font
        Get
            Return Me.mFuente
        End Get
        Set(ByVal value As System.Drawing.Font)
            Me.CambiarValorRef(value, Me.mFuente)
        End Set
    End Property

    Public Property GeneralX() As Single
        Get
            Return Me.mGeneralX
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mGeneralX)
        End Set
    End Property

    Public Property GeneralY() As Single
        Get
            Return Me.mGeneralY
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mGeneralY)
        End Set
    End Property

    Public Property CantidadX() As Single
        Get
            Return Me.mCantidadX
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mCantidadX)
        End Set
    End Property

    Public Property CantidadY() As Single
        Get
            Return Me.mCantidadY
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mCantidadY)
        End Set
    End Property

    Public Property DestinatarioX() As Single
        Get
            Return Me.mDestinatarioX
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mDestinatarioX)
        End Set
    End Property

    Public Property DestinatarioY() As Single
        Get
            Return Me.mDestinatarioY
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mDestinatarioY)
        End Set
    End Property

    Public Property CantidadLetrasX() As Single
        Get
            Return Me.mCantidadLetrasX
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mCantidadLetrasX)
        End Set
    End Property

    Public Property CantidadLetrasY() As Single
        Get
            Return Me.mCantidadLetrasY
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mCantidadLetrasY)
        End Set
    End Property

    Public Property FechaX() As Single
        Get
            Return Me.mFechaX
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mFechaX)
        End Set
    End Property

    Public Property FechaY() As Single
        Get
            Return Me.mFechaY
        End Get
        Set(ByVal value As Single)
            Me.CambiarValorVal(value, Me.mFechaY)
        End Set
    End Property

    Public Property ConfigPagina() As System.Drawing.Printing.PageSettings
        Get
            Return Me.mConfigPagina
        End Get
        Set(ByVal value As System.Drawing.Printing.PageSettings)
            Me.CambiarValorRef(value, Me.mConfigPagina)
        End Set
    End Property

#End Region

#Region "métodos"
    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If String.IsNullOrEmpty(Me.Nombre) Then
            pMensaje = "No se ha definido un nombre para la Configuración"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If mConfigPagina Is Nothing Then
            pMensaje = "No se ha definido una configuración de página para la Configuración"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return Framework.DatosNegocio.EstadoIntegridadDN.Consistente
    End Function
#End Region

End Class
