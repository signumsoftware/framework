Public Class ctrlPresupuesto
    Inherits MotorIU.ControlesP.ControladorControlBase

    Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador, ByVal ControlAsociado As MotorIU.ControlesP.IControlP)
        MyBase.New(pNavegador, ControlAsociado)
    End Sub

    Public Function RecuperarDocumentosAsociados(ByVal pGUID As String) As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
        Dim mias As New Framework.Ficheros.FicherosAS.CajonDocumentoAS()
        Return mias.ObtenerCajonDocumentosRelacionados(pGUID)
    End Function
End Class
