Imports Framework.DatosNegocio

<Serializable()> _
Public Class ProductoDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mColCoberturas As ColCoberturaDN
    Protected mColProdDependientes As ColProductoDN
    Protected mOrden As Integer

#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
        CambiarValorCol(Of ColCoberturaDN)(New ColCoberturaDN(), mColCoberturas)
        CambiarValorCol(Of ColProductoDN)(New ColProductoDN(), mColProdDependientes)
    End Sub

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mColCoberturas")> _
    Public Property ColCoberturas() As ColCoberturaDN
        Get
            Return mColCoberturas
        End Get
        Set(ByVal value As ColCoberturaDN)
            CambiarValorCol(Of ColCoberturaDN)(value, mColCoberturas)
        End Set
    End Property

    <RelacionPropCampoAtribute("mColProdDependientes")> _
    Public Property ColProdDependientes() As ColProductoDN
        Get
            Return mColProdDependientes
        End Get
        Set(ByVal value As ColProductoDN)
            CambiarValorCol(Of ColProductoDN)(value, mColProdDependientes)
        End Set
    End Property

    Public Property Orden() As Integer
        Get
            Return mOrden
        End Get
        Set(ByVal value As Integer)
            CambiarValorVal(Of Integer)(value, mOrden)
        End Set
    End Property
#End Region

#Region "Métodos"

    Public Function RecuperarCobertura(ByVal pGUID As String) As CoberturaDN
        Return mColCoberturas.RecuperarXGUID(pGUID)
    End Function

#End Region


End Class


<Serializable()> _
Public Class ColProductoDN
    Inherits ArrayListValidable(Of ProductoDN)

    Public Function RecuperarCobertura(ByVal pGUID As String) As CoberturaDN

        For Each p As ProductoDN In Me
            Dim cob As CoberturaDN = p.RecuperarCobertura(pGUID)
            If Not cob Is Nothing Then
                Return cob
            End If
        Next
        Return Nothing

    End Function
End Class
