<Serializable()> Public Class ProvinciaDN
    Inherits Framework.DatosNegocio.EntidadDN


    Public Sub New()
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente

    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pPais As PaisDN)
        Me.mNombre = pNombre
        Me.CambiarValorRef(Of PaisDN)(pPais, Me.mPais)
        Me.modificarEstado = Framework.DatosNegocio.EstadoDatosDN.Inconsistente
    End Sub



#Region "Atributos"
    Protected mPais As PaisDN
#End Region

    Public ReadOnly Property ToCadena() As String
        Get
            Return Me.mNombre & " " & Me.Pais.Nombre
        End Get
    End Property
    Public Property Pais() As PaisDN
        Get
            Return Me.mPais
        End Get
        Set(ByVal value As PaisDN)
            Me.CambiarValorRef(Of PaisDN)(value, Me.mPais)
        End Set
    End Property


End Class


<Serializable()> Public Class ColProvinciaDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of ProvinciaDN)

End Class