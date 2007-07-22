Imports FN.Localizaciones.DN
Imports FN.Localizaciones.AD
Imports Framework.LogicaNegocios.Transacciones


Public Class LocalizacionesLN

    Public Function RecuperarLocalidadporCodigoPostal(ByVal pCodigoPostal As String) As ColLocalidadDN
        Using tr As New Transaccion()
            Dim ad As New LocalizacionesAD()
            Return ad.RecuperarLocalidadesPorCodigoPostal(pCodigoPostal)
            tr.Confirmar()
        End Using
    End Function

End Class



