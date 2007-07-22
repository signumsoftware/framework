Public Class ctrlBuscadorGenerico2ctrl
    Inherits MotorIU.ControlesP.ControladorControlBase

    Public Sub New(ByVal marco As MotorIU.Motor.INavegador, ByVal control As MotorIU.ControlesP.IControlP)
        MyBase.New(marco, control)
    End Sub

#Region "métodos"
    Public Function RealizarBusqueda(ByVal filtro As MotorBusquedaDN.FiltroDN) As DataSet
        If filtro Is Nothing Then
            Return Nothing
        Else
            Dim mias As MotorBusquedaAS.GestorBusquedaAS
            mias = New MotorBusquedaAS.GestorBusquedaAS
            Return mias.RecuperarDatos(filtro)
        End If
    End Function
#End Region
End Class
