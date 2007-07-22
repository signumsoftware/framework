Public Class ctrlOperadorControlador
    Inherits MotorIU.ControlesP.ControladorControlBase

#Region "constructor"

    Public Sub New(ByVal pMotor As MotorIU.Motor.INavegador, ByVal pControl As MotorIU.ControlesP.IControlP)
        MyBase.New(pMotor, pControl)
    End Sub

#End Region

#Region "Métodos"

    Public Function RecuperarArbolEntidades() As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        Dim mias As New ClienteAS.ClienteAS
        Return mias.RecuperarArbolTiposEntNegocio()
    End Function

    Public Function RecuperarEntidadUnica(Of T)(ByVal hashValor As String) As T
        Dim miAS As New Framework.AS.DatosBasicosAS()
        Dim lista As IList

        lista = miAS.RecuperarPorValorIDenticoEnTipo(GetType(T), hashValor)
        If lista IsNot Nothing AndAlso lista.Count > 0 Then
            Return lista.Item(0)
        Else
            Return Nothing
        End If
    End Function

#End Region

End Class
