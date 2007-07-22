<Serializable()> _
Public Class PagoTrazaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Procesos.ProcesosDN.ITrazaDN


#Region "Atributos"

    Protected mPago As PagoDN
    Protected mOperacionRealizada As Framework.Procesos.ProcesosDN.OperacionRealizadaDN

    Protected mFechaCreacionTraza As Date
    Protected mActorNombre As String
    Protected mImporte As Single
    Protected mNombreDestinatario As String
    Protected mIdFiscalDestinatario As String
    Protected mCuentaOrigen As String
    Protected mCuentaDestino As String
    Protected mNumeroTalon As String
    Protected mFechaProgramadaEmision As Date
    Protected mFechaEfecto As Date
    Protected mEntidadOrigen As String
    Protected mNombreOperacion As String

#End Region

#Region "Propiedades"

    Public ReadOnly Property Pago() As PagoDN
        Get
            Return mPago
        End Get
    End Property

    Public ReadOnly Property OperacionRealizada() As Framework.Procesos.ProcesosDN.OperacionRealizadaDN
        Get
            Return mOperacionRealizada
        End Get
    End Property

    Public ReadOnly Property FechaCreacionTraza() As Date
        Get
            Return mFechaCreacionTraza
        End Get
    End Property

    Public ReadOnly Property ActorNombre() As String
        Get
            Return mActorNombre
        End Get
    End Property

    Public ReadOnly Property Importe() As Single
        Get
            Return mImporte
        End Get
    End Property

    Public ReadOnly Property NombreDestinatario() As String
        Get
            Return mNombreDestinatario
        End Get
    End Property

    Public ReadOnly Property IdFiscalDestinatario() As String
        Get
            Return mIdFiscalDestinatario
        End Get
    End Property

    Public ReadOnly Property CuentaOrigen() As String
        Get
            Return mCuentaOrigen
        End Get
    End Property

    Public ReadOnly Property CuentaDestino() As String
        Get
            Return mCuentaDestino
        End Get
    End Property

    Public ReadOnly Property NumeroTalon() As String
        Get
            Return mNumeroTalon
        End Get
    End Property

    Public ReadOnly Property FechaProgramadaEmision() As Date
        Get
            Return mFechaProgramadaEmision
        End Get
    End Property

    Public ReadOnly Property FechaEfecto() As Date
        Get
            Return mFechaEfecto
        End Get
    End Property

    Public ReadOnly Property EntidadOrigen() As String
        Get
            Return mEntidadOrigen
        End Get
    End Property

    Public ReadOnly Property NombreOperacion() As String
        Get
            Return mNombreOperacion
        End Get
    End Property

#End Region

#Region "Métodos"

    Public Sub TrazarEntidad(ByVal pEntidad As Framework.DatosNegocio.IEntidadDN) Implements Framework.Procesos.ProcesosDN.ITrazaDN.TrazarEntidad

        Dim opr As Framework.Procesos.ProcesosDN.OperacionRealizadaDN = pEntidad
        Me.CambiarValorRef(Of Framework.Procesos.ProcesosDN.OperacionRealizadaDN)(opr, mOperacionRealizada)
        Me.CambiarValorRef(Of PagoDN)(opr.ObjetoIndirectoOperacion, mPago)

        mNombreOperacion = opr.Operacion.Nombre

        '        destinatario = CType(pago.Destinatario, Object).ToString

        mFechaCreacionTraza = Now
        mFechaProgramadaEmision = mPago.FechaProgramadaEmision
        mFechaEfecto = mPago.FechaEfecto


        Dim hce As Empresas.DN.HuellaCacheEmpleadoYPuestosRDN
        If opr.SujetoOperacion IsNot Nothing AndAlso TypeOf opr.SujetoOperacion Is Empresas.DN.HuellaCacheEmpleadoYPuestosRDN Then
            hce = opr.SujetoOperacion
            mActorNombre = CType(hce, Object).ToString

        Else
            mActorNombre = CType(opr.SujetoOperacion, Object).ToString

        End If


        mImporte = mPago.Importe

        If mPago.Destinatario IsNot Nothing Then
            mNombreDestinatario = mPago.Destinatario.Nombre
            mIdFiscalDestinatario = mPago.Destinatario.IentidadFiscal.IdentificacionFiscal.Codigo

        End If


        If mPago.CuentaOrigenPago IsNot Nothing Then
            mCuentaOrigen = mPago.CuentaOrigenPago.ToString
        End If

        If mPago.Transferencia IsNot Nothing AndAlso mPago.Transferencia.CuentaDestinoPago IsNot Nothing Then
            mCuentaDestino = mPago.Transferencia.CuentaDestinoPago.ToString
        End If

        If mPago.Talon IsNot Nothing AndAlso mPago.Talon.TalonEmitido IsNot Nothing Then
            mNumeroTalon = mPago.Talon.TalonEmitido.NumeroSerie
        End If

        If mPago.Origen IsNot Nothing Then
            mEntidadOrigen = mPago.Origen.ToString
        End If



    End Sub

#End Region


End Class
