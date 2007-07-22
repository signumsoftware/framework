Imports Framework.DatosNegocio

<Serializable()> _
Public Class PolizaDN
    Inherits EntidadDN

#Region "Campos"

    ' Protected mColTomador As ColTomadorDN
    Protected mNumeroPoliza As String
    Protected mTomador As TomadorDN
    Protected mEmisoraPolizas As FN.Seguros.Polizas.DN.EmisoraPolizasDN
    '   Protected mCondicionesPago As FN.GestionPagos.DN.CondicionesPagoDN 
    Protected mFechaAlta As DateTime
    Protected mCodColaborador As String
    Protected mGuidColaborador As String
    Protected mMotivoAnulacion As MotivoAnulacionDN

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        ' Me.CambiarValorCol(Of ColTomadorDN)(New ColTomadorDN(), mColTomador)
        ' CambiarValorRef(Of FN.GestionPagos.DN.CondicionesPagoDN)(New FN.GestionPagos.DN.CondicionesPagoDN, mCondicionesPago)
        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public Property GuidColaborador() As String
        Get
            Return mGuidColaborador
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mGuidColaborador)
        End Set
    End Property

    Public Property CodColaborador() As String
        Get
            Return mCodColaborador
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mCodColaborador)
        End Set
    End Property

    Public Property FechaAlta() As DateTime
        Get
            Return mFechaAlta
        End Get
        Set(ByVal value As DateTime)
            CambiarValorVal(Of DateTime)(value, mFechaAlta)
        End Set
    End Property

    '<RelacionPropCampoAtribute("mCondicionesPago")> _
    'Public Property CondicionesPago() As FN.GestionPagos.DN.CondicionesPagoDN

    '    Get
    '        Return mCondicionesPago
    '    End Get

    '    Set(ByVal value As FN.GestionPagos.DN.CondicionesPagoDN)
    '        CambiarValorRef(Of FN.GestionPagos.DN.CondicionesPagoDN)(value, mCondicionesPago)

    '    End Set
    'End Property

    <RelacionPropCampoAtribute("mEmosoraPolizas")> _
    Public Property EmisoraPolizas() As FN.Seguros.Polizas.DN.EmisoraPolizasDN

        Get
            Return mEmisoraPolizas
        End Get

        Set(ByVal value As FN.Seguros.Polizas.DN.EmisoraPolizasDN)
            CambiarValorRef(Of FN.Seguros.Polizas.DN.EmisoraPolizasDN)(value, mEmisoraPolizas)

        End Set
    End Property

    <RelacionPropCampoAtribute("mTomador")> _
    Public Property Tomador() As TomadorDN

        Get
            Return mTomador
        End Get

        Set(ByVal value As TomadorDN)
            CambiarValorRef(Of TomadorDN)(value, mTomador)

        End Set
    End Property

    Public Property NumeroPoliza() As String
        Get
            Return mNumeroPoliza
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mNumeroPoliza)
        End Set
    End Property

    <RelacionPropCampoAtribute("mMotivoAnulacion")> _
    Public Property MotivoAnulacion() As MotivoAnulacionDN
        Get
            Return mMotivoAnulacion
        End Get
        Set(ByVal value As MotivoAnulacionDN)
            CambiarValorRef(Of MotivoAnulacionDN)(value, mMotivoAnulacion)
        End Set
    End Property

#End Region

#Region "Métodos de validación"

#End Region

#Region "Métodos"

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Me.mTomador Is Nothing Then
            pMensaje = "debe existir un tomador"
            Return EstadoIntegridadDN.Inconsistente
        End If

        If Me.mEmisoraPolizas Is Nothing Then
            pMensaje = "debe existir una entidad emisora de la poliza"
            Return EstadoIntegridadDN.Inconsistente
        End If

        'If Me.mCondicionesPago Is Nothing Then
        '    pMensaje = "debe existir unas condiciones de pago de la poliza"
        '    Return EstadoIntegridadDN.Inconsistente
        'End If


        'If String.IsNullOrEmpty(Me.mCocColaborador) <> String.IsNullOrEmpty(Me.mGuidColaborador) Then
        '    pMensaje = "No puede exitir un codigo de colaborador sin su correspondiente GUID"
        '    Return EstadoIntegridadDN.Inconsistente
        'End If


        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class
<Serializable()> _
Public Class HEPolizaDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of PolizaDN)
End Class