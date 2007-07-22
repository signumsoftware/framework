Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting


Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections
Imports Framework.LogicaNegocios.Transacciones

Imports Framework.Operaciones.OperacionesDN
Imports Framework.Usuarios.DN


Imports Framework.TiposYReflexion.LN



<TestClass()> Public Class UnitTest1
    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN
#Region "Additional test attributes"
    '
    ' You can use the following additional attributes as you write your tests:
    '
    ' Use ClassInitialize to run code before running the first test in the class
    ' <ClassInitialize()> Public Shared Sub MyClassInitialize(ByVal testContext As TestContext)
    ' End Sub
    '
    ' Use ClassCleanup to run code after all tests in a class have run
    ' <ClassCleanup()> Public Shared Sub MyClassCleanup()
    ' End Sub
    '
    ' Use TestInitialize to run code before running each test
    ' <TestInitialize()> Public Sub MyTestInitialize()
    ' End Sub
    '
    ' Use TestCleanup to run code after each test has run
    ' <TestCleanup()> Public Sub MyTestCleanup()
    ' End Sub
    '
#End Region


    <TestMethod(), Timeout(5700000)> Public Sub Pre1v0GuardarPresupuesto()

        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)
            GuardarPresupuesto()

        End Using




    End Sub


    '<TestMethod(), Timeout(5700000)> Public Sub Pre2v0AltadePolizaDesdePresupuesto()

    '    ObtenerRecurso()
    '    Using New CajonHiloLN(mRecurso)





    '        Using tr As New Transaccion


    '            Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN
    '            Dim cuestionario As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

    '            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '            Dim presup As FN.Seguros.Polizas.DN.PresupuestoDN
    '            presup = ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PresupuestoDN))(0)


    '            Dim polln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN

    '            polln.AltaDePolizaDesdePresupuesto(presup)


    '            tr.Cancelar()


    '        End Using






    '    End Using




    'End Sub



    <TestMethod(), Timeout(5700000)> Public Sub Pre3v0BajadePolizaDesdePresupuesto()

        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)





            Using tr As New Transaccion


                Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
                pr = ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN))(0)

                ' el usuario deberia introducir la fecha en al que se da de baja
                Dim fb As Date = pr.FI.AddDays(5)


                Dim polln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                ' polln.BajaDePoliza(pr, pr.PeridoCoberturaActivo, fb)
                polln.BajaDePoliza(New FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN(pr), fb)


                tr.Confirmar()


            End Using






        End Using




    End Sub
    <TestMethod(), Timeout(5700000)> Public Sub Pre2v0AltadePolizaDesdePresupuestoB()

        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)





            Using tr As New Transaccion


                Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                Dim presup As FN.Seguros.Polizas.DN.PresupuestoDN
                presup = ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PresupuestoDN))(0)


                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
                Dim lnc As New FN.Seguros.Polizas.PolizasLNC.PolizasLNC
                ' pr = lnc.AltaDePolizaDesdePresupuesto(presup, presup.FuturoTomador.ValorCifNif, "0000ccc")
                ' pr = lnc.A


                ' el usuario introduce los datos de fecha de alta
                pr.FI = pr.PeridoCoberturaActivo.Tarifa.FEfecto.Date.AddDays(5)

                ' el usuqario introduce las condiciones de pago
                pr.PeridoCoberturaActivo.CondicionesPago = New GestionPagos.DN.CondicionesPagoDN
                pr.PeridoCoberturaActivo.CondicionesPago.ModalidadDePago = GestionPagos.DN.ModalidadPago.IngresoEnCuenta
                pr.PeridoCoberturaActivo.CondicionesPago.NumeroRecibos = 4
                pr.PeridoCoberturaActivo.CondicionesPago.PlazoEjecucion.Dias = 7


                ' el usuario crea o busca un tomador
                Dim tm As New FN.Seguros.Polizas.DN.TomadorDN
                Dim pf As FN.Personas.DN.PersonaFiscalDN = ln.RecuperarLista(GetType(FN.Personas.DN.PersonaFiscalDN))(0)
                tm.EntidadFiscalGenerica = pf.EntidadFiscalGenerica
                pr.Poliza.Tomador = tm


                Dim polln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                polln.AltaDePolizapp(pr, True)


                tr.Confirmar()


            End Using






        End Using




    End Sub


    <TestMethod(), Timeout(5700000)> Public Sub GuardarPoliza()

        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)
            GuardarPolizap(False)

        End Using




    End Sub

    <TestMethod(), Timeout(5700000)> Public Sub GuardarPolizaConPagoCompensado()

        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)
            GuardarPolizaConPagoCompensadop(False)

        End Using




    End Sub


    <TestMethod(), Timeout(5700000)> Public Sub pe0v1CrearPoliza()


        GuardarPoliza()





    End Sub

    <TestMethod(), Timeout(5700000)> Public Sub pe0v3DevolverPagoEfectuado()





        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd



            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN

            Dim colDatos As New FN.GestionPagos.DN.ColPagoDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))




            Using tr As New Transaccion


                For Each pago As FN.GestionPagos.DN.PagoDN In colDatos
                    If pago.FechaEfecto > Date.MinValue AndAlso pago.FechaAnulacion = Date.MinValue Then 'es un pago efectuado
                        Dim MotorLiquidacion As New GestionPagos.LN.MotorLiquidacionLN
                        Dim colliq As FN.GestionPagos.DN.ColLiquidacionPagoDN
                        MotorLiquidacion.DevolverPago(pago.CrearPagoCompensador, colliq)
                        Exit For
                    End If
                Next


                ' verificacion de la prueba

                Dim pago1 As FN.GestionPagos.DN.PagoDN = colDatos.Item(0)
                Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = miApunteImpDLN.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

                If Math.Round(saldo, 4) <> Math.Round(pago1.ApunteImpDOrigen.Importe, 4) Then
                    Throw New ApplicationException
                End If




                ' el importe debido tendria que salir entre los importes debidos DESCOMPENSADOS dado que un pago ha sido compensado y no se ha generado otro para equilibrarlo

                Dim ej As Framework.AccesoDatos.Ejecutor
                ej = New Framework.AccesoDatos.Ejecutor(Transaccion.Actual, Recurso.Actual)

                Dim ds As Data.DataSet = ej.EjecutarDataSet("select * from vwImportesDebidosNoCubiertosConPagos where idApunteImpDOrigen=" & pago1.ApunteImpDOrigen.ID)

                If ds.Tables(0).Rows.Count = 0 Then
                    Throw New ApplicationException("deberia exitir una entrada en la viata de importes debidos descompansado")
                End If

                tr.Confirmar()

            End Using






        End Using




    End Sub

    <TestMethod(), Timeout(5700000)> Public Sub pe0v21AnularPagosNoEmitidosYCrearPagoAgrupador()





        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd



            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN

            Dim colDatos As New FN.GestionPagos.DN.ColPagoDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))




            Using tr As New Transaccion





                Dim MotorLiquidacion As New GestionPagos.LN.MotorLiquidacionLN
                Dim pagoAgrupacion As FN.GestionPagos.DN.PagoDN = MotorLiquidacion.CrearPagoAgrupadorProvisional(colDatos.Item(0))

                Dim pagoAgrupacionResultado As FN.GestionPagos.DN.PagoDN = MotorLiquidacion.AnularPagosNoEmitidosYCrearPagoAgrupador(pagoAgrupacion)




                '' verificacion de la prueba



                ' debieran estar todos anulados menos uno

                Dim gpad As New FN.GestionPagos.AD.GestionPagosAD
                Dim colDatosResultantes As New FN.GestionPagos.DN.ColPagoDN
                colDatosResultantes.AddRangeObject(gpad.RecuperarColPagosMismoOrigenImporteDebido(pagoAgrupacionResultado.ApunteImpDOrigen))

                Dim anulados As Integer

                For Each pago As FN.GestionPagos.DN.PagoDN In colDatosResultantes
                    If pago.FAnulacion = Date.MinValue Then
                        anulados += 1
                    End If
                Next

                If anulados > 1 Then
                    Throw New ApplicationException("solo un pago debiera estar no anulado tras AnularPagosNoEmitidosYCrearPagoAgrupador")

                End If



                tr.Confirmar()

            End Using



        End Using




    End Sub
    <TestMethod(), Timeout(5700000)> Public Sub pe0v2efectuarSiguientePagoPoliza()





        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd



            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN

            Dim colDatos As New FN.GestionPagos.DN.ColPagoDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))




            Using tr As New Transaccion


                For Each pago As FN.GestionPagos.DN.PagoDN In colDatos
                    If pago.FechaEfecto = Date.MinValue AndAlso pago.FechaAnulacion = Date.MinValue Then
                        Dim MotorLiquidacion As New GestionPagos.LN.MotorLiquidacionLN
                        MotorLiquidacion.EmitirPago(pago)
                        MotorLiquidacion.EfectuarYLiquidar(pago)
                        Exit For
                    End If
                Next


                ' verificacion de la prueba

                Dim pago1 As FN.GestionPagos.DN.PagoDN = colDatos.Item(0)
                Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = miApunteImpDLN.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

                If Math.Round(saldo, 4) <> Math.Round(pago1.ApunteImpDOrigen.Importe - Math.Round(pago1.Importe, 4), 4) Then
                    Throw New ApplicationException
                End If


                tr.Confirmar()

            End Using





            ' verificacion de la prueba

            'Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            'bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            'colPagos1 = New FN.GestionPagos.DN.ColPagoDN
            'colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
            'If colPagos1.Count <> 1 OrElse colPagos1.Item(0).FAnulacion = Date.MinValue Then
            '    Throw New ApplicationException("error")
            'End If


            'Dim pago1 As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0)
            'Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN
            'Dim saldo As Double = miApunteImpDLN.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

            'If saldo <> pago1.IImporteDebidoOrigen.Importe Then
            '    Throw New ApplicationException
            'End If




            'Using tr3 As New Transaccion(True)

            '    Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
            '    Dim colliq As FN.GestionPagos.DN.ColLiquidacionPagoDN

            '    For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0
            '        colliq = ml.LiquidarPago(pago)
            '    Next


            '    ' creamos un pago plainifaco para la primera
            '    Dim pagoidlq As FN.GestionPagos.DN.PagoDN

            '    ''''' creacion de pago
            '    pagoidlq = New FN.GestionPagos.DN.PagoDN
            '    pagoidlq.IImporteDebidoOrigen = colliq(0).IImporteDebidoDN
            '    pagoidlq.Importe = pagoidlq.IImporteDebidoOrigen.Importe
            '    pagoidlq.Nombre = "pago planinificado 1liquidacion"
            '    Me.GuardarDatos(pagoidlq)



            '    ' cramos un  pago planificado y lo efectuamos para la segunda
            '    pagoidlq = New FN.GestionPagos.DN.PagoDN
            '    pagoidlq.IImporteDebidoOrigen = colliq(1).IImporteDebidoDN
            '    pagoidlq.Importe = pagoidlq.IImporteDebidoOrigen.Importe
            '    pagoidlq.Nombre = "pago abona 2liquidacion"
            '    Me.GuardarDatos(pagoidlq)
            '    Dim colpagos2 As New FN.GestionPagos.DN.ColPagoDN
            '    colpagos2.Add(pagoidlq)
            '    Me.EmitirPagos(colpagos2)
            '    Me.EfectuarPagop(colpagos2)






            '    ' verificacion de la prueba
            '    bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            '    Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
            '    ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
            '    ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
            '    ' debieran de exitir 3 apuntes debidos
            '    If ColApunteImpD.Count <> 5 Then
            '        Throw New ApplicationException("error")
            '    End If




            '    Dim ColPagov As FN.GestionPagos.DN.ColPagoDN
            '    ColPagov = New FN.GestionPagos.DN.ColPagoDN
            '    ColPagov.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
            '    ' debieran de exitir 3 apuntes debidos
            '    If ColPagov.Count <> 3 Then
            '        Throw New ApplicationException("error")
            '    End If



            '    'If ColApunteImpD.Item(1).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(1).FEfecto = Date.MinValue Then
            '    '    Throw New ApplicationException("error")
            '    'End If

            '    'If ColApunteImpD.Item(2).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(2).FEfecto = Date.MinValue Then
            '    '    Throw New ApplicationException("error")
            '    'End If

            '    'If ColApunteImpD.Item(3).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(3).FEfecto = Date.MinValue Then
            '    '    Throw New ApplicationException("error")
            '    'End If


            '    ' Dim id As FN.GestionPagos.DN.ApunteImpDDN = ColApunteImpD.EliminarEntidadDNxGUID(ColApunteImpD.Item(0).GUID)(0)
            '    'If ColApunteImpD(1).Importe <> (ColApunteImpD(2).Importe + ColApunteImpD(3).Importe) Then
            '    '    Throw New ApplicationException("error")
            '    'End If

            '    tr3.Confirmar()
            'End Using

        End Using




    End Sub

    ''' <summary>
    ''' una amplicación de coboertura supone
    ''' 1º anular el origen de importe debido anteriro de la tarifa1
    ''' 2º modificar la tarifa uno, en concreto su fecha de ficnalizacion
    ''' 3º crear un nuevo Period de cobertuta con su tarifa y su origen de importe debido
    ''' 
    ''' </summary>
    ''' <remarks></remarks>
    <TestMethod(), Timeout(5700000)> Public Sub pe1v1AmpliarCoberturasdePeridodeRenovacion()





        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd

            ' Dim empln As New FN.Empresas.LN.EmpresaLN
            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colDatos As New FN.Seguros.Polizas.DN.ColPeriodoRenovacionPolizaDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))

            Dim colopc As IList = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))

            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = colopc.Item(0)
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN '=
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))

            'Dim irec As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN
            'irec.Tarifa = Me.GenerarTarifa2(colcober)
            'irec.DataSoucers.Add(irec.Tarifa)
            'irec.DataSoucers.Add(Me.GenerarCuestionarioResuelto(colCaract))


            'opc.IOperacionDN.IRecSumiValorLN = irec
            'Dim valor As Double = opc.IOperacionDN.GetValor()

            'System.Diagnostics.Debug.WriteLine("VALOR TARIFA: " & valor)



            Using tr As New Transaccion

                ' debe de contener la unica poliza creada
                Dim nuevaTarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.GenerarTarifa2(colcober)
                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = colDatos.Item(0)

                Dim col As GestionPagos.DN.ColIImporteDebidoDN
                Dim PolizaRvLc As New RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                col = PolizaRvLc.ModificarCondicionesCoberturaNoRetroactiva(pr, nuevaTarifa, GenerarCuestionarioResuelto20(colCaract), nuevaTarifa.FEfecto, 100)



                ' verificacion de la prueba
                Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(0).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                Dim saldo2 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(1).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                If saldo2 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo3 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(2).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                If saldo3 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo4 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(3).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                If saldo4 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo5 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(4).Deudora, ColApunteImpD.Item(0).Acreedora, Date.MaxValue)

                If saldo5 + saldo <> 0 Then
                    Throw New ApplicationException
                End If


                tr.Cancelar()

            End Using






            'Using tr3 As New Transaccion(True)

            '    Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
            '    Dim colliq As FN.GestionPagos.DN.ColLiquidacionPagoDN

            '    For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0
            '        colliq = ml.LiquidarPago(pago)
            '    Next


            '    ' creamos un pago plainifaco para la primera
            '    Dim pagoidlq As FN.GestionPagos.DN.PagoDN

            '    ''''' creacion de pago
            '    pagoidlq = New FN.GestionPagos.DN.PagoDN
            '    pagoidlq.IImporteDebidoOrigen = colliq(0).IImporteDebidoDN
            '    pagoidlq.Importe = pagoidlq.IImporteDebidoOrigen.Importe
            '    pagoidlq.Nombre = "pago planinificado 1liquidacion"
            '    Me.GuardarDatos(pagoidlq)



            '    ' cramos un  pago planificado y lo efectuamos para la segunda
            '    pagoidlq = New FN.GestionPagos.DN.PagoDN
            '    pagoidlq.IImporteDebidoOrigen = colliq(1).IImporteDebidoDN
            '    pagoidlq.Importe = pagoidlq.IImporteDebidoOrigen.Importe
            '    pagoidlq.Nombre = "pago abona 2liquidacion"
            '    Me.GuardarDatos(pagoidlq)
            '    Dim colpagos2 As New FN.GestionPagos.DN.ColPagoDN
            '    colpagos2.Add(pagoidlq)
            '    Me.EmitirPagos(colpagos2)
            '    Me.EfectuarPagop(colpagos2)






            '    ' verificacion de la prueba
            '    bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            '    Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
            '    ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
            '    ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
            '    ' debieran de exitir 3 apuntes debidos
            '    If ColApunteImpD.Count <> 5 Then
            '        Throw New ApplicationException("error")
            '    End If




            '    Dim ColPagov As FN.GestionPagos.DN.ColPagoDN
            '    ColPagov = New FN.GestionPagos.DN.ColPagoDN
            '    ColPagov.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
            '    ' debieran de exitir 3 apuntes debidos
            '    If ColPagov.Count <> 3 Then
            '        Throw New ApplicationException("error")
            '    End If



            '    'If ColApunteImpD.Item(1).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(1).FEfecto = Date.MinValue Then
            '    '    Throw New ApplicationException("error")
            '    'End If

            '    'If ColApunteImpD.Item(2).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(2).FEfecto = Date.MinValue Then
            '    '    Throw New ApplicationException("error")
            '    'End If

            '    'If ColApunteImpD.Item(3).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(3).FEfecto = Date.MinValue Then
            '    '    Throw New ApplicationException("error")
            '    'End If


            '    ' Dim id As FN.GestionPagos.DN.ApunteImpDDN = ColApunteImpD.EliminarEntidadDNxGUID(ColApunteImpD.Item(0).GUID)(0)
            '    'If ColApunteImpD(1).Importe <> (ColApunteImpD(2).Importe + ColApunteImpD(3).Importe) Then
            '    '    Throw New ApplicationException("error")
            '    'End If

            '    tr3.Confirmar()
            'End Using

        End Using




    End Sub

    <TestMethod(), Timeout(5700000)> Public Sub pe2v1ModificarPOSITIVATarifadePeridodeRenovacion()




        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colDatos As New FN.Seguros.Polizas.DN.ColPeriodoRenovacionPolizaDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))

            Dim colopc As IList = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))

            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = colopc.Item(0)
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN '=
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))



            Using tr As New Transaccion

                ' debe de contener la unica poliza creada
                Dim nuevaTarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.GenerarTarifa2(colcober)
                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = colDatos.Item(0)

                Dim col As GestionPagos.DN.ColIImporteDebidoDN
                Dim PolizaRvLc As New RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                col = PolizaRvLc.ModificarCondicionesCoberturaRetroactiva(pr, nuevaTarifa, GenerarCuestionarioResuelto18(colCaract), nuevaTarifa.FEfecto, 100)



                ' verificacion de la prueba
                Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN

                Dim saldo As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(0).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo)

                Dim saldo2 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(1).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo2)
                If saldo2 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo3 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(2).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo3)

                If saldo3 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo4 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(3).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo4)

                If saldo4 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo5 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(4).Deudora, ColApunteImpD.Item(0).Acreedora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo5)
                System.Diagnostics.Debug.WriteLine(saldo5 + saldo)

                If saldo5 + saldo <> 0 Then
                    Throw New ApplicationException
                End If


                tr.Cancelar()


            End Using


        End Using


    End Sub

    <TestMethod(), Timeout(5700000)> Public Sub pe4v1ModificarNEGATIVATarifadePeridodeRenovacion()



        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colDatos As New FN.Seguros.Polizas.DN.ColPeriodoRenovacionPolizaDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))

            Dim colopc As IList = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))

            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = colopc.Item(0)
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN '=
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))



            Using tr As New Transaccion

                ' debe de contener la unica poliza creada
                Dim nuevaTarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.GenerarTarifa2(colcober)
                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = colDatos.Item(0)

                Dim col As GestionPagos.DN.ColIImporteDebidoDN
                Dim PolizaRvLc As New RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                col = PolizaRvLc.ModificarCondicionesCoberturaRetroactiva(pr, nuevaTarifa, Me.GenerarCuestionarioResuelto33(colCaract), nuevaTarifa.FEfecto, 100)



                ' verificacion de la prueba
                Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN

                Dim saldo As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(0).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo)

                Dim saldo2 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(1).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo2)
                If saldo2 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo3 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(2).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo3)

                If saldo3 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo4 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(3).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo4)

                If saldo4 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo5 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(4).Deudora, ColApunteImpD.Item(0).Acreedora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo5)
                System.Diagnostics.Debug.WriteLine(saldo5 + saldo)

                If saldo5 + saldo <> 0 Then
                    Throw New ApplicationException
                End If


                tr.Cancelar()


            End Using


        End Using



    End Sub
    <TestMethod(), Timeout(5700000)> Public Sub pe3v1ModificarIGUALTarifadePeridodeRenovacion()



        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colDatos As New FN.Seguros.Polizas.DN.ColPeriodoRenovacionPolizaDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))

            Dim colopc As IList = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))

            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = colopc.Item(0)
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN '=
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))



            Using tr As New Transaccion

                ' debe de contener la unica poliza creada
                Dim nuevaTarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.GenerarTarifa1(colcober, "0000bbb")
                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = colDatos.Item(0)

                Dim col As GestionPagos.DN.ColIImporteDebidoDN
                Dim PolizaRvLc As New RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                col = PolizaRvLc.ModificarCondicionesCoberturaRetroactiva(pr, nuevaTarifa, Me.GenerarCuestionarioResuelto20(colCaract), nuevaTarifa.FEfecto, 100)



                ' verificacion de la prueba
                Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN

                Dim saldo As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(0).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo)

                Dim saldo2 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(1).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo2)
                If saldo2 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo3 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(2).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo3)

                If saldo3 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo4 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(3).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo4)

                If saldo4 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo5 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(4).Deudora, ColApunteImpD.Item(0).Acreedora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo5)
                System.Diagnostics.Debug.WriteLine(saldo5 + saldo)

                If saldo5 + saldo <> 0 Then
                    Throw New ApplicationException
                End If


                tr.Cancelar()


            End Using


        End Using



    End Sub


    <TestMethod(), Timeout(5700000)> Public Sub pe5v1ModificarNegativayPositivaTarifadePeridodeRenovacion()



        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colDatos As New FN.Seguros.Polizas.DN.ColPeriodoRenovacionPolizaDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))

            Dim colopc As IList = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))

            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = colopc.Item(0)
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN '=
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))



            Using tr As New Transaccion

                ' debe de contener la unica poliza creada
                Dim nuevaTarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.GenerarTarifa1(colcober, "0000bbb")
                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = colDatos.Item(0)

                Dim col As GestionPagos.DN.ColIImporteDebidoDN
                Dim PolizaRvLc As New RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                col = PolizaRvLc.ModificarCondicionesCoberturaRetroactiva(pr, nuevaTarifa, Me.GenerarCuestionarioResuelto33(colCaract), nuevaTarifa.FEfecto, 100)

                nuevaTarifa = Me.GenerarTarifa1(colcober, "0000bbb")
                col = PolizaRvLc.ModificarCondicionesCoberturaRetroactiva(pr, nuevaTarifa, Me.GenerarCuestionarioResuelto18(colCaract), nuevaTarifa.FEfecto, 100)








                ' verificacion de la prueba
                Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN

                Dim saldo As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(0).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo)

                Dim saldo2 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(1).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo2)
                If saldo2 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo3 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(2).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo3)

                If saldo3 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo4 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(3).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo4)

                If saldo4 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo5 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(4).Deudora, ColApunteImpD.Item(0).Acreedora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo5)
                System.Diagnostics.Debug.WriteLine(saldo5 + saldo)

                If saldo5 + saldo <> 0 Then
                    Throw New ApplicationException
                End If


                tr.Confirmar()


            End Using


        End Using



    End Sub

    <TestMethod(), Timeout(5700000)> Public Sub pe6v1BajaDetarifaEnFecha()



        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)


            ' recuperar la poliza de la bd

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colDatos As New FN.Seguros.Polizas.DN.ColPeriodoRenovacionPolizaDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))

            Dim colopc As IList = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))

            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = colopc.Item(0)
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN '=
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))



            Using tr As New Transaccion

                ' debe de contener la unica poliza creada
                Dim nuevaTarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.GenerarTarifa1(colcober, "0000bbb")
                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = colDatos.Item(0)

                ' Dim col As GestionPagos.DN.ColIImporteDebidoDN
                Dim PolizaRvLc As New RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                '  col = PolizaRvLc.BajaDePoliza(pr, pr.ColPeriodosCobertura.RecuperarActivos(0), Now)
                PolizaRvLc.BajaDePoliza(New Framework.DatosNegocio.HEDN(pr), Now)


                ' verificacion de la prueba
                Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN

                Dim saldo As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(0).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo)

                Dim saldo2 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(1).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo2)
                If saldo2 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo3 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(2).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo3)

                If saldo3 <> 0 Then
                    Throw New ApplicationException
                End If

                Dim saldo4 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(3).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                System.Diagnostics.Debug.WriteLine(saldo4)

                If saldo4 <> 0 Then
                    Throw New ApplicationException
                End If

                'Dim saldo5 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(4).Deudora, ColApunteImpD.Item(0).Acreedora, Date.MaxValue)
                'System.Diagnostics.Debug.WriteLine(saldo5)
                'System.Diagnostics.Debug.WriteLine(saldo5 + saldo)

                'If saldo5 + saldo <> 0 Then
                '    Throw New ApplicationException
                'End If


                tr.Confirmar()


            End Using


        End Using



    End Sub
    <TestMethod(), Timeout(5700000)> Public Sub pe7v1Renovacion()



        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)




            ' cargar la configuracion

            Dim bdln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colCosntatesConfigurablesSeguros As New FN.Seguros.Polizas.DN.ColConstatesConfigurablesSegurosDN
            colCosntatesConfigurablesSeguros.AddRangeObject(bdln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.ConstatesConfigurablesSegurosDN)))
            Framework.Configuracion.AppConfiguracion.DatosConfig.Item(GetType(FN.Seguros.Polizas.DN.ColConstatesConfigurablesSegurosDN).FullName) = colCosntatesConfigurablesSeguros


            ' recuperar la poliza de la bd

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colDatos As New FN.Seguros.Polizas.DN.ColPeriodoRenovacionPolizaDN
            colDatos.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))

            Dim colopc As IList = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))

            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = colopc.Item(0)
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN '=
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))



            Using tr As New Transaccion

                ' debe de contener la unica poliza creada
                Dim nuevaTarifa As FN.Seguros.Polizas.DN.TarifaDN = Me.GenerarTarifa1(colcober, "0000bbb")
                Dim pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = colDatos.Item(0)

                Dim pColpagos As FN.GestionPagos.DN.ColPagoDN
                Dim nuevoPR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
                Dim PolizaRvLc As New RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
                nuevoPR = PolizaRvLc.RenovacionPoliza(pr.Periodo.FF.AddDays(1), pr, pColpagos)


                '' verificacion de la prueba
                'Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                'Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                'bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                'ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                'ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                'Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN

                'Dim saldo As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(0).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                'System.Diagnostics.Debug.WriteLine(saldo)

                'Dim saldo2 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(1).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                'System.Diagnostics.Debug.WriteLine(saldo2)
                'If saldo2 <> 0 Then
                '    Throw New ApplicationException
                'End If

                'Dim saldo3 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(2).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                'System.Diagnostics.Debug.WriteLine(saldo3)

                'If saldo3 <> 0 Then
                '    Throw New ApplicationException
                'End If

                'Dim saldo4 As Double = miApunteImpDLN.Saldo(ColApunteImpD.Item(3).Acreedora, ColApunteImpD.Item(0).Deudora, Date.MaxValue)
                'System.Diagnostics.Debug.WriteLine(saldo4)

                'If saldo4 <> 0 Then
                '    Throw New ApplicationException
                'End If



                tr.Confirmar()


            End Using


        End Using



    End Sub


    <TestMethod(), Timeout(5700000)> Public Sub ProbarGrafo()

        ObtenerRecurso()
        Using New CajonHiloLN(mRecurso)
            ProbarGrafop()
        End Using

    End Sub

    <TestMethod(), Timeout(5700000)> Public Sub CrearElEntorno()


        ObtenerRecurso()

        Dim gbd As New FN.RiesgosVehiculos.AD.RiesgosVehiculosGBD(mRecurso)

        gbd.EliminarRelaciones()
        gbd.EliminarTablas()
        gbd.EliminarVistas()

        gbd.CrearTablas()
        gbd.CrearVistas()











    End Sub

    <TestMethod(), Timeout(5700000)> Public Sub CargarDatos()


        ObtenerRecurso()



        Using New CajonHiloLN(mRecurso)
            Dim gpt As New GestionPagosLNTest.UnitTest1
            gpt.mRecurso = Me.mRecurso
            gpt.CargarDatos()



            Using tr As New Transaccion

                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

                Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
                map = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
                map.VCOrigenImpdev = RecuperarVinculoClase(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN))
                map.VCLiquidadorConcreto = RecuperarVinculoClase(GetType(GestionPagosLNTest.LiquidadorConcretoPruebaLN)) ' ojo esto habria que cambiarlo porque se desconocen cuales son las liquidaciones para un paago manual
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(map)

                ' dar de alata la entidad encargada de emiitr polizas


                Dim emi As New FN.Seguros.Polizas.DN.EmisoraPolizasDN
                Dim empln As New FN.Empresas.LN.EmpresaLN

                emi.EnidadFiscalGenerica = empln.RecuperarEmpresaFiscalxCIF("B83204586").EntidadFiscalGenerica
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(emi)



                ''''''''''''''''''''''''''
                '' crear las relaciones de documentos requeridos y coberturas


                '' creción de los tipos de documentos
                'Dim coltdoc As New Framework.Ficheros.FicherosDN.ColTipoFicheroDN
                'Dim tipoDoc As Framework.Ficheros.FicherosDN.TipoFicheroDN

                'tipoDoc = New Framework.Ficheros.FicherosDN.TipoFicheroDN
                'tipoDoc.Nombre = "Ficha tecnica"
                'coltdoc.Add(tipoDoc)

                'tipoDoc = New Framework.Ficheros.FicherosDN.TipoFicheroDN
                'tipoDoc.Nombre = "Carnet conducir"
                'coltdoc.Add(tipoDoc)

                'tipoDoc = New Framework.Ficheros.FicherosDN.TipoFicheroDN
                'tipoDoc.Nombre = "Certificado 125"
                'coltdoc.Add(tipoDoc)

                'Me.GuardarDatos(coltdoc)



                '' creacion de los documentos requeridos asociados a coberturtas
                'Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                'Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN
                'colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

                'Dim coldocumentoRequerido As New Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN
                'Dim documentoRequerido As Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN

                'documentoRequerido = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
                'documentoRequerido.TipoDoc = coltdoc.RecuperarPrimeroXNombre("Ficha tecnica")
                'documentoRequerido.ColEntidadesRequeridoras.AñadirHuellaPara(colcober.RecuperarPrimeroXNombre("RCO"))
                'coldocumentoRequerido.Add(documentoRequerido)

                'documentoRequerido = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
                'documentoRequerido.TipoDoc = coltdoc.RecuperarPrimeroXNombre("Carnet conducir")
                'documentoRequerido.ColEntidadesRequeridoras.AñadirHuellaPara(colcober.RecuperarPrimeroXNombre("RCO"))
                'coldocumentoRequerido.Add(documentoRequerido)

                'Me.GuardarDatos(coldocumentoRequerido)


                tr.Confirmar()

            End Using


        End Using


    End Sub



    <TestMethod()> Public Sub probarGrafoSimple()

        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            probarGrafoSimplep()

        End Using



    End Sub

    Private Sub ProbarGrafop()




        Using tr As New Transaccion




            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colopc As IList = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))

            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = colopc.Item(0)
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN '=
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))

            Dim irec As FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN
            irec = New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN
            irec.Tarifa = Me.GenerarTarifa(colcober)
            irec.DataSoucers.Add(irec.Tarifa)
            irec.DataSoucers.Add(Me.GenerarCuestionarioResuelto20MC18(colCaract))


            opc.IOperacionDN.IRecSumiValorLN = irec
            Dim valor As Double = opc.IOperacionDN.GetValor()

            System.Diagnostics.Debug.WriteLine("VALOR TARIFA: " & valor)



            For Each om As Framework.DatosNegocio.HEDN In irec.DataResults
                '  System.Diagnostics.Debug.WriteLine("VALOR TARIFA: ch" & om.GUID & " co" & om.GUIDCobertura & " i" & om.GUIDImpuesto & " R" & om.GUIDReferida & " T" & om.GUIDTarifa)
                System.Diagnostics.Debug.WriteLine(" ch" & om.GUID & " R" & om.GUIDReferida)


            Next



            Me.GuardarDatos(irec.DataResults)

            tr.Confirmar()


            '' impromir el data resoucer
            'System.Diagnostics.Debug.WriteLine("")

            'For Each o As Object In irec.DataResults
            '    System.Diagnostics.Debug.WriteLine(o.ToString)
            'Next






        End Using



    End Sub


    Private Function GuardarPolizaConPagoCompensadop(ByVal pCancelarModificaciones As Boolean) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN



        Using tr As New Transaccion


            ' crear los datos en el configuracion de la aplicación

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)


            Dim prp As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
            prp = CrearPolizap()
            Me.GuardarDatos(prp)

            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' generar un importe debido a fabor del tomador

            Dim tomador As FN.Seguros.Polizas.DN.TomadorDN = prp.Poliza.Tomador


            ' crear el origen debido
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            Dim origen As FN.GestionPagos.DN.OrigenIdevBaseDN
            origen = CrearOrigenImportedebido(100, tomador.EntidadFiscalGenerica, correduria)
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(origen)
            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim trln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.TarificadorRVLN
            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN

            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))
            trln.TarificarTarifa(prp.Poliza.Tomador.ValorBonificacion, prp.ColPeriodosCobertura.Item(0).Tarifa, Me.GenerarCuestionarioResuelto20(colCaract))


            Dim miPolizaRvLcLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
            miPolizaRvLcLN.GenerarCargosPara(Nothing, prp, prp.ColPeriodosCobertura.Item(0), 100)
            GuardarPolizaConPagoCompensadop = prp



            If pCancelarModificaciones Then
                tr.Cancelar()
                Return prp
            End If
            tr.Confirmar()

        End Using





    End Function
    Public Function CrearOrigenImportedebido(ByVal pimporte As Double, ByVal acreedora As FN.Localizaciones.DN.IEntidadFiscalDN, ByVal deudora As FN.Localizaciones.DN.IEntidadFiscalDN) As FN.GestionPagos.DN.IOrigenIImporteDebidoDN
        Dim origen As FN.GestionPagos.DN.OrigenIdevBaseDN
        origen = New FN.GestionPagos.DN.OrigenIdevBaseDN

        'Dim aleatorio As New Random


        origen.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(origen)
        origen.IImporteDebidoDN.Importe = pimporte
        origen.IImporteDebidoDN.Acreedora = acreedora
        origen.IImporteDebidoDN.Deudora = deudora

        origen.IImporteDebidoDN.FCreación = Now
        origen.IImporteDebidoDN.FEfecto = Now.AddDays(5)
        Return origen

    End Function

    Private Function GuardarPolizap(ByVal pCancelarModificaciones As Boolean) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN



        Using tr As New Transaccion


            ' crear los datos en el configuracion de la aplicación

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
            Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

            Dim prp As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
            prp = CrearPolizap()



            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))

            Dim cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = Me.GenerarCuestionarioResuelto20(colCaract)
            Dim hcr As New Framework.Cuestionario.CuestionarioDN.HeCuestionarioResueltoDN
            hcr.AsignarEntidadReferida(cr)
            CType(prp.ColPeriodosCobertura.RecuperarActivos(0).Tarifa.DatosTarifa, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN).HeCuestionarioResuelto = hcr
            ' Me.GuardarDatos(prp)

            Dim trln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.TarificadorRVLN
            trln.TarificarTarifa(prp.Poliza.Tomador.ValorBonificacion, prp.ColPeriodosCobertura.Item(0).Tarifa, cr)


            Dim polln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
            polln.AltaDePolizapp(prp, False)



            Dim colpagos As FN.GestionPagos.DN.ColPagoDN



            'Dim miPolizaRvLcLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
            'colpagos = miPolizaRvLcLN.GenerarCargosPara(Nothing, prp, prp.ColPeriodosCobertura.Item(0), 100)
            'GuardarPolizap = prp


            '' verificacion de la prueba

            'Dim pago1 As FN.GestionPagos.DN.PagoDN = colpagos.Item(0)
            'Dim miApunteImpDLN As New FN.GestionPagos.LN.ApunteImpDLN
            'Dim saldo As Double = miApunteImpDLN.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

            'If saldo <> Math.Round(pago1.ApunteImpDOrigen.Importe, 4) Then
            '    Throw New ApplicationException
            'End If



            'If pCancelarModificaciones Then
            '    tr.Cancelar()
            '    Return prp
            'End If
            tr.Confirmar()

        End Using





    End Function


    'Private Sub TarificarTarifa(ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN)
    '    Dim irec As FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN
    '    irec = New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN

    '    Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '    Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
    '    colCaract.AddRangeObject(LN.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))


    '    irec.Tarifa = pTarifa
    '    irec.DataSoucers.Add(irec.Tarifa)
    '    irec.DataSoucers.Add(Me.GenerarCuestionarioResuelto(colCaract))

    '    Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = ln.RecuperarGenerico("1", GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))
    '    opc.IOperacionDN.IRecSumiValorLN = irec
    '    System.Diagnostics.Debug.WriteLine(opc.IOperacionDN.GetValor())

    'End Sub


    'Private Function GuardarPresupuesto() As FN.Seguros.Polizas.DN.DocAsociadoPolizaDN


    Private Function GuardarPresupuesto() As Framework.Ficheros.FicherosDN.CajonDocumentoDN



        Using tr As New Transaccion


            Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN
            Dim cuestionario As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            tarifa = Me.GenerarTarifa1(colcober, "0001bbb")

            Dim colCaract As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN
            colCaract.AddRangeObject(ln.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))

            cuestionario = GenerarCuestionarioResuelto20(colCaract)

            Dim futurosTomadores As New FN.Localizaciones.DN.ColEntidadFiscalGenericaDN
            futurosTomadores.AddRangeObject(ln.RecuperarLista(GetType(FN.Localizaciones.DN.EntidadFiscalGenericaDN)))
            Dim futuroTomador As New FN.Seguros.Polizas.DN.FuturoTomadorDN

            Dim efg As FN.Localizaciones.DN.EntidadFiscalGenericaDN = futurosTomadores.RecuperarPorIdentificacionFiscal("45274941Q")
            futuroTomador.NIFCIFFuturoTomador = efg.ValorCifNif
            futuroTomador.NIFCIFFuturoTomador = efg.Nombre
            futuroTomador.Direccion = efg.IentidadFiscal.DomicilioFiscal
            futuroTomador.ValorBonificacion = 1

            If futuroTomador Is Nothing Then
                Throw New ApplicationException("se debia haber recuperado un tomador futuro")
            End If

            Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)

            Dim emi As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)



            ' tarificación de la tarifa




            Dim trln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.TarificadorRVLN
            trln.TarificarTarifa(futuroTomador.ValorBonificacion, tarifa, cuestionario)


            ' perido de validez del presupuesto

            Dim tiempoValidez As New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias
            tiempoValidez.Meses = 1

            Dim polln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PolizaRvLcLN
            '     Dim DocAsociadoPoliza As FN.Seguros.Polizas.DN.DocAsociadoPolizaDN = polln.GuardarPresupuestoYAsociarDocumento(emi, futuroTomador, tarifa, cuestionario, tiempoValidez)
            '  Dim DocAsociadoPoliza As FN.Seguros.Polizas.DN.DocAsociadoPolizaDN = polln.GuardarPresupuestoYAsociarDocumento(emi, tarifa, cuestionario, tiempoValidez)
            Dim DocAsociadoPoliza As Framework.Ficheros.FicherosDN.CajonDocumentoDN = polln.GuardarPresupuestoYAsociarDocumento(emi, tarifa, cuestionario, tiempoValidez)



            tr.Confirmar()

            Return DocAsociadoPoliza
        End Using





    End Function

    Private Function CrearPolizap() As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN



        ' crear la tarifa

        Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
        Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN
        colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))



        Dim pol As New FN.Seguros.Polizas.DN.PolizaDN
        Dim polpr As New FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
        polpr.Poliza = pol

        Dim pc As FN.Seguros.Polizas.DN.PeriodoCoberturaDN
        pc = New FN.Seguros.Polizas.DN.PeriodoCoberturaDN


        pc.Tarifa = Me.GenerarTarifa1(colcober, "0000bbb")
        pc.Periodo.FI = pc.Tarifa.FEfecto

        polpr.FI = pc.Tarifa.FEfecto
        polpr.FF = pc.Tarifa.FEfecto.AddYears(1)


        polpr.ColPeriodosCobertura.Add(pc)



        Dim empln As New FN.Personas.LN.PersonaLN
        Dim tm As New Seguros.Polizas.DN.TomadorDN
        tm.Nombre = "rogelio tomador"
        tm.ValorBonificacion = 1

        Dim ief As FN.Localizaciones.DN.IEntidadFiscalDN = empln.RecuperarPersonaFiscalxNIF("45274941Q")
        If ief Is Nothing Then
            Throw New ApplicationException("La entidad fiscal no puede ser nothng")
        End If

        tm.EntidadFiscalGenerica = ief.EntidadFiscalGenerica
        pol.Tomador = tm
        pol.EmisoraPolizas = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)


        Return polpr



    End Function


    Public Sub probarGrafoSimplep()


        Using tr As New Transaccion


            Dim bln As New ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colprimas As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN
            colprimas.AddRangeObject(bln.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.PrimaBaseRVDN)))

            '  CrearGrafoTarificacion(colprimas)

            tr.Confirmar()

        End Using



    End Sub

    Private Function GenerarCuestionarioResuelto20(ByVal pcolCaract As Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN) As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

        Dim cur As New Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim cu As New Framework.Cuestionario.CuestionarioDN.CuestionarioDN
        cur.CuestionarioDN = cu

        Dim IValorCaracteristica, ivedad As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("CYLD")
        IValorCaracteristica.Valor = 450
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("CARN")
        IValorCaracteristica.Valor = 12
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("EDAD")
        IValorCaracteristica.Valor = 20
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)
        ivedad = IValorCaracteristica

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("ZONA")
        IValorCaracteristica.Valor = 45
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("ANTG")
        IValorCaracteristica.Valor = 0
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)



        'IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        'IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("MCND")
        'IValorCaracteristica.Valor = 18
        'IValorCaracteristica.ValorCaracPadre = ivedad
        'AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)





        'IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        'IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("PROM")
        'IValorCaracteristica.Valor = 31
        'AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)





        Return cur


    End Function

    Private Function GenerarCuestionarioResuelto20MC18(ByVal pcolCaract As Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN) As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

        Dim cur As New Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim cu As New Framework.Cuestionario.CuestionarioDN.CuestionarioDN
        cur.CuestionarioDN = cu

        Dim IValorCaracteristica, ivedad As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("CYLD")
        IValorCaracteristica.Valor = 450
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("CARN")
        IValorCaracteristica.Valor = 12
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("EDAD")
        IValorCaracteristica.Valor = 20
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)
        ivedad = IValorCaracteristica

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("ZONA")
        IValorCaracteristica.Valor = 45
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("ANTG")
        IValorCaracteristica.Valor = 0
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)



        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("MCND")
        IValorCaracteristica.Valor = 18
        IValorCaracteristica.ValorCaracPadre = ivedad
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)





        'IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        'IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("PROM")
        'IValorCaracteristica.Valor = 31
        'AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)





        Return cur


    End Function


    Private Function GenerarCuestionarioResuelto33(ByVal pcolCaract As Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN) As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

        Dim cur As New Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim cu As New Framework.Cuestionario.CuestionarioDN.CuestionarioDN
        cur.CuestionarioDN = cu

        Dim IValorCaracteristica, ivedad As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("CYLD")
        IValorCaracteristica.Valor = 450
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("CARN")
        IValorCaracteristica.Valor = 12
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("EDAD")
        IValorCaracteristica.Valor = 33
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)
        ivedad = IValorCaracteristica

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("ZONA")
        IValorCaracteristica.Valor = 45
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("ANTG")
        IValorCaracteristica.Valor = 0
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)



        'IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        'IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("MCND")
        'IValorCaracteristica.Valor = 18
        'IValorCaracteristica.ValorCaracPadre = ivedad
        'AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)





        'IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        'IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("PROM")
        'IValorCaracteristica.Valor = 31
        'AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)





        Return cur


    End Function


    Private Function GenerarCuestionarioResuelto18(ByVal pcolCaract As Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN) As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN

        Dim cur As New Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
        Dim cu As New Framework.Cuestionario.CuestionarioDN.CuestionarioDN
        cur.CuestionarioDN = cu

        Dim IValorCaracteristica, ivedad As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("CYLD")
        IValorCaracteristica.Valor = 450
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("CARN")
        IValorCaracteristica.Valor = 12
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("EDAD")
        IValorCaracteristica.Valor = 18
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)
        ivedad = IValorCaracteristica

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("ZONA")
        IValorCaracteristica.Valor = 45
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)

        IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("ANTG")
        IValorCaracteristica.Valor = 0
        AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)



        'IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        'IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("MCND")
        'IValorCaracteristica.Valor = 18
        'IValorCaracteristica.ValorCaracPadre = ivedad
        'AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)





        'IValorCaracteristica = New Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN
        'IValorCaracteristica.Caracteristica = pcolCaract.RecuperarPrimeroXNombre("PROM")
        'IValorCaracteristica.Valor = 31
        'AñadirvalorACuestirnarioResuelto(cur, IValorCaracteristica)





        Return cur


    End Function


    Private Sub AñadirvalorACuestirnarioResuelto(ByVal pCur As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal IValorCaracteristica As Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN)


        Dim preg As Framework.Cuestionario.CuestionarioDN.PreguntaDN
        Dim respuesta As Framework.Cuestionario.CuestionarioDN.RespuestaDN

        preg = New Framework.Cuestionario.CuestionarioDN.PreguntaDN
        preg.CaracteristicaDN = IValorCaracteristica.Caracteristica
        pCur.CuestionarioDN.ColPreguntaDN.Add(preg)
        respuesta = New Framework.Cuestionario.CuestionarioDN.RespuestaDN
        respuesta.PreguntaDN = preg
        respuesta.IValorCaracteristicaDN = IValorCaracteristica
        pCur.ColRespuestaDN.Add(respuesta)


    End Sub


    Private Function GenerarTarifa1(ByVal colCober As FN.Seguros.Polizas.DN.ColCoberturaDN, ByVal pValorMatricula As String) As FN.Seguros.Polizas.DN.TarifaDN

        Dim tr As New FN.Seguros.Polizas.DN.TarifaDN
        Dim p As FN.Seguros.Polizas.DN.ProductoDN
        tr.FEfecto = Now

        'p = New FN.Seguros.Polizas.DN.ProductoDN
        'p.Nombre = "Basico"

        'p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("RCO"))
        'p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("RCV"))
        'p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("DEF"))


        Dim mibln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Dim colp As New FN.Seguros.Polizas.DN.ColProductoDN
        colp.AddRangeObject(mibln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.ProductoDN)))
        p = colp.RecuperarPrimeroXNombre("BASIC")


        tr.ColLineaProducto.Add(New FN.Seguros.Polizas.DN.LineaProductoDN)
        tr.ColLineaProducto(0).Producto = p


        Dim bln As New ClaseBaseLN.BaseTransaccionConcretaLN
        Dim rm As New FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Dim modeloDatos As FN.RiesgosVehiculos.DN.ModeloDatosDN = bln.RecuperarGenerico(12, GetType(FN.RiesgosVehiculos.DN.ModeloDatosDN))
        rm.ModeloDatos = modeloDatos
        rm.Modelo = modeloDatos.Modelo
        rm.Matriculado = modeloDatos.Matriculado
        rm.Cilindrada = 125
        rm.Matricula = New FN.RiesgosVehiculos.DN.MatriculaDN

        rm.Matricula.ValorMatricula = pValorMatricula
        rm.Matricula.TipoMatricula = DN.TipoMatricula.NormalTM
        tr.Riesgo = rm
        tr.FEfecto = New Date(2007, 6, 2)
        tr.AMD.Anyos = 1


        Dim btLN As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
        btLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
        Dim colFrac As New FN.GestionPagos.DN.ColFraccionamientoDN
        colFrac.AddRangeObjectUnico(btLN.RecuperarLista(GetType(FN.GestionPagos.DN.FraccionamientoDN)))
        tr.Fraccionamiento = colFrac.Item(0)

        ' aqui poner datos especificos de la tarifa pra un riesgo veiculo

        Dim dt As New FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN
        dt.ValorBonificacion = 1
        tr.DatosTarifa = dt


        Return tr


    End Function


    Private Function GenerarTarifa2(ByVal colCober As FN.Seguros.Polizas.DN.ColCoberturaDN) As FN.Seguros.Polizas.DN.TarifaDN

        Dim tr As New FN.Seguros.Polizas.DN.TarifaDN
        Dim p As FN.Seguros.Polizas.DN.ProductoDN
        tr.FEfecto = Now

        p = New FN.Seguros.Polizas.DN.ProductoDN
        p.Nombre = "Basico"

        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("RCO"))
        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("RCV"))
        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("DEF"))
        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("RI"))
        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("DAÑOS"))

        tr.ColLineaProducto.Add(New FN.Seguros.Polizas.DN.LineaProductoDN)
        tr.ColLineaProducto(0).Producto = p


        Dim bln As New ClaseBaseLN.BaseTransaccionConcretaLN
        Dim rm As New FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Dim modeloDatos As FN.RiesgosVehiculos.DN.ModeloDatosDN = bln.RecuperarGenerico(12, GetType(FN.RiesgosVehiculos.DN.ModeloDatosDN))
        rm.Modelo = modeloDatos.Modelo
        rm.Matriculado = modeloDatos.Matriculado
        rm.Cilindrada = 450

        tr.Riesgo = rm
        tr.FEfecto = New Date(2007, 3, 3)





        ' aqui poner datos especificos de la tarifa pra un riesgo veiculo


        tr.DatosTarifa = New FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN


        Return tr


    End Function

    Private Function GenerarTarifa(ByVal colCober As FN.Seguros.Polizas.DN.ColCoberturaDN) As FN.Seguros.Polizas.DN.TarifaDN

        Dim tr As New FN.Seguros.Polizas.DN.TarifaDN
        Dim p As FN.Seguros.Polizas.DN.ProductoDN
        tr.FEfecto = Now

        p = New FN.Seguros.Polizas.DN.ProductoDN
        p.Nombre = "Basico"

        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("RCO"))
        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("RCV"))
        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("DEF"))
        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("RI"))
        p.ColCoberturas.Add(colCober.RecuperarPrimeroXNombre("DAÑOS"))

        tr.ColLineaProducto.Add(New FN.Seguros.Polizas.DN.LineaProductoDN)
        tr.ColLineaProducto(0).Producto = p


        Dim bln As New ClaseBaseLN.BaseTransaccionConcretaLN
        Dim rm As New FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Dim modeloDatos As FN.RiesgosVehiculos.DN.ModeloDatosDN = bln.RecuperarGenerico(12, GetType(FN.RiesgosVehiculos.DN.ModeloDatosDN))
        rm.Modelo = modeloDatos.Modelo
        rm.Matriculado = modeloDatos.Matriculado
        rm.Cilindrada = 450

        tr.Riesgo = rm
        tr.FEfecto = New Date(2004, 2, 2)





        ' aqui poner datos especificos de la tarifa pra un riesgo veiculo


        tr.DatosTarifa = New FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN


        Return tr


    End Function


    Private Sub GuardarDatos(ByVal objeto As Object)
        Using tr As New Transaccion
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(objeto)

            tr.Confirmar()

        End Using
    End Sub

    Private Function GuardarDatos(ByVal col As IEnumerable, ByVal transaccionesIndividuales As Boolean) As ArrayList

        Dim al As New ArrayList

        For Each o As Object In col

            Using tr As New Transaccion(transaccionesIndividuales)
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

                Try
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    gi.Guardar(o)
                    tr.Confirmar()
                Catch ex As Exception

                    al.Add(o)
                    tr.Cancelar()
                End Try
            End Using

        Next

        Return al

    End Function

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)
        If mRecurso Is Nothing Then
            connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
            htd.Add("connectionstring", connectionstring)
            mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        End If
        If Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos Is Nothing Then
            Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposMotosTest
        End If

    End Sub

    Private Function RecuperarVinculoClase(ByVal tipo As System.Type) As VinculoClaseDN
        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            Return tyrLN.CrearVinculoClase(tipo)
        End Using

    End Function


    '<TestMethod()> Public Sub ProbarDesdeCrearGrafoTarificacion()
    '    ObtenerRecurso()

    '    Using New CajonHiloLN(mRecurso)


    '        Dim ColModuladorRVSV As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
    '        Dim ColPrimabaseRVSV As New FN.RiesgosVehiculos.DN.ColPrimabaseRVSVDN




    '        Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    '        Using tr As New Transaccion(True)

    '            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '            ColPrimabaseRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)))

    '            tr.Confirmar()

    '        End Using

    '        Using tr As New Transaccion(True)

    '            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '            ColModuladorRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))

    '            tr.Confirmar()

    '        End Using




    '        Dim ColImpuestoRVSV As New FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN
    '        Using tr As New Transaccion(True)

    '            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '            ColImpuestoRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)))

    '            tr.Confirmar()

    '        End Using





    '        Using tr As New Transaccion(True)


    '            CrearGrafoTarificacionp(ColPrimabaseRVSV.RecuperarColCoberturaDN, ColPrimabaseRVSV, ColModuladorRVSV, ColImpuestoRVSV)

    '            ' TODO: alex probar transacciones anidadadas de sqls
    '            'Using tr1 As New Transaccion(True)
    '            '    ProbarGrafop()
    '            '    tr1.Cancelar()
    '            'End Using

    '            ProbarGrafop()
    '            tr.Cancelar()

    '        End Using





    '    End Using


    'End Sub

    '<TestMethod(), Timeout(5700000)> Public Sub CargarPrimasBase()


    '    ObtenerRecurso()



    '    Using New CajonHiloLN(mRecurso)
    '        Dim colprimas As FN.RiesgosVehiculos.DN.ColPrimabaseRVSVDN
    '        Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD
    '        '       Dim ColModuladorRVSV As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN


    '        Using tr As New Transaccion(True)
    '            colprimas = ad.CargarPrimasBase()
    '            tr.Confirmar()
    '        End Using


    '        'Using tr As New Transaccion(True)
    '        '    ColModuladorRVSV = ad.CargarModuladores(colprimas.RecuperarColPrimaBaseRVDN, colprimas.RecuperarColCategoriasDN, colprimas.RecuperarColCoberturaDN)
    '        '    tr.Confirmar()
    '        'End Using

    '        'Using tr As New Transaccion(True)
    '        '    CrearGrafoTarificacionp(colprimas, ColModuladorRVSV)
    '        '    tr.Confirmar()
    '        'End Using




    '    End Using


    'End Sub

    '<TestMethod()> Public Sub CrearGrafoTarificacion()
    '    ObtenerRecurso()

    '    Using New CajonHiloLN(mRecurso)


    '        Dim ColModuladorRVSV As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
    '        Dim ColPrimabaseRVSV As New FN.RiesgosVehiculos.DN.ColPrimabaseRVSVDN




    '        Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    '        Using tr As New Transaccion(True)

    '            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '            ColPrimabaseRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)))

    '            tr.Confirmar()

    '        End Using

    '        Using tr As New Transaccion(True)

    '            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '            ColModuladorRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))

    '            tr.Confirmar()

    '        End Using




    '        Dim ColImpuestoRVSV As New FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN
    '        Using tr As New Transaccion(True)

    '            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '            ColImpuestoRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)))

    '            tr.Confirmar()

    '        End Using


    '        CrearGrafoTarificacionp(ColPrimabaseRVSV.RecuperarColCoberturaDN, ColPrimabaseRVSV, ColModuladorRVSV, ColImpuestoRVSV)

    '    End Using


    'End Sub
    '<TestMethod()> Public Sub CargarModuladores()

    '    ObtenerRecurso()

    '    Using New CajonHiloLN(mRecurso)
    '        CargarModuladoresp()

    '    End Using




    'End Sub
    '<TestMethod()> Public Sub CargarModuladores2Conductor()

    '    ObtenerRecurso()

    '    Using New CajonHiloLN(mRecurso)
    '        CargarModuladores2Conductorp()

    '    End Using




    'End Sub

    '<TestMethod()> Public Sub CargarImpuesto()

    '    ObtenerRecurso()

    '    Using New CajonHiloLN(mRecurso)
    '        CargarImpuestop()

    '    End Using




    'End Sub

    'Private Sub CargarImpuestop()




    '    Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN
    '    Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    '    Using tr As New Transaccion(True)

    '        lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '        colcober.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

    '        tr.Confirmar()

    '    End Using


    '    Using tr As New Transaccion


    '        Dim PrimasBaseLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PrimasBaseLN

    '        Dim ColImpuestoRVSV As New FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN
    '        Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD

    '        ColImpuestoRVSV = ad.CargarImpuestosModuladores(colcober)


    '        tr.Confirmar()

    '    End Using





    'End Sub

    'Private Sub CargarModuladores2Conductorp()






    '    Dim ColModuladorRVSV As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
    '    Dim colprimas As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN
    '    '        Dim ColValorIntervalNumMap As New Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN
    '    Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD
    '    Dim ColCaracteristica As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN

    '    Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    '    Using tr As New Transaccion(True)

    '        Dim PrimasBaseLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PrimasBaseLN
    '        colprimas.AddRangeObject(PrimasBaseLN.RecuperarLista())

    '        'lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '        'ColValorIntervalNumMap.AddRangeObject(lng.RecuperarLista(GetType(Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN)))

    '        lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '        ColCaracteristica.AddRangeObject(lng.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))


    '        lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '        ColModuladorRVSV.AddRangeObject(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))



    '        tr.Confirmar()

    '    End Using



    '    Using tr As New Transaccion

    '        ColModuladorRVSV.AddRangeObject(ad.CargarModuladoresMultiConductor(ColCaracteristica, colprimas, colprimas.RecuperarColCategoriasDN, colprimas.RecuperarColCoberturaDN))
    '        tr.Confirmar()

    '    End Using





    'End Sub

    'Private Sub CargarModuladoresp()




    '    Dim PrimasBaseLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PrimasBaseLN

    '    Dim ColModuladorRVSV As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
    '    Dim colprimas As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN
    '    Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD





    '    Using tr As New Transaccion



    '        colprimas.AddRangeObject(PrimasBaseLN.RecuperarLista())


    '        ColModuladorRVSV = ad.CargarModuladores(colprimas, colprimas.RecuperarColCategoriasDN, colprimas.RecuperarColCoberturaDN)

    '        tr.Confirmar()

    '    End Using





    'End Sub

    'Private Sub CrearGrafoTarificacionp(ByVal pColcober As FN.Seguros.Polizas.DN.ColCoberturaDN, ByVal pColPrimabaseRVSV As FN.RiesgosVehiculos.DN.ColPrimabaseRVSVDN, ByVal pColModuladores As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN, ByVal pColImpuestoRVSV As FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN)




    '    Using tr As New Transaccion


    '        ' creamos el flujo
    '        Dim colopRamaCober As New Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN
    '        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '        Dim suministradorvPrimas As FN.RiesgosVehiculos.DN.PrimabaseRVSVDN
    '        Dim nombreCobertura As String


    '        ''''''''''''''''''''''''''''''''''''''''''''''
    '        '       RAMA RCO
    '        ''''''''''''''''''''''''''''''''''''''''''''''


    '        For Each cober As FN.Seguros.Polizas.DN.CoberturaDN In pColcober




    '            nombreCobertura = cober.Nombre


    '            ' recuperamos el objeto  que tine todas las primas base para una cobertura (para todas las categorias de esa cobertura)
    '            suministradorvPrimas = pColPrimabaseRVSV.RecuperarxNombreCobertura(nombreCobertura)
    '            ' la operacion de prima base
    '            op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '            op.Nombre = "opPB" & nombreCobertura
    '            op.Operando1 = New SumiValFijoDN(1) ' esto debe sustituirse por la operación vinculada a un suministrador de valor que sera un modulador
    '            op.Operando2 = suministradorvPrimas
    '            op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN
    '            op.DebeCachear = True


    '            op = GenerarOperacionModulador(pColModuladores, op, nombreCobertura, "ANTG", True)
    '            If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
    '                Throw New ApplicationException
    '            End If
    '            op = GenerarOperacionModulador(pColModuladores, op, nombreCobertura, "CARN", True)
    '            If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
    '                Throw New ApplicationException
    '            End If
    '            op = GenerarOperacionModulador(pColModuladores, op, nombreCobertura, "CYLD", True)
    '            If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
    '                Throw New ApplicationException
    '            End If
    '            op = GenerarOperacionModulador(pColModuladores, op, nombreCobertura, "EDAD", True)
    '            If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
    '                Throw New ApplicationException
    '            End If
    '            op = GenerarOperacionModulador(pColModuladores, op, nombreCobertura, "ZONA", True)
    '            If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
    '                Throw New ApplicationException
    '            End If

    '            op = GenerarOperacionModulador(pColModuladores, op, nombreCobertura, "MCND", True)
    '            If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
    '                Throw New ApplicationException
    '            End If



    '            ' op = GenerarOperacionModulador(pColModuladores, op, nombreCobertura, "PROM", False)

    '            If cober.Nombre = "RCO" Then

    '            End If


    '            '''''''''''''''''''''''''''''''''''
    '            '  impuestos
    '            '''''''''''''''''''''''''''''''''''

    '            Dim ColOperacionSimpleBase As Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN = GenerarColOperacionesDeImpuestos(cober, op, pColImpuestoRVSV)
    '            Dim opeResumen As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = GeneracionOperacionSumaResumen("Suma Impuestos", op, ColOperacionSimpleBase)
    '            opeResumen.DebeCachear = True
    '            If opeResumen.Operando1 Is Nothing OrElse opeResumen.Operando2 Is Nothing Then
    '                Throw New ApplicationException
    '            End If


    '            colopRamaCober.Add(opeResumen)


    '        Next




    '        Dim opeResumenTotal As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = GeneracionOperacionSumaResumen("Suma Total", New SumiValFijoDN(0), colopRamaCober)
    '        opeResumenTotal.DebeCachear = True


    '        Dim opr As New Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
    '        opr.Nombre = "grafototal"
    '        opr.IOperacionDN = opeResumenTotal
    '        Me.GuardarDatos(opr)


    '        tr.Confirmar()

    '    End Using





    'End Sub

    'Private Function GeneracionOperacionSumaResumen(ByVal pNombreOperacion As String, ByVal pOpPrimaNeta As Framework.Operaciones.OperacionesDN.ISuministradorValorDN, ByVal pColOperacionSimpleBase As Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN


    '    Select Case pColOperacionSimpleBase.Count



    '        Case Is = 0

    '            Return Nothing

    '        Case Is = 1
    '            Dim opsumPrecendete As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '            opsumPrecendete = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '            opsumPrecendete.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
    '            opsumPrecendete.Operando1 = pOpPrimaNeta
    '            opsumPrecendete.Operando2 = pColOperacionSimpleBase.Item(0) ' esta es la operacion que resume prima neta
    '            opsumPrecendete.Nombre = pNombreOperacion
    '            opsumPrecendete.DebeCachear = True
    '            Return opsumPrecendete

    '        Case Is > 1
    '            Dim opsumNueva, opsumPrecendete As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '            'opsumPrecendete = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '            'opsumPrecendete.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
    '            'opsumPrecendete.Operando1 = pColOperacionSimpleBase.Item(0)
    '            'opsumPrecendete.Operando2 = pColOperacionSimpleBase.Item(1)
    '            'opsumPrecendete.Nombre = CType(opsumPrecendete.Operando1, Object).ToString & "-s-" & CType(opsumPrecendete.Operando2, Object).ToString
    '            opsumPrecendete = pColOperacionSimpleBase.Item(0)
    '            'opsumPrecendete.Nombre = CType(opsumPrecendete.Operando1, Object).ToString & "-s-" & CType(opsumPrecendete.Operando2, Object).ToString

    '            For a As Integer = 1 To pColOperacionSimpleBase.Count - 1
    '                opsumNueva = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '                opsumNueva.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
    '                opsumNueva.Operando1 = opsumPrecendete
    '                opsumNueva.Operando2 = pColOperacionSimpleBase.Item(a)
    '                opsumPrecendete = opsumNueva
    '                opsumPrecendete.Nombre = CType(opsumPrecendete.Operando1, Object).ToString & "-SS-" & CType(opsumPrecendete.Operando2, Object).ToString

    '            Next


    '            opsumNueva = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '            opsumNueva.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
    '            opsumNueva.Operando1 = opsumPrecendete 'esta es la operacion que corresponde a la suma de todos los impuestos
    '            opsumNueva.Operando2 = pOpPrimaNeta ' esta es la operacion que resume prima neta
    '            opsumNueva.Nombre = pNombreOperacion
    '            opsumNueva.DebeCachear = True


    '            Return opsumNueva

    '    End Select





    'End Function

    'Private Function GenerarColOperacionesDeImpuestos(ByVal cober As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pOpPrimaNeta As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pColImpuestoRVSV As FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN) As Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN

    '    Dim col As New Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN
    '    Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

    '    ' obtener la cobertura que es base en la rama

    '    'Dim ModuladorRVSV As FN.RiesgosVehiculos.DN.ModuladorRVSVDN = pOpPrimaNeta.Operando2
    '    'Dim cober As FN.Seguros.Polizas.DN.CoberturaDN = ModuladorRVSV.Cobertura

    '    For Each imp As FN.RiesgosVehiculos.DN.ImpuestoRVSVDN In pColImpuestoRVSV
    '        If imp.Cobertura.GUID = cober.GUID Then
    '            op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '            op.DebeCachear = True

    '            op.Operando2 = imp ' el impuesto simpre se posiciona en el operado 2

    '            Select Case imp.Operadoraplicable
    '                Case "+"
    '                    op.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
    '                    op.Operando1 = New SumiValFijoDN(0)
    '                    op.Nombre = CType(op.Operando1, Object).ToString & "-+-" & CType(op.Operando2, Object).ToString

    '                Case "*"
    '                    op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN
    '                    op.Operando1 = pOpPrimaNeta
    '                    op.Nombre = CType(op.Operando1, Object).ToString & "-*-" & CType(op.Operando2, Object).ToString

    '                Case Else
    '                    Throw New ApplicationException("operador aplicable no reconocido")
    '            End Select
    '            col.Add(op)
    '        End If
    '    Next


    '    Return col
    'End Function


    'Private Function GenerarOperacionModulador(ByVal pColModuladores As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN, ByVal pOpPrecedente As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pNombreCobertura As String, ByVal pNombreModulador As String, ByVal pdebeCachear As Boolean) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

    '    Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN


    '    Dim col As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN = pColModuladores.Recuperar(pNombreCobertura, pNombreModulador) ' para todas las categorias


    '    Select Case col.Count


    '        Case Is = 0
    '            'Throw New ApplicationException("al menos debia haberse recuperado un modulador")
    '            Return pOpPrecedente
    '        Case Is = 1


    '        Case Else
    '            Throw New ApplicationException("solo debia haberse recuperado un modulador")
    '    End Select

    '    ' la operacion de coeficientes
    '    op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
    '    op.Nombre = "op-" & pNombreCobertura & "-" & pNombreModulador
    '    op.Operando1 = pOpPrecedente
    '    op.Operando2 = col.Item(0)
    '    op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN
    '    op.DebeCachear = pdebeCachear


    '    Return op

    'End Function

