<Serializable()> _
Public Class CanalEntradaDocsDN
    Inherits Framework.DatosNegocio.EntidadDN
    Protected mHuellaNodoTipoEntNegoio As HuellaNodoTipoEntNegoioDN
    Protected mTipoEntNegocioReferidora As TipoEntNegoioDN
    Protected mActividad As Boolean
    Protected mRuta As String
    Protected mIncidentado As Boolean
    Protected mTipoCanal As TipoCanalDN

    Public Sub New()

    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pRuta As String, ByVal pActividad As Boolean, ByVal pTipoCanal As TipoCanalDN)

        Me.mNombre = pNombre
        Me.mRuta = pRuta
        Me.mActividad = pActividad
        Me.mTipoCanal = pTipoCanal
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.SinModificar

    End Sub

    Public Property TipoCanal() As TipoCanalDN
        Get
            Return Me.mTipoCanal
        End Get
        Set(ByVal value As TipoCanalDN)
            Me.CambiarValorVal(Of TipoCanalDN)(value, Me.mTipoCanal)
        End Set
    End Property

    Public Property HuellaNodoTipoEntNegoio() As HuellaNodoTipoEntNegoioDN
        Get
            Return Me.mHuellaNodoTipoEntNegoio
        End Get
        Set(ByVal value As HuellaNodoTipoEntNegoioDN)
            Me.CambiarValorRef(Of HuellaNodoTipoEntNegoioDN)(value, mHuellaNodoTipoEntNegoio)
        End Set
    End Property
    Public Property Incidentado() As Boolean
        Get
            Return Me.mIncidentado
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, Me.mIncidentado)

        End Set
    End Property
    Public Property Ruta() As String
        Get
            Return Me.mRuta
        End Get
        Set(ByVal value As String)
            Dim mensaje As String
            If valRuta(mensaje, value) Then
                Me.CambiarValorVal(Of String)(value, Me.mRuta)
            Else
                Throw New Framework.DatosNegocio.ApplicationExceptionDN(mensaje)
            End If

        End Set
    End Property
    Public Property TipoEntNegocioReferidora() As TipoEntNegoioDN
        Get
            Return mTipoEntNegocioReferidora
        End Get
        Set(ByVal value As TipoEntNegoioDN)
            Me.CambiarValorRef(Of TipoEntNegoioDN)(value, Me.mTipoEntNegocioReferidora)
        End Set
    End Property


    Public Property Actividad() As Boolean
        Get
            Return Me.mActividad
        End Get
        Set(ByVal value As Boolean)
            Me.CambiarValorVal(Of Boolean)(value, Me.mActividad)
        End Set
    End Property


    Private Function valRuta(ByRef mensaje As String, ByVal pRuta As String) As Boolean

        If pRuta Is Nothing OrElse pRuta = "" Then
            Return False
        Else
            Return True
        End If


    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN

        If Me.mTipoCanal Is Nothing Then
            pMensaje = "mTipoCanal no puede ser nulo"
            Return Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente
        End If

        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

End Class
<Serializable()> _
Public Class ColCanalEntradaDocsDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of CanalEntradaDocsDN)

    Public Function RecuperarXRuta(ByVal pRuta As String) As CanalEntradaDocsDN

        Dim elemento As CanalEntradaDocsDN
        For Each elemento In Me
            If elemento.Ruta = pRuta Then
                Return elemento
            End If

        Next

    End Function
End Class
