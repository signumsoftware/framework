Imports Framework.LogicaNegocios.Transacciones

Public Class NotaLN



    Public Function RecuperarDTSNotas(ByVal pEntidad As Framework.DatosNegocio.IEntidadBaseDN) As Data.DataSet



        Using tr As New Transaccion

            Dim ad As New NotasAD.NotaAD

            RecuperarDTSNotas = ad.RecuperarDTSNotas(pEntidad)
            tr.Confirmar()

        End Using



    End Function




End Class