End Class




Public Class GestorMapPersistenciaCamposMotosTest
    Inherits GestorMapPersistenciaCamposLN

    Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As InfoDatosMapInstClaseDN = Nothing
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

        ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
        If (InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
            If mapinst Is Nothing Then
                mapinst = New InfoDatosMapInstClaseDN
            End If
            Me.MapearClase("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
        End If


        If InstanciacionReflexionHelperLN.HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadTemporalDN)) Then
            If mapinst Is Nothing Then
                mapinst = New InfoDatosMapInstClaseDN
            End If

            Dim mapSubInst As New InfoDatosMapInstClaseDN
            ' mapeado de la clase referida por el campo
            mapSubInst.NombreCompleto = "Framework.DatosNegocio.EntidadTemporalDN"
            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPeriodo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.MapSubEntidad = mapSubInst

        End If


        Return mapinst
    End Function

    Private Function RecuperarMap_Framework_AIQB(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        ' ficheros
        If pTipo Is GetType(Framework.GestorInformes.ContenedorPlantilla.DN.HuellaFicheroPlantillaDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.ITabla) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaPrincipalAIQB)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaRelacionadaAIQB)))

            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If

        If pTipo Is GetType(Framework.GestorInformes.AdaptadorInformesQueryBuilding.DN.TablaPrincipalAIQB) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSQLDefinicion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mParametros"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mfkTabla"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        Return Nothing

    End Function

    Private Function RecuperarMap_Framework_DatosNegocio(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.INodoTDN`1[[DatosNegocioTest.HojaDeNodoDeT")) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If

        If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.INodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN")) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If

        If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.ColINodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN, AmvDocumentosDN")) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            Return mapinst
        End If


    End Function

    Private Function RecuperarMap_Framework_Tarificador(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


    End Function

    Private Function RecuperarMap_Framework_Ficheros(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        If (pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)) Then

            'Me.MapearClase("mDatos", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            Me.MapearClase("mDatos", CampoAtributoDN.NoProcesar, campodatos, mapinst)

            Return mapinst

        End If

        If pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN) Then

            'Me.MapearClase("mDatos", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            Me.MapearClase("mDatos", CampoAtributoDN.NoProcesar, campodatos, mapinst)

            Return mapinst

        End If

        If (pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            Return mapinst
        End If
    End Function

    Private Function RecuperarMap_Framework_Cuestionario(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(Framework.Cuestionario.CuestionarioDN.IValorCaracteristicaDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Cuestionario.CuestionarioDN.ValorCaracteristicaFechaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Cuestionario.CuestionarioDN.ValorTextoCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Cuestionario.CuestionarioDN.ValorNumericoCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Cuestionario.CuestionarioDN.ValorBooleanoCaracterisitcaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorMCNDCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorCPCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorDireccionNoUnicaCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorLocalidadCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorMarcaCaracterisitcaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorModeloCaracteristicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ValorSexoCaracteristicaDN)))

            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

    End Function

    Private Function RecuperarMap_Framework_Operaciones(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)) Then
            Dim alentidades As New ArrayList
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mToSt"
            campodatos.TamañoCampo = 1200

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombre"
            campodatos.TamañoCampo = 1200

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If


        If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.IOperacionSimpleDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

        If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.ISuministradorValorDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.SumiValFijoDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ComisionRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.FraccionamientoRVSVDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.BonificacionRVSVDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

        'ImpuestoRVSVDN

        If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.IOperadorDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.SumaOperadorDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.TruncarOperadorDN)))
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.RedondeoOperadorDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

    End Function

    Private Function RecuperarMap_Framework_Procesos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(Framework.Procesos.ProcesosDN.OperacionRealizadaDN)) Then
            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN
            ''''''''''''''''''''''

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSujetoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoDirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.TarifaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PresupuestoDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoIndirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst


            Return mapinst
        End If
    End Function

    Private Function RecuperarMap_Framework_TiposYReflexion(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(Framework.TiposYReflexion.DN.VinculoClaseDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombreClase"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If

        Return Nothing
    End Function

    Private Function RecuperarMap_MNavegacionDatosDN(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing
        If (pTipo Is GetType(MNavegacionDatosDN.EntidadNavDN)) Then
            Dim alentidades As New ArrayList

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mVinculoClase"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEntidadNavDN ADD CONSTRAINT tlEntidadNavDNvc UNIQUE  (idVinculoClase)"))

            Return mapinst
        End If


        Return Nothing
    End Function

    Private Function RecuperarMap_Framework_Usuarios(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If pTipo Is GetType(Framework.Usuarios.DN.DatosIdentidadDN) Then

            Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
            Me.MapearClase("mNick", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.UsuarioDN) Then
            Dim mapinstSub As New InfoDatosMapInstClaseDN
            Dim alentidades As New ArrayList

            Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.HuellaOperadorDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
            Me.VincularConClase("mEntidadUser", New ElementosDeEnsamblado("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.RolDN) Then

            Me.MapearClase("mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)

            Return mapinst
        End If


        If pTipo Is GetType(Framework.Usuarios.DN.PrincipalDN) Then
            Me.MapearClase("mClavePropuesta", CampoAtributoDN.NoProcesar, campodatos, mapinst)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.PermisoDN) Then
            Me.MapearClase("mDatoRef", CampoAtributoDN.NoProcesar, campodatos, mapinst)
            Return mapinst
        End If

        If pTipo Is GetType(Framework.Usuarios.DN.TipoPermisoDN) Then
            Me.MapearClase("mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)
            Return mapinst
        End If



        If (pTipo Is GetType(Framework.Usuarios.DN.AutorizacionRelacionalDN)) Then

            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.Usuarios.DN.PrincipalDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.TipoEntidadOrigenDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.TipoEmpresaDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColEntidadesRelacionadas"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst


        End If


    End Function

    Private Function RecuperarMap_FN_RiesgosVehiculos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.MatriculaDN)) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorMatricula"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlMatriculaDN ADD CONSTRAINT MatriculaDNvm UNIQUE  (ValorMatricula)"))

            Return mapinst

        End If



        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoCoberturaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))

            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColOrigenes"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst

        End If


        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.CategoriaDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombre"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            Return mapinst
        End If


        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.MarcaDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNombre"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModeloDN)) Then

            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlModeloDN ADD CONSTRAINT tlModeloDNNombreidMarca UNIQUE  (Nombre,idMarca)"))
            Return mapinst
        End If

        If (pTipo Is GetType(Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN)) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()

            mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.NifDN).FullName
            ParametrosGeneralesNoProcesar(mapSubInst)
            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPlazo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ComisionRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.FraccionamientoRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.BonificacionRVSVDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIRecSumiValorLN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCacheado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            Return mapinst
        End If

        If (pTipo Is GetType(FN.RiesgosVehiculos.DN.BonificacionRVDN)) Then
            Dim mapinstSub As InfoDatosMapInstClaseDN

            mapinstSub = New InfoDatosMapInstClaseDN
            mapinstSub.NombreCompleto = GetType(Framework.DatosNegocio.IntvaloNumericoDN).FullName
            ParametrosGeneralesNoProcesar(mapinstSub)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.MapSubEntidad = mapinstSub
            campodatos.NombreCampo = "mIntervaloNumerico"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

            Return mapinst
        End If

        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModuladorRVDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValorCacheado"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
        '    Return mapinst
        'End If

    End Function

    Private Function RecuperarMap_FN_Trabajos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        If (pTipo Is GetType(FN.Trabajos.DN.AsignacionTrabajoDN)) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadAsignada"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst
        End If


        Return Nothing
    End Function

    Private Function RecuperarMap_FN_Polizas(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing


        If (pTipo Is GetType(FN.Seguros.Polizas.DN.TomadorDN)) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIdentificacionFiscal"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo) ' no se debieran admitir nulos

            Return mapinst
        End If

        If (pTipo Is GetType(FN.Seguros.Polizas.DN.IRiesgoDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst
        End If


        If (pTipo Is GetType(FN.Seguros.Polizas.DN.TarifaDN)) Then
            Dim alentidades As ArrayList
            Dim mapinstSub As InfoDatosMapInstClaseDN

            ''''''''''''''''''''
            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mRiesgo"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)

            'alentidades = New ArrayList
            'mapinstSub = New InfoDatosMapInstClaseDN
            'alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN)))
            'mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            'campodatos.MapSubEntidad = mapinstSub
            ''''''''''''''''''''

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatosTarifa"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)


            alentidades = New ArrayList
            mapinstSub = New InfoDatosMapInstClaseDN
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN)))
            mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            campodatos.MapSubEntidad = mapinstSub
            '''''''''''''''''''

            mapinstSub.NombreCompleto = GetType(Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias).FullName
            ParametrosGeneralesNoProcesar(mapinstSub)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.MapSubEntidad = mapinstSub
            campodatos.NombreCampo = "mAMD"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mGrupoFraccionamientos"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mFraccionamientosXML"
            campodatos.TamañoCampo = 2000

            Return mapinst
        End If




    End Function

    Private Function RecuperarMap_FN_GestionPagos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        If (pTipo Is GetType(FN.GestionPagos.DN.PlazoEfectoDN)) Then

            '''''''''''''''''''
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPlazoEjecucion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            '''''''''''''''''''

            Return mapinst
        End If

        If (pTipo Is GetType(FN.GestionPagos.DN.CondicionesPagoDN)) Then

            '''''''''''''''''''
            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mTitulares"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPlazoEjecucion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            '''''''''''''''''''

            Return mapinst
        End If




        ' Para la prueba de mapeado en la interface
        If (pTipo Is GetType(FN.GestionPagos.DN.PagoDN)) Then
            Dim alentidades As New ArrayList

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mDeudor"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mDestinatario"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mIImporteDebidoDN"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)



            Return mapinst
        End If


        If (pTipo Is GetType(FN.GestionPagos.DN.NotificacionPagoDN)) Then
            Dim alentidades As New ArrayList

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSujeto"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            Return mapinst
        End If




        If pTipo Is GetType(FN.GestionPagos.DN.PagoDN) Then

            mapinst.ColTiposTrazas = New List(Of System.Type)
            mapinst.ColTiposTrazas.Add(GetType(FN.GestionPagos.DN.PagoTrazaDN))

            Return mapinst
        End If


        If pTipo Is GetType(FN.GestionPagos.DN.NotificacionPagoDN) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.Usuarios.DN.PrincipalDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(Framework.Usuarios.DN.PrincipalDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mSujeto"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

        If pTipo Is GetType(FN.GestionPagos.DN.TalonDocumentoDN) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNumeroSerie"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If

        If pTipo Is GetType(FN.GestionPagos.DN.ContenedorRTFDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mArrayString"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

            Return mapinst
        End If

        If pTipo Is GetType(FN.GestionPagos.DN.OrigenDN) Then
            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlOrigenDN ADD CONSTRAINT tlOrigenDNTipoEntidadOrigenDN UNIQUE  (IDEntidad,idTipoEntidadOrigen)"))
            Return mapinst

        End If

        If pTipo Is GetType(FN.GestionPagos.DN.ContenedorImagenDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mImagen"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

            Return mapinst
        End If

        If pTipo Is GetType(FN.GestionPagos.DN.ConfiguracionImpresionTalonDN) Then

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mFuente"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mConfigPagina"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mFirma"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


            Return mapinst
        End If

        If (pTipo Is GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)) Then


            'Dim alentidades As ArrayList
            'Dim mapSubInst As InfoDatosMapInstClaseDN

            'mapSubInst = New InfoDatosMapInstClaseDN
            'alentidades = New ArrayList

            'mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            'alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoCoberturaDN)))
            'alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
            'alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))

            'mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mColOrigenes"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            'campodatos.MapSubEntidad = mapSubInst


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColIEntidad"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            Return mapinst

        End If

        ' ojo esto es una mera prueba
        If (pTipo Is GetType(FN.GestionPagos.DN.LiquidacionPagoDN)) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColIEntidad"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst

        End If

        If (pTipo Is GetType(FN.GestionPagos.DN.IImporteDebidoDN)) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            'alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))
            'mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            Return mapinst
        End If


        If (pTipo Is GetType(FN.GestionPagos.DN.IOrigenIImporteDebidoDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            Return mapinst
        End If





    End Function

    Private Function RecuperarMap_FN_Empresas(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        If pTipo Is GetType(FN.Empresas.DN.EntidadColaboradoraDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()
            Dim alentidades As ArrayList

            mapSubInst = New InfoDatosMapInstClaseDN()
            alentidades = New ArrayList()
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.AgrupacionDeEmpresasDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.SedeEmpresaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpleadoDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadAsociada"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            mapSubInst = New InfoDatosMapInstClaseDN()
            alentidades = New ArrayList()
            alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.DatosColaboradorDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDatosAdicionales"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEntidadColaboradoraDN ADD CONSTRAINT tlEntidadColaboradoraDNUnicoCodColab UNIQUE  (CodigoColaborador)"))

            Return mapinst

        End If


        If pTipo Is GetType(FN.Empresas.DN.EmpresaDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()
            Dim alentidades As ArrayList

            mapSubInst = New InfoDatosMapInstClaseDN()
            alentidades = New ArrayList()
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaFiscalDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadFiscal"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEmpresaDN ADD CONSTRAINT tlEmpresaDNEmpFiscal UNIQUE  (CIFNIF)"))

            Return mapinst

        End If

        If pTipo Is GetType(FN.Empresas.DN.EmpresaFiscalDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()

            ' mapeado de la clase referida por el campo
            mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.CifDN).FullName

            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapSubInst
            campodatos.NombreCampo = "mCodigo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            ' FIN    mapeado de la clase referida por el campo ******************

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mCif"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            campodatos.MapSubEntidad = mapSubInst


            Return mapinst
        End If

        If pTipo Is GetType(FN.Empresas.DN.EmpleadoDN) Then
            mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEmpleadoDN ADD CONSTRAINT tlEmpleadoDNPersonaEmpresa UNIQUE  (CIFNIFEmpresa,NIFPersona,Periodo_FFinal)"))
            Return mapinst

        End If

        If pTipo Is GetType(FN.Empresas.DN.EmpleadoYPuestosRDN) Then
            Dim alentidades As New ArrayList()
            Dim mapSubInst As New InfoDatosMapInstClaseDN()

            alentidades.Add(New VinculoClaseDN("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mGUIDEmpleado"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

    End Function


    Private Function RecuperarMap_FN_Financiero(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing




        If (pTipo Is GetType(FN.Financiero.DN.CCCDN)) Then
            Dim alentidades As ArrayList


            '''''''''''''''''''
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mTitulares"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            '''''''''''''''''''




            Return mapinst
        End If

        '' TODO: alex pruba a comentar esto y crear el entorno el mensaje del motor ad no es muy explicativo y no sabes de que campo de que clase proviene la referencia a la interface

        'If (pTipo Is GetType(FN.Financiero.DN.CuentaBancariaDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mTitulares"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


        '    Return mapinst
        'End If


        If pTipo.FullName = GetType(FN.Financiero.DN.CuentaBancariaDN).FullName Then
            Dim mapSubInst As InfoDatosMapInstClaseDN

            ' mapeado de la clase referida por el campo IBAN
            mapSubInst = New InfoDatosMapInstClaseDN()
            mapSubInst.NombreCompleto = GetType(FN.Financiero.DN.IBANDN).FullName

            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mIBAN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            campodatos.MapSubEntidad = mapSubInst

            ' mapeado de la clase referida por el campo CCC
            mapSubInst = New InfoDatosMapInstClaseDN()
            mapSubInst.NombreCompleto = GetType(FN.Financiero.DN.CCCDN).FullName

            ParametrosGeneralesNoProcesar(mapSubInst)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mCCC"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If







    End Function

    Private Function RecuperarMap_FN_Personas(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing
        If pTipo Is GetType(FN.Personas.DN.PersonaDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN

            ' mapeado de la clase referida por el campo
            mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.NifDN).FullName
            ParametrosGeneralesNoProcesar(mapSubInst)
            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapSubInst
            campodatos.NombreCampo = "mCodigo"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


            ' FIN    mapeado de la clase referida por el campo ******************

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mNIF"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If

    End Function

    Private Function RecuperarMap_FN_Localizaciones(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        If (pTipo Is GetType(FN.Localizaciones.DN.IDatoContactoDN)) Then
            Dim alentidades As New ArrayList
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.EmailDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.TelefonoDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.DireccionNoUnicaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.PaginaWebDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Localizaciones.DN.ContactoGenericoDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
            Return mapinst

        End If

        If pTipo Is GetType(FN.Localizaciones.DN.EntidadFiscalGenericaDN) Then

            campodatos = New InfoDatosMapInstCampoDN()
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mValorCifNif"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

            Return mapinst
        End If


        If (pTipo Is GetType(FN.Localizaciones.DN.IEntidadFiscalDN)) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaFiscalDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If




    End Function



    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        mapinst = RecuperarMap_Framework_AIQB(pTipo)
        If Not mapinst Is Nothing Then
            Return mapinst
        End If

        mapinst = RecuperarMap_Framework_Ficheros(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = RecuperarMap_FN_Polizas(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = RecuperarMap_FN_Financiero(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If


        mapinst = Me.RecuperarMap_FN_Empresas(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_GestionPagos(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_Localizaciones(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_Personas(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_Polizas(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_FN_RiesgosVehiculos(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Cuestionario(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Ficheros(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Operaciones(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Procesos(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Tarificador(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If

        mapinst = Me.RecuperarMap_Framework_Usuarios(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If


        mapinst = Me.RecuperarMap_Framework_DatosNegocio(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If



        mapinst = Me.RecuperarMap_FN_Trabajos(pTipo)
        If mapinst IsNot Nothing Then
            Return mapinst
        End If


        Return Nothing





        'FINZONA: Financiero ________________________________________________________________


        'If (pTipo Is GetType(FN.Seguros.Polizas.DN.PolizaDN)) Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mColaboradorComercial"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    Return mapinst
        'End If

        '' Para la prueba de mapeado en la interface
        'If (pTipo Is GetType(FN.GestionPagos.DN.PagoDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDeudor"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDestinatario"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mIImporteDebidoDN"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    Return mapinst
        'End If

        'If (pTipo Is GetType(FN.Financiero.DN.CCCDN)) Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mTitulares"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    Return mapinst
        'End If

        'If (pTipo Is GetType(FN.Financiero.DN.CuentaBancariaDN)) Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mTitulares"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    Return mapinst
        'End If

        'If (pTipo Is GetType(FN.GestionPagos.DN.NotificacionPagoDN)) Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mSujeto"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    Return mapinst
        'End If

        ''---------------------------------------------------------------------------------------------------------------------

        ''ZONA: PersonaDN ________________________________________________________________

        'If pTipo Is GetType(FN.Personas.DN.PersonaDN) Then
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN

        '    ' mapeado de la clase referida por el campo
        '    mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.CifDN).FullName
        '    ParametrosGeneralesNoProcesar(mapSubInst)
        '    campodatos = New InfoDatosMapInstCampoDN()
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mCodigo"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


        '    ' FIN    mapeado de la clase referida por el campo ******************

        '    campodatos = New InfoDatosMapInstCampoDN()
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNIF"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
        '    campodatos.MapSubEntidad = mapSubInst



        '    Return mapinst
        'End If

        ' ''FINZONA: PersonaDN ________________________________________________________________
        ' ''ZONA: EmpresaDN ________________________________________________________________


        'If pTipo Is GetType(FN.Empresas.DN.EmpresaDN) Then
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN()
        '    Dim alentidades As ArrayList

        '    mapSubInst = New InfoDatosMapInstClaseDN()
        '    alentidades = New ArrayList()
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaFiscalDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mEntidadFiscal"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEmpresaDN ADD CONSTRAINT tlEmpresaDNEmpFiscal UNIQUE  (CIFNIF)"))

        '    Return mapinst

        'End If

        'If pTipo Is GetType(FN.Empresas.DN.EmpresaFiscalDN) Then
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN()

        '    ' mapeado de la clase referida por el campo
        '    mapSubInst.NombreCompleto = GetType(FN.Localizaciones.DN.CifDN).FullName

        '    ParametrosGeneralesNoProcesar(mapSubInst)

        '    campodatos = New InfoDatosMapInstCampoDN()
        '    campodatos.InfoDatosMapInstClase = mapSubInst
        '    campodatos.NombreCampo = "mCodigo"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


        '    ' FIN    mapeado de la clase referida por el campo ******************

        '    campodatos = New InfoDatosMapInstCampoDN()
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mCif"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
        '    campodatos.MapSubEntidad = mapSubInst


        '    Return mapinst
        'End If

        'If pTipo Is GetType(FN.Empresas.DN.EmpleadoDN) Then
        '    mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlEmpleadoDN ADD CONSTRAINT tlEmpleadoDNPersonaEmpresa UNIQUE  (CIFNIFEmpresa,NIFPersona,Periodo_FFinal)"))
        '    Return mapinst

        'End If

        'If pTipo Is GetType(FN.Empresas.DN.EmpleadoYPuestosRDN) Then
        '    Dim alentidades As New ArrayList()
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN()

        '    alentidades.Add(New VinculoClaseDN("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.ActualizarClasesHuella) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN()
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mGUIDEmpleado"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst
        'End If

        ' ''FINZONA: EmpresaDN ________________________________________________________________


        'If (pTipo Is GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)) Then


        '    'Dim alentidades As ArrayList
        '    'Dim mapSubInst As InfoDatosMapInstClaseDN

        '    'mapSubInst = New InfoDatosMapInstClaseDN
        '    'alentidades = New ArrayList

        '    'mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
        '    'alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoCoberturaDN)))
        '    'alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
        '    'alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))

        '    'mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    'campodatos = New InfoDatosMapInstCampoDN
        '    'campodatos.InfoDatosMapInstClase = mapinst
        '    'campodatos.NombreCampo = "mColOrigenes"
        '    'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    'campodatos.MapSubEntidad = mapSubInst


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mColIEntidad"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    Return mapinst

        'End If

        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)) Then


        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList

        '    mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoCoberturaDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN)))

        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mColOrigenes"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst

        'End If



        '' ojo esto es una mera prueba
        'If (pTipo Is GetType(FN.GestionPagos.DN.LiquidacionPagoDN)) Then


        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList

        '    mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mColIEntidad"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst

        'End If


        'If (pTipo Is GetType(FN.Localizaciones.DN.IEntidadFiscalDN)) Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Personas.DN.PersonaFiscalDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    Return mapinst
        'End If


        'If (pTipo Is GetType(FN.GestionPagos.DN.ApunteImpDDN)) Then


        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList

        '    mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mAcreedora "
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst

        'End If

        'If (pTipo Is GetType(FN.GestionPagos.DN.IImporteDebidoDN)) Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    Return mapinst
        'End If


        'If (pTipo Is GetType(FN.GestionPagos.DN.IOrigenIImporteDebidoDN)) Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    Return mapinst
        'End If


        ' ''ZONA: UsuarioDN ________________________________________________________________

        ' ''Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.

        'If pTipo Is GetType(DatosIdentidadDN) Then

        '    Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst
        'End If

        'If pTipo Is GetType(UsuarioDN) Then
        '    Dim mapinstSub As New InfoDatosMapInstClaseDN
        '    Dim alentidades As New ArrayList

        '    'Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.HuellaOperadorDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
        '    Me.VincularConClase("mEntidadUser", New ElementosDeEnsamblado("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

        '    Return mapinst
        'End If

        'If pTipo Is GetType(PrincipalDN) Then
        '    Me.MapearClase("mClavePropuesta", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Return mapinst
        'End If

        'If pTipo Is GetType(Framework.Usuarios.DN.PermisoDN) Then
        '    Me.MapearClase("mDatoRef", CampoAtributoDN.NoProcesar, campodatos, mapinst)
        '    Return mapinst
        'End If

        'If pTipo Is GetType(Framework.Usuarios.DN.TipoPermisoDN) Then
        '    Me.MapearClase("mNombre", CampoAtributoDN.UnicoEnFuenteDatosoNulo, campodatos, mapinst)
        '    Return mapinst
        'End If

        ' ''FINZONA: UsuarioDN ________________________________________________________________

        'If (pTipo Is GetType(Framework.Procesos.ProcesosDN.OperacionRealizadaDN)) Then
        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN
        '    ''''''''''''''''''''''

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList
        '    alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mSujetoOperacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst


        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mObjetoIndirectoOperacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mObjetoDirectoOperacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
        '    campodatos.MapSubEntidad = mapSubInst

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mObjetoIndirectoOperacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst
        'End If

        'If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)) Then
        '    Dim alentidades As New ArrayList
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mIRecSumiValorLN"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mToSt"
        '    campodatos.TamañoCampo = 1200

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNombre"
        '    campodatos.TamañoCampo = 1200

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValorCacheado"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
        '    Return mapinst
        'End If


        'If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.IOperacionSimpleDN)) Then
        '    Dim alentidades As New ArrayList
        '    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    Return mapinst

        'End If

        'If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.ISuministradorValorDN)) Then
        '    Dim alentidades As New ArrayList
        '    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.SumiValFijoDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    Return mapinst

        'End If

        ''ImpuestoRVSVDN

        'If (pTipo Is GetType(Framework.Operaciones.OperacionesDN.IOperadorDN)) Then
        '    Dim alentidades As New ArrayList
        '    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.SumaOperadorDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN)))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    Return mapinst

        'End If


        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.MarcaDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNombre"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)


        '    Return mapinst
        'End If


        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModeloDN)) Then
        '    'Dim alentidades As New ArrayList

        '    'campodatos = New InfoDatosMapInstCampoDN
        '    'campodatos.InfoDatosMapInstClase = mapinst
        '    'campodatos.NombreCampo = "mNombre"
        '    'campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlModeloDN ADD CONSTRAINT tlModeloDNNombreidMarca UNIQUE  (Nombre,idMarca)"))
        '    Return mapinst
        'End If



        'If (pTipo Is GetType(FN.Seguros.Polizas.DN.TarifaDN)) Then
        '    Dim alentidades As ArrayList
        '    Dim mapinstSub As InfoDatosMapInstClaseDN

        '    '''''''''''''''''''
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mRiesgo"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)

        '    alentidades = New ArrayList
        '    mapinstSub = New InfoDatosMapInstClaseDN
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN)))
        '    mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    campodatos.MapSubEntidad = mapinstSub
        '    '''''''''''''''''''

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDatosTarifa"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)


        '    alentidades = New ArrayList
        '    mapinstSub = New InfoDatosMapInstClaseDN
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN)))
        '    mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades
        '    campodatos.MapSubEntidad = mapinstSub
        '    '''''''''''''''''''


        '    Return mapinst
        'End If


        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mIRecSumiValorLN"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValorCacheado"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)



        '    Return mapinst
        'End If


        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mIRecSumiValorLN"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValorCacheado"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    Return mapinst
        'End If


        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mIRecSumiValorLN"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValorCacheado"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
        '    Return mapinst
        'End If

        'If (pTipo Is GetType(FN.RiesgosVehiculos.DN.ModuladorRVDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValorCacheado"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
        '    Return mapinst
        'End If







        'ZONA: AMVGDocs_______________________________________________________________________________


        'If (pTipo.FullName.Contains("Nodo")) Then
        '    Beep()
        'End If


















        ' Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN()




        'ZONA: PersonaDN ________________________________________________________________



        ''FINZONA: PersonaDN ________________________________________________________________
        ''ZONA: EmpresaDN ________________________________________________________________



        ''FINZONA: EmpresaDN ________________________________________________________________






        'If (pTipo Is GetType(FN.GestionPagos.DN.ApunteImpDDN)) Then


        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList

        '    mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mAcreedora "
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst

        'End If

        'If (pTipo Is GetType(FN.GestionPagos.DN.ApunteImpDDN)) Then



        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mHuellaIOrigenImpDebDN"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

        '    Return mapinst

        'End If



        ''ZONA: UsuarioDN ________________________________________________________________

        ''Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.



        ''FINZONA: UsuarioDN ________________________________________________________________








        'ZONA: Gestión de talones       _________________________________________________________________



        'If pTipo Is GetType(Framework.Procesos.ProcesosDN.OperacionRealizadaDN) Then
        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN
        '    ''''''''''''''''''''''

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList
        '    alentidades.Add(New VinculoClaseDN(GetType(PrincipalDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mSujetoOperacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst


        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mObjetoIndirectoOperacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mObjetoDirectoOperacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
        '    campodatos.MapSubEntidad = mapSubInst

        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mObjetoIndirectoOperacion"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst
        'End If
        'FINZONA: Gestión de talones    _________________________________________________________________












        'If (pTipo Is GetType(FN.Empresas.DN.EmpresaDN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mEntidadFiscal"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


        '    Return mapinst
        'End If

        'If (pTipo Is GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)) Then
        '    Dim alentidades As New ArrayList

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDatos"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


        '    Return mapinst
        'End If


        Return Nothing

    End Function


    Protected Overrides Sub Finalize()
        MyBase.Finalize()
    End Sub

End Class



