Imports Framework.DatosNegocio
<Serializable()> _
Public Class TipoEntNegoioDN
    Inherits Framework.DatosNegocio.EntidadDN

    Public Sub New()

    End Sub

    Public Sub New(ByVal pNombre As String)
        MyBase.New("", pNombre, Nothing, False)
    End Sub









#Region "métodos"
    Public Overrides Function ToString() As String
        Return Me.Nombre
    End Function
#End Region
End Class

<Serializable()> _
Public Class ColTipoEntNegoioDN
    Inherits Framework.DatosNegocio.ArrayListValidable(Of TipoEntNegoioDN)

End Class

