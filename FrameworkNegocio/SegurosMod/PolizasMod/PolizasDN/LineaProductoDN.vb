Imports Framework.DatosNegocio

<Serializable()> _
Public Class LineaProductoDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mProducto As ProductoDN
    Protected mAlcanzable As Boolean
    Protected mEstablecido As Boolean
    Protected mImporteLP As Double
    Protected mOfertado As Boolean

#End Region

#Region "Propiedades"

    Public Property Ofertado() As Boolean
        Get
            Return mOfertado
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mOfertado)
        End Set
    End Property


    <RelacionPropCampoAtribute("mProducto")> _
    Public Property Producto() As ProductoDN
        Get
            Return mProducto
        End Get
        Set(ByVal value As ProductoDN)
            CambiarValorRef(Of ProductoDN)(value, mProducto)
        End Set
    End Property

    Public Property Alcanzable() As Boolean
        Get
            Return mAlcanzable
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mAlcanzable)
        End Set
    End Property

    Public Property Establecido() As Boolean
        Get
            Return mEstablecido
        End Get
        Set(ByVal value As Boolean)
            CambiarValorVal(Of Boolean)(value, mEstablecido)
        End Set
    End Property

    Public Property ImporteLP() As Double
        Get
            Return mImporteLP
        End Get
        Set(ByVal value As Double)
            CambiarValorVal(Of Double)(value, mImporteLP)
        End Set
    End Property

#End Region

#Region "Métodos"

    Public Function RecuperarCobertura(ByVal pGUID As String) As CoberturaDN
        Return Me.mProducto.RecuperarCobertura(pGUID)
    End Function

    Public Overrides Function EstadoIntegridad(ByRef pMensaje As String) As Framework.DatosNegocio.EstadoIntegridadDN
        If mProducto Is Nothing Then
            pMensaje = "El producto asociado a la línea de producto no puede ser nulo"
            Return EstadoIntegridadDN.Inconsistente
        End If

        'TODO: Hay que recuperar el importe de la tarifa para cachear el valor
        Return MyBase.EstadoIntegridad(pMensaje)
    End Function

#End Region

End Class


<Serializable()> _
Public Class ColLineaProductoDN
    Inherits ArrayListValidable(Of LineaProductoDN)

    Public Function RecuperarLineaProductoxProducto(ByVal producto As ProductoDN) As LineaProductoDN
        If producto IsNot Nothing Then
            For Each lp As LineaProductoDN In Me
                If lp.Producto IsNot Nothing AndAlso lp.Producto.GUID = producto.GUID Then
                    Return lp
                End If
            Next
        End If

        Return Nothing
    End Function
    Public Function RecuperarColProductos() As ColProductoDN
        Dim col As New ColProductoDN

        For Each lp As LineaProductoDN In Me
            col.Add(lp.Producto)
        Next

        Return col

    End Function

End Class