#Region "Importaciones"

Imports Framework.TiposYReflexion.DN

#End Region

Namespace DN
    Public Class CampoPostRecuperacionDN

#Region "Atributos"
        Private _InfoCampo As InfoTypeInstCampoRefDN
        Private mClave As String
#End Region

#Region "Constructores"
        Public Sub New(ByVal pClave As String, ByVal pInfoCampo As InfoTypeInstCampoRefDN)
            mClave = pClave
            _InfoCampo = pInfoCampo
        End Sub
#End Region

#Region "Propiedades"
        Public Property Clave() As String
            Get
                Return mClave
            End Get
            Set(ByVal Value As String)
                mClave = Value
            End Set
        End Property

        Public Property InfoCampo() As InfoTypeInstCampoRefDN
            Get
                Return _InfoCampo
            End Get
            Set(ByVal Value As InfoTypeInstCampoRefDN)
                _InfoCampo = Value
            End Set
        End Property
#End Region

    End Class
End Namespace
