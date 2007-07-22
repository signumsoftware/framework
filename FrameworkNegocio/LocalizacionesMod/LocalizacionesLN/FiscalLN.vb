Imports Framework.LogicaNegocios.Transacciones
Public Class FiscalLN

    Public Function RecuperarEntidadFiscalGenerica(ByVal pCifNif As String) As FN.Localizaciones.DN.EntidadFiscalGenericaDN


        Using tr As New Transaccion


            Dim ad As New FN.Localizaciones.AD.FiscalAD
            RecuperarEntidadFiscalGenerica = ad.RecuperarEntidadFiscalGenerica(pCifNif)

            tr.Confirmar()

        End Using



    End Function




End Class
