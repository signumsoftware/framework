Namespace DN
    Public Class CondicionCampoDN
        Implements ICondicionDN

#Region "Atributos"
        Private mCampo As String
        Private mValor As Object
        Private mOperador As String
        Private mTipoCampo As System.Type
#End Region

#Region "Propiedades"
        Public Property Campo() As String
            Get
                Return mCampo
            End Get
            Set(ByVal Value As String)
                mCampo = Value
            End Set
        End Property

        Public Property Valor() As Object
            Get
                Return mValor
            End Get
            Set(ByVal Value As Object)
                mValor = Value
            End Set
        End Property

        Public Property Operador() As String
            Get
                Return mOperador
            End Get
            Set(ByVal Value As String)
                mOperador = Value
            End Set
        End Property

        Public Property TipoCampo() As System.Type
            Get
                Return mTipoCampo
            End Get
            Set(ByVal Value As System.Type)
                mTipoCampo = Value
            End Set
        End Property
#End Region

    End Class
End Namespace
