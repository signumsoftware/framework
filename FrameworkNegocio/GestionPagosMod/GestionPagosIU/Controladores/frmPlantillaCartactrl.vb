Public Class frmPlantillaCartactrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub

    Public Function GuardarPlantilla(ByVal pPlantilla As FN.GestionPagos.DN.PlantillaCartaDN) As FN.GestionPagos.DN.PlantillaCartaDN
        Dim mias As New FN.GestionPagos.AS.PagosAS

        Return mias.GuardarPlantillaCarta(pPlantilla)
    End Function

End Class
