Imports Framework.LogicaNegocios.Transacciones
Public Class ControladorOperacionesEjemplo


    Public Function AltaContendoraEntidadPruebas(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object) As ContenedoraEntidadDePruebaDN





        Using tr As New Transaccion


            Dim ebp As EntidadDePruebaDN = pTransicionRealizada.OperacionRealizadaOrigen.ObjetoIndirectoOperacion
            Dim cep As New ContenedoraEntidadDePruebaDN
            cep.EntidadDePrueba = ebp

            pTransicionRealizada.OperacionRealizadaDestino.ObjetoDirectoOperacion = ebp

            AltaContendoraEntidadPruebas = cep

            tr.Confirmar()

        End Using




    End Function



End Class
