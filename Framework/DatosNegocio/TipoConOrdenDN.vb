''' <summary>
''' Clase que especifica un tipo cualquiera al que se le asiganará un Nombre y un Orden numérico
''' </summary>
''' <remarks></remarks>
<Serializable()> _
Public Class TipoConOrdenDN
    Inherits EntidadTipoDN

#Region "Atributos"
    Protected mOrden As String
#End Region

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pNombre As String, ByVal pOrden As String)
        MyBase.New(pNombre)

        Dim mensaje As String
        mensaje = ""

        If Me.ValOrden(mensaje, pOrden) Then
            Me.CambiarValorVal(Of String)(pOrden, Me.mOrden)
        End If

        Me.modificarEstado = EstadoDatosDN.SinModificar
    End Sub

#End Region

#Region "Propiedades"

    Public Overridable Property Orden() As String
        Get
            Return mOrden
        End Get
        Set(ByVal value As String)
            Dim mensaje As String
            mensaje = ""
            If Me.ValOrden(mensaje, value) Then
                Me.CambiarValorVal(Of String)(value, Me.mOrden)
            Else
                Throw New ApplicationException(mensaje)
            End If
        End Set
    End Property

#End Region

#Region "Validaciones"

    Private Function ValOrden(ByRef mensaje As String, ByVal pOrden As String) As Boolean
        If pOrden Is Nothing OrElse pOrden = "" Then
            mensaje = "El campo orden no puede estar vacío"
            Return False
        End If

        Return True


    End Function
#End Region

End Class

<Serializable()> _
Public Class ColTipoConOrdenDN
    Inherits ArrayListValidable(Of TipoConOrdenDN)

End Class

Public Class OrdenarTiposPorOrden
    Implements Collections.Generic.IComparer(Of TipoConOrdenDN)

    Public Function Compare(ByVal x As TipoConOrdenDN, ByVal y As TipoConOrdenDN) As Integer Implements System.Collections.Generic.IComparer(Of TipoConOrdenDN).Compare
        If Not IsNumeric(x.Orden) OrElse Not IsNumeric(y.Orden) Then
            Throw New Exception("No se puede ordenar, el campo no es numérico")
        End If
        If CDbl(x.Orden) > CDbl(y.Orden) Then
            Return 1
        Else
            If CDbl(x.Orden) = CDbl(y.Orden) Then
                Return 0
            Else
                Return -1
            End If
        End If
    End Function
End Class