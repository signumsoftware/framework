Imports FN.RiesgosVehiculos.DN

Public Class frmTarifaCtrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Function CalcularNivelBonificacion(ByVal valorBonificacion As Double, ByVal categoria As CategoriaDN, ByVal bonificacion As BonificacionDN, ByVal fecha As Date) As String
        Dim mias As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS()
        Return mias.CalcularNivelBonificacion(valorBonificacion, categoria, bonificacion, fecha)
    End Function

End Class
