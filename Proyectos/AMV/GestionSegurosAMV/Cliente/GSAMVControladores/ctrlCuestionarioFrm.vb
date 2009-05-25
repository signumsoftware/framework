Imports Framework.DatosNegocio
Imports Framework.DatosNegocio.Localizaciones.Temporales

Public Class ctrlCuestionarioFrm
    Inherits MotorIU.FormulariosP.ControladorFormBase


#Region "Constructores"

    Public Sub New()

    End Sub

    Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador)
        MyBase.New(pNavegador)
    End Sub

#End Region

    Public Function GenerarPresupuestoxCuestionarioRes(ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN) As FN.Seguros.Polizas.DN.PresupuestoDN
        Dim miAS As New GSAMVAS.CuestionarioAS()
        Return miAS.GenerarPresupuestoxCuestionarioRes(cuestionarioR)
    End Function

    Public Function GenerarTarifaxCuestionarioRes(ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal tiempoTarificado As AnyosMesesDias) As FN.Seguros.Polizas.DN.TarifaDN
        Dim miAS As New GSAMVAS.CuestionarioAS()
        Return miAS.GenerarTarifaxCuestionarioRes(cuestionarioR, tiempoTarificado)
    End Function

    Public Function RecuperarCuestionarioFecha(ByVal fechaEC As Date) As Framework.Cuestionario.CuestionarioDN.CuestionarioDN
        Dim cuestionarioActivo As Framework.Cuestionario.CuestionarioDN.CuestionarioDN = Nothing
        Dim miAS As New Framework.AS.DatosBasicosAS()

        Dim colCuest As New Framework.Cuestionario.CuestionarioDN.ColCuestionarioDN()
        colCuest.AddRangeObject(miAS.RecuperarListaTipos(GetType(Framework.Cuestionario.CuestionarioDN.CuestionarioDN)))

        Return colCuest.RecuperarCuestionarioxFecha(Now())

    End Function

End Class
