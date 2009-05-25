Public Class ctrlDireccionNoUnica
    Inherits MotorIU.ControlesP.ControladorControlBase

    Public Sub New(ByVal pNavegador As MotorIU.Motor.INavegador, ByVal pControlAsociado As MotorIU.ControlesP.IControlP)
        MyBase.New(pNavegador, pControlAsociado)
    End Sub

    Public Function ObtenerLocalidades() As FN.Localizaciones.DN.ColLocalidadDN
        Dim mias As New Framework.AS.DatosBasicosAS
        Dim micol As New FN.Localizaciones.DN.ColLocalidadDN()
        Dim lista As IList = mias.RecuperarListaTipos(GetType(FN.Localizaciones.DN.LocalidadDN))
        For Each loc As FN.Localizaciones.DN.LocalidadDN In lista
            micol.Add(loc)
        Next
        Return micol
    End Function

    Public Function ObtenerLocalidadPorCodigoPostal(ByVal pCodigoPostal As String) As FN.Localizaciones.DN.ColLocalidadDN
        Dim mias As New FN.Localizaciones.AS.LocalizacionesAS()
        Return mias.RecuperarLocalidadPorCodigoPostal(pCodigoPostal)
    End Function

    Public Function ObtenerTiposVia() As List(Of FN.Localizaciones.DN.TipoViaDN)
        Dim mias As New Framework.AS.DatosBasicosAS
        Dim micol As New List(Of FN.Localizaciones.DN.TipoViaDN)
        Dim lista As IList = mias.RecuperarListaTipos(GetType(FN.Localizaciones.DN.TipoViaDN))
        For Each tv As FN.Localizaciones.DN.TipoViaDN In lista
            micol.Add(tv)
        Next
        Return micol
    End Function
End Class
