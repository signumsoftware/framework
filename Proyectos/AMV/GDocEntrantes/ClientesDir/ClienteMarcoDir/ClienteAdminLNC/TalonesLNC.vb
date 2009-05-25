Imports Framework.Procesos

Imports FN.GestionPagos.DN

Public Class TalonesLNC

    Public Function SumarUno(ByVal pTalon As PagoDN, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN) As Framework.DatosNegocio.IEntidadBaseDN

        pTalon.Importe += 1
        Dim opas As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return opas.EjecutarOperacion(pTalon, pTransicionRealizada, Nothing)

    End Function


    Public Function RestarUno(ByVal pTalon As PagoDN, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN) As Framework.DatosNegocio.IEntidadBaseDN

        pTalon.Importe -= 1
        Dim opas As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return opas.EjecutarOperacion(pTalon, pTransicionRealizada, Nothing)

    End Function
    Public Function RestarCinco(ByVal pTalon As PagoDN, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN) As Framework.DatosNegocio.IEntidadBaseDN

        pTalon.Importe -= 5
        Dim opas As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return opas.EjecutarOperacion(pTalon, pTransicionRealizada, Nothing)

    End Function
    Public Function SumarCinco(ByVal pTalon As PagoDN, ByVal pTransicionRealizada As ProcesosDN.TransicionRealizadaDN) As Framework.DatosNegocio.IEntidadBaseDN

        pTalon.Importe += 5
        Dim opas As New Framework.Procesos.ProcesosAS.OperacionesAS
        Return opas.EjecutarOperacion(pTalon, pTransicionRealizada, Nothing)

    End Function
End Class
