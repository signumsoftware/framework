Public Class frmAdministracionArbolctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub

    Public Function RecuperarArbolEntidades() As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.RecuperarArbolTiposEntNegocio()
    End Function

    Public Function GuardarArbol(ByVal pArbol As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN) As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.GuardarArbol(pArbol)
    End Function
End Class
