Imports Framework.DatosNegocio.Arboles
<Serializable()> _
Public Class NodoTipoEntNegoioDN
    Inherits Framework.DatosNegocio.Arboles.NodoBaseTDN(Of TipoEntNegoioDN)

#Region "atributos"

#End Region

#Region "métodos"
    Public Overrides Function ToString() As String
        Return Me.Nombre
    End Function
#End Region
End Class
<Serializable()> _
Public Class ColNodoTipoEntNegoioDN
    Inherits ColNodoBaseTDN(Of NodoTipoEntNegoioDN)
    Public Overrides Function RecuperarColHojasContenidas() As System.Collections.IList

        Return MyBase.RecuperarColHojasContenidas(New Framework.DatosNegocio.ArrayListValidable(Of TipoEntNegoioDN))


        '  Return MyBase.RecuperarColHojasContenidas()

    End Function
End Class
<Serializable()> _
Public Class HuellaNodoTipoEntNegoioDN
    Inherits Framework.DatosNegocio.HuellaEntidadTipadaDN(Of NodoTipoEntNegoioDN)
    Public Sub New()

    End Sub
    Public Sub New(ByVal pNodoTipoEntNegoioDN As NodoTipoEntNegoioDN)
        MyBase.New(pNodoTipoEntNegoioDN, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir)
    End Sub

End Class