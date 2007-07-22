Public Class frmPruebaPlantillaReemplazoctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub

    Public Function RecuperarTodosReemplazos() As List(Of FN.GestionPagos.DN.ReemplazosTextoCartasDN)
        Return LNC.RecuperarTodosReemplazos()
    End Function


End Class
