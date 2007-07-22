Imports Framework.DatosNegocio

<Serializable()> _
Public Class CoberturaDN
    Inherits EntidadDN

#Region "Atributos"

    Protected mCompania As CompaniaDN
    Protected mDescripcion As String

#End Region

#Region "Propiedades"

    <RelacionPropCampoAtribute("mCompania")> _
    Public Property Compania() As CompaniaDN
        Get
            Return mCompania
        End Get
        Set(ByVal value As CompaniaDN)
            CambiarValorRef(Of CompaniaDN)(value, mCompania)
        End Set
    End Property

    Public Property Descripcion() As String
        Get
            Return mDescripcion
        End Get
        Set(ByVal value As String)
            CambiarValorVal(Of String)(value, mDescripcion)
        End Set
    End Property

#End Region

End Class


<Serializable()> _
Public Class ColCoberturaDN
    Inherits ArrayListValidable(Of CoberturaDN)
    Public Overrides Function ToString() As String

        Dim cadena As String = ""

        For Each cob As CoberturaDN In Me
            cadena += cob.ToString & ", "

        Next

        Return cadena.Substring(0, cadena.Length - 2)
    End Function
End Class