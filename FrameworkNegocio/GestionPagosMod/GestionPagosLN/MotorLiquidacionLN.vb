Imports Framework.LogicaNegocios.Transacciones
Public Class MotorLiquidacionLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    Implements FN.GestionPagos.LN.ILiquidadorConcretoLN










    Public Function AnularOCompensarPago(ByVal pPagoCompensador As DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As OperacionILiquidadorConcretoLN Implements ILiquidadorConcretoLN.AnularOCompensarPago
        Using tr As New Transaccion

            If pPagoCompensador.PagoCompensado Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Un pago compensador debe tener un pago compensado no nulo")
            End If

            AnularOCompensarPago = RecuperarLiquidadorConcreto(pPagoCompensador).AnularOCompensarPago(pPagoCompensador, colLiqPago)
            tr.Confirmar()

        End Using
    End Function

    Public Overridable Function AnularOrigenImpDeb(ByVal pOrigenImpDeb As DN.IOrigenIImporteDebidoDN, ByVal pfechaAnulacion As Date) As DN.ColLiquidacionPagoDN Implements ILiquidadorConcretoLN.AnularOrigenImpDeb

        ' consiste en anular o compensar todos los pagos asociados a su importe debido producto




        'Using tr As New Transaccion



        '    Dim ColPago As FN.GestionPagos.DN.ColPagoDN
        '    Dim ad As New FN.GestionPagos.AD.LiquidacionPagoAD
        '    ColPago = ad.RecuperarPagos(CType(pOrigenImpDeb.IImporteDebidoDN, FN.GestionPagos.DN.ApunteImpDDN))

        '    Dim col As New FN.GestionPagos.DN.ColLiquidacionPagoDN

        '    For Each p As FN.GestionPagos.DN.PagoDN In ColPago
        '        Dim ColLiquidacionPago As FN.GestionPagos.DN.ColLiquidacionPagoDN

        '        If Me.AnularOCompensarPago(p.CrearPagoCompensador, ColLiquidacionPago) = OperacionILiquidadorConcretoLN.Ninguna Then
        '            Throw New ApplicationException("No fue posible anular o compesanr el pago id: " & p.ID & " asociado al importe debido id:" & pOrigenImpDeb.IImporteDebidoDN.ID)
        '        End If
        '        col.AddRange(ColLiquidacionPago)
        '    Next
        '    tr.Confirmar()
        '    Return col
        'End Using


        Using tr As New Transaccion


            AnularOrigenImpDeb = RecuperarLiquidadorConcreto(pOrigenImpDeb).AnularOrigenImpDeb(pOrigenImpDeb, pfechaAnulacion)
            tr.Confirmar()

        End Using



    End Function

    Public Sub AnularPago(ByVal pPago As DN.PagoDN) Implements ILiquidadorConcretoLN.AnularPago
        Using tr As New Transaccion


            RecuperarLiquidadorConcreto(pPago).AnularPago(pPago)
            tr.Confirmar()

        End Using
    End Sub

    Public Function CompensarPago(ByVal pPagoCompensador As DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As DN.PagoDN Implements ILiquidadorConcretoLN.CompensarPago
        Using tr As New Transaccion

            If pPagoCompensador.PagoCompensado Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Un pago compensador debe tener un pago compensado no nulo")
            End If

            CompensarPago = RecuperarLiquidadorConcreto(pPagoCompensador).CompensarPago(pPagoCompensador, colLiqPago)
            tr.Confirmar()

        End Using
    End Function
    '''' <summary>
    '''' procedimiento encargado de guardar un nuevo importe debido y de ordenar la liquidación de otros importes debidos a liquidadores concretos
    '''' </summary>
    '''' <param name="origen"></param>
    '''' <remarks></remarks>
    'Public Function GuardarNuevoapunteImporteDebido(ByVal origen As DN.IOrigenIImporteDebidoDN) As FN.GestionPagos.DN.ColIImporteDebidoDN Implements ILiquidadorConcretoLN.GuardarNuevoapunteImporteDebido
    '    Using tr As New Transaccion


    '        Dim recliquidador As New RecuperadorLiquidadoresConcretosLN
    '        Dim liquidador As ILiquidadorConcretoLN = recliquidador.RecuperarLiquidador(origen)

    '        liquidador.GuardarNuevoapunteImporteDebido(origen)
    '        tr.Confirmar()

    '    End Using
    'End Function

    Public Function LiquidarPago(ByVal pPago As DN.PagoDN) As DN.ColLiquidacionPagoDN Implements ILiquidadorConcretoLN.LiquidarPago
        Using tr As New Transaccion


            LiquidarPago = RecuperarLiquidadorConcreto(pPago).LiquidarPago(pPago)
            tr.Confirmar()

        End Using
    End Function

    ''' <summary>
    ''' recupera el liquidador asigando a el origen del importe debido, del importe debido base del pago
    ''' </summary>
    ''' <param name="pPago"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function RecuperarLiquidadorConcreto(ByVal pPago As DN.PagoDN) As FN.GestionPagos.LN.ILiquidadorConcretoLN
        Dim recliquidador As New RecuperadorLiquidadoresConcretosLN
        Return recliquidador.RecuperarLiquidador(pPago.RecuperarImportedebidoBase.HuellaIOrigenImpDebDN.TipoEntidadReferida, Now)

    End Function


    Public Function RecuperarLiquidadorConcreto(ByVal pIOrigenIImporteDebido As DN.IOrigenIImporteDebidoDN) As FN.GestionPagos.LN.ILiquidadorConcretoLN
        Dim recliquidador As New RecuperadorLiquidadoresConcretosLN
        Return recliquidador.RecuperarLiquidador(CType(pIOrigenIImporteDebido, Object).GetType, Now)

    End Function




    Public Sub EfectuarPago(ByVal pPago As DN.PagoDN) Implements ILiquidadorConcretoLN.EfectuarPago
        Using tr As New Transaccion


            RecuperarLiquidadorConcreto(pPago).EfectuarPago(pPago)
            tr.Confirmar()

        End Using
    End Sub

    Public Sub EmitirPago(ByVal pPago As DN.PagoDN) Implements ILiquidadorConcretoLN.EmitirPago
        Using tr As New Transaccion


            RecuperarLiquidadorConcreto(pPago).EmitirPago(pPago)
            tr.Confirmar()

        End Using
    End Sub

    Public Function DevolverPago(ByVal pPagoCompensador As DN.PagoDN, ByRef colLiqPago As DN.ColLiquidacionPagoDN) As DN.PagoDN Implements ILiquidadorConcretoLN.DevolverPago
        Using tr As New Transaccion


            DevolverPago = RecuperarLiquidadorConcreto(pPagoCompensador).DevolverPago(pPagoCompensador, colLiqPago)
            tr.Confirmar()

        End Using
    End Function

    Public Function EfectuarYLiquidar(ByVal pPago As DN.PagoDN) As DN.ColLiquidacionPagoDN Implements ILiquidadorConcretoLN.EfectuarYLiquidar
        Using tr As New Transaccion


            EfectuarYLiquidar = RecuperarLiquidadorConcreto(pPago).EfectuarYLiquidar(pPago)
            tr.Confirmar()

        End Using
    End Function

    Public Function AnularOrigenImpDebSinCompensarPagosEfectuados(ByVal pOrigenImpDeb As DN.IOrigenIImporteDebidoDN, ByVal pfechaAnulacion As Date) As DN.ColLiquidacionPagoDN Implements ILiquidadorConcretoLN.AnularOrigenImpDebSinCompensarPagosEfectuados
        Using tr As New Transaccion


            AnularOrigenImpDebSinCompensarPagosEfectuados = RecuperarLiquidadorConcreto(pOrigenImpDeb).AnularOrigenImpDebSinCompensarPagosEfectuados(pOrigenImpDeb, pfechaAnulacion)
            tr.Confirmar()

        End Using

    End Function

    Public Function CompensarLiquidaconesDePago(ByVal pPago As DN.PagoDN, ByRef colLiqPago As DN.ColLiquidacionPagoDN) As DN.PagoDN Implements ILiquidadorConcretoLN.CompensarLiquidaconesDePago


        Using tr As New Transaccion

            CompensarLiquidaconesDePago = RecuperarLiquidadorConcreto(pPago).CompensarLiquidaconesDePago(pPago, colLiqPago)
            tr.Confirmar()

        End Using


    End Function

    Public Function AnularPagosNoEmitidosYCrearPagoAgrupador(ByVal pPagoAgrupador As DN.PagoDN) As DN.PagoDN Implements ILiquidadorConcretoLN.AnularPagosNoEmitidosYCrearPagoAgrupador

        Using tr As New Transaccion

            AnularPagosNoEmitidosYCrearPagoAgrupador = RecuperarLiquidadorConcreto(pPagoAgrupador).AnularPagosNoEmitidosYCrearPagoAgrupador(pPagoAgrupador)
            tr.Confirmar()

        End Using

    End Function

    Public Function CrearPagoAgrupadorProvisional(ByVal pPago As DN.PagoDN) As DN.PagoDN Implements ILiquidadorConcretoLN.CrearPagoAgrupadorProvisional
        Using tr As New Transaccion

            CrearPagoAgrupadorProvisional = RecuperarLiquidadorConcreto(pPago).CrearPagoAgrupadorProvisional(pPago)
            tr.Confirmar()

        End Using
    End Function

    'Public Function CrearPagoAgrupadorProvisional1(ByVal pHuellaPago As Framework.DatosNegocio.HEDN) As DN.PagoDN Implements ILiquidadorConcretoLN.CrearPagoAgrupadorProvisional
    '    Using tr As New Transaccion

    '        CrearPagoAgrupadorProvisional1 = RecuperarLiquidadorConcreto(pHuellaPago).CrearPagoAgrupadorProvisional(pHuellaPago)
    '        tr.Confirmar()

    '    End Using
    'End Function
End Class
