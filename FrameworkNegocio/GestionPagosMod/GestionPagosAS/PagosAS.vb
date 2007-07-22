Imports Framework.AS
Imports Framework.Utilidades

Public Class PagosAS
    Inherits Framework.AS.BaseAS

    Public Function GuardarTalonDoc(ByVal pTDoc As GestionPagos.DN.TalonDocumentoDN) As GestionPagos.DN.TalonDocumentoDN
        Dim ws As New PagosWS.PagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return Serializador.DesSerializar(Serializador.Serializar(pTDoc))

    End Function

    Public Function CargarPagos(ByVal pDts As GestionPagos.DN.dtsGestionPagos, ByVal tipoOrigen As FN.GestionPagos.DN.TipoEntidadOrigenDN, ByVal operacion As Framework.Procesos.ProcesosDN.OperacionDN) As Data.DataSet

        Dim ws As New PagosWS.PagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim ba As Byte() = Framework.Utilidades.Serializador.Serializar(pDts)
        Dim tipoEO As Byte() = Framework.Utilidades.Serializador.Serializar(tipoOrigen)
        Dim op As Byte() = Framework.Utilidades.Serializador.Serializar(operacion)

        Return ws.CargarPagos(ba, tipoEO, op)

    End Function

    Public Function AltaModificacionProveedores(ByVal pDts As GestionPagos.DN.dtsGestionPagos) As Data.DataSet

        Dim ws As New PagosWS.PagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim ba As Byte() = Framework.Utilidades.Serializador.Serializar(pDts)

        Return ws.AltaModificacionProveedores(ba)

    End Function

    Public Function GuardarPlantillaCarta(ByVal pPlantillaCarta As FN.GestionPagos.DN.PlantillaCartaDN) As FN.GestionPagos.DN.PlantillaCartaDN
        Dim ws As New PagosWS.PagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return Serializador.DesSerializar(ws.GuardarPlantillaCarta(Serializador.Serializar(pPlantillaCarta)))

    End Function

    Public Function RecuperarTalonDN(ByVal pID As String) As GestionPagos.DN.TalonDN
        Dim ws As New PagosWS.PagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return Serializador.DesSerializar(ws.RecuperarTalonDN(pID))
    End Function

    Public Function GuardarConfiguracionImpresionTalon(ByVal pConfiguracionImpresion As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN) As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
        Dim ws As New PagosWS.PagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return Serializador.DesSerializar(ws.GuardarConfiguracionImpresion(Serializador.Serializar(pConfiguracionImpresion)))
    End Function

    Public Function GuardarTalonDN(ByVal pTalonDN As FN.GestionPagos.DN.TalonDN) As FN.GestionPagos.DN.TalonDN
        Dim ws As New PagosWS.PagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        Return Serializador.DesSerializar(ws.GuardarTalonDN(Serializador.Serializar(pTalonDN)))
    End Function

    Public Function RecuperarFicherosTransferenciasActivos() As GestionPagos.DN.ColFicheroTransferenciaDN
        Dim ws As New PagosWS.PagosWS()
        Dim colFT As GestionPagos.DN.ColFicheroTransferenciaDN

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()

        colFT = Serializador.DesSerializar(ws.RecuperarFicherosTransferenciasActivos())

        Return colFT

    End Function

End Class
