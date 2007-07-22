Imports Framework.Procesos



Public Class ProcesosLNC

    Public Function SumarUno(ByVal pEnt As EntidadDePrueba, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN) As Framework.DatosNegocio.IEntidadBaseDN

        pEnt.Importe += 1
        Dim opas As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return opas.EjecutarOperacion(pEnt, pTransicionRealizada, Nothing)

    End Function


    Public Function RestarUno(ByVal pEnt As EntidadDePrueba, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN) As Framework.DatosNegocio.IEntidadBaseDN

        pEnt.Importe -= 1
        Dim opas As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return opas.EjecutarOperacion(pEnt, pTransicionRealizada, Nothing)

    End Function
    Public Function RestarCinco(ByVal pEnt As EntidadDePrueba, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN) As Framework.DatosNegocio.IEntidadBaseDN

        pEnt.Importe -= 5
        Dim opas As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return opas.EjecutarOperacion(pEnt, pTransicionRealizada, Nothing)

    End Function
    Public Function SumarCinco(ByVal pEnt As EntidadDePrueba, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN) As Framework.DatosNegocio.IEntidadBaseDN

        pEnt.Importe += 5
        Dim opas As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return opas.EjecutarOperacion(pEnt, pTransicionRealizada, Nothing)

    End Function
End Class


