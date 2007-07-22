Module Module1

    Public Sub Main()
        Application.EnableVisualStyles()

        Dim tablanavegacion As New Hashtable

        tablanavegacion.Add("PlantillaCartaModelo", New MotorIU.Motor.Destino(GetType(GestionPagos.IU.frmPlantillaCarta), GetType(GestionPagos.IU.frmPlantillaCartactrl))) ' "TalonesIU.frmPlantillaCarta", "TalonesIU.frmPlantillaCartactrl"))
        tablanavegacion.Add("PruebaPlantillaCarta", New MotorIU.Motor.Destino(GetType(GestionPagos.IU.frmPruebaPlantillaReemplazo), GetType(GestionPagos.IU.frmPruebaPlantillaReemplazoctrl))) ' "TalonesIU.frmPruebaPlantillaReemplazo", "TalonesIU.frmPruebaPlantillaReemplazoctrl"))
        tablanavegacion.Add("PreImpresion", New MotorIU.Motor.Destino(GetType(GestionPagos.IU.frmPreImpresion), GetType(GestionPagos.IU.frmPreImpresionctrl))) '"TalonesIU.frmPreImpresion", "TalonesIU.frmPreImpresionctrl"))
        tablanavegacion.Add("PostImpresion", New MotorIU.Motor.Destino(GetType(GestionPagos.IU.frmPostImpresion), GetType(GestionPagos.IU.frmPostImpresionctrl))) ' "TalonesIU.frmPostImpresion", "TalonesIU.frmPostImpresionctrl"))

        Dim mimarco As New Marco(tablanavegacion)

        Application.Run()

    End Sub

End Module

Public Class Marco
    Inherits MotorIU.Motor.NavegadorBase

    Public Sub New(ByVal pTablaNavegacion As Hashtable)
        MyBase.New(pTablaNavegacion)

        Me.Navegar("Autorizacion", Me, Nothing, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosIniciales(), Nothing)
    End Sub
End Class

Public Class ControladorGenerico
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub
End Class