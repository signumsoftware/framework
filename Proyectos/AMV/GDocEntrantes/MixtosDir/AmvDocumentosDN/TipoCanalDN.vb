<Serializable()> _
Public Class TipoCanalDN
    Inherits Framework.DatosNegocio.TipoConOrdenDN

#Region "Constructores"

    Public Sub New()
        MyBase.New()
    End Sub

    Public Sub New(ByVal pNombre As String, ByVal orden As String)
        MyBase.New(pNombre, orden)
    End Sub

#End Region

#Region "Propiedades"

    Public Overrides Function ToString() As String
        Return Me.Nombre
    End Function

#End Region

End Class

<Serializable()> _
Public Class ColTipoCanalDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of TipoCanalDN)

   
End Class
