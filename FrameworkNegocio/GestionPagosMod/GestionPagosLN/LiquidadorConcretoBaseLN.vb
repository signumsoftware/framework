
Imports Framework.LogicaNegocios.Transacciones
Public MustInherit Class LiquidadorConcretoBaseLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    Implements FN.GestionPagos.LN.ILiquidadorConcretoLN


    Public Shared mRecuperarColLiquidacionMap As FN.GestionPagos.DN.ColLiquidacionMapDN

    Public Sub New()
        mRecuperarColLiquidacionMap = New FN.GestionPagos.DN.ColLiquidacionMapDN
    End Sub
    Private Function RecuperarColLiquidacionMapDN(ByVal pRefrescar As Boolean) As FN.GestionPagos.DN.ColLiquidacionMapDN


        If mRecuperarColLiquidacionMap.Count = 0 OrElse pRefrescar Then

            mRecuperarColLiquidacionMap.AddRangeObject(Me.RecuperarLista(Of FN.GestionPagos.DN.LiquidacionMapDN)())
        End If

        Return mRecuperarColLiquidacionMap
    End Function

    ''' <summary>
    ''' metodo sobreescribible que pertite a las clases derivadas modificar als entidades que deben de usarse como elemento de filtro en los mapeados de liquidacion
    ''' </summary>
    ''' <param name="pPago"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overridable Function RecuperarEntidadesrelacidadasdeLiquidacion(ByVal pPago As FN.GestionPagos.DN.PagoDN) As Framework.DatosNegocio.ColHEDN



        Using tr As New Transaccion


            Dim ad As New FN.GestionPagos.AD.OrigenImporteDebidoAD

            Dim oid As FN.GestionPagos.DN.OrigenIdevBaseDN = ad.Recuperar(pPago.ApunteImpDOrigen)
            RecuperarEntidadesrelacidadasdeLiquidacion = oid.ColHEDN

            tr.Confirmar()

        End Using






    End Function

    ''' <summary>
    ''' convierte un pago en pagado y
    ''' genera las liquidaciones necesarias para dicho pago segun su origen de importe debido
    ''' </summary>
    ''' <param name="pPago"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overridable Function LiquidarPago(ByVal pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.ColLiquidacionPagoDN Implements FN.GestionPagos.LN.ILiquidadorConcretoLN.LiquidarPago


        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion

            ' el pago debe tener un iporte debido
            If pPago.ApunteImpDOrigen Is Nothing OrElse pPago.ApunteImpDProducto Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("el pago debe de tener un importe debido")
            End If

            ' el pago debe disponer de una FechaEfecto
            If pPago.FechaEfecto = Date.MinValue Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("el pago debe de tener una fecha de efecto")
            End If

            If pPago.ApunteImpDOrigen.FAnulacion <> Date.MinValue Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("el pago debe de tener una fecha de efecto")
            End If



            ' recuperar el mapeado de liquidacion
            Dim miRecuperarColLiquidacionMap As FN.GestionPagos.DN.ColLiquidacionMapDN = RecuperarColLiquidacionMapDN(False)

            ' recuperar la coleccion de guid de entidades relacioandas con el origen importe debido dorigen del pago
            Dim colhednRelacioandas As Framework.DatosNegocio.ColHEDN = RecuperarEntidadesrelacidadasdeLiquidacion(pPago)

            ' buscamos las entidades entre los mapeados para ver que liquidacion le corresponde
            Dim ColLiquidacionMapAplicables As FN.GestionPagos.DN.ColLiquidacionMapDN = miRecuperarColLiquidacionMap.FiltrarXEntidadesReferidas(colhednRelacioandas)


            ''''''''''''''''''''''''''''''''''''''''''
            ' ejecutar los apuntes preparatorios de liquidacion para las map seleccioandas
            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''



            Dim primerPago As Boolean ' determinar del numero de pago que es en la serie de pagos de un id valor cacheado en el apgo
            primerPago = (pPago.PosicionPago = DN.PosicionPago.Primero)


            Dim colliq As New FN.GestionPagos.DN.ColLiquidacionPagoDN
            For Each lqmap As FN.GestionPagos.DN.LiquidacionMapDN In ColLiquidacionMapAplicables


                Dim deudor As Localizaciones.DN.EntidadFiscalGenericaDN = pPago.ApunteImpDOrigen.Acreedora
                Dim Acreedor As Localizaciones.DN.EntidadFiscalGenericaDN = lqmap.EntidadLiquidadora


                Dim liq As FN.GestionPagos.DN.LiquidacionPagoDN
                liq = New FN.GestionPagos.DN.LiquidacionPagoDN
                liq.pago = pPago
                liq.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(liq)
                liq.IImporteDebidoDN.Deudora = deudor
                liq.IImporteDebidoDN.Acreedora = Acreedor
                liq.ColHeCausas.Add(lqmap.HeCausaLiquidacion)

                Select Case lqmap.TipoCalculoImporte
                    Case DN.TipoCalculoImporte.Porcentual
                        liq.IImporteDebidoDN.Importe = CalcualrImporteLiquidacionFraccioanble(colhednRelacioandas, primerPago, pPago, lqmap)
                    Case DN.TipoCalculoImporte.Fijo
                        liq.IImporteDebidoDN.Importe = lqmap.PorcentageOValor
                    Case Else
                        Throw New Framework.LogicaNegocios.ApplicationExceptionLN("TipoCalculoImporte no reconocido")
                End Select


                If liq.IImporteDebidoDN.Importe <> 0 Then
                    ' la fecha de efecto del nuevo importe debido será la fecha de efecto del pago más el tiempo de debora que se indique en el mapeado de liquidacion
                    liq.IImporteDebidoDN.FEfecto = lqmap.Aplazamiento.IncrementarFecha(pPago.FechaEfecto)
                    liq.IImporteDebidoDN.FCreación = Now
                    colliq.Add(liq)
                End If


            Next


            Me.GuardarGenerico(colliq)

            tr.Confirmar()

            Return colliq
        End Using


    End Function

    Protected Overridable Function CalcualrImporteLiquidacionFraccioanble(ByVal colhednRelacioandas As Framework.DatosNegocio.ColHEDN, ByVal primerPago As Boolean, ByVal pPago As FN.GestionPagos.DN.PagoDN, ByVal lqmap As FN.GestionPagos.DN.LiquidacionMapDN) As Double
        Return pPago.Importe * lqmap.PorcentageOValor
    End Function



    ''' <summary>
    ''' un pago solo es anulable si no esta ni siquiera emitido
    ''' un pago anulado no se puede reactivar
    ''' dado que un pago anulable no puede haber sido emitido no hay liquidaciones que compensar
    ''' </summary>
    ''' <param name="pPago"></param>
    ''' <remarks></remarks>
    Public Overridable Sub AnularPago(ByVal pPago As FN.GestionPagos.DN.PagoDN) Implements FN.GestionPagos.LN.ILiquidadorConcretoLN.AnularPago
        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion
            ' si el pago esta emitido (recivo) o  pagado (tranferencia) o liquidado no es anulable
            'podriamos revisar si tiene realciones con alguno de los bjetos anteriores
            ' dado que un pago anulable no puede haber sido emitido no hay liquidaciones que compensar


            ' en este caso el pago es anulable
            Dim mensaje As String

            If Not pPago.Anulable(mensaje) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
            End If

            ' verificar que el pago no tiene liquidaciones creadas
            ' no parece una buena prueba ya que un pago puede haberse realizado y no por ello tener liquidaciones asociadas


            pPago.AnularPago(mensaje, Now)
            Me.GuardarGenerico(pPago)

            tr.Confirmar()

        End Using

    End Sub







    ''' <summary>
    '''  a partir de un pago compensador donde se informa de el importe y otras caracteristicas del pago
    '''  generan las liquidaciones compensatorias de ls lquidaciones que tubiera asignadas el pago original
    ''' </summary>
    ''' <param name="pPagoCompensador"></param>
    ''' <param name="colLiqPago"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Overridable Function CompensarPago(ByVal pPagoCompensador As FN.GestionPagos.DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As FN.GestionPagos.DN.PagoDN Implements FN.GestionPagos.LN.ILiquidadorConcretoLN.CompensarPago
        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion

            ' el pago siempre es compensable  por otro pago donde los aacreedores y deudores sean inversos a el poago que compensa
            ' pero no tiene sentido compensar un pago que ni tansiquiera se ha hemitido, tedria sentido eliminarlo
            ' tampoco puede ser compensado un pago que ya haya sido compensado
            Dim mensaje As String

            If pPagoCompensador.EstadoIntegridad(mensaje) = Framework.DatosNegocio.EstadoIntegridadDN.Inconsistente Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
            End If



            ' en el proceso de compensación un pago compensador no puede tener fecha de efecto dado que tidavia no se ha efectuiado
            If pPagoCompensador.FechaEfecto <> Date.MinValue Then
                Throw New ApplicationException("en el proceso de compensación un pago compensador no puede tener fecha de efecto dado que tidavia no se ha efectuiado")
            End If

            CompensarLiquidaconesDePago(pPagoCompensador, colLiqPago)

            Me.GuardarGenerico(pPagoCompensador)
            CompensarPago = pPagoCompensador

            tr.Confirmar()
        End Using
    End Function

    Public Function CompensarLiquidaconesDePago(ByVal pPagoCompensador As DN.PagoDN, ByRef colLiqPago As DN.ColLiquidacionPagoDN) As DN.PagoDN Implements ILiquidadorConcretoLN.CompensarLiquidaconesDePago
        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion

            ' el pago siempre es compensable  por otro pago donde los aacreedores y deudores sean inversos a el poago que compensa
            ' pero no tiene sentido compensar un pago que ni tansiquiera se ha hemitido, tedria sentido eliminarlo
            ' tampoco puede ser compensado un pago que ya haya sido compensado
            Dim mensaje As String



            ' verificar si el pago ya ha sido compensado
            Dim ad As New FN.GestionPagos.AD.GestionPagosAD
            Dim idPagoCompenador As String = ad.RecuperarIDPagoCompensador(pPagoCompensador.PagoCompensado)
            If Not String.IsNullOrEmpty(idPagoCompenador) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("el pago de id " & pPagoCompensador.PagoCompensado.ID & " ya está compeando por el pago de id:" & idPagoCompenador)
            End If


            ' en este caso el pago es compensable

            ' para compensarlo tenemos que 
            ' obtener todos las liquidaciones realizadas  
            ' crear el pago compesado 
            ' crear una liquidación de compensacion por cada una de las recuperadas
            ' el importe de las lisquidaciones de compesación sera, el mismo si el importe del pago compensador es el mismo que el del pago compensado
            ' o el equivalente porcentual si el importe es distinto


            ' recuperar mediante un ad especifico todas las liquidaciones de pago
            '            Dim liqad As New FN.GestionPagos.AD.LiquidacionPagoAD
            ' Dim colLiquidacionesOriginales As FN.GestionPagos.DN.ColLiquidacionPagoDN = liqad.RecuperarXPago(pPagoCompensador.PagoCompensado)

            ' las liquidaciones cuyos importes debido no hayan generado pagos pueden ser anuladas, el resto pueden ser compensadas

            Dim lqad As New FN.GestionPagos.AD.LiquidacionPagoAD
            Dim colLqCompensables, colLqAnulables As FN.GestionPagos.DN.ColLiquidacionPagoDN
            lqad.RecuperarXPago(pPagoCompensador.PagoCompensado, colLqCompensables, colLqAnulables)


            ' crear las liquidaciones compesadoras
            colLiqPago = CrearColLiquidacionCompensadaAPorcentage(pPagoCompensador, colLqCompensables)
            Me.GuardarGenerico(colLiqPago)

            ' anular liquidaciones
            AnularColLiquidaciones(colLqAnulables)


            tr.Confirmar()
        End Using
    End Function


    Private Function AnularColLiquidaciones(ByVal pColLiquidacionPago As FN.GestionPagos.DN.ColLiquidacionPagoDN)
        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion
            For Each lq As FN.GestionPagos.DN.LiquidacionPagoDN In pColLiquidacionPago

                AnularLiquidacion(lq)

            Next
            tr.Confirmar()
        End Using
    End Function



    Public Sub AnularLiquidacion(ByVal pLiquidacionPago As FN.GestionPagos.DN.LiquidacionPagoDN)

        ' una liquidación sera anulable siempre que su importe debido producto , no tenga asociado pagos emitidos o superiro
        ' si una liquidación tine pagos planificados asociados eston deberan ser anulados a su vez


        ' verificar si hay pagos que anular
        Dim ad As New FN.GestionPagos.AD.LiquidacionPagoAD
        Dim colp As FN.GestionPagos.DN.ColPagoDN = ad.RecuperarPagos(pLiquidacionPago)


        For Each pago As FN.GestionPagos.DN.PagoDN In colp
            ' si el pago no se puede anular no no se puede anular la liquidación y debio ser compensada
            Me.AnularPago(pago)
        Next


        pLiquidacionPago.Anular()
        Me.Guardar(pLiquidacionPago)





    End Sub

    Private Function CrearColLiquidacionCompensadaAPorcentage(ByVal pPagoCompensador As FN.GestionPagos.DN.PagoDN, ByVal colLiquidacionesOriginales As FN.GestionPagos.DN.ColLiquidacionPagoDN) As FN.GestionPagos.DN.ColLiquidacionPagoDN


        Dim colLiqCompensatorias As New FN.GestionPagos.DN.ColLiquidacionPagoDN


        For Each lq As FN.GestionPagos.DN.LiquidacionPagoDN In colLiquidacionesOriginales
            colLiqCompensatorias.Add(lq.CrearLiqPagoCompensatoria(pPagoCompensador))
        Next

        Return colLiqCompensatorias
    End Function


    Public Function AnularOCompensarPago(ByVal pPagoCompensador As DN.PagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As OperacionILiquidadorConcretoLN Implements ILiquidadorConcretoLN.AnularOCompensarPago
        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion
            If pPagoCompensador.PagoCompensado.FechaEmision = Date.MinValue AndAlso pPagoCompensador.PagoCompensado.FechaEfecto = Date.MinValue Then
                ' el pago es anulable porque no se ha emitido
                Me.AnularPago(pPagoCompensador.PagoCompensado)

                tr.Confirmar()
                Return OperacionILiquidadorConcretoLN.PagoAnulado

            Else
                ' el pago ya no es anulable porque al menos se ha emitido

                If pPagoCompensador.PagoCompensado.FechaEfecto <> Date.MinValue Then
                    ' el pago hatenido efecto y en principio es compesable

                    ' verificar si el pago ya ha sido compeado

                    Dim ad As New FN.GestionPagos.AD.GestionPagosAD
                    Dim idPagoCompenador As String = ad.RecuperarIDPagoCompensador(pPagoCompensador.PagoCompensado)
                    If Not String.IsNullOrEmpty(idPagoCompenador) Then
                        tr.Confirmar()
                        Return OperacionILiquidadorConcretoLN.Ninguna
                    End If

                    Me.CompensarPago(pPagoCompensador, colLiqPago)
                    tr.Confirmar()
                    Return OperacionILiquidadorConcretoLN.PagoCompensado
                Else
                    ' el pago no es anulable ni compesable
                    tr.Confirmar()
                    Return OperacionILiquidadorConcretoLN.Ninguna
                End If


            End If



        End Using
    End Function

    Public Overridable Function AnularOrigenImpDeb(ByVal pOrigenImpDeb As DN.IOrigenIImporteDebidoDN, ByVal pFechaEfecto As Date) As DN.ColLiquidacionPagoDN Implements ILiquidadorConcretoLN.AnularOrigenImpDeb

        '' el id es anulable si no se ha geneado un pago para el o si todos los pagos son  anulables o compenables
        'Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion


        '    Dim colLQTot As New DN.ColLiquidacionPagoDN


        '    ' recuperamos los pagos relacioandos con el importe debido
        '    Dim colpagos As FN.GestionPagos.DN.ColPagoDN


        '    ' procedemos a su anulacion
        '    For Each pago As FN.GestionPagos.DN.PagoDN In colpagos
        '        Dim colLQ As DN.ColLiquidacionPagoDN
        '        Me.AnularOCompensarPago(pago, colLQ)
        '        colLQTot.AddRange(colLQ)
        '    Next

        '    AnularOrigenImpDeb = colLQTot

        '    tr.Confirmar()

        'End Using



        Using tr As New Transaccion

            'Throw New NotImplementedException


            Dim mensaje As String

            If Not pOrigenImpDeb.Anulable(mensaje) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
            End If


            Dim ColPago As FN.GestionPagos.DN.ColPagoDN
            Dim ad As New FN.GestionPagos.AD.LiquidacionPagoAD

            ColPago = ad.RecuperarPagos(CType(pOrigenImpDeb.IImporteDebidoDN, FN.GestionPagos.DN.ApunteImpDDN))

            Dim col As New FN.GestionPagos.DN.ColLiquidacionPagoDN

            For Each p As FN.GestionPagos.DN.PagoDN In ColPago
                Dim ColLiquidacionPago As FN.GestionPagos.DN.ColLiquidacionPagoDN

                If Me.AnularOCompensarPago(p.CrearPagoCompensador, ColLiquidacionPago) = OperacionILiquidadorConcretoLN.Ninguna Then
                    Throw New ApplicationException("No fue posible anular o compesanr el pago id: " & p.ID & " asociado al importe debido id:" & pOrigenImpDeb.IImporteDebidoDN.ID)
                End If
                col.AddRange(ColLiquidacionPago)
            Next


            pOrigenImpDeb.Anular(pFechaEfecto)
            pOrigenImpDeb.IImporteDebidoDN.HuellaIOrigenImpDebDN.AsignarEntidad(pOrigenImpDeb)
            Me.GuardarGenerico(pOrigenImpDeb)

            tr.Confirmar()
            Return col
        End Using



    End Function
    Public Function AnularOrigenImpDebSinCompensarPagosEfectuados(ByVal pOrigenImpDeb As DN.IOrigenIImporteDebidoDN, ByVal pFechaEfecto As Date) As DN.ColLiquidacionPagoDN Implements ILiquidadorConcretoLN.AnularOrigenImpDebSinCompensarPagosEfectuados

        Using tr As New Transaccion

            Dim mensaje As String

            If Not pOrigenImpDeb.Anulable(mensaje) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
            End If


            Dim ColPago As FN.GestionPagos.DN.ColPagoDN
            Dim ad As New FN.GestionPagos.AD.LiquidacionPagoAD
            ColPago = ad.RecuperarPagos(CType(pOrigenImpDeb.IImporteDebidoDN, FN.GestionPagos.DN.ApunteImpDDN))

            Dim col As New FN.GestionPagos.DN.ColLiquidacionPagoDN

            For Each p As FN.GestionPagos.DN.PagoDN In ColPago



                If p.FechaEfecto <> Date.MinValue OrElse p.FechaEmision <> Date.MinValue Then

                    ' el pago no es  anulable ' pero queremos compesar las liquidaciones que tiene asociadas

                    If p.FechaEfecto <> Date.MinValue Then
                        ' no queremos anular el pago (como movimiento de dimero del cliente hacia la aseguradora por la prestacion de un servicio que posteriromente ha sido anulado)
                        ' pero si queremos anular o compesar las liquidaciones que este puedo generar por que lo son sobre coberturas y condiciones que han cambiado posiblemente

                        Dim ColLiquidacionPago As FN.GestionPagos.DN.ColLiquidacionPagoDN
                        Me.CompensarLiquidaconesDePago(p, ColLiquidacionPago)
                        col.AddRange(ColLiquidacionPago)


                    Else
                        ' TODO: importante
                        ' ojo que pasa con las liquidaciones de un pago que esta emitido pero que  todabia no se ha efectuado,
                        ' la idea es que al estar referido a un importe debido origen anulado no debieran de generarse
                        ' implementado en el metodo liquidarpago

                    End If
                Else
                    Me.AnularPago(p)


                End If



            Next

            pOrigenImpDeb.Anular(pFechaEfecto)
            pOrigenImpDeb.IImporteDebidoDN.HuellaIOrigenImpDebDN.AsignarEntidad(pOrigenImpDeb)
            Me.GuardarGenerico(pOrigenImpDeb)


            tr.Confirmar()
            Return col
        End Using
    End Function

    Public Overridable Sub EfectuarPago(ByVal pPago As DN.PagoDN) Implements ILiquidadorConcretoLN.EfectuarPago



        Using tr As New Transaccion


            If pPago.FechaEfecto > Date.MinValue Then
                Throw New ApplicationException("el pago ya esta efectuado y no puede volver a ejecutarse.")
            End If

            pPago.FechaEfecto = Now
            pPago.GenerarImporteDebidoProducto()

            Me.Guardar(Of DN.PagoDN)(pPago)



            tr.Confirmar()

        End Using




    End Sub


    Public Overridable Sub EmitirPago(ByVal pPago As DN.PagoDN) Implements ILiquidadorConcretoLN.EmitirPago
        Using tr As New Transaccion


            If pPago.FechaEfecto > Date.MinValue Then
                Throw New ApplicationException("el pago ya esta efectuado y no puede emitirse.")
            End If
            Dim mensaje As String
            If Not pPago.EmitirPago(mensaje) Then
                Throw New ApplicationException(mensaje)
            End If


            Me.Guardar(Of DN.PagoDN)(pPago)
            tr.Confirmar()

        End Using


    End Sub

    Public Function DevolverPago(ByVal pPagoCompensador As DN.PagoDN, ByRef colLiqPago As DN.ColLiquidacionPagoDN) As DN.PagoDN Implements ILiquidadorConcretoLN.DevolverPago


        'Using tr As New Transaccion
        '    Dim pago As FN.GestionPagos.DN.PagoDN
        '    colLiqPago = New DN.ColLiquidacionPagoDN

        '    Dim colLiqPagoNuevas As DN.ColLiquidacionPagoDN
        '    colLiqPagoNuevas = New DN.ColLiquidacionPagoDN
        '    pago = Me.CompensarPago(pPagoCompensador, colLiqPagoNuevas)
        '    colLiqPago.AddRange(colLiqPagoNuevas)

        '    colLiqPagoNuevas = Me.EfectuarYLiquidar(pago)
        '    colLiqPago.AddRange(colLiqPagoNuevas)

        '    tr.Confirmar()

        '    Return pago
        'End Using


        Using tr As New Transaccion

            If pPagoCompensador.PagoCompensado.FechaEfecto = Date.MinValue Then
                Throw New ApplicationException("el pago a compesar debe estar efectuado")
            End If



            Dim pago As FN.GestionPagos.DN.PagoDN

            pago = Me.CompensarPago(pPagoCompensador, colLiqPago)
            Me.EfectuarPago(pago)

            tr.Confirmar()

            Return pago
        End Using




    End Function



    Public Function EfectuarYLiquidar(ByVal pPago As DN.PagoDN) As DN.ColLiquidacionPagoDN Implements ILiquidadorConcretoLN.EfectuarYLiquidar




        Using tr As New Transaccion


            Me.EfectuarPago(pPago)
            EfectuarYLiquidar = Me.LiquidarPago(pPago)
            tr.Confirmar()

        End Using




    End Function


    ''' <summary>
    ''' dado un pago que debe compensar a todos los pagos no anulados de un importe debido
    '''  obtiene su importe debido anula todos los pagos pendientes de emision
    ''' y verifica que el nuevo pago creadosu importe coincide con  el importe de los pagos anulados
    ''' </summary>
    ''' <param name="pPago"></param>
    ''' <remarks></remarks>
    Public Overridable Function AnularPagosNoEmitidosYCrearPagoAgrupador(ByVal pPago As FN.GestionPagos.DN.PagoDN) As FN.GestionPagos.DN.PagoDN Implements ILiquidadorConcretoLN.AnularPagosNoEmitidosYCrearPagoAgrupador
        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion



            If Not String.IsNullOrEmpty(pPago.ID) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("el pago ya ha sido credo")
            End If


            ' recuperar el conjuento de pagos NO ANULADOS y  pendientes de ser efectuados
            Dim miad As New FN.GestionPagos.AD.GestionPagosAD
            Dim colpagos As FN.GestionPagos.DN.ColPagoDN = miad.RecuperarColPagosMismoOrigenImporteDebido(pPago.ApunteImpDOrigen)

            ' todos los pagos deben tnener igual deudor y acreedor
            If Not colpagos.SoloDosEntidadesFiscalesMismoSentido Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("no todos los pagos tiene el mismo sentodo o las mismas entidades")
            End If
            If Not pPago.Destinatario.GUID = colpagos(0).Destinatario.GUID Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El destinatario no coincide")
            End If
            If Not pPago.Deudor.GUID = colpagos(0).Deudor.GUID Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El deudor no coincide")
            End If


            Dim colpagosNoAnulados, colpagosNoAnuladosNoEfectuados As FN.GestionPagos.DN.ColPagoDN
            colpagosNoAnulados = colpagos.RecuperarColPagos(DN.FiltroPago.No, DN.FiltroPago.Todos, DN.FiltroPago.Todos)
            colpagosNoAnuladosNoEfectuados = colpagosNoAnulados.RecuperarColPagos(DN.FiltroPago.No, DN.FiltroPago.No, DN.FiltroPago.No)

            ' calcualr el importe total
            Dim importePagoAgrupador As Double = CalcualrImportePAgoAgrupador(colpagos)
            Dim colpagosNoAnuladosNoEmitidosNoEfectuados As FN.GestionPagos.DN.ColPagoDN = colpagos.RecuperarColPagos(DN.FiltroPago.No, DN.FiltroPago.No, DN.FiltroPago.No)

            ' verificar el pago creado
            If pPago.Importe <> importePagoAgrupador Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El importe del pago no coincide con el importe de los pagos a anaualar")
            End If

            ' anular cada uno de los apagos de la  coleccion
            Dim mensaje As String
            For Each pg As FN.GestionPagos.DN.PagoDN In colpagosNoAnuladosNoEmitidosNoEfectuados

                ' en este caso el pago es anulable
                If Not pg.Anulable(mensaje) Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
                End If

                pg.AnularPago(mensaje, Now)
                Me.GuardarGenerico(pg)
            Next



            ' guardar el pago agrupador 
            Me.GuardarGenerico(pPago)
            AnularPagosNoEmitidosYCrearPagoAgrupador = pPago
            tr.Confirmar()

        End Using

    End Function

    'Public Function CrearPagoAgrupadorProvisional(ByVal pHuellaPago As Framework.DatosNegocio.HEDN) As DN.PagoDN Implements ILiquidadorConcretoLN.CrearPagoAgrupadorProvisional


    '    Using tr As New Transaccion

    '        If Not pHuellaPago.TipoEntidadReferida Is GetType(DN.PagoDN) Then
    '            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("la huella no tine un tipo compatible")
    '        End If

    '        Dim pago As DN.PagoDN = Me.RecuperarGenerico(pHuellaPago)
    '        CrearPagoAgrupadorProvisional = CrearPagoAgrupadorProvisional(pago)



    '        tr.Confirmar()

    '    End Using



    'End Function




    Private Function CalcualrImportePAgoAgrupador(ByVal colpagos As FN.GestionPagos.DN.ColPagoDN) As Double
        Dim colpagosNoAnulados, colpagosNoAnuladosNoEmitidosNoEfectuados, colpagosNoAnuladosEmitidosNoEfectuados, colpagosNoAnuladosEfectuados As FN.GestionPagos.DN.ColPagoDN
        colpagosNoAnulados = colpagos.RecuperarColPagos(DN.FiltroPago.No, DN.FiltroPago.Todos, DN.FiltroPago.Todos)
        colpagosNoAnuladosEfectuados = colpagosNoAnulados.RecuperarColPagos(DN.FiltroPago.No, DN.FiltroPago.Todos, DN.FiltroPago.Si)
        colpagosNoAnuladosEmitidosNoEfectuados = colpagosNoAnulados.RecuperarColPagos(DN.FiltroPago.No, DN.FiltroPago.Si, DN.FiltroPago.No)
        colpagosNoAnuladosNoEmitidosNoEfectuados = colpagosNoAnulados.RecuperarColPagos(DN.FiltroPago.No, DN.FiltroPago.No, DN.FiltroPago.No)


        ' calcualr el importe total
        Dim Importedescubierto As Double = +colpagosNoAnuladosEfectuados.ImporteDescontadoCompensaciones(colpagos(0).Destinatario) - colpagosNoAnuladosEfectuados.ImporteDescontadoCompensaciones(colpagos(0).Destinatario)

        Dim importePagoAgrupador As Double = colpagos(0).ApunteImpDOrigen.Importe - Importedescubierto
        Return importePagoAgrupador
    End Function

    Public Function CrearPagoAgrupadorProvisional(ByVal pPago As DN.PagoDN) As DN.PagoDN Implements ILiquidadorConcretoLN.CrearPagoAgrupadorProvisional
        Using tr As New Framework.LogicaNegocios.Transacciones.Transaccion


            ' recuperar el conjuento de pagos NO ANULADOS y  pendientes de ser efectuados
            Dim miad As New FN.GestionPagos.AD.GestionPagosAD
            Dim colpagos As FN.GestionPagos.DN.ColPagoDN = miad.RecuperarColPagosMismoOrigenImporteDebido(pPago.ApunteImpDOrigen)


            If colpagos.Count = 0 Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(" No existen pagos programados no emitidos, no anulados y no efectuados para el importe debido: " & pPago.ApunteImpDOrigen.ID)
            End If


            ' todos los pagos deben tnener igual deudor y acreedor
            If Not colpagos.SoloDosEntidadesFiscalesMismoSentido Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("no todos los pagos tiene el mismo sentodo o las mismas entidades")
            End If


            Dim importePagoAgrupador As Double = CalcualrImportePAgoAgrupador(colpagos)
            Dim colpagosNoAnuladosNoEmitidosNoEfectuados As FN.GestionPagos.DN.ColPagoDN = colpagos.RecuperarColPagos(DN.FiltroPago.No, DN.FiltroPago.No, DN.FiltroPago.No)

            ' anular cada uno de los apagos de la  coleccion
            Dim mensaje As String
            For Each pg As FN.GestionPagos.DN.PagoDN In colpagosNoAnuladosNoEmitidosNoEfectuados
                ' en este caso el pago es anulable
                If Not pg.Anulable(mensaje) Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
                End If

            Next


            Dim pagoAgrupador As New FN.GestionPagos.DN.PagoDN
            pagoAgrupador.Destinatario = pPago.Destinatario
            pagoAgrupador.Deudor = pPago.Deudor
            pagoAgrupador.Importe = importePagoAgrupador
            pagoAgrupador.ApunteImpDOrigen = pPago.ApunteImpDOrigen

            CrearPagoAgrupadorProvisional = pagoAgrupador

            ' guardar el pago agrupador 
            tr.Confirmar()

        End Using
    End Function
End Class
