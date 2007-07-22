
<Serializable()> Public Class TransferenciaDN

    Inherits Framework.DatosNegocio.EntidadDN

    Protected mCuentaDestinoPago As Financiero.DN.CuentaBancariaDN
    Protected mConcepto As String
    Protected mPago As PagoDN

#Region "propiedades"
    Public ReadOnly Property Destinatario() As FN.Localizaciones.DN.IEntidadFiscalDN
        Get
            If Not Me.mPago Is Nothing Then
                Return Me.mPago.Destinatario
            Else
                Return Nothing
            End If
        End Get
    End Property

    Public ReadOnly Property ImportePago() As Single
        Get
            If Not Me.mPago Is Nothing Then
                Return Me.mPago.Importe
            Else
                Return 0
            End If
        End Get
    End Property

    Public Property CuentaDestinoPago() As Financiero.DN.CuentaBancariaDN
        Get
            Return Me.mCuentaDestinoPago
        End Get
        Set(ByVal value As Financiero.DN.CuentaBancariaDN)
            Me.CambiarValorRef(value, Me.mCuentaDestinoPago)
        End Set
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

    Public Property Concepto() As String
        Get
            Return Me.mConcepto
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(value, Me.mConcepto)
        End Set
    End Property

    Public Property Pago() As PagoDN
        Get
            Return Me.mPago
        End Get
        Set(ByVal value As PagoDN)
            Me.CambiarValorRef(value, Me.mPago)
        End Set
    End Property
#End Region

#Region "métodos"

    Public Overrides Function InstanciarEntidad(ByVal pTipo As System.Type, ByVal pPropidadDestino As System.Reflection.PropertyInfo) As Framework.DatosNegocio.IEntidadBaseDN


        If pTipo Is GetType(Financiero.DN.CuentaBancariaDN) OrElse Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(pTipo, GetType(Financiero.DN.CuentaBancariaDN)) Then
            Dim cb As Financiero.DN.CuentaBancariaDN
            If pPropidadDestino.Name = "CuentaDestinoPago" Then

                If Me.mPago.Destinatario Is Nothing Then
                    Return Nothing
                Else
                    cb = Activator.CreateInstance(pTipo)
                    cb.Titulares.Add(Me.mPago.Destinatario)
                End If
                Return cb
            End If

        End If


        Return Nothing

    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If String.IsNullOrEmpty(Me.mConcepto) Then
            pMensaje = "El Concepto de la transferencia no puede estar vacío"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        If Me.mCuentaDestinoPago Is Nothing Then
            pMensaje = "La Cuenta de Destino de la transferencia no puede estar vacía"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        Else
            If Not Me.mCuentaDestinoPago.Titulares.Contiene(Me.mPago.Destinatario, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                pMensaje = "El Destinatario de la transferencia no es Titular de la Cuenta de Destino"
                Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
            End If
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function
#End Region


End Class
