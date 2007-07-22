Public Class ControlHelper
    Public Shared Function ObtenerFormularioPadre(ByVal pControl As Control) As Form
        Dim padre As Control = pControl.Parent
        If Not TypeOf padre Is Form Then
            Return ObtenerFormularioPadre(padre)
        Else
            Return padre
        End If
    End Function
End Class
