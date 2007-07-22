Imports Framework.LogicaNegocios.Transacciones
Public Class LiquidadorConcretoRVLN
    Inherits FN.GestionPagos.LN.LiquidadorConcretoBaseLN



    Private Function RecuperarEntidadObjetivoLiquidacionColaborador(ByVal codColaborador As String) As FN.Localizaciones.DN.EntidadFiscalGenericaDN


        Using tr As New Transaccion

            Dim empad As New FN.Empresas.AD.EmpresaAD

            Dim colab As FN.Empresas.DN.EntidadColaboradoraDN = empad.RecuperarColaborador(codColaborador)

            'If colab.Nombre <> lqmap.CodGrupoLiquidacion Then
            '    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("el grupo de liquidacion no corresponde con la entidad liquidadora")
            'End If



            If TypeOf colab.EntidadAsociada Is FN.Empresas.DN.EmpresaDN Then
                Dim emp As FN.Empresas.DN.EmpresaDN = colab.EntidadAsociada
                RecuperarEntidadObjetivoLiquidacionColaborador = emp.EntidadFiscal

            ElseIf TypeOf colab.EntidadAsociada Is FN.Empresas.DN.EmpleadoDN Then
                Dim emp As FN.Empresas.DN.SedeEmpresaDN = colab.EntidadAsociada
                RecuperarEntidadObjetivoLiquidacionColaborador = emp.Empresa.EntidadFiscal

            ElseIf TypeOf colab.EntidadAsociada Is FN.Empresas.DN.EmpleadoDN Then
                Dim emp As FN.Empresas.DN.EmpresaDN = colab.EntidadAsociada
                RecuperarEntidadObjetivoLiquidacionColaborador = emp.EntidadFiscal

            Else
                Return Nothing

            End If


            tr.Confirmar()

        End Using

    End Function


    Public Overrides Function LiquidarPago(ByVal pPago As GestionPagos.DN.PagoDN) As GestionPagos.DN.ColLiquidacionPagoDN



        Using tr As New Transaccion


            Dim colLiq As GestionPagos.DN.ColLiquidacionPagoDN = MyBase.LiquidarPago(pPago)

            ' añadir las liquidaciones concretas con cmerciales
            Dim oidad As New FN.RiesgosVehiculos.AD.PeriodoRenovacionPolizaOidAD

            Dim oid As FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN = oidad.RecuperarPeriodoRenovacionPolizaOidDN(pPago, True)



            ' si exite y el pago era contra un importe debido generado por el primer perido de renovacion hay que liquidar
            If Not oid Is Nothing AndAlso Not String.IsNullOrEmpty(oid.PeriodoRenovacionPoliza.Poliza.CodColaborador) AndAlso oid.PeriodoRenovacionPoliza.FI = oid.PeriodoRenovacionPoliza.Poliza.FechaAlta Then


                Dim primerPago As Boolean ' determinar del numero de pago que es en la serie de pagos de un id valor cacheado en el apgo
                primerPago = (pPago.PosicionPago = GestionPagos.DN.PosicionPago.Primero)

                ' recuperar la coleccion de guid de entidades relacioandas con el origen importe debido dorigen del pago
                Dim colhednRelacioandas As Framework.DatosNegocio.ColHEDN = RecuperarEntidadesRelacidadasDeLiquidacion(pPago)


                ' hay que pagar a colaboradores por cada pago vinculado a una poliza del primer perido de renovacion

                Dim LiquidacionPagoAD As New FN.GestionPagos.AD.LiquidacionMapAD
                Dim lqmap As FN.GestionPagos.DN.LiquidacionMapDN = LiquidacionPagoAD.Recuperar("COLABORADORES-Comerciales").Item(0)  ' recuperar por el nombre del gurpo comerciales

                Dim deudor As Localizaciones.DN.EntidadFiscalGenericaDN = pPago.ApunteImpDOrigen.Acreedora
                Dim Acreedor As Localizaciones.DN.EntidadFiscalGenericaDN = RecuperarEntidadObjetivoLiquidacionColaborador(oid.PeriodoRenovacionPoliza.Poliza.CodColaborador)


                Dim liq As FN.GestionPagos.DN.LiquidacionPagoDN
                liq = New FN.GestionPagos.DN.LiquidacionPagoDN
                liq.pago = pPago
                liq.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(liq)
                liq.IImporteDebidoDN.Deudora = deudor
                liq.IImporteDebidoDN.Acreedora = Acreedor
                liq.ColHeCausas.Add(lqmap.HeCausaLiquidacion)

                Select Case lqmap.TipoCalculoImporte
                    Case GestionPagos.DN.TipoCalculoImporte.Porcentual
                        liq.IImporteDebidoDN.Importe = CalcualrImporteLiquidacionFraccioanble(colhednRelacioandas, primerPago, pPago, lqmap)
                    Case GestionPagos.DN.TipoCalculoImporte.Fijo
                        liq.IImporteDebidoDN.Importe = lqmap.PorcentageOValor
                    Case Else
                        Throw New Framework.LogicaNegocios.ApplicationExceptionLN("TipoCalculoImporte no reconocido")
                End Select


                If liq.IImporteDebidoDN.Importe <> 0 Then
                    ' la fecha de efecto del nuevo importe debido será la fecha de efecto del pago más el tiempo de debora que se indique en el mapeado de liquidacion
                    liq.IImporteDebidoDN.FEfecto = lqmap.Aplazamiento.IncrementarFecha(pPago.FechaEfecto)
                    liq.IImporteDebidoDN.FCreación = Now
                    colLiq.Add(liq)
                End If



                Me.GuardarGenerico(colLiq)

            End If

            LiquidarPago = colLiq

            tr.Confirmar()

        End Using






    End Function


    Public Overrides Function AnularOrigenImpDeb(ByVal pOrigenImpDeb As GestionPagos.DN.IOrigenIImporteDebidoDN, ByVal pFechaEfecto As Date) As GestionPagos.DN.ColLiquidacionPagoDN

        ' añade la funcionalidad de liquidacion para los colaboradores comerciales en la cual:
        ' si en una poliza de un solo año antes de su primera renovacion es anulada, hay que compesar los pagos efectuados a los colaborradores comerciales
        Dim colLiq As New FN.GestionPagos.DN.ColLiquidacionPagoDN
        If TypeOf pOrigenImpDeb Is FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN Then
            Dim oidp As FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN = pOrigenImpDeb
            ' si el perido de renovacion a eliminar tine la misma fecha de alta de la poliza es que es el primero
            If oidp.PeriodoRenovacionPoliza.FI = oidp.PeriodoRenovacionPoliza.Poliza.FechaAlta Then
                ' como es el primero hay que compensar los pagos que por esta poliza se ubieran programado y no anulado a ese colaborador

                Dim miad As New FN.RiesgosVehiculos.AD.RiesgosVehiculosAD
                Dim colpagosaAnularoCompensar As FN.GestionPagos.DN.ColPagoDN = miad.RecuperarPagos(oidp, oidp.PeriodoRenovacionPoliza.Poliza.ID)
                Dim mliq As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colpagosaAnularoCompensar
                    Dim micolLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN
                    mliq.AnularOCompensarPago(pago, micolLiq)
                    colLiq.AddRange(micolLiq)
                Next

            End If
            ' 
        End If
        Dim micolLiq2 As FN.GestionPagos.DN.ColLiquidacionPagoDN = MyBase.AnularOrigenImpDeb(pOrigenImpDeb, pFechaEfecto)

        colLiq.AddRange(micolLiq2)
        Return colLiq

    End Function




    Public Overrides Function RecuperarEntidadesRelacidadasDeLiquidacion(ByVal pPago As FN.GestionPagos.DN.PagoDN) As Framework.DatosNegocio.ColHEDN

        ' se recipera la tarifa y en los datos de tarifa se 
        Dim miad As New FN.RiesgosVehiculos.AD.RiesgosVehiculosAD
        Dim datos As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = miad.RecuperarDatosTarifaVehiculosDN(pPago)
        Return datos.RecuperarCausas

    End Function


    Protected Overrides Function CalcualrImporteLiquidacionFraccioanble(ByVal colhednRelacioandas As Framework.DatosNegocio.ColHEDN, ByVal primerPago As Boolean, ByVal pPago As FN.GestionPagos.DN.PagoDN, ByVal lqmap As FN.GestionPagos.DN.LiquidacionMapDN) As Double




        Dim miad As New FN.RiesgosVehiculos.AD.RiesgosVehiculosAD


        Dim datos As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = miad.RecuperarDatosTarifaVehiculosDN(pPago)

        If datos Is Nothing Then

            Return MyBase.CalcualrImporteLiquidacionFraccioanble(colhednRelacioandas, primerPago, pPago, lqmap)
        Else

            Return CalcualrImporteLiquidacionFraccioanbleDeTarifa(datos, colhednRelacioandas, primerPago, pPago, lqmap)
        End If

    End Function



    Private Function ImporteTotalDeLaCausaDeLiquidacion(ByVal datos As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN, ByVal colhednRelacioandas As Framework.DatosNegocio.ColHEDN, ByVal lqmap As FN.GestionPagos.DN.LiquidacionMapDN, ByVal fraccioanble As Boolean) As Double



        'se buscan las causas (coberturas a cuyos importes modulados hay que liquidar , antes de comisiones)
        Dim datosCache As FN.RiesgosVehiculos.DN.ColIOperacionCausaRVCacheDN


        If lqmap.CausaPrimaModulada Then


            ' primas moduladas

            ImporteTotalDeLaCausaDeLiquidacion = datos.ImportePrimaModuladaFraccionable + datos.ImportePrimaModuladaNoFraccioable


        Else
            datosCache = datos.RecuperarEntidadCachexGuidCausa(lqmap.HeCausaLiquidacion.GUIDReferida)
            If datosCache.Count = 0 Then
                Return 0
            End If
            ImporteTotalDeLaCausaDeLiquidacion = datosCache.CalcuarTotalValorresultadoOpr



        End If



        '' verificacion de coincidencia de fraccionamiento
        'If Not fraccioanble = datosCache(0).Fraccionable Then
        '    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("la informacion de fraccionameitno es discrepante ")
        'End If




    End Function


    Private Function PagoPolizaFraccionable(ByVal lqmap As FN.GestionPagos.DN.LiquidacionMapDN) As Boolean

        If lqmap.HeCausaLiquidacion Is Nothing Then
            Return True
        End If


        If lqmap.HeCausaLiquidacion.EntidadReferida Is Nothing Then
            Me.RecuperarGenerico(lqmap.HeCausaLiquidacion)
        End If

        ' verificar si es fraccioanble la causa de liquidacion
        Dim fraccioanble As Boolean = True
        If TypeOf lqmap.HeCausaLiquidacion.EntidadReferida Is FN.RiesgosVehiculos.DN.ImpuestoDN Then
            fraccioanble = CType(lqmap.HeCausaLiquidacion.EntidadReferida, FN.RiesgosVehiculos.DN.ImpuestoDN).Fraccionable
        ElseIf TypeOf lqmap.HeCausaLiquidacion.EntidadReferida Is FN.RiesgosVehiculos.DN.ComisionDN Then
            fraccioanble = CType(lqmap.HeCausaLiquidacion.EntidadReferida, FN.RiesgosVehiculos.DN.ComisionDN).Fraccionable
        ElseIf TypeOf lqmap.HeCausaLiquidacion.EntidadReferida Is FN.Seguros.Polizas.DN.CoberturaDN Then
            '  fraccioanble = CType(lqmap.HeCausaLiquidacion.EntidadReferida, FN.Seguros.Polizas.DN.CoberturaDN).Fraccionable
            fraccioanble = True
        End If

        Return fraccioanble
    End Function

    Protected Function CalcualrImporteLiquidacionFraccioanbleDeTarifa(ByVal datos As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN, ByVal colhednRelacioandas As Framework.DatosNegocio.ColHEDN, ByVal primerPago As Boolean, ByVal pPago As FN.GestionPagos.DN.PagoDN, ByVal lqmap As FN.GestionPagos.DN.LiquidacionMapDN) As Double




        ' verificar si es fraccioanble la causa de liquidacion
        Dim fraccioanble As Boolean = PagoPolizaFraccionable(lqmap)



        Dim miImporteTotalDeLaCausaDeLiquidacion As Double = ImporteTotalDeLaCausaDeLiquidacion(datos, colhednRelacioandas, lqmap, fraccioanble)




        ' clacualr el porcentage de pago
        Dim totalImportesNoFraccioanbles As Double = datos.TotalImporteNoFraccioanble
        Dim totalImportesFraccioanbles As Double = datos.TotalImporteFraccioanble
        Dim portetageIncidentiaCausaLiquEnTotal As Double = miImporteTotalDeLaCausaDeLiquidacion / totalImportesFraccioanbles




        'clacluar el importe dependiendo de si es fraccioanble o no
        If fraccioanble Then
            Dim importeDescontadoFraccioanble As Double

            If primerPago Then
                importeDescontadoFraccioanble = pPago.Importe - totalImportesNoFraccioanbles
            Else
                importeDescontadoFraccioanble = pPago.Importe
            End If

            If totalImportesFraccioanbles < 0 Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("el valor no puede ser ")
            End If

            Dim valorLiquidable As Double = importeDescontadoFraccioanble * portetageIncidentiaCausaLiquEnTotal

            ' como es fraccioanble lo imputado es sobre el importe pagado de la causa
            Return valorLiquidable * lqmap.PorcentageOValor

        Else
            If primerPago Then ' lo no fraccionable solo procede en el primer pago
                ' como no es fraccionable la imputacioón es necesariamente por todo el importe de la causa
                Return miImporteTotalDeLaCausaDeLiquidacion * lqmap.PorcentageOValor
            Else
                Return 0
            End If
        End If


    End Function






End Class
