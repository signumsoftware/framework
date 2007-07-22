Imports Framework.Procesos.ProcesosDN
Imports FN.GestionPagos.DN

Public Class GuardasFlujoTalonesLN

#Region "Métodos"

    Public Function GuardaFirmaAutomatica(ByVal transicionRealizada As TransicionRealizadaDN) As Boolean
        Dim pago As PagoDN

        pago = RecuperarPagoTR(transicionRealizada)


        Dim pln As New PagosLN
        Dim limitep As LimitePagoDN = pln.RecuperarLimitePago


        If pago.Importe > limitep.LimiteFirmaAutomatica Then
            Return False
        End If

        Return True

    End Function

    Public Function GuardaGenerarTransferencia(ByVal transicionRealizada As TransicionRealizadaDN) As Boolean
        Dim pago As PagoDN

        pago = RecuperarPagoTR(transicionRealizada)

        If pago.Transferencia Is Nothing Then
            Return False
        End If

        Return True

    End Function

    Public Function GuardaImprimirTalon(ByVal transicionRealizada As TransicionRealizadaDN) As Boolean
        Dim pago As PagoDN

        pago = RecuperarPagoTR(transicionRealizada)

        If pago.Talon Is Nothing Then
            Return False
        End If

        Return True

    End Function

    Private Function RecuperarPagoTR(ByVal transicionRealizada As TransicionRealizadaDN) As PagoDN
        If transicionRealizada Is Nothing OrElse transicionRealizada.OperacionRealizadaOrigen Is Nothing OrElse transicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion Is Nothing OrElse Not TypeOf (transicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion) Is PagoDN Then
            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El objeto TransicionRealizadaDN no es válido para aplicar la condición de guarda")
        End If

        Return transicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion

    End Function

#End Region

End Class
