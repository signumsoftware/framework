
<Serializable()> Public Class DatoConfigurableDN
    Inherits DatosNegocio.EntidadDN

    Protected mTipoDatoConfigurable As TipoDatoConfigurableDN
    Protected mValorNumerico As Double
    Protected mValorTexto As String

    Public Sub New()
        Me.modificarEstado = EstadoDatosDN.Inconsistente
    End Sub
    Public Sub New(ByVal pNombre As String, ByVal pTipoDatoConfigurable As TipoDatoConfigurableDN, ByVal pValorNumerico As Double, ByVal pValorTexto As String)

        Me.CambiarValorVal(Of String)(pValorTexto, Me.mValorTexto)
        Me.CambiarValorVal(Of String)(pNombre, Me.mNombre)
        Me.CambiarValorVal(Of Double)(pValorNumerico, mValorNumerico)
        Me.CambiarValorRef(Of TipoDatoConfigurableDN)(pTipoDatoConfigurable, mTipoDatoConfigurable)
        Me.modificarEstado = EstadoDatosDN.SinModificar

    End Sub

    Public Property ValorNumerico() As Double
        Get
            Return Me.mValorNumerico
        End Get
        Set(ByVal value As Double)
            Me.CambiarValorVal(Of Double)(value, mValorNumerico)
        End Set
    End Property

    Public Property ValorTexto() As String
        Get
            Return mValorTexto
        End Get
        Set(ByVal value As String)
            Me.CambiarValorVal(Of String)(value, mValorTexto)
        End Set
    End Property
    Public Property TipoDatoConfigurableDN() As TipoDatoConfigurableDN
        Get
            Return Me.mTipoDatoConfigurable
        End Get
        Set(ByVal value As TipoDatoConfigurableDN)
            Me.CambiarValorRef(Of TipoDatoConfigurableDN)(value, mTipoDatoConfigurable)
        End Set
    End Property

    Public Property EsNumerico() As Boolean
        Get
            Return Me.mTipoDatoConfigurable.EsNumerico
        End Get
        Set(ByVal value As Boolean)
            Me.mTipoDatoConfigurable.EsNumerico = value
        End Set
    End Property


End Class


<Serializable()> Public Class ColDatoConfigurableDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of DatoConfigurableDN)

    Public Sub New()
        MyBase.New()
    End Sub

    Protected Overrides Sub OnElementoaEliminar(ByVal pSender As Object, ByVal pElemento As Object, ByRef permitir As Boolean)
        If CType(pElemento, Framework.DatosNegocio.DatoConfigurableDN).Nombre = "Cuantia para Toyota" Then
            Throw New ApplicationException("Este elemento no se puede eliminar de la colección")
        End If
        MyBase.OnElementoaEliminar(pSender, pElemento, permitir)
    End Sub
End Class