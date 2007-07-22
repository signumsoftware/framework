
Public Interface ILiquidadorConcretoLN
    ' Function GuardarNuevoapunteImporteDebido(ByVal origen As FN.GestionPagos.DN.IOrigenIImporteDebidoDN) As FN.GestionPagos.DN.ColIImporteDebidoDN

    Sub EfectuarPago(ByVal pPago As FN.GestionPagos.DN.PagoDN)


    Function LiquidarPago(ByVal pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.ColLiquidacionPagoDN

    ''' <summary>
    ''' a partir de un pago crea otro cuya mision es agrupar a todos los pagos no anulados y no emitidos
    ''' con este pago se puede llamar a el metodo AnularPagosNoEmitidosYCrearPagoAgrupador para proceder ala terminacion de la operacion
    ''' </summary>
    ''' <param name="pPago"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function CrearPagoAgrupadorProvisional(ByVal pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.PagoDN
    ' Function CrearPagoAgrupadorProvisional(ByVal pHuellaPago As Framework.DatosNegocio.HEDN) As FN.GestionPagos.DN.PagoDN

    Function AnularPagosNoEmitidosYCrearPagoAgrupador(ByVal pPagoAgrupador As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.PagoDN

    ''' <summary>
    ''' ++Objetivo
    '''
    ''' ++Precondiciones
    '''		el pago no debe estar ya anulado
    ''' 	el pago no puede estar emitido
    ''' 	  
    ''' ++Postcondiciones
    '''		el pago queda anulado 
    '''     no computa como pago activo para el importe debido
    '''     no puede ser origen para liquidaciones
    ''' 		   
    ''' ++ Notas
    ''' </summary>
    ''' <param name="pPago"></param>
    ''' <remarks></remarks>
    Sub AnularPago(ByVal pPago As FN.GestionPagos.DN.PagoDN)




    ''' <summary>
    ''' en este caso se trata de un pago que ya se ha efectuado y que es devuelto por una entidad bancaria
    ''' acepta un pago compensador (el pago de devolucion) del pago original
    ''' no ejecta la emisión del pago ya que la operión no requeire nuestra emisión 
    ''' si efectua el pago 
    ''' </summary>
    ''' <param name="pPagoCompensador"></param>
    ''' <param name="colLiqPago"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function DevolverPago(ByVal pPagoCompensador As FN.GestionPagos.DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As FN.GestionPagos.DN.PagoDN

    Function EfectuarYLiquidar(ByVal pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.ColLiquidacionPagoDN



    Function CompensarPago(ByVal pPagoCompensador As FN.GestionPagos.DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As FN.GestionPagos.DN.PagoDN
    Function CompensarLiquidaconesDePago(ByVal pPago As FN.GestionPagos.DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As FN.GestionPagos.DN.PagoDN

    ''' <summary>
    ''' ++ Objetivo
    ''' supone anular el origen de un importe debido
    ''' 
    ''' ++Precondiciones
    ''' el origen debido no puede estar previamente anulado
    ''' 
    ''' ++Postcondiciones
    ''' el origen debido quedará anuado.
    ''' el importedebido generado tambien quedará anulado
    ''' los pagos generados quedarán anulados o compensados
    ''' las liquidaciones de los pagos quedarán anuladas o compensadas
    ''' 
    ''' ++Notas
    ''' 
    ''' No es posible reactivar un origen de importedebido anulado, 
    ''' pero si es posible generar otro apartir de este si el primero está anulado
    ''' </summary>
    ''' <param name="pOrigenImpDeb"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function AnularOrigenImpDeb(ByVal pOrigenImpDeb As FN.GestionPagos.DN.IOrigenIImporteDebidoDN, ByVal pFechaEfecto As Date) As FN.GestionPagos.DN.ColLiquidacionPagoDN



    Function AnularOrigenImpDebSinCompensarPagosEfectuados(ByVal pOrigenImpDeb As FN.GestionPagos.DN.IOrigenIImporteDebidoDN, ByVal pFechaEfecto As Date) As FN.GestionPagos.DN.ColLiquidacionPagoDN



    ''' <summary>
    ''' anula o compensa un pago
    '''IMPORTANTE: el pago compesado queda programado, pero No Efectuado
    ''' </summary>
    ''' <param name="pPagoCompensador"></param>
    ''' <param name="colLiqPago"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Function AnularOCompensarPago(ByVal pPagoCompensador As FN.GestionPagos.DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As OperacionILiquidadorConcretoLN

    Sub EmitirPago(ByVal pPago As FN.GestionPagos.DN.PagoDN)

End Interface


Public Enum OperacionILiquidadorConcretoLN
    PagoAnulado
    PagoCompensado
    Ninguna
End Enum