Imports FN.Localizaciones.DN
'TODO: por implementar  vwCuentasXTitularesTodos

<Serializable()> Public Class CuentaBancariaDN
    Inherits Framework.DatosNegocio.EntidadDN

    Protected mIBAN As IBANDN
    Protected mCCC As CCCDN
    Protected mTitulares As ColEntidadFiscalGenericaDN


    Public Sub New()
        Me.CambiarValorRef(Of ColEntidadFiscalGenericaDN)(New ColEntidadFiscalGenericaDN, mTitulares)
        Me.CambiarValorRef(Of IBANDN)(New IBANDN, mIBAN)
        Me.CambiarValorRef(Of CCCDN)(New CCCDN, mCCC)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub

#Region "propiedades"
    Public Property IBAN() As IBANDN
        Get
            Return Me.mIBAN
        End Get
        Set(ByVal value As IBANDN)
            Me.CambiarValorRef(value, Me.mIBAN)
        End Set
    End Property

    Public Property CCC() As CCCDN
        Get
            Return Me.mCCC
        End Get
        Set(ByVal value As CCCDN)
            Me.CambiarValorRef(value, Me.mCCC)
        End Set
    End Property

    Public Property Titulares() As ColEntidadFiscalGenericaDN
        Get
            Return Me.mTitulares
        End Get
        Set(ByVal value As ColEntidadFiscalGenericaDN)
            Me.CambiarValorCol(value, Me.mTitulares)
        End Set
    End Property
#End Region



    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If String.IsNullOrEmpty(mIBAN.Codigo) AndAlso String.IsNullOrEmpty(mCCC.Codigo) Then
            pMensaje = "Una cuenta bancaria requiere almentos de un codigo decuenta nacional o internacional válido"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If




        If Me.mTitulares.Count < 1 Then
            pMensaje = "Una cuenta bancaria requiere almentos de un titular  válido"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente

        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

End Class
