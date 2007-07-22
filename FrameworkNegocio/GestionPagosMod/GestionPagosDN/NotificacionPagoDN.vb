Imports Framework.DatosNegocio
<Serializable()> Public Class NotificacionPagoDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mPago As PagoDN
    Protected mOrigen As OrigenNotificacion
    Protected mSujeto As IEntidadDN  ' quien realiza la notuificacion
    Protected mComunicado As String
    Protected mModificable As Boolean = True

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pSujeto As IEntidadDN, ByVal pPago As PagoDN, ByVal pOrigenNotificacion As OrigenNotificacion, ByVal pNombre As String, ByVal pComunicado As String)

        Me.CambiarValorVal(Of String)(pNombre, Me.mNombre)
        Me.CambiarValorVal(Of String)(pComunicado, mComunicado)


        Me.CambiarValorRef(Of IEntidadDN)(pSujeto, mSujeto)
        '  Me.CambiarValorRef(Of PagoDN)(pPago, mPago)
        Me.Pago = pPago
        Me.CambiarValorVal(Of OrigenNotificacion)(pOrigenNotificacion, mOrigen)
        If pOrigenNotificacion = OrigenNotificacion.Automatica Then
            mModificable = False
        End If
    End Sub

#End Region

#Region "Propiedades"

    Public Property Pago() As PagoDN
        Get
            Return Me.mPago
        End Get
        Set(ByVal value As PagoDN)

            If mModificable Then
                Me.CambiarValorRef(Of PagoDN)(value, mPago)
                mPago.ColNotificacionPago.Add(Me)
            End If
        End Set
    End Property

    Public Property Origen() As OrigenNotificacion
        Get
            Return Me.mOrigen
        End Get
        Set(ByVal value As OrigenNotificacion)

            If mModificable Then
                Me.CambiarValorVal(Of OrigenNotificacion)(value, mOrigen)

            End If

        End Set
    End Property

    Public Property Comunicado() As String
        Get
            Return Me.mComunicado
        End Get
        Set(ByVal value As String)

            If mModificable Then

                Me.CambiarValorVal(Of String)(value, mComunicado)

            End If
        End Set
    End Property

    Public Overrides Property Nombre() As String
        Get

            Return MyBase.Nombre
        End Get
        Set(ByVal value As String)
            If mModificable Then
                MyBase.Nombre = value
            End If

        End Set
    End Property

#End Region

End Class



Public Enum OrigenNotificacion
    Automatica
    Manual
End Enum


<Serializable()> Public Class ColNotificacionPagoDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of NotificacionPagoDN)

End Class