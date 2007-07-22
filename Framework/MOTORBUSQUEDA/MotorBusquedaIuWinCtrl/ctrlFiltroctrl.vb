Imports MotorBusquedaBasicasDN
Public Class ctrlFiltroctrl
    Inherits MotorIU.ControlesP.ControladorControlBase

    Public Sub New(ByVal pMarco As MotorIU.Motor.INavegador, ByVal pControl As MotorIU.ControlesP.IControlP)
        MyBase.New(pMarco, pControl)
    End Sub

    Public Function CargarEstructura(ByVal pParametroCargaEstructura As ParametroCargaEstructuraDN) As MotorBusquedaDN.EstructuraVistaDN

        Dim mias As MotorBusquedaAS.GestorBusquedaAS
        mias = New MotorBusquedaAS.GestorBusquedaAS

        If String.IsNullOrEmpty(pParametroCargaEstructura.NombreVistaVis) OrElse pParametroCargaEstructura.TipodeEntidad Is Nothing Then

            ' 1º si se tratara de una interface hay que veriicar que el tipo de entre los que implementan 

            Dim rmv As New MV2DN.RecuperadorMapeadoXFicheroXMLAD(Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis"))
            Dim datosBusqueda As String = rmv.RecuperarInstanciaMap(pParametroCargaEstructura.TipodeEntidad).DatosBusqueda

            If Not pParametroCargaEstructura.CargarDesdeTexto(datosBusqueda) Then
                pParametroCargaEstructura.CargarDesdeTipo(pParametroCargaEstructura.TipodeEntidad)

            End If


        End If
        Return mias.RecuperarEstructuraVista(pParametroCargaEstructura)





        Return Nothing

    End Function
End Class
