Imports Framework.Cuestionario.CuestionarioDN
Imports FN.RiesgosVehiculos.DN
Imports FN.Seguros.Polizas.DN

Public Class frmTarifaCtrl
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Function CalcularNivelBonificacion(ByVal valorBonificacion As Double, ByVal categoria As CategoriaDN, ByVal bonificacion As BonificacionDN, ByVal fecha As Date) As String
        Dim mias As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS()
        Return mias.CalcularNivelBonificacion(valorBonificacion, categoria, bonificacion, fecha)
    End Function

    Public Function RecuperarCuestionarioR(ByVal huellaCR As HeCuestionarioResueltoDN) As CuestionarioResueltoDN
        Dim basicoAS As New Framework.AS.DatosBasicosAS()
        Dim heCR As HeCuestionarioResueltoDN
        Dim cr As CuestionarioResueltoDN

        heCR = basicoAS.RecuperarGenerico(huellaCR)
        cr = CType(heCR.EntidadReferida, CuestionarioResueltoDN)

        Return cr
    End Function

    Public Function GuardarTarifa(ByVal tarifa As TarifaDN) As TarifaDN
        Dim basicoAS As New Framework.AS.DatosBasicosAS()
        Return basicoAS.GuardarDNGenerico(tarifa)
    End Function

End Class
