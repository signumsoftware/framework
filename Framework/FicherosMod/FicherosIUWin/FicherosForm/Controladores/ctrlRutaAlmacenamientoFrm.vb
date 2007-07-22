Imports FN.Ficheros
Imports Framework.Ficheros
Public Class ctrlRutaAlmacenamientoFrm
    Inherits MotorIU.FormulariosP.ControladorFormBase

#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador)
        MyBase.New(pNavegador)
    End Sub

#End Region

#Region "Métodos"

    Public Function RecuperarListadoRutas() As IList(Of FicherosDN.RutaAlmacenamientoFicherosDN)
        Dim miAS As New FicherosAS.RutaAlmacenamientoFicherosAS()
        Return miAS.RecuperarListadoRutas()
    End Function

    Public Function GuardarRutaAlmacenamientoF(ByVal rutaAlmacenamientoF As FicherosDN.RutaAlmacenamientoFicherosDN) As FicherosDN.RutaAlmacenamientoFicherosDN
        Dim miAS As New FicherosAS.RutaAlmacenamientoFicherosAS()
        Return miAS.GuardarRutaAlmacenamientoF(rutaAlmacenamientoF)
    End Function

    Public Function CerrarRaf(ByVal rutaAlmacenamiento As FicherosDN.RutaAlmacenamientoFicherosDN) As FicherosDN.RutaAlmacenamientoFicherosDN
        Dim miAS As New FicherosAS.RutaAlmacenamientoFicherosAS()
        Return miAS.CerrarRaf(rutaAlmacenamiento)
    End Function

#End Region

End Class
