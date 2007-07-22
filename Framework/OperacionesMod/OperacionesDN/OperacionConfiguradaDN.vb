
<Serializable()> _
Public Class OperacionConfiguradaDN
    Inherits Framework.DatosNegocio.EntidadTemporalDN

    Protected mFechaCreacion As Date = Now.Date()
    Protected mTipoOperacionConfiguradaDN As TipoOperacionConfiguradaDN
    Protected mIOperacionDN As IOperacionSimpleDN

    Protected mColSumValOperMapDN As ColSumValOperMapDN


    Public ReadOnly Property FechaCreacion() As Date
        Get
            Return Me.mFechaCreacion
        End Get
    End Property


    Public Property ColSuministradorValorOperacionMapDN() As ColSumValOperMapDN
        Get
            Return mColSumValOperMapDN
        End Get
        Set(ByVal value As ColSumValOperMapDN)
            Me.CambiarValorRef(Of ColSumValOperMapDN)(value, mColSumValOperMapDN)
        End Set
    End Property

    Public Property IOperacionDN() As IOperacionSimpleDN
        Get
            Return mIOperacionDN
        End Get
        Set(ByVal value As IOperacionSimpleDN)
            Me.CambiarValorRef(Of IOperacionSimpleDN)(value, mIOperacionDN)

        End Set
    End Property

    Public Property TipoOperacionConfiguradaDN() As TipoOperacionConfiguradaDN
        Get
            Return Me.mTipoOperacionConfiguradaDN

        End Get
        Set(ByVal value As TipoOperacionConfiguradaDN)
            Me.CambiarValorRef(Of TipoOperacionConfiguradaDN)(value, mTipoOperacionConfiguradaDN)
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN
        If mFechaCreacion > Me.mPeriodo.FInicio Then
            pMensaje = "la fecha de creación debe ser previa a la fecha de entrada en vigor"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function
End Class
