Imports Framework.AS
Imports Framework.Utilidades

Public Class GestionPagosAS
    Inherits Framework.AS.BaseAS



    Public Function CargarAgrupacionID(ByVal pAgrupApunteImpDDN As GestionPagos.DN.AgrupApunteImpDDN) As GestionPagos.DN.AgrupApunteImpDDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim AgrupApunteImpDDN As Byte() = Framework.Utilidades.Serializador.Serializar(pAgrupApunteImpDDN)
        CargarAgrupacionID = Framework.Utilidades.Serializador.DesSerializar(ws.CargarAgrupacionID(AgrupApunteImpDDN))


    End Function

    Public Function CrearAgrupacionID(ByVal pParEntFiscalGenericaParam As GestionPagos.DN.ParEntFiscalGenericaParamDN) As GestionPagos.DN.ParEntFiscalGenericaParamDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim ParEntFiscalGenericaParam As Byte() = Framework.Utilidades.Serializador.Serializar(pParEntFiscalGenericaParam)
        CrearAgrupacionID = Framework.Utilidades.Serializador.DesSerializar(ws.CrearAgrupacionID(ParEntFiscalGenericaParam))


    End Function

    Public Function BuscarImportesDebidosLibres(ByVal pParEntFiscalGenericaParam As GestionPagos.DN.ParEntFiscalGenericaParamDN) As Data.DataSet

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim ParEntFiscalGenericaParam As Byte() = Framework.Utilidades.Serializador.Serializar(pParEntFiscalGenericaParam)
        BuscarImportesDebidosLibres = Framework.Utilidades.Serializador.DesSerializar(ws.BuscarImportesDebidosLibres(ParEntFiscalGenericaParam))


    End Function



    Public Function AnularPagosNoEmitidosYCrearPagoAgrupador(ByVal pPagoAgrupador As GestionPagos.DN.PagoDN) As GestionPagos.DN.PagoDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim PagoAgrupador As Byte() = Framework.Utilidades.Serializador.Serializar(pPagoAgrupador)

        Return Framework.Utilidades.Serializador.DesSerializar(ws.AnularPagosNoEmitidosYCrearPagoAgrupador(PagoAgrupador))


    End Function






    Public Function CrearPagoAgrupadorProvisional(ByVal pPago As GestionPagos.DN.PagoDN) As GestionPagos.DN.PagoDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim pagocomp As Byte() = Framework.Utilidades.Serializador.Serializar(pPago)

        Return Framework.Utilidades.Serializador.DesSerializar(ws.CrearPagoAgrupadorProvisional(pagocomp))


    End Function


    Public Function CompensarPago(ByVal pPagoCompensador As GestionPagos.DN.PagoDN) As GestionPagos.DN.PagoDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim pagocomp As Byte() = Framework.Utilidades.Serializador.Serializar(pPagoCompensador)

        Return Framework.Utilidades.Serializador.DesSerializar(ws.CompensarPago(pagocomp))


    End Function
    Public Function AnularPago(ByVal pPago As GestionPagos.DN.PagoDN) As GestionPagos.DN.PagoDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim pagocomp As Byte() = Framework.Utilidades.Serializador.Serializar(pPago)

        Return Framework.Utilidades.Serializador.DesSerializar(ws.AnularPago(pagocomp))


    End Function
    Public Function EfectuarPago(ByVal pPago As GestionPagos.DN.PagoDN) As GestionPagos.DN.PagoDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim pagocomp As Byte() = Framework.Utilidades.Serializador.Serializar(pPago)

        Return Framework.Utilidades.Serializador.DesSerializar(ws.EfectuarPago(pagocomp))

    End Function

    Public Function LiquidarPago(ByVal pPago As GestionPagos.DN.PagoDN) As GestionPagos.DN.PagoDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim pagocomp As Byte() = Framework.Utilidades.Serializador.Serializar(pPago)

        Return Framework.Utilidades.Serializador.DesSerializar(ws.LiquidarPago(pagocomp))

    End Function
    Public Function EfectuarYLiquidarPago(ByVal pPago As GestionPagos.DN.PagoDN) As GestionPagos.DN.ColLiquidacionPagoDN

        Dim ws As New GestionPagosWS.GestionPagosWS

        ws.Url = RedireccionURL(ws.Url)
        ws.CookieContainer = ContenedorSessionAS.contenedorSessionC()
        Dim pagocomp As Byte() = Framework.Utilidades.Serializador.Serializar(pPago)

        Return Framework.Utilidades.Serializador.DesSerializar(ws.EfectuarYLiquidarPago(pagocomp))

    End Function

End Class
