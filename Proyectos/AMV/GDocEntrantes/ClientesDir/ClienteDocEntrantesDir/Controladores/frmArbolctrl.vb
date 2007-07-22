Public Class frmArbolctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub

    Public Function RecuperarArbolEntidades() As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim mias As New ClienteAS.ClienteAS
        Dim operador As AmvDocumentosDN.OperadorDN
        Dim principal As Framework.Usuarios.DN.PrincipalDN

        principal = Me.Marco.DatosMarco("Principal")
        operador = principal.UsuarioDN.HuellaEntidadUserDN.EntidadReferida

        Dim cabecera As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        cabecera = mias.RecuperarArbolTiposEntNegocio()
        If Not operador Is Nothing Then
            cabecera.NodoTipoEntNegoio.PodarNodosHijosNoContenedoresHojas(operador.ColTipoEntNegoio, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
        End If

        Return cabecera
    End Function


    Public Function BalizaNumCanalesTipoEntNeg() As Data.DataSet
        Dim mias As New ClienteAS.ClienteAS()
        Return mias.BalizaNumCanalesTipoEntNeg()
    End Function


End Class
