Imports Framework.Cuestionario.CuestionarioDN
Imports Framework

<Serializable()> _
Public Class ValorDireccionNoUnicaCaracteristicaDN
    Inherits Framework.DatosNegocio.EntidadDN
    Implements Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN

    Protected mDireccion As FN.Localizaciones.DN.DireccionNoUnicaDN
    Protected mFechaefectoValor As DateTime
    Protected mCaracteristica As CaracteristicaDN
    Protected mIVCPadre As IValorCaracteristicaDN


    <Framework.DatosNegocio.RelacionPropCampoAtribute("mCaracteristica")> Public Property Caracteristica() As CaracteristicaDN Implements IValorCaracteristicaDN.Caracteristica
        Get
            Return Me.mCaracteristica
        End Get
        Set(ByVal value As CaracteristicaDN)
            Me.CambiarValorRef(Of CaracteristicaDN)(value, Me.mCaracteristica)
        End Set
    End Property

    Public Property ValorDireccion() As FN.Localizaciones.DN.DireccionNoUnicaDN
        Get
            Return Me.mDireccion
        End Get
        Set(ByVal value As FN.Localizaciones.DN.DireccionNoUnicaDN)
            Me.CambiarValorRef(Of FN.Localizaciones.DN.DireccionNoUnicaDN)(value, Me.mDireccion)
        End Set
    End Property

    Public Property Valor() As Object Implements IValorCaracteristicaDN.Valor
        Get
            Return Me.ValorDireccion
        End Get
        Set(ByVal value As Object)
            Me.ValorDireccion = value
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
            Return mFechaefectoValor
        End Get
        Set(ByVal value As Date)
            CambiarValorVal(Of Date)(value, mFechaefectoValor)
        End Set
    End Property

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As DatosNegocio.EstadoIntegridadDN

        If Not Me.mCaracteristica.Padre Is Nothing AndAlso Me.mIVCPadre Is Nothing Then
            pMensaje = "Dado que la caracteristica es subordinada, mVNCPadre no puede ser nothing"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

    Public Function ClonarIValorCaracteristica() As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN Implements Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN.ClonarIValorCaracteristica
        Dim valorClon As ValorDireccionNoUnicaCaracteristicaDN
        valorClon = Me.CloneSuperficialSinIdentidad()
        valorClon.mDireccion = Me.mDireccion.CloneSuperficialSinIdentidad()
        valorClon.FechaEfectoValor = Date.MinValue

        If Me.mIVCPadre IsNot Nothing Then
            valorClon.mIVCPadre = Me.mIVCPadre.ClonarIValorCaracteristica()
        End If

        Return valorClon
    End Function
End Class


