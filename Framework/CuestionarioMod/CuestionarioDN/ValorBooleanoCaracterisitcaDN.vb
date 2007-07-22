<Serializable()> _
Public Class ValorBooleanoCaracterisitcaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IValorCaracteristicaDN


    Protected mValorBooleano As Boolean
    Protected mCaracteristica As CaracteristicaDN
    Protected mIVCPadre As IValorCaracteristicaDN
    Protected mFechaEfectoValor As Date


    Public Property ValorBooleano() As Boolean
        Get
            Return Me.mValorBooleano
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, Me.mValorBooleano)
        End Set
    End Property

    <Framework.DatosNegocio.RelacionPropCampoAtribute("mCaracteristica")> Public Property Caracteristica() As CaracteristicaDN Implements IValorCaracteristicaDN.Caracteristica
        Get
            Return Me.mCaracteristica
        End Get
        Set(ByVal value As CaracteristicaDN)
            Me.CambiarValorRef(Of CaracteristicaDN)(value, Me.mCaracteristica)
        End Set
    End Property

    Public Property Valor() As Object Implements IValorCaracteristicaDN.Valor
        Get
            Return mValorBooleano
        End Get
        Set(ByVal value As Object)
            mValorBooleano = value
        End Set
    End Property

    Public Property ValorCaracPadre() As IValorCaracteristicaDN Implements IValorCaracteristicaDN.ValorCaracPadre
        Get
            Return Me.mIVCPadre
        End Get
        Set(ByVal value As IValorCaracteristicaDN)
            Me.CambiarValorRef(Of IValorCaracteristicaDN)(value, Me.mIVCPadre)
        End Set
    End Property

    Public Property FechaEfectoValor() As Date Implements IValorCaracteristicaDN.FechaEfectoValor
        Get
            Return mFechaEfectoValor
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaEfectoValor)
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN


        If Not Me.mCaracteristica.Padre Is Nothing AndAlso Me.mIVCPadre Is Nothing Then

            pMensaje = "Dado que la caracteristica tine una caracteristica padre el valor debiera de disponer de un valor padre"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente

        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function ClonarIValorCaracteristica() As IValorCaracteristicaDN Implements IValorCaracteristicaDN.ClonarIValorCaracteristica
        Dim valorClon As ValorBooleanoCaracterisitcaDN
        valorClon = Me.CloneSuperficialSinIdentidad()
        valorClon.FechaEfectoValor = Date.MinValue

        If Me.mIVCPadre IsNot Nothing Then
            valorClon.mIVCPadre = Me.mIVCPadre.ClonarIValorCaracteristica()
        End If

        Return valorClon
    End Function
End Class
