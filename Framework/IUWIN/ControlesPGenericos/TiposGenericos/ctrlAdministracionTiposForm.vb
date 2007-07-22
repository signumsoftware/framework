Public Class ctrlAdministracionTiposForm
    Inherits MotorIU.FormulariosP.ControladorFormBase

#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pMotor As MotorIU.Motor.INavegador)
        MyBase.New(pMotor)
    End Sub

#End Region

#Region "Métodos"

    Public Sub GuardarListaTipos(ByVal listaTipos As System.Collections.IList)
        Dim miAS As New Framework.AS.DatosBasicosAS()
        miAS.GuardarListaTipos(listaTipos)
    End Sub

    Public Function RecuperarListaTipos(ByVal tipo As System.Type) As IList
        Dim miAS As New Framework.AS.DatosBasicosAS()
        Return miAS.RecuperarListaTipos(tipo)
    End Function

#End Region

End Class
