Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Ficheros.FicherosDN
Public Class IdentificacionDocumentoLN


    Public Function RecuperarOcrearIdentitific(ByVal pTipoFichero As TipoFicheroDN, ByVal Identificacion As String) As IdentificacionDocumentoDN




        Using tr As New Transaccion


            Dim ad As New FicherosAD.IdentificacionDocumentoAD(Transaccion.Actual, Recurso.Actual)

            RecuperarOcrearIdentitific = ad.RecuperarOcrearIdentitific(pTipoFichero, Identificacion)

            tr.Confirmar()

        End Using





    End Function



End Class
