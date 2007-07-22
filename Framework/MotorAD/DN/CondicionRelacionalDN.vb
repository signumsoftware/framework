Namespace DN
    Public Class CondicionRelacionalDN
        Implements ICondicionDN

#Region "Atributos"
        Private mOperador As String
        Private _factor1 As ICondicionDN
        Private _factor2 As ICondicionDN
#End Region

#Region "Propiedades"
        Public Property Operador() As String
            Get
                Return mOperador
            End Get
            Set(ByVal Value As String)
                mOperador = Value
            End Set
        End Property

        Public Property Factor1() As ICondicionDN
            Get
                Return Me._factor1
            End Get
            Set(ByVal Value As ICondicionDN)
                _factor1 = Value
            End Set
        End Property

        Public Property Factor2() As ICondicionDN
            Get
                Return Me._factor2
            End Get
            Set(ByVal Value As ICondicionDN)
                _factor2 = Value
            End Set
        End Property
#End Region

    End Class
End Namespace
