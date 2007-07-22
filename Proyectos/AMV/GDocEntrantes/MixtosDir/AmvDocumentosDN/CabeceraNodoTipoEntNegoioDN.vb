<Serializable()> _
Public Class CabeceraNodoTipoEntNegoioDN
    Inherits Framework.DatosNegocio.EntidadDN
    Private mNodoTipoEntNegoio As NodoTipoEntNegoioDN
    Public Sub New()

    End Sub
    Public Property NodoTipoEntNegoio() As NodoTipoEntNegoioDN
        Get
            Return mNodoTipoEntNegoio
        End Get
        Set(ByVal value As NodoTipoEntNegoioDN)
            If value.Padre Is Nothing Then
                Me.CambiarValorRef(Of NodoTipoEntNegoioDN)(value, mNodoTipoEntNegoio)

            Else
                Throw New Framework.DatosNegocio.ApplicationExceptionDN("un nodo raiz de arbol no puede tener padre")
            End If
        End Set
    End Property

End Class
