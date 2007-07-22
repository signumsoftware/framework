
<Serializable()> Public Class TalonDN

    Inherits Framework.DatosNegocio.EntidadDN


#Region "atributos"
    Protected mPlantillaCarta As PlantillaCartaDN
    Protected mHuellaRTF As HuellaContenedorRTFDN
    Protected mColTalonDocumento As ColTalonDocumentoDN
    Protected mTalonEmitido As TalonDocumentoDN
    Protected mPago As PagoDN
    Protected mDireccionEnvio As FN.Localizaciones.DN.DireccionNoUnicaDN
#End Region


#Region "propiedades"

    ''' <summary>
    ''' La dirección a la que se va a enviar el talón. Por defecto será la misma
    ''' que el domicilio fiscal del Destinatario del talón (si lo tiene)
    ''' </summary>
    Public Property DireccionEnvio() As FN.Localizaciones.DN.DireccionNoUnicaDN
        Get
            Return Me.mDireccionEnvio
        End Get
        Set(ByVal value As FN.Localizaciones.DN.DireccionNoUnicaDN)
            Me.CambiarValorRef(value, Me.mDireccionEnvio)
        End Set
    End Property

    Public Property PlantillaCarta() As PlantillaCartaDN
        Get
            Return Me.mPlantillaCarta
        End Get
        Set(ByVal value As PlantillaCartaDN)
            Me.CambiarValorRef(value, Me.mPlantillaCarta)
        End Set
    End Property

    Public Property HuellaRTF() As HuellaContenedorRTFDN
        Get
            Return Me.mHuellaRTF
        End Get
        Set(ByVal value As HuellaContenedorRTFDN)
            Me.CambiarValorRef(value, Me.mHuellaRTF)
        End Set
    End Property

    Public ReadOnly Property ImportePago() As Single
        Get
            If Not Me.mPago Is Nothing Then
                Return Me.mPago.Importe
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public Property ColTalonesImpresos() As ColTalonDocumentoDN
        Get
            Return Me.mColTalonDocumento
        End Get
        Set(ByVal value As ColTalonDocumentoDN)
            Me.CambiarValorCol(value, Me.mColTalonDocumento)
        End Set
    End Property

    Public ReadOnly Property TalonEmitido() As TalonDocumentoDN
        Get
            'itera por la colección y devuelve el talón correcto
            If Not Me.mColTalonDocumento Is Nothing Then
                For Each t As TalonDocumentoDN In Me.mColTalonDocumento
                    If Not t.Anulado Then
                        Return t
                    End If
                Next
            End If

            'si llega aquí es que no hay ningún talón ok
            Return Nothing
        End Get
    End Property

    Public Property Pago() As PagoDN
        Get
            Return Me.mPago
        End Get
        Set(ByVal value As PagoDN)
            Me.CambiarValorRef(value, Me.mPago)
            'por defecto, si lo hay, ponemos el domicilio fiscal del destinatario
            If Not Me.mPago Is Nothing AndAlso Not Me.mPago.Destinatario Is Nothing Then
                Me.DireccionEnvio = Me.mPago.Destinatario.IentidadFiscal.DomicilioFiscal
            End If
        End Set
    End Property

    Public ReadOnly Property Destinatario() As FN.Localizaciones.DN.IEntidadFiscalDN
        Get
            If Not Me.mPago Is Nothing Then
                Return Me.mPago.Destinatario
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public ReadOnly Property CuentaOrigenPago() As Financiero.DN.CuentaBancariaDN
        Get
            If Not Me.mPago Is Nothing Then
                Return Me.mPago.CuentaOrigenPago
            Else
                Return Nothing
            End If
        End Get
    End Property


#End Region

#Region "métodos"
    Private Function ValidarEstadoColTalonD(ByRef mensaje As String, ByVal colTalonD As ColTalonDocumentoDN) As Boolean
        If colTalonD IsNot Nothing AndAlso colTalonD.NumeroTalonesSinAnular > 1 Then
            mensaje = "Para un mismo talón, no pueden existir más de un talón en estado correcto"
            Return False
        End If

        Return True
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If Not Me.ValidarEstadoColTalonD(pMensaje, Me.mColTalonDocumento) Then
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function
#End Region


End Class
