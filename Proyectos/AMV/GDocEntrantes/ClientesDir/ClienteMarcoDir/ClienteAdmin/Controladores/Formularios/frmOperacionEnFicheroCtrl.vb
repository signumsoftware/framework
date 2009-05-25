Imports AmvDocumentosDN

Public Class frmOperacionEnFicheroCtrl
    Inherits MotorIU.FormulariosP.ControladorFormBase


#Region "Métodos"

    Public Function RecuperarOperacionxID(ByVal idOperacion As String) As OperacionEnRelacionENFicheroDN
        Dim miAS As New ClienteAS.ClienteAS()
        Return miAS.RecuperarOperacionxID(idOperacion)
    End Function

#End Region

End Class
