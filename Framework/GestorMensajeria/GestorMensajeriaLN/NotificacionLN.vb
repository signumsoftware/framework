Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Mensajeria.GestorMensajeriaDN

Public Class NotificacionLN
    Inherits Framework.ClaseBaseLN.BaseGenericLN

#Region "Constructores"

    Public Sub New(ByVal pTL As ITransaccionLogicaLN, ByVal pRec As IRecursoLN)
        MyBase.New(pTL, pRec)
    End Sub

#End Region

#Region "Métodos"

    Public Overloads Function Guardar(ByVal notificacion As NotificacionDN) As NotificacionDN
        Return MyBase.Guardar(Of NotificacionDN)(notificacion)
    End Function

#End Region

End Class
