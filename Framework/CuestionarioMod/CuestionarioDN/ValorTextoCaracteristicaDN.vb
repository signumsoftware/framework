<Serializable()> _
Public Class ValorTextoCaracteristicaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements IValorCaracteristicaDN

    Protected mValorTexto As String
    Protected mCaracteristica As CaracteristicaDN
    Protected mIVCPadre As IValorCaracteristicaDN
    Protected mFechaEfectoValor As Date

    Public Property ValorTexto() As String
        Get
            Return Me.mValorTexto
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, Me.mValorTexto)
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
            Return ValorTexto
        End Get
        Set(ByVal value As Object)
            ValorTexto = value
        End Set
    End Property

    Public Property ValorCaracPadre() As IValorCaracteristicaDN Implements IValorCaracteristicaDN.ValorCaracPadre
        Get
            Return mIVCPadre
        End Get
        Set(ByVal value As IValorCaracteristicaDN)
            Me.CambiarValorRef(Of IValorCaracteristicaDN)(value, mIVCPadre)
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
            pMensaje = "Dado que la caracteristica es subordinada, mVNCPadre no puede ser nothing"
            Return DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function ClonarIValorCaracteristica() As IValorCaracteristicaDN Implements IValorCaracteristicaDN.ClonarIValorCaracteristica
        Dim valorClon As ValorTextoCaracteristicaDN
        valorClon = Me.CloneSuperficialSinIdentidad()
        valorClon.FechaEfectoValor = Date.MinValue

        If Me.mIVCPadre IsNot Nothing Then
            valorClon.mIVCPadre = Me.mIVCPadre.ClonarIValorCaracteristica()
        End If

        Return valorClon
    End Function
End Class
