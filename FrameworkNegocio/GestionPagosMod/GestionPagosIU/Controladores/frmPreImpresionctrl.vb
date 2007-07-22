Public Class frmPreImpresionctrl
    Inherits MotorIU.FormulariosP.ControladorFormBase


    Public Function RecuperarTodosReemplazos() As List(Of FN.GestionPagos.DN.ReemplazosTextoCartasDN)
        Return LNC.RecuperarTodosReemplazos()
    End Function

    Public Function RecuperarTalonDN(ByVal IDTalon As String) As FN.GestionPagos.DN.TalonDN
        Dim mias As New GestionPagos.AS.PagosAS

        Return mias.RecuperarTalonDN(IDTalon)

    End Function

    Public Function RecuperarPagoDN(ByVal pID As String) As FN.GestionPagos.DN.PagoDN
        Dim mias As New Framework.AS.DatosBasicosAS

        Return mias.RecuperarGenerico(pID, GetType(FN.GestionPagos.DN.PagoDN))
    End Function

End Class
