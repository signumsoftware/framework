Imports FN.RiesgosVehiculos.DN
Imports FN.Seguros.Polizas.DN

Public Class ctrlTarifa
    Inherits MotorIU.ControlesP.ControladorControlBase

    Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador, ByVal ControlAsociado As MotorIU.ControlesP.IControlP)
        MyBase.New(pNavegador, ControlAsociado)
    End Sub

    Public Function RecuperarProductos() As FN.Seguros.Polizas.DN.ColProductoDN
        Dim col As New FN.Seguros.Polizas.DN.ColProductoDN()
        Dim mias As New Framework.AS.DatosBasicosAS()
        Dim ar As IList = mias.RecuperarListaTipos(GetType(FN.Seguros.Polizas.DN.ProductoDN))
        If Not ar Is Nothing Then
            For Each lp As FN.Seguros.Polizas.DN.ProductoDN In ar
                col.Add(lp)
            Next
        End If
        Return col
    End Function

    Public Function RecuperarProductosModelo(ByVal modelo As ModeloDN, ByVal matriculada As Boolean, ByVal fecha As Date) As ColProductoDN
        Dim mias As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS()
        Return mias.RecuperarProductosModelo(modelo, matriculada, fecha)
    End Function


    Public Function Tarificar(ByVal Tarifa As FN.Seguros.Polizas.DN.TarifaDN) As FN.Seguros.Polizas.DN.TarifaDN
        Dim mias As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS()
        Return mias.TarificarTarifa(Tarifa)
    End Function

    Public Function TarificarRenovacion(ByVal Tarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal numSiniestros As Integer) As FN.Seguros.Polizas.DN.TarifaDN
        Dim mias As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS()
        Return mias.TarificarTarifa(Tarifa)
    End Function

End Class
