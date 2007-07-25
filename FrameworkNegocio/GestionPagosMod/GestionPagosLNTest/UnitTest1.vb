Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting
Imports Microsoft.SqlServer.Dts.Runtime.Wrapper

Imports Framework.LogicaNegocios.Transacciones
Imports FN.Localizaciones.DN

Imports Framework.Usuarios.DN
Imports Framework.Usuarios.LN
Imports FN.Personas.DN

Imports Framework.Procesos.ProcesosLN
Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN
Imports System.Collections

<TestClass()> Public Class UnitTest1

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



    Public mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN = Nothing

    'use sspruebasft
    'select * from vwApunteImpDebHuellaOrigen
    'SELECT     * FROM        vwPagosOrigenImpDeb
    'SELECT     * FROM        vwPagosOrigenPago
    'select * from vwLiquidacionAIDPago

    <TestMethod()> Public Sub pe1v0CrearOrigenImportedebidoManual()




        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim coldeudoras As New ColIEntidadFiscalDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                coldeudoras.AddRangeObject(bgln.RecuperarLista(GetType(FN.Empresas.DN.EmpresaFiscalDN)))

                Dim iefacreedora As IEntidadFiscalDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                iefacreedora = bgln.RecuperarListaCondicional(GetType(FN.Empresas.DN.EmpresaFiscalDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlEmpresaFiscalDN", "Cif_Codigo", "B83204586")).Item(0)
                coldeudoras.EliminarEntidadDNxGUID(iefacreedora.GUID)


                ' crear el origen debido
                Dim origen As FN.GestionPagos.DN.OrigenIdevBaseDN
                origen = CrearOrigenImportedebido(iefacreedora, coldeudoras)
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                gi.Guardar(origen)




                Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = ln.Saldo(origen.IImporteDebidoDN.Acreedora, origen.IImporteDebidoDN.Deudora, origen.IImporteDebidoDN.FEfecto)

                'If saldo <> origen.IImporteDebidoDN.Importe Then
                '    Throw New ApplicationException
                'End If

                tr.Confirmar()


            End Using
        End Using


    End Sub



    <TestMethod()> Public Sub pe1v2AnularOrigenIDEnUnaOperacionImportesDebidos()


        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim ColAgrupApunteImpD As New FN.GestionPagos.DN.ColAgrupApunteImpDDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColAgrupApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))

                Dim aid As FN.GestionPagos.DN.ApunteImpDDN = ColAgrupApunteImpD(0).ColApunteImpDDN(0)
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                gi.Recuperar(aid.HuellaIOrigenImpDebDN)



                ' se recupera un origen de importe debido coyo aid esta agrupado en una agrupacion
                ' al intentar anular el origen de importe debido debe dar un error
                Dim oid As FN.GestionPagos.DN.IOrigenIImporteDebidoDN = aid.HuellaIOrigenImpDebDN.EntidadReferida

                Dim excepcion As Boolean
                Try


                    Dim colLiqpago As FN.GestionPagos.DN.ColLiquidacionPagoDN
                    Dim mln As New FN.GestionPagos.LN.MotorLiquidacionLN

                    colLiqpago = mln.AnularOrigenImpDeb(oid, Now)

                    excepcion = False


                Catch ex As Exception
                    excepcion = True
                End Try

                If Not excepcion Then
                    Throw New ApplicationException
                End If

                '  tr.Confirmar()


            End Using
        End Using

    End Sub


    <TestMethod()> Public Sub pe1v3CrearPagoIDEnUnaOperacionImportesDebidos()


        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim ColAgrupApunteImpD As New FN.GestionPagos.DN.ColAgrupApunteImpDDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColAgrupApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))






                Dim aid As FN.GestionPagos.DN.ApunteImpDDN = ColAgrupApunteImpD(0).ColApunteImpDDN(0)


                ' se crea un pago sobre un apunte importedebido  que forma parte de una agrupacion
                Dim excepcion As Boolean
                Try

                    Dim pago As New FN.GestionPagos.DN.PagoDN

                    pago.ApunteImpDOrigen = aid
                    pago.FechaProgramadaEmision = Now.AddDays(1)

                    Dim pagosln As New FN.GestionPagos.LN.MotorLiquidacionLN
                    pagosln.GuardarGenerico(pago)



                    excepcion = False


                Catch ex As Exception
                    excepcion = True
                End Try

                If Not excepcion Then
                    Throw New ApplicationException
                End If

                '  tr.Confirmar()


            End Using
        End Using

    End Sub




    <TestMethod()> Public Sub pe1v4ModificarAgrupacion()


        CrearElRecurso("")


        Dim aid As FN.GestionPagos.DN.ApunteImpDDN
        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim ColAgrupApunteImpD As New FN.GestionPagos.DN.ColAgrupApunteImpDDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColAgrupApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))

                Dim ag As FN.GestionPagos.DN.AgrupApunteImpDDN = ColAgrupApunteImpD(0)
                aid = ColAgrupApunteImpD(0).ColApunteImpDDN(0)

                'eliminamos uno de los pagos de la agrupacion
                ag.ColApunteImpDDN.Remove(aid)

                If Not String.IsNullOrEmpty(aid.GUIDAgrupacion) Then
                    Throw New ApplicationException
                End If
                ag.Actualizar()


                Dim agln As New FN.GestionPagos.LN.AgrupApunteImpDLN
                agln.GuardarAgrupacion(ag)



                tr.Confirmar()


            End Using

            ' gaurdamos el apunte el cual debe dar erroe de concurrencia porrque al guardar la agrupacion tambien se guardan los apuntes eliminados
            Dim excepcion As Boolean
            Using tr2 As New Transaccion(False)

                Try
                    Dim agln As New FN.GestionPagos.LN.AgrupApunteImpDLN
                    agln.GuardarGenerico(aid)
                    excepcion = False
                Catch ex As Exception
                    excepcion = True
                End Try


            End Using

            If Not excepcion Then
                Throw New ApplicationException
            End If

        End Using


    End Sub


    <TestMethod()> Public Sub pe1v5AnularAgrupacion()



        CrearElRecurso("")


        Dim aid As FN.GestionPagos.DN.ApunteImpDDN
        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim ColAgrupApunteImpD As New FN.GestionPagos.DN.ColAgrupApunteImpDDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColAgrupApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))

                Dim ag As FN.GestionPagos.DN.AgrupApunteImpDDN = ColAgrupApunteImpD(0)
                aid = ColAgrupApunteImpD(0).ColApunteImpDDN(0)



                Dim agln As New FN.GestionPagos.LN.AgrupApunteImpDLN
                agln.AnularAgrupacion(ag, Now)



                tr.Confirmar()


            End Using

            '' gaurdamos el apunte el cual debe dar erroe de concurrencia porrque al guardar la agrupacion tambien se guardan los apuntes eliminados
            'Dim excepcion As Boolean
            'Using tr2 As New Transaccion(False)

            '    Try
            '        Dim agln As New FN.GestionPagos.LN.AgrupApunteImpDLN
            '        agln.GuardarGenerico(aid)
            '        excepcion = False
            '    Catch ex As Exception
            '        excepcion = True
            '    End Try


            'End Using

            'If Not excepcion Then
            '    Throw New ApplicationException
            'End If

        End Using


    End Sub



    <TestMethod()> Public Sub pe1v6AnularAgrupacionAIDProductoReferidoPorPago()


        CrearElRecurso("")


        Dim aid As FN.GestionPagos.DN.ApunteImpDDN
        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim ColAgrupApunteImpD As New FN.GestionPagos.DN.ColAgrupApunteImpDDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColAgrupApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))

                Dim ag As FN.GestionPagos.DN.AgrupApunteImpDDN = ColAgrupApunteImpD(0)
                aid = ColAgrupApunteImpD(0).ColApunteImpDDN(0)




                Dim pago As New FN.GestionPagos.DN.PagoDN
                pago.ApunteImpDOrigen = ag.IImporteDebidoDN
                pago.Importe = ag.IImporteDebidoDN.Importe / 2
                pago.FechaProgramadaEmision = ag.IImporteDebidoDN.FEfecto.AddDays(2)


                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                gi.Guardar(pago)


                Dim agln As New FN.GestionPagos.LN.AgrupApunteImpDLN
                agln.AnularAgrupacion(ag, Now)



                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                Dim pagobd As FN.GestionPagos.DN.PagoDN = gi.Recuperar(pago.ID, GetType(FN.GestionPagos.DN.PagoDN))


                If Not pagobd.FechaAnulacion <> Date.MinValue Then
                    Throw New ApplicationException
                End If


                tr.Confirmar()


            End Using


        End Using


    End Sub

    <TestMethod()> Public Sub pe1v7AnularAgrupacionAIDProductoAsuvezAgrupado()

        CrearElRecurso("")


        Dim aid As FN.GestionPagos.DN.ApunteImpDDN
        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim ColAgrupApunteImpD As New FN.GestionPagos.DN.ColAgrupApunteImpDDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColAgrupApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN)))

                Dim ag As FN.GestionPagos.DN.AgrupApunteImpDDN = ColAgrupApunteImpD(0)
                aid = ColAgrupApunteImpD(0).ColApunteImpDDN(0)


                ' agrupamos el apunte debido producto de la primera agrupacion en una segunda
                Dim ColApunteImpD As New FN.GestionPagos.DN.ColApunteImpDDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColApunteImpD.Add(aid)

                Dim agln As New FN.GestionPagos.LN.AgrupApunteImpDLN
                Dim agid As New FN.GestionPagos.DN.AgrupApunteImpDDN


                Dim excepcion As Boolean
                Try

                    agid.ColApunteImpDDN.AddRangeObjectUnico(ColApunteImpD)
                Catch ex As Exception
                    excepcion = True
                End Try



                If Not excepcion Then
                    Throw New ApplicationException
                End If




            End Using


        End Using


    End Sub
    <TestMethod()> Public Sub pe1v1CrearAgrupacioncionesImportesDebidos()




        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)



            Using tr As New Transaccion


                ' crear unos pagos de pruebas con sus origenes
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN


                ' recuperar empresas

                Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

                Dim ColApunteImpD As New FN.GestionPagos.DN.ColApunteImpDDN
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                ColApunteImpD = ColApunteImpD.SeleccioanrSoloDosEntidadesFiscales(ColApunteImpD(0).Acreedora, ColApunteImpD(0).Deudora)





                Dim agln As New FN.GestionPagos.LN.AgrupApunteImpDLN
                Dim agid As New FN.GestionPagos.DN.AgrupApunteImpDDN
                agid.ColApunteImpDDN.AddRangeObjectUnico(ColApunteImpD)
                agid.IImporteDebidoDN.Acreedora = ColApunteImpD(0).Acreedora
                agid.IImporteDebidoDN.Deudora = ColApunteImpD(0).Deudora
                agid.Actualizar()
                Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = ln.Saldo(agid.IImporteDebidoDN.Acreedora, agid.IImporteDebidoDN.Deudora, agid.IImporteDebidoDN.FEfecto)




                ''agid.ColApunteImpDDN.Remove(agid.ColApunteImpDDN(0))

                agln.GuardarAgrupacion(agid)




                Dim saldo2 As Double = ln.Saldo(agid.IImporteDebidoDN.Acreedora, agid.IImporteDebidoDN.Deudora, agid.IImporteDebidoDN.FEfecto)


                If saldo <> saldo2 Then
                    Throw New ApplicationException
                End If

                tr.Confirmar()


            End Using
        End Using


    End Sub



    '<TestMethod()> Public Sub pe1v1AnularOrigenImportedebidoManual()
    '    ' este metodo lo tine que implementar en casda origen de importe debido
    '    ' el importe debido siempre es anulable pero desde su origen
    '    ' si el import  debido tenia pagos habra que porceder a su anulación o compesacion


    '    CrearElRecurso("")

    '    Using New CajonHiloLN(mRecurso)

    '        Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
    '        Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

    '        bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
    '        colPagos0 = New FN.GestionPagos.DN.ColPagoDN
    '        colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



    '        Using tr3 As New Transaccion(True)

    '            Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

    '            For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0

    '                Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN

    '                If ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) <> FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.Ninguna Then
    '                    Throw New ApplicationException("todos los pagos debian de haberse anulado")
    '                End If
    '            Next

    '            ' verificacion de la prueba
    '            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
    '            colPagos1 = New FN.GestionPagos.DN.ColPagoDN
    '            colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
    '            If colPagos1.Count <> 1 OrElse colPagos1.Item(0).FAnulacion <> Date.MinValue Then
    '                Throw New ApplicationException("error")
    '            End If

    '            tr3.Cancelar()
    '        End Using

    '    End Using


    'End Sub
    <TestMethod()> Public Sub pe2v0CrearCrearPagoPlanificado()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)
            crearElEntornoB()
        End Using



        Using New CajonHiloLN(mRecurso)




            Dim colPagos0 As FN.GestionPagos.DN.ColPagoDN


            Using tr1 As New Transaccion(True)
                colPagos0 = CrearColPagos("", 1)


                Dim pago As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0)
                Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = ln.Saldo(pago.Destinatario, pago.Deudor, Date.MaxValue)

                If saldo <> pago.ApunteImpDOrigen.Importe Then
                    Throw New ApplicationException
                End If





                tr1.Confirmar()






            End Using


        End Using


    End Sub
    <TestMethod()> Public Sub pe2v1AnularCompensarCrearPagoPlanificado()




        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0

                    Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                    If Not ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) = FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.PagoAnulado Then
                        Throw New ApplicationException("todos los pagos debian de haberse anulado")
                    End If
                Next

                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
                If colPagos1.Count <> 1 OrElse colPagos1.Item(0).FAnulacion = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                Dim pago1 As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0)
                Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = ln.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

                If saldo <> pago1.ApunteImpDOrigen.Importe Then
                    Throw New ApplicationException
                End If




                tr3.Cancelar()
            End Using

        End Using


    End Sub


    <TestMethod()> Public Sub pe3v0EmitirPagoPlanificado()



        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0
                    ml.EmitirPago(pago)
                Next

                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
                If colPagos1.Count <> 1 OrElse colPagos1.Item(0).FAnulacion <> Date.MinValue OrElse colPagos1.Item(0).FechaEmision = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                Dim pago1 As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0)
                Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = ln.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

                If saldo <> pago1.ApunteImpDOrigen.Importe Then
                    Throw New ApplicationException
                End If





                tr3.Confirmar()
            End Using

        End Using

    End Sub

    <TestMethod()> Public Sub pe3v1AnularCompensarPagoEmitido()


        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0

                    Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                    If ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) <> FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.Ninguna Then
                        Throw New ApplicationException("todos los pagos debian de haberse anulado")
                    End If
                Next

                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
                If colPagos1.Count <> 1 OrElse colPagos1.Item(0).FAnulacion <> Date.MinValue Then
                    Throw New ApplicationException("error")
                End If

                tr3.Cancelar()
            End Using

        End Using

    End Sub

    <TestMethod()> Public Sub pe4v0EfectuarPagoEmitido()


        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0
                    ml.EfectuarPago(pago)
                Next

                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
                If colPagos1.Count <> 1 OrElse colPagos1.Item(0).FAnulacion <> Date.MinValue OrElse colPagos1.Item(0).FechaEmision = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                Dim pago1 As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0)
                Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = ln.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

                If saldo <> 0 Then
                    Throw New ApplicationException
                End If



                tr3.Confirmar()
            End Using

        End Using

    End Sub

    <TestMethod()> Public Sub pe4v1AnularCompensarPagoEfectuado()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0

                    Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN
                    Dim pc As FN.GestionPagos.DN.PagoDN = pago.CrearPagoCompensador

                    If ml.AnularOCompensarPago(pc, colLiq) <> FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.PagoCompensado Then
                        Throw New ApplicationException("todos los pagos debian de haberse anulado")
                    End If

                    ' como el pago compensado no queda efectuado lo efectuamos
                    'ml.EmitirPago(pc)
                    'ml.EfectuarPago(pc)

                Next

                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))


                If colPagos1.Count <> 2 OrElse colPagos1.Item(0).FAnulacion <> Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                If colPagos1.Count <> 2 OrElse colPagos1.Item(1).FAnulacion <> Date.MinValue OrElse colPagos1.Item(1).FechaEfecto <> Date.MinValue OrElse colPagos1.Item(1).PagoCompensado IsNot colPagos1.Item(0) Then
                    Throw New ApplicationException("error")
                End If

                'Dim pago1 As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0)
                'Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                'Dim saldo As Double = ln.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

                'If saldo <> pago1.IImporteDebidoOrigen.Importe Then
                '    Throw New ApplicationException
                'End If



                tr3.Confirmar()
            End Using

        End Using


    End Sub





    <TestMethod()> Public Sub pe4v01DevolverPagoEfectuado()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN



                Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN
                Dim pc As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0).CrearPagoCompensador
                ' hay que poner la fecha de emisión de la devolución cuando fue realizada por el banco
                Dim mensaje As String
                If Not pc.RegistrarFechaEmisionPagoYaEmitido(Now, mensaje) Then
                    Throw New ApplicationException(mensaje)
                End If
                Dim pdevuleto As FN.GestionPagos.DN.PagoDN = ml.DevolverPago(pc, colLiq)





                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



                If colPagos1.Count <> 2 Then
                    Throw New ApplicationException("error")
                End If

                If colPagos1.Item(0).FAnulacion <> Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                If colPagos1.Item(1).FAnulacion <> Date.MinValue OrElse colPagos1.Item(1).FechaEfecto = Date.MinValue OrElse colPagos1.Item(1).PagoCompensado IsNot colPagos1.Item(0) Then
                    Throw New ApplicationException("error")
                End If

                Dim pago1 As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0)
                Dim ln As New FN.GestionPagos.LN.ApunteImpDLN
                Dim saldo As Double = ln.Saldo(pago1.Destinatario, pago1.Deudor, Date.MaxValue)

                If saldo <> pago1.ApunteImpDOrigen.Importe Then
                    Throw New ApplicationException
                End If



                tr3.Cancelar()
            End Using

        End Using


    End Sub


    <TestMethod()> Public Sub pe4v2AnularCompensarPagoEfectuadoCompensadoYReAnulCompPagoCompensado()
        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
                Dim pago As FN.GestionPagos.DN.PagoDN
                Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN


                pago = colPagos0(0) ' este pago ya esta compensado y no se puede ni anular ni  compensar
                If ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) <> FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.Ninguna Then
                    Throw New ApplicationException("todos los pagos debian de haberse anulado")
                End If

                pago = colPagos0(1) ' este pago es el que compensaba el pago anteriro que como no se ha efectuado puede ser anulado
                If ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) <> FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.PagoAnulado Then
                    Throw New ApplicationException("todos los pagos debian de haberse anulado")
                End If

                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))


                If colPagos1.Count <> 2 Then
                    Throw New ApplicationException("error")
                End If

                ' el primero no puede estar anulado y debe estar efectuado
                If colPagos1.Item(0).FAnulacion <> Date.MinValue OrElse colPagos1.Item(0).FechaEfecto = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If

                ' el segundo debe ser el compensador del primero y no estar efectuado y debe estar anulado
                If colPagos1.Item(1).FAnulacion = Date.MinValue OrElse colPagos1.Item(1).FechaEfecto <> Date.MinValue OrElse colPagos1.Item(1).PagoCompensado IsNot colPagos1.Item(0) Then
                    Throw New ApplicationException("error")
                End If



                tr3.Confirmar()
            End Using

        End Using
    End Sub
    <TestMethod()> Public Sub pe5v0LiquidarPagoEfectuado()

        CrearElRecurso("")

        Me.CrearElEntorno()
        pe2v0CrearCrearPagoPlanificado()
        Me.pe3v0EmitirPagoPlanificado()
        Me.pe4v0EfectuarPagoEmitido()


        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
                Dim colliq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0
                    colliq = ml.LiquidarPago(pago)
                Next

                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                ' debieran de exitir 3 apuntes debidos
                If ColApunteImpD.Count <> 4 Then
                    Throw New ApplicationException("error")
                End If
                If ColApunteImpD.Item(1).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(1).FEfecto = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If

                If ColApunteImpD.Item(2).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(2).FEfecto = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If

                If ColApunteImpD.Item(3).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(3).FEfecto = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                ' Dim id As FN.GestionPagos.DN.ApunteImpDDN = ColApunteImpD.EliminarEntidadDNxGUID(ColApunteImpD.Item(0).GUID)(0)
                'If ColApunteImpD(1).Importe <> (ColApunteImpD(2).Importe + ColApunteImpD(3).Importe) Then
                '    Throw New ApplicationException("error")
                'End If

                tr3.Confirmar()
            End Using

        End Using


    End Sub
    <TestMethod()> Public Sub pe5v1AnularCompensarPagoLiquidado()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0

                    Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                    If ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) <> FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.PagoCompensado Then
                        Throw New ApplicationException("todos los pagos debian de haberse anulado")
                    End If
                Next

                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))


                If colPagos1.Count <> 2 OrElse colPagos1.Item(0).FAnulacion <> Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                If colPagos1.Count <> 2 OrElse colPagos1.Item(1).FAnulacion <> Date.MinValue OrElse colPagos1.Item(1).FechaEfecto <> Date.MinValue OrElse colPagos1.Item(1).PagoCompensado IsNot colPagos1.Item(0) Then
                    Throw New ApplicationException("error")
                End If


                Dim colliq2 As New FN.GestionPagos.DN.ColLiquidacionPagoDN
                colliq2.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.LiquidacionPagoDN)))

                ' debieran de haber dos liquidaciones anuladas
                If colliq2.Count <> 2 Then
                    Throw New ApplicationException("error")
                End If
                If colliq2(0).FAnulacion = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If
                If colliq2(1).FAnulacion = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If
                tr3.Confirmar()
            End Using

        End Using

    End Sub



    <TestMethod()> Public Sub pe6v0LiquidarPagoEfectuadoyPagarLiquidaciones()



        Me.CrearElEntorno()
        pe2v0CrearCrearPagoPlanificado()
        Me.pe3v0EmitirPagoPlanificado()
        Me.pe4v0EfectuarPagoEmitido()


        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0 As FN.GestionPagos.DN.ColPagoDN





            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))

            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
                Dim colliq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagos0
                    colliq = ml.LiquidarPago(pago)
                Next


                ' creamos un pago plainifaco para la primera
                Dim pagoidlq As FN.GestionPagos.DN.PagoDN

                ''''' creacion de pago
                pagoidlq = New FN.GestionPagos.DN.PagoDN
                pagoidlq.ApunteImpDOrigen = colliq(0).IImporteDebidoDN
                pagoidlq.Importe = pagoidlq.ApunteImpDOrigen.Importe
                pagoidlq.Nombre = "pago planinificado 1liquidacion"
                Me.GuardarDatos(pagoidlq)



                ' cramos un  pago planificado y lo efectuamos para la segunda
                pagoidlq = New FN.GestionPagos.DN.PagoDN
                pagoidlq.ApunteImpDOrigen = colliq(1).IImporteDebidoDN
                pagoidlq.Importe = pagoidlq.ApunteImpDOrigen.Importe
                pagoidlq.Nombre = "pago abona 2liquidacion"
                Me.GuardarDatos(pagoidlq)
                Dim colpagos2 As New FN.GestionPagos.DN.ColPagoDN
                colpagos2.Add(pagoidlq)
                Me.EmitirPagos(colpagos2)
                Me.EfectuarPagop(colpagos2)






                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

                Dim ColApunteImpD As FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD = New FN.GestionPagos.DN.ColApunteImpDDN
                ColApunteImpD.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.ApunteImpDDN)))
                ' debieran de exitir 3 apuntes debidos
                If ColApunteImpD.Count <> 5 Then
                    Throw New ApplicationException("error")
                End If




                Dim ColPagov As FN.GestionPagos.DN.ColPagoDN
                ColPagov = New FN.GestionPagos.DN.ColPagoDN
                ColPagov.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))
                ' debieran de exitir 3 apuntes debidos
                If ColPagov.Count <> 3 Then
                    Throw New ApplicationException("error")
                End If



                'If ColApunteImpD.Item(1).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(1).FEfecto = Date.MinValue Then
                '    Throw New ApplicationException("error")
                'End If

                'If ColApunteImpD.Item(2).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(2).FEfecto = Date.MinValue Then
                '    Throw New ApplicationException("error")
                'End If

                'If ColApunteImpD.Item(3).FAnulacion <> Date.MinValue OrElse ColApunteImpD.Item(3).FEfecto = Date.MinValue Then
                '    Throw New ApplicationException("error")
                'End If


                ' Dim id As FN.GestionPagos.DN.ApunteImpDDN = ColApunteImpD.EliminarEntidadDNxGUID(ColApunteImpD.Item(0).GUID)(0)
                'If ColApunteImpD(1).Importe <> (ColApunteImpD(2).Importe + ColApunteImpD(3).Importe) Then
                '    Throw New ApplicationException("error")
                'End If

                tr3.Confirmar()
            End Using

        End Using


    End Sub

    <TestMethod()> Public Sub pe6v1AnularCompensarPagoLiquidadoLqPagada()
        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colPagos0 = New FN.GestionPagos.DN.ColPagoDN
            colPagos0.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN


                Dim pago As FN.GestionPagos.DN.PagoDN = colPagos0.Item(0)
                Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                If ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) <> FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.PagoCompensado Then
                    Throw New ApplicationException("todos los pagos debian de haberse anulado")
                End If


                ' verificacion de la prueba
                bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))


                If colPagos1.Count <> 4 Then
                    Throw New ApplicationException("error")
                End If



                If colPagos1.Item(0).FAnulacion <> Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                'If colPagos1.Item(1).FAnulacion <> Date.MinValue OrElse colPagos1.Item(1).FechaEfecto <> Date.MinValue OrElse colPagos1.Item(1).PagoCompensado IsNot colPagos1.Item(0) Then
                '    Throw New ApplicationException("error")
                'End If


                Dim colliq2 As New FN.GestionPagos.DN.ColLiquidacionPagoDN
                colliq2.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.LiquidacionPagoDN)))

                ' debieran de haber dos liquidaciones anuladas
                If colliq2.Count <> 3 Then
                    Throw New ApplicationException("error")
                End If
                If colliq2(0).FAnulacion = Date.MinValue Then
                    Throw New ApplicationException("error")
                End If
                If colliq2(1).FAnulacion <> Date.MinValue Then
                    Throw New ApplicationException("error")
                End If

                If colliq2(2).FAnulacion <> Date.MinValue Then
                    Throw New ApplicationException("error")
                End If


                If colliq2(2).IImporteDebidoDN.Importe = colliq2(0).IImporteDebidoDN.Importe Then
                    Throw New ApplicationException("error")
                End If
                If colliq2(2).LiquidacionCompensada Is colliq2(0) Then
                    Throw New ApplicationException("error")
                End If
                tr3.Cancelar()
            End Using

        End Using
    End Sub


    <TestMethod()> Public Sub pe7v0AnularCompensarOrigenID()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)



            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim oidm As FN.GestionPagos.DN.OrigenIdevBaseDN = bgln.RecuperarLista(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN))(0)



            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
                Dim ColLiquidacionPago As New FN.GestionPagos.DN.ColLiquidacionPagoDN
                Dim pfechaAnulacion As Date
                pfechaAnulacion = Now
                ColLiquidacionPago = ml.AnularOrigenImpDeb(oidm, pfechaAnulacion)




                ' verificacion de la prueba
                'bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                'colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                'colPagos1.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))


                'If colPagos1.Count <> 4 Then
                '    Throw New ApplicationException("error")
                'End If



                'If colPagos1.Item(0).FAnulacion <> Date.MinValue Then
                '    Throw New ApplicationException("error")
                'End If


                ''If colPagos1.Item(1).FAnulacion <> Date.MinValue OrElse colPagos1.Item(1).FechaEfecto <> Date.MinValue OrElse colPagos1.Item(1).PagoCompensado IsNot colPagos1.Item(0) Then
                ''    Throw New ApplicationException("error")
                ''End If


                'Dim colliq2 As New FN.GestionPagos.DN.ColLiquidacionPagoDN
                'colliq2.AddRangeObject(bgln.RecuperarLista(GetType(FN.GestionPagos.DN.LiquidacionPagoDN)))

                '' debieran de haber dos liquidaciones anuladas
                'If colliq2.Count <> 3 Then
                '    Throw New ApplicationException("error")
                'End If
                'If colliq2(0).FAnulacion = Date.MinValue Then
                '    Throw New ApplicationException("error")
                'End If
                'If colliq2(1).FAnulacion <> Date.MinValue Then
                '    Throw New ApplicationException("error")
                'End If

                'If colliq2(2).FAnulacion <> Date.MinValue Then
                '    Throw New ApplicationException("error")
                'End If


                'If colliq2(2).IImporteDebidoDN.Importe = colliq2(0).IImporteDebidoDN.Importe Then
                '    Throw New ApplicationException("error")
                'End If
                'If colliq2(2).LiquidacionCompensada Is colliq2(0) Then
                '    Throw New ApplicationException("error")
                'End If
                tr3.Confirmar()
            End Using

        End Using


    End Sub



    <TestMethod()> Public Sub CrearElEntorno()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            crearElEntornoB()
        End Using

    End Sub

    <TestMethod()> Public Sub CrearColPagos()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            CrearColPagos("", 1)
        End Using

    End Sub


    <TestMethod()> Public Sub CrearPagoConIDCompensadores()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            CrearPagoConIDCompensadoresp("")

        End Using

    End Sub



    <TestMethod()> Public Sub EfectuarPago()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim mensaje As String

            'Using tr As New Transaccion

            '    Dim pg As Framework.DatosNegocio.EntidadDN
            '    Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            '    gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            '    pg = gi.Recuperar("1", GetType(FN.Localizaciones.DN.EntidadFiscalGenericaDN))
            '    System.Diagnostics.Debug.WriteLine(pg.EstadoIntegridad(mensaje))


            '    gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            '    pg = gi.Recuperar("1", GetType(FN.Empresas.DN.EmpresaFiscalDN))
            '    System.Diagnostics.Debug.WriteLine(pg.EstadoIntegridad(mensaje))

            '    gi = New GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            '    pg = gi.Recuperar("1", GetType(FN.GestionPagos.DN.PagoDN))
            '    System.Diagnostics.Debug.WriteLine(pg.EstadoIntegridad(mensaje))



            '    tr.Confirmar()

            'End Using






            Using tr As New Transaccion


                Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                Dim colPagos As New FN.GestionPagos.DN.ColPagoDN

                colPagos.AddRangeObject(ln.RecuperarLista(GetType(FN.GestionPagos.DN.PagoDN)))


                System.Diagnostics.Debug.WriteLine(colPagos.Item(0).EstadoIntegridad(mensaje))
                Me.EmitirPagos(colPagos)
                Me.EfectuarPagop(colPagos)
                ' LiquidarPagos(colPagos)
                tr.Confirmar()

            End Using








        End Using

    End Sub









    <TestMethod()> Public Sub LiquidarPagos()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)

            Dim colPagos As FN.GestionPagos.DN.ColPagoDN

            colPagos = CrearColPagos("", 1)
            Me.EmitirPagos(colPagos)
            Me.EfectuarPagop(colPagos)
            LiquidarPagos(colPagos)

        End Using

    End Sub


    <TestMethod()> Public Sub CompensarPago()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)
            CompensarPagosOriginales()

        End Using

    End Sub

    <TestMethod()> Public Sub CompensarPagoCompensado()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)
            CompensarPagosCompensados()

        End Using

    End Sub
    <TestMethod()> Public Sub CompensarPagoCompensadoCompensado()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)
            CompensarPagosCompensadosCompensados()

        End Using

    End Sub



    <TestMethod()> Public Sub AnularPagos()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)
            AnularPagosOriginales()

        End Using

    End Sub


    <TestMethod()> Public Sub AnularCompensarPagosOriginales2()

        CrearElRecurso("")

        Using New CajonHiloLN(mRecurso)
            AnularCompensarPagosOriginales()

        End Using

    End Sub



#Region "Metodos "

    Public Sub EmitirPagos(ByVal colPagos As FN.GestionPagos.DN.ColPagoDN)
        ' TODO:ojo esto debes repasarlo
        For Each pagoOriginal As FN.GestionPagos.DN.PagoDN In colPagos
            pagoOriginal.EmitirPago("")
        Next

    End Sub

    Public Sub EfectuarPagop(ByVal colPagos As FN.GestionPagos.DN.ColPagoDN)



        Using tr As New Transaccion

            Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

            ' TODO:ojo esto debes repasarlo
            For Each pago As FN.GestionPagos.DN.PagoDN In colPagos
                ml.EfectuarPago(pago)

            Next


            tr.Confirmar()

        End Using






    End Sub

    Public Function CompensarPagosCompensadosCompensados() As FN.GestionPagos.DN.ColPagoDN
        Dim colPagosCompensados, colPagosCompensadores As New FN.GestionPagos.DN.ColPagoDN
        Dim colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN

        colPagosCompensados = CompensarPagosCompensados()
        Me.EmitirPagos(colPagosCompensados)
        Me.EfectuarPagop(colPagosCompensados)

        For Each pagoOriginal As FN.GestionPagos.DN.PagoDN In colPagosCompensados

            Dim p As FN.GestionPagos.DN.PagoDN = pagoOriginal.CrearPagoCompensador()
            colPagosCompensadores.Add(p)
        Next

        colPagosCompensados = CompesarPagos(colPagosCompensadores, colLiqPago)
        Return colPagosCompensados



    End Function

    Public Function CompensarPagosCompensados() As FN.GestionPagos.DN.ColPagoDN
        Dim colPagosCompensados, colPagosCompensadores As New FN.GestionPagos.DN.ColPagoDN
        Dim colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN

        colPagosCompensados = CompensarPagosOriginales()
        Me.EmitirPagos(colPagosCompensados)
        Me.EfectuarPagop(colPagosCompensados)

        For Each pagoOriginal As FN.GestionPagos.DN.PagoDN In colPagosCompensados

            Dim p As FN.GestionPagos.DN.PagoDN = pagoOriginal.CrearPagoCompensador()
            ' p.FechaEfecto = pagoOriginal.FechaEfecto.AddMinutes(1)
            colPagosCompensadores.Add(p)
        Next

        colPagosCompensados = CompesarPagos(colPagosCompensadores, colLiqPago)
        Return colPagosCompensados



    End Function


    'Public Function AnularOCompensarPagosOriginales() As FN.GestionPagos.DN.ColPagoDN

    '    Dim colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN
    '    Dim colPagos, colPagosCompensados As FN.GestionPagos.DN.ColPagoDN

    '    colPagos = CrearColPagos("", 2)
    '    Dim pagoNoLiquidado As FN.GestionPagos.DN.PagoDN = colPagos.EliminarEntidadDN(colPagos.Item(0).ID).Item(0) ' para que solo se liquide uno de los pagos y el otro no

    '    LiquidarPagos(colPagos) ' liquidamos todos los pagos menos uno

    '    colPagos.Add(pagoNoLiquidado) ' volvemos a añadir el pago a la col

    '    ' creamos un pago compensador de cada pago
    '    Dim colPagosCompensadores As New FN.GestionPagos.DN.ColPagoDN

    '    For Each pagoOriginal As FN.GestionPagos.DN.PagoDN In colPagos

    '        Dim p As FN.GestionPagos.DN.PagoDN = pagoOriginal.CrearPagoCompensador()
    '        colPagosCompensadores.Add(p)

    '    Next


    '    colPagosCompensados = CompesarOAnularPagos(colPagosCompensadores, colLiqPago)

    '    Return colPagosCompensados

    'End Function



    ''' <summary>
    ''' crea 1 pagos 
    ''' primero lo efenctua
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function CompensarPagosOriginales() As FN.GestionPagos.DN.ColPagoDN



        Dim pagoCompensador As FN.GestionPagos.DN.PagoDN

        Dim colPagos0, colPagos1, colPagosCompensadores, colPagosCompensados As FN.GestionPagos.DN.ColPagoDN

        Dim colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN


        Using tr0 As New Transaccion

            Using tr1 As New Transaccion(True)
                colPagos0 = CrearColPagos("", 4)
                tr1.Confirmar()
            End Using


            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            '  el primero lo compensa sin efectuarse --> error
            colPagos1 = New FN.GestionPagos.DN.ColPagoDN
            colPagos1.Add(colPagos0.Item(0))

            ' Me.EfectuarPagop(colPagos1)
            ' LiquidarPagos(colPagos1)


            pagoCompensador = colPagos1.Item(0).CrearPagoCompensador()
            colPagosCompensadores = New FN.GestionPagos.DN.ColPagoDN
            colPagosCompensadores.Add(pagoCompensador)


            Using tr2 As New Transaccion(True)
                Try
                    CompesarPagos(colPagosCompensadores, colLiqPago)
                    Throw New ApplicationException("debia haberse dado excepción")
                Catch ex As Exception

                End Try
                tr2.Cancelar()

            End Using
            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            '  el segundo lo efectua y lo compensa -->ok crea un pago de compensacion
            colPagos1 = New FN.GestionPagos.DN.ColPagoDN
            colPagos1.Add(colPagos0.Item(1))
            Me.EmitirPagos(colPagos1)
            Me.EfectuarPagop(colPagos1)
            ' LiquidarPagos(colPagos1)



            pagoCompensador = colPagos1.Item(0).CrearPagoCompensador()
            colPagosCompensadores = New FN.GestionPagos.DN.ColPagoDN
            colPagosCompensadores.Add(pagoCompensador)
            CompesarPagos(colPagosCompensadores, colLiqPago)
            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            'el tercero lo efectua + liquida -- ok compensa el pago y compensa la liquidación

            colPagos1 = New FN.GestionPagos.DN.ColPagoDN
            colPagos1.Add(colPagos0.Item(2))
            Me.EmitirPagos(colPagos1)

            Me.EfectuarPagop(colPagos1)
            LiquidarPagos(colPagos1)

            pagoCompensador = colPagos1.Item(0).CrearPagoCompensador()
            '  pagoCompensador.FechaEfecto = colPagos1.Item(0).FechaEfecto.AddMinutes(1)
            colPagosCompensadores = New FN.GestionPagos.DN.ColPagoDN
            colPagosCompensadores.Add(pagoCompensador)
            CompensarPagosOriginales = CompesarPagos(colPagosCompensadores, colLiqPago)
            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''




            tr0.Confirmar()

        End Using

















        'Dim colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN
        'Dim colPagos, colPagosCompensados As FN.GestionPagos.DN.ColPagoDN

        'colPagos = CrearColPagos("", 3)
        'Me.EfectuarPagop(colPagos)
        'LiquidarPagos(colPagos)


        '' creamos un pago compensador de cada pago
        'Dim colPagosCompensadores As New FN.GestionPagos.DN.ColPagoDN

        'For Each pagoOriginal As FN.GestionPagos.DN.PagoDN In colPagos

        '    Dim p As FN.GestionPagos.DN.PagoDN = pagoOriginal.CrearPagoCompensador()
        '    ' p.FechaEfecto = pagoOriginal.FechaEfecto.AddMinutes(1) ' este valor debiera ser determinado por el compensador
        '    colPagosCompensadores.Add(p)

        'Next


        'colPagosCompensados = CompesarPagos(colPagosCompensadores, colLiqPago)

        'Return colPagosCompensados

    End Function





    Public Function CrearPagoConIDCompensadoresp(ByVal conex As String) As FN.GestionPagos.DN.PagoDN




        Using tr1 As New Transaccion()


            ' crear unos pagos de pruebas con sus origenes
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN



            ' recuperar empresas

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

            Dim coldeudoras As New ColIEntidadFiscalDN
            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()


            coldeudoras.AddRangeObject(bgln.RecuperarLista(GetType(FN.Empresas.DN.EmpresaFiscalDN)))

            Dim iefacreedora As IEntidadFiscalDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            iefacreedora = bgln.RecuperarListaCondicional(GetType(FN.Empresas.DN.EmpresaFiscalDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlEmpresaFiscalDN", "Cif_Codigo", "B83204586")).Item(0)

            coldeudoras.EliminarEntidadDNxGUID(iefacreedora.GUID)

            Dim colapCompensantes As New FN.GestionPagos.DN.ColIImporteDebidoDN

            ' crear el origen de importe debido
            Dim origen As FN.GestionPagos.DN.OrigenIdevBaseDN

            ' primer origen correspondiente a un servicio prestado
            origen = New FN.GestionPagos.DN.OrigenIdevBaseDN
            origen.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(origen)
            origen.IImporteDebidoDN.Importe = 40
            origen.IImporteDebidoDN.Acreedora = iefacreedora.EntidadFiscalGenerica
            origen.IImporteDebidoDN.Deudora = coldeudoras.Item(0).EntidadFiscalGenerica
            origen.IImporteDebidoDN.FCreación = Now
            origen.IImporteDebidoDN.FEfecto = Now
            colapCompensantes.Add(origen.IImporteDebidoDN)
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(origen)

            '''''''''''''''''''''''

            'mporte debido que presupone un pago previo realizado por el cliente o cualquier otro derecho adquirido del cliente para empresa que presta los servicios
            origen = New FN.GestionPagos.DN.OrigenIdevBaseDN
            origen.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(origen)
            origen.IImporteDebidoDN.Importe = 50
            origen.IImporteDebidoDN.Acreedora = coldeudoras.Item(0).EntidadFiscalGenerica
            origen.IImporteDebidoDN.Deudora = iefacreedora.EntidadFiscalGenerica
            origen.IImporteDebidoDN.FCreación = Now
            origen.IImporteDebidoDN.FEfecto = Now
            colapCompensantes.Add(origen.IImporteDebidoDN)
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(origen)
            '''''''''''''''''''''''

            ' luego debiera haber hasta este punto un saldo a fabor del cliente de 10



            ' primer origen correspondiente a un servicio prestado para el cual es el pago compensado
            origen = New FN.GestionPagos.DN.OrigenIdevBaseDN
            origen.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(origen)
            origen.IImporteDebidoDN.Importe = 30
            origen.IImporteDebidoDN.Acreedora = iefacreedora.EntidadFiscalGenerica
            origen.IImporteDebidoDN.Deudora = coldeudoras.Item(0).EntidadFiscalGenerica
            origen.IImporteDebidoDN.FCreación = Now
            origen.IImporteDebidoDN.FEfecto = Now
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(origen)
            '''''''''''''''''''''''


            ' creacion de pago
            Dim pago As FN.GestionPagos.DN.PagoDN
            pago = New FN.GestionPagos.DN.PagoDN

            pago.ColApunteImpDCompensantes.AddRangeObject(colapCompensantes)

            pago.ApunteImpDOrigen = origen.IImporteDebidoDN
            pago.Importe = 20
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
            gi.Guardar(pago)




            tr1.Confirmar()

        End Using












    End Function




    Public Function CrearColPagos(ByVal conex As String, ByVal iteraciones As Int64) As FN.GestionPagos.DN.ColPagoDN



        Using tr As New Transaccion


            CrearColPagos = New FN.GestionPagos.DN.ColPagoDN

            ' crear unos pagos de pruebas con sus origenes
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN



            ' recuperar empresas

            Dim bgln As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

            Dim coldeudoras As New ColIEntidadFiscalDN
            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()


            coldeudoras.AddRangeObject(bgln.RecuperarLista(GetType(FN.Empresas.DN.EmpresaFiscalDN)))

            Dim iefacreedora As IEntidadFiscalDN

            bgln = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            iefacreedora = bgln.RecuperarListaCondicional(GetType(FN.Empresas.DN.EmpresaFiscalDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlEmpresaFiscalDN", "Cif_Codigo", "B83204586")).Item(0)

            coldeudoras.EliminarEntidadDNxGUID(iefacreedora.GUID)


            For a As Int64 = 1 To iteraciones
                System.Diagnostics.Debug.WriteLine(a)

                Using tr1 As New Transaccion(True)


                    ' crear el origen debido
                    Dim origen As FN.GestionPagos.DN.OrigenIdevBaseDN
                    origen = CrearOrigenImportedebido(iefacreedora, coldeudoras)
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                    gi.Guardar(origen)



                    ''''' creacion de pago
                    Dim pago As FN.GestionPagos.DN.PagoDN
                    pago = New FN.GestionPagos.DN.PagoDN
                    pago.ApunteImpDOrigen = origen.IImporteDebidoDN
                    pago.Importe = origen.IImporteDebidoDN.Importe
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
                    gi.Guardar(pago)

                    CrearColPagos.Add(pago)


                    tr1.Confirmar()

                End Using

            Next


            tr.Confirmar()




        End Using


    End Function



    Public Function LiquidarPagos(ByVal colpagos As FN.GestionPagos.DN.ColPagoDN) As FN.GestionPagos.DN.ColLiquidacionPagoDN



        Using tr As New Transaccion

            LiquidarPagos = New FN.GestionPagos.DN.ColLiquidacionPagoDN

            For Each pago As FN.GestionPagos.DN.PagoDN In colpagos


                '' liquidar el pago
                Dim col As FN.GestionPagos.DN.ColLiquidacionPagoDN
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
                col = ml.LiquidarPago(pago)

                LiquidarPagos.AddRange(col)


            Next


            tr.Confirmar()

        End Using




    End Function


    Public Function CompesarOAnularPagos(ByVal colpagos As FN.GestionPagos.DN.ColPagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As FN.GestionPagos.DN.ColPagoDN



        Using tr As New Transaccion

            CompesarOAnularPagos = New FN.GestionPagos.DN.ColPagoDN
            colLiqPago = New FN.GestionPagos.DN.ColLiquidacionPagoDN

            For Each pago As FN.GestionPagos.DN.PagoDN In colpagos
                Dim colliq As New FN.GestionPagos.DN.ColLiquidacionPagoDN
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
                If ml.AnularOCompensarPago(pago, colliq) = FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.PagoAnulado Then
                    'anulado
                    CompesarOAnularPagos.Add(pago)
                Else
                    ' compensado

                End If

                colLiqPago.AddRange(colliq)
            Next


            tr.Confirmar()

        End Using




    End Function

    Public Function CompesarPagos(ByVal colpagos As FN.GestionPagos.DN.ColPagoDN, ByRef colLiqPago As FN.GestionPagos.DN.ColLiquidacionPagoDN) As FN.GestionPagos.DN.ColPagoDN



        Using tr As New Transaccion

            CompesarPagos = New FN.GestionPagos.DN.ColPagoDN
            colLiqPago = New FN.GestionPagos.DN.ColLiquidacionPagoDN

            For Each pago As FN.GestionPagos.DN.PagoDN In colpagos
                Dim colliq As New FN.GestionPagos.DN.ColLiquidacionPagoDN
                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN
                CompesarPagos.Add(ml.CompensarPago(pago, colliq))
                colLiqPago.AddRange(colliq)
            Next


            tr.Confirmar()

        End Using




    End Function


    Public Function AnularCompensarPagosOriginales() As FN.GestionPagos.DN.ColPagoDN

        Dim colPagosCreadosNoEmitidos, colPagosCreadosEmitidos, colPagosLiquidados As FN.GestionPagos.DN.ColPagoDN





        Using tr0 As New Transaccion



            Using tr1 As New Transaccion(True)


                colPagosCreadosNoEmitidos = CrearColPagos("", 4)
                tr1.Confirmar()
            End Using

            Using trG As New Transaccion(True)




                colPagosLiquidados = New FN.GestionPagos.DN.ColPagoDN
                colPagosLiquidados.Add(colPagosCreadosNoEmitidos.EliminarEntidadDN(colPagosCreadosNoEmitidos.Item(0), Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos).Item(0))
                Me.EmitirPagos(colPagosLiquidados)
                Me.EfectuarPagop(colPagosLiquidados)



                colPagosCreadosEmitidos = New FN.GestionPagos.DN.ColPagoDN
                colPagosCreadosEmitidos.Add(colPagosCreadosNoEmitidos.EliminarEntidadDN(colPagosCreadosNoEmitidos.Item(0), Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos).Item(0))
                Me.EmitirPagos(colPagosCreadosEmitidos)
            End Using



            Using tr2 As New Transaccion(True)

                ' liquidamos el primer pago


                LiquidarPagos(colPagosLiquidados)
                tr2.Confirmar()

            End Using





            Using tr3 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagosCreadosNoEmitidos

                    Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                    If Not ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) = FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.PagoAnulado Then
                        Throw New ApplicationException("todos los pagos debian de haberse anulado")
                    End If
                Next
                tr3.Confirmar()
            End Using



            Using tr4 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagosLiquidados

                    Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                    If Not ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) = FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.PagoCompensado Then
                        Throw New ApplicationException("todos los pagos debian de haberse compensado")
                    End If
                Next
                tr4.Confirmar()
            End Using


            Using tr4 As New Transaccion(True)

                Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

                For Each pago As FN.GestionPagos.DN.PagoDN In colPagosCreadosEmitidos

                    Dim colLiq As FN.GestionPagos.DN.ColLiquidacionPagoDN

                    If Not ml.AnularOCompensarPago(pago.CrearPagoCompensador, colLiq) = FN.GestionPagos.LN.OperacionILiquidadorConcretoLN.Ninguna Then
                        Throw New ApplicationException("todos los pagos debian de haberse compensado")
                    End If
                Next
                tr4.Confirmar()
            End Using




            tr0.Confirmar()

        End Using


        Return Nothing


    End Function
    ''' <summary>
    ''' crea 4 pagos
    ''' el primero (efectua e intenta anular --> con resultado error)
    ''' el segundo (efectua + liquida e intenta anular --> con resultado error)
    ''' el tercero  ( anula -->con resultado ok)
    ''' el cuarto queda sin procesar
    ''' </summary>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function AnularPagosOriginales() As FN.GestionPagos.DN.ColPagoDN

        Dim colPagos0, colPagos1 As FN.GestionPagos.DN.ColPagoDN


        Using tr0 As New Transaccion

            Using tr1 As New Transaccion(True)


                colPagos0 = CrearColPagos("", 4)
                tr1.Confirmar()
            End Using



            ' Efectua y liquidamos el primer pago
            colPagos1 = New FN.GestionPagos.DN.ColPagoDN
            colPagos1.Add(colPagos0.Item(0))
            Me.EmitirPagos(colPagos1)
            Me.EfectuarPagop(colPagos1)
            ' LiquidarPagos(colPagos1)

            Using tr2 As New Transaccion(True)
                Try
                    Me.AnularPagos(colPagos1)
                    Throw New ApplicationException("debia haberse dado excepción")
                Catch ex As Exception

                End Try
                tr2.Cancelar()

            End Using




            ' anulacion de los pagos segudo y tercero
            colPagos1 = New FN.GestionPagos.DN.ColPagoDN
            colPagos1.Add(colPagos0.Item(1))
            Me.EmitirPagos(colPagos1)

            Me.EfectuarPagop(colPagos1)
            LiquidarPagos(colPagos1)
            Using tr3 As New Transaccion(True)

                Try
                    Me.AnularPagos(colPagos1)
                    Throw New ApplicationException("debia haberse dado excepción")
                Catch ex As Exception

                End Try
                tr3.Cancelar()
            End Using



            Using tr3 As New Transaccion(True)

                colPagos1 = New FN.GestionPagos.DN.ColPagoDN
                colPagos1.Add(colPagos0.Item(3))
                ' Me.EmitirPagos(colPagos1)

                'Me.EfectuarPagop(colPagos1)
                'LiquidarPagos(colPagos1)

                Me.AnularPagos(colPagos1)

                tr3.Confirmar()
            End Using

            tr0.Confirmar()

        End Using


        Return Nothing


    End Function

    Public Sub AnularPagos(ByVal colpagos As FN.GestionPagos.DN.ColPagoDN)



        Using tr As New Transaccion
            Dim ml As New FN.GestionPagos.LN.MotorLiquidacionLN

            For Each pago As FN.GestionPagos.DN.PagoDN In colpagos
                ml.AnularPago(pago)
            Next


            tr.Confirmar()

        End Using




    End Sub
    Private Sub CrearElRecurso(ByVal connectionstring As String)
        Dim htd As New System.Collections.Generic.Dictionary(Of String, Object)

        If connectionstring Is Nothing OrElse connectionstring = "" Then
            connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
        End If

        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a AMV", "sqls", htd)

        'Asignamos el mapeado de  gestor de instanciación
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposGestionPagos

    End Sub

    Private Sub CrearRolesYusuarios()

        ''''''''''''''''''''''''''''''''''''''
        'Creo los casos de uso de  la aplicacion
        ''''''''''''''''''''''''''''''''''''''

        'recupero todos los metodos de sistema
        Dim rolat As RolDN

        Dim usurios As Framework.Usuarios.LN.RolLN
        usurios = New Framework.Usuarios.LN.RolLN(Nothing, mRecurso)
        rolat = usurios.GeneraRolAutorizacionTotal("Administrador Total")
        Me.GuardarDatos(rolat)
        AsignarOperacionesCasoUsoTotal()
        Me.GuardarDatos(rolat)





    End Sub
    Public Sub CargarDatosBasicos(ByVal crearArbol As Boolean)
        ''''''''''''''''''''''''''''''''''''''
        'Creo los casos de uso de  la aplicacion
        ''''''''''''''''''''''''''''''''''''''


        'CrearRolesYusuarios()

        ''''''''''''''''''''''''''''''''''''''
        ''Creo los datos de tipos
        '''''''''''''''''''''''''''''''''''''''
        'Dim tipoOp As AmvDocumentosDN.TipoOperacionREnFDN
        'tipoOp = New AmvDocumentosDN.TipoOperacionREnFDN
        'Me.InsertarTiposDatos(tipoOp.RecuperarTiposTodos)


        'Dim estadoRENF As AmvDocumentosDN.EstadosRelacionENFicheroDN
        'estadoRENF = New AmvDocumentosDN.EstadosRelacionENFicheroDN
        'Me.InsertarTiposDatos(estadoRENF.RecuperarTiposTodos)


        ''''''''''''''''''''''''''''''''''''''
        ''Creo el arbol vacio
        '''''''''''''''''''''''''''''''''''''''

        'If crearArbol Then



        '    Dim nodo As AmvDocumentosDN.NodoTipoEntNegoioDN

        '    nodo = New AmvDocumentosDN.NodoTipoEntNegoioDN
        '    nodo.Nombre = "Entidades Referidoras"

        '    Dim cabeceraNodo As AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        '    cabeceraNodo = New AmvDocumentosDN.CabeceraNodoTipoEntNegoioDN
        '    cabeceraNodo.NodoTipoEntNegoio = nodo

        '    Me.GuardarDatos(cabeceraNodo)

        'End If


        'Cargar los tipos de vía
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("AVENIDA"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("BARRIO"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("CALLE"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("RONDA"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PASAJE"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("GLORIETA"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("CALLEJÓN"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PÓLIGONO"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("CAMINO"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("COSTANILLA"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("COLONIA"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PLAZA"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PARQUE"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("CARRETERA"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("PASEO"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("TRAVESIA"))
        Me.GuardarDatos(New FN.Localizaciones.DN.TipoViaDN("URBANIZACIÓN"))

        'Me.CargarLocalidades("C:\COMPARTIDO\provinciasypoblaciones.xml")

        Dim x As New FN.Personas.DN.TipoSexo()
        x.Nombre = "sin determinar"
        Me.GuardarDatos(x)

        Dim y As New FN.Personas.DN.TipoSexo()
        y.Nombre = "hombre"
        Me.GuardarDatos(y)

        Dim z As New FN.Personas.DN.TipoSexo()
        z.Nombre = "mujer"
        Me.GuardarDatos(z)


        Dim localidad As Framework.DatosNegocio.IEntidadDN

        Dim calle As FN.Localizaciones.DN.TipoViaDN = Nothing

        Dim objLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
        Dim lista As System.Collections.IList

        lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", "Alcobendas y La Moraleja"))
        localidad = lista.Item(0)

        Dim DireccionNoUnica As FN.Localizaciones.DN.DireccionNoUnicaDN

        DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()

        Dim listaTipoVia As System.Collections.IList = objLN.RecuperarLista(GetType(FN.Localizaciones.DN.TipoViaDN))
        For Each tipoVia As FN.Localizaciones.DN.TipoViaDN In listaTipoVia
            If tipoVia.Nombre = "AVENIDA" Then
                calle = tipoVia
                Exit For
            End If
        Next

        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
        DireccionNoUnica.TipoVia = calle
        DireccionNoUnica.Via = "Bruselas"
        DireccionNoUnica.Numero = 38
        DireccionNoUnica.CodPostal = 28108
        DireccionNoUnica.Localidad = localidad

        Me.GuardarDatos(DireccionNoUnica)


        '''''''''''''''''''''''''''''
        ' Se crean los datos de Empresas relacioandas
        '''''''''' ''''''''''''''''''''''''''
        'Dim empresa As FN.Empresas.DN.EmpresaDN
        'Dim empresaFiscal As FN.Empresas.DN.EmpresaFiscalDN

        'Dim sede As FN.Empresas.DN.SedeEmpresaDN
        'Dim tipoSede As FN.Empresas.DN.TipoSedeDN


        'empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()
        'empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("B83204586")
        'empresaFiscal.RazonSocial = "AMV Hispania S.L."
        'empresaFiscal.DomicilioFiscal = DireccionNoUnica
        'empresaFiscal.NombreComercial = "AMV Hispania"

        'empresa = New FN.Empresas.DN.EmpresaDN
        'empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
        'empresa.TipoEmpresaDN.Nombre = "Correduría de seguros"
        'empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica

        'Me.GuardarDatos(empresaFiscal)
        'Me.GuardarDatos(empresa)

        'sede = New FN.Empresas.DN.SedeEmpresaDN()
        'sede.Nombre = "Alcobendas"
        'tipoSede = New FN.Empresas.DN.TipoSedeDN()
        'tipoSede.Nombre = "Central"
        'sede.TipoSede = tipoSede
        'sede.SedePrincipal = True
        'sede.Empresa = empresa
        'sede.Direccion = DireccionNoUnica
        'Me.GuardarDatos(sede)


    End Sub
    Private Sub CargarLocalidadesCodPostal(ByVal ruta As String)
        Dim str As New IO.StreamReader(ruta, System.Text.Encoding.Default)
        Dim colPa As New FN.Localizaciones.DN.ColPaisDN()
        Dim colPr As New FN.Localizaciones.DN.ColProvinciaDN()

        Do Until str.EndOfStream
            Dim linea As String
            linea = str.ReadLine()

            If Not String.IsNullOrEmpty(linea) Then
                Dim valores() As String = linea.Split(ControlChars.Tab)
                ProcesarRegistro(valores(0), valores(1), valores(2), valores(3), colPa, colPr)
            End If

        Loop
    End Sub

    Private Sub ProcesarRegistro(ByVal localidad As String, ByVal codPostal As String, ByVal provincia As String, ByVal pais As String, ByRef colPaises As FN.Localizaciones.DN.ColPaisDN, ByRef colpProvincias As FN.Localizaciones.DN.ColProvinciaDN)
        Dim objPais As FN.Localizaciones.DN.PaisDN
        Dim objProv As FN.Localizaciones.DN.ProvinciaDN = Nothing
        Dim objLoc As FN.Localizaciones.DN.LocalidadDN = Nothing
        Dim objCP As FN.Localizaciones.DN.CodigoPostalDN = Nothing
        Dim lista As IList
        Dim objLN As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using New CajonHiloLN(mRecurso)
            objLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            'Se recupera el pais
            objPais = colPaises.RecuperarPrimeroXNombre(pais)
            If objPais Is Nothing Then
                objPais = New FN.Localizaciones.DN.PaisDN(pais)
                colPaises.Add(objPais)
            End If

            'se recupera la provincia
            objProv = colpProvincias.RecuperarPrimeroXNombre(provincia)
            If objProv Is Nothing Then
                objProv = New FN.Localizaciones.DN.ProvinciaDN(provincia, objPais)
                colpProvincias.Add(objProv)
            End If

            'se recupera la localidad
            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", localidad))
            If lista IsNot Nothing Then
                For Each loc As FN.Localizaciones.DN.LocalidadDN In lista
                    If loc.Provincia.GUID = objProv.GUID Then
                        objLoc = loc
                    End If
                Next
            End If
            If objLoc Is Nothing Then
                objLoc = New FN.Localizaciones.DN.LocalidadDN(localidad, objProv)
            End If

            'se recupera el código postal
            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.CodigoPostalDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlCodigoPostalDN", "Nombre", codPostal))
            If lista IsNot Nothing AndAlso lista.Count = 1 Then
                objCP = lista.Item(0)
            Else
                objCP = New FN.Localizaciones.DN.CodigoPostalDN(codPostal)
            End If

            If objLoc.ColCodigoPostal Is Nothing Then
                objLoc.ColCodigoPostal = New FN.Localizaciones.DN.ColCodigoPostalDN()
            End If

            If Not objLoc.ColCodigoPostal.Contiene(objCP, CoincidenciaBusquedaEntidadDN.Todos) Then
                objLoc.ColCodigoPostal.Add(objCP)
            End If

            Me.GuardarDatos(objLoc)

        End Using

    End Sub


    Public Sub crearElEntornoB()



        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.VaciarCacheTablasGeneradasParaTipos()


        Dim gbd As FN.GestionPagos.AD.GestionPagosGBD
        gbd = New FN.GestionPagos.AD.GestionPagosGBD(mRecurso)


        gbd.EliminarVistas()
        gbd.EliminarRelaciones()
        gbd.EliminarTablas()
        gbd.CrearTablas()
        gbd.CrearVistas()


        CargarDatos()








    End Sub



    Public Sub CargarDatos()

        PublicarGrafoGestionTalones()
        PublicarFachada()


        'CargarLocalidadesCodPostal("D:\Signum\Signum\Proyectos\AMV\SolucionAMV\FicherosPrueba\LocalidadesCodPostalProvinciaPais.txt")
        'Ejecutar paquete carga localidades

        Dim integrationServices As New Application()
        Dim miPaquete As Package
        Dim rutaPaquete As String = "D:\Signum\FrameworkNegocio\LocalizacionesMod\LocalizacionesAD\CargaDatos\Package.dtsx"

        If System.IO.File.Exists(rutaPaquete) Then
            miPaquete = integrationServices.LoadPackage(rutaPaquete, True, Nothing)
        Else
            Throw New ApplicationException("Localización inválida del fichero: " & rutaPaquete)
        End If

        miPaquete.Execute()


        CargarDatosBasicos(True)

        CargarDatosPruebas(Nothing)

        ' crear la entidad emisora de polizas

        ' crear el mapeado origen --- liquidador concreto



    End Sub


    Private Sub CrearLiquidadoresConcretos()
        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

        Dim map As FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN '= New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN

        'map.VCOrigenImpdev = New Framework.TiposYReflexion.DN.VinculoClaseDN(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN))
        'map.VCLiquidadorConcreto = New Framework.TiposYReflexion.DN.VinculoClaseDN(GetType(LiquidadorConcretoPruebaLN)) ' ojo esto habria que cambiarlo porque se desconocen cuales son las liquidaciones para un paago manual

        map = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
        map.VCOrigenImpdev = RecuperarVinculoClase(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN))
        map.VCLiquidadorConcreto = RecuperarVinculoClase(GetType(LiquidadorConcretoPruebaLN)) ' ojo esto habria que cambiarlo porque se desconocen cuales son las liquidaciones para un paago manual
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(map)

        map = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
        map.VCOrigenImpdev = RecuperarVinculoClase(GetType(FN.GestionPagos.DN.AgrupApunteImpDDN))
        map.VCLiquidadorConcreto = RecuperarVinculoClase(GetType(LiquidadorConcretoPruebaLN)) ' ojo esto habria que cambiarlo porque se desconocen cuales son las liquidaciones para un paago manual
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(map)

        map = New FN.GestionPagos.DN.LiquidadorConcretoOrigenIDMapDN
        map.VCOrigenImpdev = RecuperarVinculoClase(GetType(FN.GestionPagos.DN.LiquidacionPagoDN))
        map.VCLiquidadorConcreto = RecuperarVinculoClase(GetType(LiquidadorConcretoPruebaLN)) ' ojo esto habria que cambiarlo porque se desconocen cuales son las liquidaciones para un paago manual
        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, mRecurso)
        gi.Guardar(map)
    End Sub

    Public Function CrearOrigenImportedebido(ByVal acreedora As IEntidadFiscalDN, ByVal coldeudoras As ColIEntidadFiscalDN) As FN.GestionPagos.DN.IOrigenIImporteDebidoDN
        Dim origen As FN.GestionPagos.DN.OrigenIdevBaseDN
        origen = New FN.GestionPagos.DN.OrigenIdevBaseDN

        Dim aleatorio As New Random


        origen.IImporteDebidoDN = New FN.GestionPagos.DN.ApunteImpDDN(origen)
        origen.IImporteDebidoDN.Importe = 100
        origen.IImporteDebidoDN.Acreedora = acreedora.EntidadFiscalGenerica
        origen.IImporteDebidoDN.Deudora = coldeudoras.Item(aleatorio.Next(0, coldeudoras.Count - 1)).EntidadFiscalGenerica

        origen.IImporteDebidoDN.FCreación = Now
        origen.IImporteDebidoDN.FEfecto = Now.AddDays(5)
        Return origen

    End Function

    Public Sub PublicarFachada()
        ' Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("GDocEntrantesFS", Me.mrecurso)
        Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("UsuariosFS", mRecurso)
        '  Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("FicherosFS", mrecurso)
        ' Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("MotorBusquedaFS", mrecurso)
        Framework.FachadaLogica.GestorFachadaFL.PublicarFachada("ProcesosFS", mRecurso)
    End Sub

    Private Sub CargarDatosPruebas(ByRef colief As FN.Localizaciones.DN.ColIEntidadFiscalDN)

        Using New CajonHiloLN(mRecurso)
            Dim localidad1, localidad As Framework.DatosNegocio.IEntidadDN

            Dim calle As FN.Localizaciones.DN.TipoViaDN = Nothing

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            Dim objLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim lista As IList

            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", "Madrid"))
            localidad = lista.Item(0)

            lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", "Pozuelo de Alarcón"))
            localidad1 = lista.Item(0)

            Dim DireccionNoUnica, DireccionNoUnica2, DireccionNoUnica3 As FN.Localizaciones.DN.DireccionNoUnicaDN
            Dim coldir As New List(Of FN.Localizaciones.DN.DireccionNoUnicaDN)

            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()

            Dim listaTipoVia As IList = objLN.RecuperarLista(GetType(FN.Localizaciones.DN.TipoViaDN))
            For Each tipoVia As FN.Localizaciones.DN.TipoViaDN In listaTipoVia
                If tipoVia.Nombre = "CALLE" Then
                    calle = tipoVia
                    Exit For
                End If
            Next

            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "cornejero"
            DireccionNoUnica.Numero = 22
            DireccionNoUnica.CodPostal = 28220
            DireccionNoUnica.Localidad = localidad1

            Me.GuardarDatos(DireccionNoUnica)
            DireccionNoUnica2 = DireccionNoUnica
            coldir.Add(DireccionNoUnica)


            ' localidad = Me.GuardarDatos(New FN.Localizaciones.DN.LocalidadDN("Moratalz", entidad))
            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "Pinacle"
            DireccionNoUnica.Numero = 98
            DireccionNoUnica.CodPostal = 28010
            DireccionNoUnica.Localidad = localidad

            Me.GuardarDatos(DireccionNoUnica)
            DireccionNoUnica3 = DireccionNoUnica
            coldir.Add(DireccionNoUnica)



            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "pimpollo rojo"
            DireccionNoUnica.Numero = 33
            DireccionNoUnica.CodPostal = 28011
            DireccionNoUnica.Localidad = localidad

            Me.GuardarDatos(DireccionNoUnica)
            coldir.Add(DireccionNoUnica)

            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "pimpollo verde"
            DireccionNoUnica.Numero = "35"
            DireccionNoUnica.CodPostal = "28012"
            DireccionNoUnica.Localidad = localidad

            Me.GuardarDatos(DireccionNoUnica)
            coldir.Add(DireccionNoUnica)



            DireccionNoUnica = New FN.Localizaciones.DN.DireccionNoUnicaDN()
            DireccionNoUnica.TipoVia = calle
            DireccionNoUnica.Via = "pimpollo colorado"
            DireccionNoUnica.Numero = "36"
            DireccionNoUnica.CodPostal = "28013"
            DireccionNoUnica.Localidad = localidad

            Me.GuardarDatos(DireccionNoUnica)
            coldir.Add(DireccionNoUnica)





            '''''''''''''''''''''''''''''
            ' Se crean los datos de Empresas relacioandas
            '''''''''' ''''''''''''''''''''''''''
            colief = New FN.Localizaciones.DN.ColIEntidadFiscalDN

            Dim empresa As FN.Empresas.DN.EmpresaDN
            Dim empresaFiscal As FN.Empresas.DN.EmpresaFiscalDN

            Dim sede As FN.Empresas.DN.SedeEmpresaDN
            Dim tipoSede As FN.Empresas.DN.TipoSedeDN

            empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()
            empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("A81948077")
            empresaFiscal.RazonSocial = "Ende yo caliente SA"
            empresaFiscal.DomicilioFiscal = DireccionNoUnica2
            empresaFiscal.NombreComercial = "Endesa"

            empresa = New FN.Empresas.DN.EmpresaDN
            empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
            empresa.TipoEmpresaDN.Nombre = "Normal"
            empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica

            Me.GuardarDatos(empresa)
            colief.Add(empresaFiscal)

            sede = New FN.Empresas.DN.SedeEmpresaDN()
            sede.Nombre = "Moratalaz"
            tipoSede = New FN.Empresas.DN.TipoSedeDN()
            tipoSede.Nombre = "Central"
            sede.TipoSede = tipoSede
            sede.SedePrincipal = True
            sede.Empresa = empresa
            sede.Direccion = DireccionNoUnica
            Me.GuardarDatos(sede)



            Dim cuentab As FN.Financiero.DN.CuentaBancariaDN

            cuentab = New FN.Financiero.DN.CuentaBancariaDN
            cuentab.Nombre = "cuenta de pagos"
            cuentab.CCC = New FN.Financiero.DN.CCCDN
            cuentab.CCC.Codigo = "00120345030000067890"
            cuentab.Titulares.Add(empresa.EntidadFiscal)
            Me.GuardarDatos(cuentab)


            Dim persona As FN.Personas.DN.PersonaDN
            persona = New FN.Personas.DN.PersonaDN

            persona.Nombre = "Pablo" '
            persona.Apellido = "Ramirez Ocaña"
            persona.NIF = New FN.Localizaciones.DN.NifDN("45274941Q")

            Dim ipf As FN.Personas.DN.PersonaFiscalDN
            ipf = New FN.Personas.DN.PersonaFiscalDN
            ipf.DomicilioFiscal = DireccionNoUnica3
            ipf.Persona = persona
            Me.GuardarDatos(ipf)
            colief.Add(ipf)




            empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()
            empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("A47053210")
            empresaFiscal.RazonSocial = "Rio tinto explosivos S.A."
            empresaFiscal.DomicilioFiscal = DireccionNoUnica2
            empresaFiscal.NombreComercial = "Explotame Explo"

            empresa = New FN.Empresas.DN.EmpresaDN
            empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
            empresa.TipoEmpresaDN.Nombre = "tipo 2"
            empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica

            Me.GuardarDatos(empresa)
            colief.Add(empresaFiscal)


            empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()
            empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("B82785809")
            empresaFiscal.RazonSocial = "Pavos y pollos S.A."
            empresaFiscal.DomicilioFiscal = coldir(3)
            empresaFiscal.NombreComercial = "Explotame"

            empresa = New FN.Empresas.DN.EmpresaDN
            empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
            empresa.TipoEmpresaDN.Nombre = "tipo 2"
            empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica

            Me.GuardarDatos(empresa)
            colief.Add(empresaFiscal)


            empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()
            empresaFiscal.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN("B83204586")
            empresaFiscal.RazonSocial = "AMV Hispania SL"
            empresaFiscal.DomicilioFiscal = coldir(4)
            empresaFiscal.NombreComercial = "AMV Hispania"

            empresa = New FN.Empresas.DN.EmpresaDN
            empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
            empresa.TipoEmpresaDN.Nombre = "tipo 2"
            empresa.EntidadFiscal = empresaFiscal.EntidadFiscalGenerica

            Me.GuardarDatos(empresa)
            colief.Add(empresaFiscal)

            sede = New FN.Empresas.DN.SedeEmpresaDN()
            sede.Nombre = "Alcobendas"
            tipoSede = New FN.Empresas.DN.TipoSedeDN()
            tipoSede.Nombre = "Sede España"
            sede.TipoSede = tipoSede
            sede.SedePrincipal = True
            sede.Empresa = empresa
            sede.Direccion = DireccionNoUnica
            Me.GuardarDatos(sede)


        End Using



    End Sub



    'Private Sub CargarDatosX()

    '    Using New CajonHiloLN(mRecurso)
    '        Dim localidad As Framework.DatosNegocio.IEntidadDN

    '        Dim calle As TipoViaDN = Nothing

    '        Dim objLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
    '        Dim lista As IList
    '        lista = objLN.RecuperarListaCondicional(GetType(LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("tlLocalidadDN", "Nombre", "ALCOBENDAS"))

    '        If lista Is Nothing OrElse lista.Count = 0 Then
    '            Throw New ApplicationException("no se pudo recuperar la localidad para AMV")
    '        Else
    '            localidad = lista.Item(0)
    '        End If

    '        Dim DireccionNoUnica As DireccionNoUnicaDN

    '        DireccionNoUnica = New DireccionNoUnicaDN()

    '        Dim listaTipoVia As IList = objLN.RecuperarLista(GetType(TipoViaDN))
    '        For Each tipoVia As TipoViaDN In listaTipoVia
    '            If tipoVia.Nombre = "AVENIDA" Then
    '                calle = tipoVia
    '                Exit For
    '            End If
    '        Next

    '        DireccionNoUnica.TipoVia = calle
    '        DireccionNoUnica.Via = "Bruselas"
    '        DireccionNoUnica.Numero = 38
    '        DireccionNoUnica.CodPostal = 28108
    '        DireccionNoUnica.Localidad = localidad

    '        '''''''''''''''''''''''''''''''''''''
    '        ' Se crean los datos de Empresa
    '        '''''''''' ''''''''''''''''''''''''''

    '        Dim empresa As FN.Empresas.DN.EmpresaDN
    '        Dim sede As FN.Empresas.DN.SedeEmpresaDN
    '        Dim tipoSede As FN.Empresas.DN.TipoSedeDN
    '        Dim empresaFiscal As FN.Empresas.DN.EmpresaFiscalDN
    '        Dim cuentab As FN.Financiero.DN.CuentaBancariaDN

    '        empresa = New FN.Empresas.DN.EmpresaDN()
    '        empresaFiscal = New FN.Empresas.DN.EmpresaFiscalDN()

    '        empresaFiscal.IdentificacionFiscal = New CifDN("B83204586")
    '        empresaFiscal.RazonSocial = "AMV Hispania S.L.U."
    '        empresaFiscal.DomicilioFiscal = DireccionNoUnica
    '        empresaFiscal.NombreComercial = "AMV"

    '        empresa.EntidadFiscal = empresaFiscal
    '        empresa.TipoEmpresaDN = New FN.Empresas.DN.TipoEmpresaDN()
    '        empresa.TipoEmpresaDN.Nombre = "Correduría de seguros"

    '        Me.GuardarDatos(empresa)
    '        Me.GuardarDatos(empresaFiscal)


    '        sede = New FN.Empresas.DN.SedeEmpresaDN()
    '        sede.Nombre = "Alcobendas"
    '        tipoSede = New FN.Empresas.DN.TipoSedeDN()
    '        tipoSede.Nombre = "Sede España"
    '        sede.TipoSede = tipoSede
    '        sede.SedePrincipal = True
    '        sede.Empresa = empresa
    '        sede.Direccion = DireccionNoUnica
    '        Me.GuardarDatos(sede)


    '        cuentab = New FN.Financiero.DN.CuentaBancariaDN()
    '        cuentab.Nombre = "Cuenta de AMV"
    '        cuentab.CCC = New FN.Financiero.DN.CCCDN
    '        cuentab.CCC.Codigo = "21002287750200123033"
    '        cuentab.Titulares.Add(empresa.EntidadFiscal)
    '        Me.GuardarDatos(cuentab)

    '        'Carga de los casos de uso
    '        ' AsignarOperacionesCasoUso()

    '        'Carga de loa datos de los departamentos y puestos
    '        ' CrearDepartamentosPuestos(empresa)

    '        'Datos del límite de los pagos
    '        'Dim limitepago As New FN.GestionPagos.DN.LimitePagoDN()
    '        'limitepago.LimiteFirmaAutomatica = 200
    '        'limitepago.LimiteAviso = 400
    '        'GuardarDatos(limitepago)

    '    End Using

    'End Sub

    Private Sub CrearDepartamentosPuestos(ByVal empresa As FN.Empresas.DN.EmpresaDN)
        Using New CajonHiloLN(mRecurso)
            Dim cb As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim lista As IList
            Dim colRol As Framework.Usuarios.DN.ColRolDN
            Dim colRolAux As Framework.Usuarios.DN.ColRolDN


            colRol = New Framework.Usuarios.DN.ColRolDN
            lista = cb.RecuperarLista(GetType(Framework.Usuarios.DN.RolDN))
            For Each rol As Framework.Usuarios.DN.RolDN In lista
                colRol.Add(rol)
            Next

            Dim departamento As FN.Empresas.DN.DepartamentoDN
            Dim departamentoTarea As FN.Empresas.DN.DepartamentoNTareaNDN
            Dim puesto As FN.Empresas.DN.PuestoDN


            'Departamento Contabilidad
            departamento = New FN.Empresas.DN.DepartamentoDN()
            departamento.Nombre = "Contabilidad"
            departamento.Empresa = empresa
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador contabilidad"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable contabilidad"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Gestor impresión talones"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Gestor transferencias"))
            departamentoTarea = New FN.Empresas.DN.DepartamentoNTareaNDN()
            departamentoTarea.Departamento = departamento
            departamentoTarea.ColRoles = colRolAux

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador contabilidad"))
            puesto.Nombre = "Operador contabilidad"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable contabilidad"))
            puesto.Nombre = "Responsable contabilidad"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Gestor impresión talones"))
            puesto.Nombre = "Gestor impresión talones"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Gestor transferencias"))
            puesto.Nombre = "Gestor transferencias"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            'Departamento Dirección
            departamento = New FN.Empresas.DN.DepartamentoDN()
            departamento.Nombre = "Dirección"
            departamento.Empresa = empresa
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Dirección empresa"))
            departamentoTarea = New FN.Empresas.DN.DepartamentoNTareaNDN()
            departamentoTarea.Departamento = departamento
            departamentoTarea.ColRoles = colRolAux

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            'Se añade el permiso para el límite de firma al rol de dirección
            Dim rolDireccion As RolDN = colRol.RecuperarPrimeroXNombre("Dirección empresa")
            Dim permisoD As New PermisoDN()
            permisoD.TipoPermiso = New TipoPermisoDN("LimiteFirmaPago")
            permisoD.EsRef = False
            permisoD.DatoVal = "1000"
            permisoD.Nombre = "Rol Dirección empresa"
            rolDireccion.ColPermisos = New ColPermisoDN()
            rolDireccion.ColPermisos.Add(permisoD)
            colRolAux.Add(rolDireccion)
            puesto.Nombre = "Dirección"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)



            colRol = New Framework.Usuarios.DN.ColRolDN
            lista = cb.RecuperarLista(GetType(Framework.Usuarios.DN.RolDN))
            For Each rol As Framework.Usuarios.DN.RolDN In lista
                colRol.Add(rol)
            Next


            'Departamento Siniestros
            departamento = New FN.Empresas.DN.DepartamentoDN()
            departamento.Nombre = "Siniestros"
            departamento.Empresa = empresa

            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador Siniestros"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable Siniestros"))


            departamentoTarea = New FN.Empresas.DN.DepartamentoNTareaNDN()
            departamentoTarea.Departamento = departamento
            departamentoTarea.ColRoles = New ColRolDN()
            ' departamentoTarea.ColRoles.AddRange(colRolAux)
            departamentoTarea.ColRoles.Add(colRolAux(0))
            departamentoTarea.ColRoles.Add(colRolAux(1))
            Me.GuardarDatos(departamentoTarea)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador Siniestros"))
            puesto.Nombre = "Operador Siniestros"

            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = New ColRolDN()
            puesto.ColRoles.AddRange(colRolAux)
            Me.GuardarDatos(puesto)


            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable Siniestros"))
            puesto.Nombre = "Responsable Siniestros"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            'Departamento Gestión
            departamento = New FN.Empresas.DN.DepartamentoDN()
            departamento.Nombre = "Gestión"
            departamento.Empresa = empresa
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador Gestión"))
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable Gestión"))
            departamentoTarea = New FN.Empresas.DN.DepartamentoNTareaNDN()
            departamentoTarea.Departamento = departamento
            departamentoTarea.ColRoles = colRolAux

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Operador Gestión"))
            puesto.Nombre = "Operador Gestión"

            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

            puesto = New FN.Empresas.DN.PuestoDN()
            colRolAux = New Framework.Usuarios.DN.ColRolDN()
            colRolAux.Add(colRol.RecuperarPrimeroXNombre("Responsable Gestión"))
            puesto.Nombre = "Responsable Gestión"
            puesto.DepartamentoNTareaN = departamentoTarea
            puesto.ColRoles = colRolAux
            Me.GuardarDatos(puesto)

        End Using

    End Sub



    Private Sub AsignarOperacionesCasoUso()

        Using New CajonHiloLN(mRecurso)
            Dim cb As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()

            Dim colOp As New Framework.Procesos.ProcesosDN.ColOperacionDN()
            Dim colOpAux As Framework.Procesos.ProcesosDN.ColOperacionDN
            Dim colCU As New ColCasosUsoDN()
            Dim cu As CasosUsoDN

            Dim lista As IList

            lista = cb.RecuperarLista(GetType(Framework.Procesos.ProcesosDN.OperacionDN))
            For Each operacion As Framework.Procesos.ProcesosDN.OperacionDN In lista
                colOp.Add(operacion)
            Next

            lista = cb.RecuperarLista(GetType(Framework.Usuarios.DN.CasosUsoDN))
            For Each casoUso As Framework.Usuarios.DN.CasosUsoDN In lista
                colCU.Add(casoUso)
            Next

            'Alta negocio
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Alta Negocio"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Gestión Talones"))
            cu = colCU.RecuperarPrimeroXNombre("Alta negocio")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Alta contabilidad
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Alta Contabilidad"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Gestión Talones"))
            cu = colCU.RecuperarPrimeroXNombre("Alta contabilidad")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Operar pago negocio
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Guardar Negocio"))
            cu = colCU.RecuperarPrimeroXNombre("Operar pago negocio")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Validar pago negocio
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Validación Negocio"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular N"))
            cu = colCU.RecuperarPrimeroXNombre("Validar pago negocio")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Operar pago contabilidad
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Guardar Contabilidad"))
            cu = colCU.RecuperarPrimeroXNombre("Operar pago contabilidad")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Validar pago contabilidad
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Validación Contabilidad"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Rechazar N"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular C"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Firma Dirección Automatica"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Firma Automatica-Manual"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Pago Talon"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Pago Transferencia"))
            cu = colCU.RecuperarPrimeroXNombre("Validar pago contabilidad")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Verificar cobro
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Verificación Cobro Contabilidad"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anulación con Alta Contabilidad"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular Pago"))
            cu = colCU.RecuperarPrimeroXNombre("Verificar cobro")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Firmar pago
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Firma Dirección"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Rechazar N"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Rechazar C"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular D"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Pago Talon"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Decisión Pago Transferencia"))
            cu = colCU.RecuperarPrimeroXNombre("Firmar pago")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Regenerar fichero transferencias
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Regenerar FT"))
            cu = colCU.RecuperarPrimeroXNombre("Regenerar fichero transferencias")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Reactivar talón anulado impresión
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Reactivacion Anulación Impresión Dirección"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anular D"))
            cu = colCU.RecuperarPrimeroXNombre("Reactivar talón anulado impresión")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Imprimir talón
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Impresión"))
            cu = colCU.RecuperarPrimeroXNombre("Imprimir talón")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Verificar impresión talón
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Validación Impresión"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Anulación Impresión"))
            cu = colCU.RecuperarPrimeroXNombre("Verificar impresión talón")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

            'Generar fichero transferencias
            colOpAux = New Framework.Procesos.ProcesosDN.ColOperacionDN()
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Adjuntar Fichero Transferencia"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Gestión Ficheros Transferencias"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Alta FT"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Guardar FT"))
            colOpAux.Add(colOp.RecuperarPrimeroXNombre("Generar FT"))
            cu = colCU.RecuperarPrimeroXNombre("Generar fichero transferencias")
            cu.ColOperaciones = colOpAux
            GuardarDatos(cu)

        End Using

    End Sub



    Private Function RecuperarVinculoMetodo(ByVal nombreMetodo As String, ByVal tipo As System.Type) As VinculoMetodoDN
        Dim vm As VinculoMetodoDN

        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            vm = New VinculoMetodoDN(nombreMetodo, New VinculoClaseDN(tipo))

            Return tyrLN.CrearVinculoMetodo(vm.RecuperarMethodInfo())
        End Using

    End Function

    Private Function RecuperarVinculoClase(ByVal tipo As System.Type) As VinculoClaseDN
        Using New CajonHiloLN(mRecurso)
            Dim tyrLN As New Framework.TiposYReflexion.LN.TiposYReflexionLN()
            Return tyrLN.CrearVinculoClase(tipo)
        End Using

    End Function


    Public Sub PublicarGrafoGestionTalones()
        ' flujo de talones

        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 1º 
        ' dn o dns a las cuales se vincula el flujo
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

        Dim vc1DN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(FN.GestionPagos.DN.PagoDN))
        Dim ColVc As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        ColVc.Add(vc1DN)

        'FIN ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' 2º 
        '  creacion de las operaciones
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN

        ' operacion que engloba todo el flujo
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestión Talones", ColVc, "element_into.ico", True)))

        ' negocio
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta Negocio", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Guardar Negocio", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Validación Negocio", ColVc, "element_into.ico", True)))

        '' operacion de pueba

        'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion Compuesta", ColVc)))
        'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion SumarUno", ColVc)))
        'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion RestarUno", ColVc)))
        'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion SumarCinco", ColVc)))
        'colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Operacion RestarCinco", ColVc)))

        '' FIN operacion de pueba


        'contabilidad
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta Contabilidad", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Guardar Contabilidad", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Validación Contabilidad", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Verificación Cobro Contabilidad", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anulación con Alta Contabilidad", ColVc, "element_into.ico", True)))

        ' direccion
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Firma Dirección", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Firma Dirección Automatica", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Decisión Firma Automatica-Manual", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Reactivacion Anulación Impresión Dirección", ColVc, "element_into.ico", True)))

        ' impresion
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Decisión Pago Talon", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Impresión", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Validación Impresión", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anulación Impresión", ColVc, "element_into.ico", True)))

        'Adjuntar a fichero de transferencias
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Decisión Pago Transferencia", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Adjuntar Fichero Transferencia", ColVc, "element_into.ico", True)))

        'Operaciones de rechazo y anulación del pago
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Rechazar N", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Rechazar C", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular N", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular C", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular D", ColVc, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Anular Pago", ColVc, "element_into.ico", True)))

        ' operaciones genericas fuera de proceso
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("GuardarDNGenerico", ColVc, "element_into.ico", True)))


        'Operaciones de los ficheros de transferencias
        Dim vcFTDN As Framework.TiposYReflexion.DN.VinculoClaseDN = RecuperarVinculoClase(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN))
        Dim ColVcFT As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
        ColVcFT.Add(vcFTDN)

        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Gestión Ficheros Transferencias", ColVcFT, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Alta FT", ColVcFT, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Guardar FT", ColVcFT, "Guardar.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Generar FT", ColVcFT, "element_into.ico", True)))
        colop.Add(GuardarDatos(ProcesosHelperLN.AltaOperacion("Regenerar FT", ColVcFT, "element_into.ico", True)))



        ''''''''''''''''''''''''''''''''''''''''''''
        ' 3º
        ' creacion de las Transiciones
        ''''''''''''''''''''''''''''''''''''''''''''
        'Dim tran As Framework.Procesos.ProcesosDN.TransicionDN

        Dim colVM As New ColVinculoMetodoDN()

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestión Talones", "Alta Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestión Talones", "Alta Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Validación Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Anular N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))


        '' prueba subordiandas ''''''''''''''''''''''''''''''''''''''

        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Negocio", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, False))

        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Operacion SumarUno", Framework.Procesos.ProcesosDN.TipoTransicionDN.Subordianda, False, True))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Operacion RestarUno", Framework.Procesos.ProcesosDN.TipoTransicionDN.Subordianda, False, True))

        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion RestarUno", "Operacion RestarCinco", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion SumarUno", "Operacion SumarCinco", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, False))

        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion RestarCinco", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, False))

        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion SumarCinco", "Operacion Compuesta", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, False))

        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Operacion Compuesta", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, False))

        '' fin prueba subordiandas''''''''''''''''''''''''''''''''''''''''''''''''


        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Contabilidad", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Contabilidad", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta Contabilidad", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Negocio", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Negocio", "Validación Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Negocio", "Anular N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar N", "Guardar Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar N", "Validación Negocio", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar N", "Anular N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Negocio", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Negocio", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Negocio", "Rechazar N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Negocio", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Contabilidad", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Contabilidad", "Rechazar N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Contabilidad", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar Contabilidad", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar C", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar C", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Rechazar C", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Contabilidad", "Decisión Firma Automatica-Manual", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Firma Dirección", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Rechazar N", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Rechazar C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Anular D", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Firma Automatica-Manual", "Firma Dirección Automatica", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaFirmaAutomatica", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Firma Dirección", "Decisión Pago Talon", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaImprimirTalon", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Firma Dirección", "Decisión Pago Transferencia", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaGenerarTransferencia", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Firma Dirección Automatica", "Decisión Pago Talon", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaImprimirTalon", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Firma Dirección Automatica", "Decisión Pago Transferencia", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, RecuperarVinculoMetodo("GuardaGenerarTransferencia", GetType(FN.GestionPagos.LN.GuardasFlujoTalonesLN)), False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Pago Talon", "Impresión", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Decisión Pago Transferencia", "Adjuntar Fichero Transferencia", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Impresión", "Validación Impresión", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Impresión", "Anulación Impresión", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación Impresión", "Reactivacion Anulación Impresión Dirección", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación Impresión", "Anular D", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Reactivacion Anulación Impresión Dirección", "Impresión", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Impresión", "Verificación Cobro Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Impresión", "Anulación con Alta Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Validación Impresión", "Anular Pago", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación con Alta Contabilidad", "Guardar Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación con Alta Contabilidad", "Anular C", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anulación con Alta Contabilidad", "Validación Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Adjuntar Fichero Transferencia", "Verificación Cobro Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Adjuntar Fichero Transferencia", "Anulación con Alta Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Adjuntar Fichero Transferencia", "Anular Pago", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Generación Fichero Transferencia", "Regenerar Fichero Transferencia", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Regenerar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Verificación Cobro Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Anulación con Alta Contabilidad", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        'GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Anular Pago", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        '---------------------------------------------------------------------------------------------------

        ' transiciones de fin
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Verificación Cobro Contabilidad", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))

        ' transiciones de fin por anulación
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anular N", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anular C", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anular D", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Anular Pago", "Gestión Talones", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, True, Nothing, False))

        ' FIN flujo de talones 

        ' Flujo de ficheros de transferencias

        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Gestión Ficheros Transferencias", "Alta FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Inicio, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta FT", "Guardar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Alta FT", "Generar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar FT", "Guardar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Guardar FT", "Generar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Generar FT", "Regenerar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))
        GuardarDatos(ProcesosHelperLN.AltaTransicion(colop, "Regenerar FT", "Regenerar FT", Framework.Procesos.ProcesosDN.TipoTransicionDN.Normal, False, Nothing, False))

        ' FIN flujo de ficheros de transferencias



        ' publicar los controladores


        ' Framework.FachadaLogica.GestorFachadaFL.PublicarMetodos("ProcesosFS", Me.mrecurso)

        'Dim opln As New Framework.Procesos.ProcesosLN.OperacionesLN
        'Dim ejc As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN = opln.RecuperarEjecutorCliente("Servidor")


        Dim ejClienteS, ejClienteC As Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        Dim clienteS, clienteC As Framework.Procesos.ProcesosDN.ClientedeFachadaDN

        ' crecion de los clientes del grafo
        clienteS = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN
        clienteS.Nombre = "Servidor"

        clienteC = New Framework.Procesos.ProcesosDN.ClientedeFachadaDN
        clienteC.Nombre = "Cliente1"

        ejClienteS = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        ejClienteS.ClientedeFachada = clienteS

        ejClienteC = New Framework.Procesos.ProcesosDN.EjecutoresDeClienteDN
        ejClienteC.ClientedeFachada = clienteC


        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Gestión Talones", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Negocio", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar Negocio", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Negocio", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar N", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular N", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS))) ' en este caso debiea de enrutarse al metodo que le pone la fecha de anulación

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Contabilidad", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar Contabilidad", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Contabilidad", RecuperarVinculoMetodo("ValidarPagoAsignado", GetType(FN.GestionPagos.LN.PagosLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Verificación Cobro Contabilidad", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anulación con Alta Contabilidad", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar C", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular C", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Firma Dirección", RecuperarVinculoMetodo("FirmarPagoAsignado", GetType(FN.GestionPagos.LN.PagosLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Firma Dirección Automatica", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Firma Automatica-Manual", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Pago Talon", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Pago Transferencia", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivacion Anulación Impresión Dirección", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular D", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Impresión", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Impresión", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anulación Impresión", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular Pago", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Adjuntar Fichero Transferencia", RecuperarVinculoMetodo("AdjuntarPagoFT", GetType(FN.GestionPagos.LN.PagosLN)), ejClienteS)))

        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta FT", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar FT", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Generar FT", RecuperarVinculoMetodo("GuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))
        ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Regenerar FT", RecuperarVinculoMetodo("NoGuardarGenerico", GetType(GestorEjecutoresLN)), ejClienteS)))


        '' pruebas

        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion Compuesta", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarUno", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarUno", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarCinco", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarCinco", "GuardarGenerico", GetType(GestorEjecutoresLN), ejClienteS))

        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion Compuesta", "EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS), ejClienteC))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarUno", "SumarUno", GetType(ClienteAdminLNC.TalonesLNC), ejClienteC))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarUno", "RestarUno", GetType(ClienteAdminLNC.TalonesLNC), ejClienteC))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion RestarCinco", "RestarCinco", GetType(ClienteAdminLNC.TalonesLNC), ejClienteC))
        'ejClienteS.ColVcEjecutorDeVerboEnCliente.Add(ProcesosHelperLN.VinculacionVerbo(colop, "Operacion SumarCinco", "SumarCinco", GetType(ClienteAdminLNC.TalonesLNC), ejClienteC))

        '' finc pruebas

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Gestión Talones", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Negocio", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar Negocio", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Negocio", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar N", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))
        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular N", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta Contabilidad", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar Contabilidad", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Contabilidad", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Verificación Cobro Contabilidad", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anulación con Alta Contabilidad", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Rechazar C", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))
        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular C", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Firma Dirección", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Firma Dirección Automatica", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Firma Automatica-Manual", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Pago Talon", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Decisión Pago Transferencia", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Reactivacion Anulación Impresión Dirección", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular D", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))

        '  ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Impresión", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Impresión", RecuperarVinculoMetodo("ImprimirUnico", GetType(FN.GestionPagos.IU.AdaptadorImpresion)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Validación Impresión", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anulación Impresión", RecuperarVinculoMetodo("EjecutarOperacion", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Anular Pago", RecuperarVinculoMetodo("AdjuntarNotaaPago", GetType(FN.GestionPagos.IU.NotificacionesPagoCtrl)), ejClienteC)))

        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Adjuntar Fichero Transferencia", RecuperarVinculoMetodo("AdjuntarPagoUnicoFT", GetType(FN.GestionPagos.IU.AdaptadorFicherosTransferencias)), ejClienteC)))

        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Alta FT", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Guardar FT", RecuperarVinculoMetodo("EjecutarOperacionModificarObjeto", GetType(Framework.Procesos.ProcesosAS.OperacionesAS)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Generar FT", RecuperarVinculoMetodo("GenerarFicheroTransferencia", GetType(FN.GestionPagos.LNC.PagosLNC)), ejClienteC)))
        ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "Regenerar FT", RecuperarVinculoMetodo("GenerarFicheroTransferencia", GetType(FN.GestionPagos.LNC.PagosLNC)), ejClienteC)))

        'ejClienteC.ColVcEjecutorDeVerboEnCliente.Add(GuardarDatos(ProcesosHelperLN.VinculacionVerbo(colop, "GuardarDNGenerico", RecuperarVinculoMetodo("GuardarDNGenerico", GetType(Framework.AS.MV2AS)), ejClienteC)))

        Me.GuardarDatos(ejClienteC)
        Me.GuardarDatos(ejClienteS)

    End Sub



    Private Sub AsignarOperacionesCasoUsoTotal()
        Dim casosUsoLN As New CasosUsoLN(Nothing, mRecurso)
        Dim colCasosUso As New ColCasosUsoDN()
        Dim casoUsoTotal As CasosUsoDN

        colCasosUso.AddRange(casosUsoLN.RecuperarListaCasosUso())
        casoUsoTotal = colCasosUso.RecuperarPrimeroXNombre("Todos los permisos")

        Using New CajonHiloLN(mRecurso)
            Dim cb As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim col As IList = cb.RecuperarLista(GetType(Framework.Procesos.ProcesosDN.OperacionDN))

            For Each op As Framework.Procesos.ProcesosDN.OperacionDN In col
                casoUsoTotal.ColOperaciones.Add(op)
            Next
            Me.GuardarDatos(casoUsoTotal)
        End Using

    End Sub

    Public Function GuardarDatos(ByVal pEntidad As IEntidadDN) As IEntidadDN




        Using tr As New Transaccion


            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(pEntidad)

            tr.Confirmar()

        End Using





        Return pEntidad

    End Function


#End Region


End Class



Public Class GestorMapPersistenciaCamposGestionPagos
    Inherits GestorMapPersistenciaCamposLN

    'TODO: ALEX por implementar: recuperar el mapeado de instanciacion de la fuente de datos para el tipo a procesar
    Public Overrides Function RecuperarMapPersistenciaCampos(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As InfoDatosMapInstClaseDN = Nothing
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing

        mapinst = RecuperarMapPersistenciaCamposPrivado(pTipo)

        ' ojo esta modificación se debe aplicar siempre si el tipo hereda de una huella es decir en el metodo que lo llamo
        If (TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipo)) Then
            If mapinst Is Nothing Then
                mapinst = New InfoDatosMapInstClaseDN
            End If
            Me.MapearClase("mEntidadReferidaHuella", CampoAtributoDN.SoloGuardarYNoReferido, campodatos, mapinst)
        End If


        If TiposYReflexion.LN.InstanciacionReflexionHelperLN.HeredaDe(pTipo, GetType(Framework.DatosNegocio.EntidadTemporalDN)) Then
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


    Public Overrides Function RecuperarMapPersistenciaCamposPrivado(ByVal pTipo As System.Type) As InfoDatosMapInstClaseDN
        Dim mapinst As New InfoDatosMapInstClaseDN
        Dim campodatos As InfoDatosMapInstCampoDN = Nothing



        ''ZONA: UsuarioDN ________________________________________________________________

        ''Mapeado de UsuarioDN, donde la clase mapea sus interfaces, y solo es para ella.

        If pTipo Is GetType(DatosIdentidadDN) Then

            Me.MapearClase("mHashClave", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

            Return mapinst
        End If

        If pTipo Is GetType(UsuarioDN) Then
            Dim mapinstSub As New InfoDatosMapInstClaseDN
            Dim alentidades As New ArrayList

            'Me.VincularConClase("mHuellaEntidadUserDN", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.HuellaOperadorDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)
            Me.VincularConClase("mEntidadUser", New ElementosDeEnsamblado("EmpresasDN", GetType(FN.Empresas.DN.HuellaCacheEmpleadoYPuestosRDN).FullName), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)

            Return mapinst
        End If

        If pTipo Is GetType(PrincipalDN) Then
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

        ''FINZONA: UsuarioDN ________________________________________________________________

        ''ZONA: Framework.DatosNegocio ________________________________________________________________



        'If (pTipo Is GetType(Framework.Usuarios.DN.AutorizacionRelacionalDN)) Then

        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList

        '    mapSubInst.NombreCompleto = GetType(Framework.Usuarios.DN.PrincipalDN).FullName
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.TipoEntidadOrigenDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.TipoEmpresaDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mColEntidadesRelacionadas"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst


        'End If


        'If pTipo Is GetType(Framework.DatosNegocio.Arboles.NodoBaseDN) Then

        '    Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst

        'End If

        'If pTipo Is GetType(Framework.DatosNegocio.Arboles.ColNodosDN) Then

        '    Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst

        'End If

        'If pTipo Is GetType(DatosNegocio.ArrayListValidable) Then

        '    Me.MapearClase("mValidador", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst

        'End If

        ''FINZONA: Framework.DatosNegocio ________________________________________________________________


        ''ZONA: AMVGDocs_______________________________________________________________________________


        ''If (pTipo.FullName.Contains("Nodo")) Then
        ''    Beep()
        ''End If



        'If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.INodoTDN`1[[DatosNegocioTest.HojaDeNodoDeT")) Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    Return mapinst
        'End If




        'If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.INodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN")) Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    Return mapinst
        'End If



        'If (pTipo.FullName.Contains("Framework.DatosNegocio.Arboles.ColINodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN, AmvDocumentosDN")) Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    Return mapinst
        'End If



        ''Framework.DatosNegocio.Arboles.ColINodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN, AmvDocumentosDN
        ''Framework.DatosNegocio.Arboles.INodoTDN`1[[AmvDocumentosDN.TipoEntNegoioDN
        ''AmvDocumentosDN.NodoTipoEntNegoioDN
        'If (pTipo.FullName.Contains("AmvDocumentosDN.NodoTipoEntNegoioDN")) Then

        '    Dim mapinstSub As New InfoDatosMapInstClaseDN
        '    Dim alentidades As New ArrayList

        '    Dim lista As List(Of ElementosDeEnsamblado)


        '    lista = New List(Of ElementosDeEnsamblado)
        '    lista.Add(New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
        '    VincularConClase("mPadre", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)


        '    'lista = New List(Of ElementosDeEnsamblado)
        '    'lista.Add(New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
        '    'VincularConClase("mHijos", lista, CampoAtributoDN.InterfaceImplementadaPor, Nothing, Nothing, mapinst, Nothing)

        '    VincularConClase("mHijos", New ElementosDeEnsamblado("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"), CampoAtributoDN.InterfaceImplementadaPor, alentidades, campodatos, mapinst, mapinstSub)



        '    'mapinstSub = New InfoDatosMapInstClaseDN
        '    'alentidades = New ArrayList

        '    'alentidades.Add(New VinculoClaseDN("AmvDocumentosDN", "AmvDocumentosDN.NodoTipoEntNegoioDN"))
        '    'mapinstSub.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    'campodatos = New InfoDatosMapInstCampoDN
        '    'campodatos.InfoDatosMapInstClase = mapinst
        '    'campodatos.NombreCampo = "mHijos"
        '    'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    'campodatos.MapSubEntidad = mapinstSub


        '    Me.MapearClase("mValidadorp", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.MapearClase("mValidadorh", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "FN.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN") Then

        '    'Me.MapearClase("mDatos", CampoAtributoDN.PersistenciaContenidaSerializada, campodatos, mapinst)
        '    Me.MapearClase("mDatos", CampoAtributoDN.NoProcesar, campodatos, mapinst)

        '    Return mapinst

        'End If

        'If pTipo.FullName = "Framework.Mensajeria.GestorMensajeriaDN.CausaDN" Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValorCausa"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    Return mapinst
        'End If
        '' Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN()
        'If (pTipo.FullName = "Framework.Mensajeria.GestorMensajeriaDN.IDestinoDN") Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN("GestorMails", "Framework.Mensajeria.GestorMails.DN.DestinoMailDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "Framework.Mensajeria.GestorMensajeriaDN.DatosMensajeDN") Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mDatos"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "Framework.Mensajeria.GestorMails.DN.SobreDN") Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mMensaje"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mXmlMensaje"
        '    campodatos.TamañoCampo = 2000

        '    Return mapinst
        'End If

        ''ZONA: Gestión de talones       _________________________________________________________________


        'If pTipo Is GetType(FN.GestionPagos.DN.PagoDN) Then

        '    mapinst.ColTiposTrazas = New List(Of System.Type)
        '    mapinst.ColTiposTrazas.Add(GetType(FN.GestionPagos.DN.PagoTrazaDN))

        '    Return mapinst
        'End If


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



        If pTipo Is GetType(FN.GestionPagos.DN.TalonDN) Then
            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mHuellaRTF"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mPlantillaCarta"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColTalonDocumento"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)



            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mTalonEmitido"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)



            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mDireccionEnvio"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)










            Return mapinst
        End If

        'If pTipo Is GetType(FN.GestionPagos.DN.TalonDocumentoDN) Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNumeroSerie"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    Return mapinst
        'End If

        'If pTipo Is GetType(FN.GestionPagos.DN.ContenedorRTFDN) Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mArrayString"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If

        'If pTipo Is GetType(FN.GestionPagos.DN.OrigenDN) Then
        '    mapinst.ColTriger.Add(New Triger("", "ALTER TABLE tlOrigenDN ADD CONSTRAINT tlOrigenDNTipoEntidadOrigenDN UNIQUE  (IDEntidad,idTipoEntidadOrigen)"))
        '    Return mapinst

        'End If

        'If pTipo Is GetType(FN.GestionPagos.DN.ContenedorImagenDN) Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mImagen"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If

        'If pTipo Is GetType(FN.GestionPagos.DN.ConfiguracionImpresionTalonDN) Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mFuente"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mConfigPagina"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mFirma"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)


        '    Return mapinst
        'End If


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


            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoIndirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoDirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.NoProcesar)
            campodatos.MapSubEntidad = mapSubInst

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mObjetoIndirectoOperacion"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst
        End If
        ''FINZONA: Gestión de talones    _________________________________________________________________


        ''ZONA: EmpresaDN ________________________________________________________________

        If pTipo Is GetType(FN.Empresas.DN.EmpresaDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()
            Dim alentidades As ArrayList

            mapSubInst = New InfoDatosMapInstClaseDN()
            alentidades = New ArrayList()
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
            alentidades.Add(New VinculoClaseDN(GetType(PersonaFiscalDN)))
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
            mapSubInst.NombreCompleto = GetType(CifDN).FullName

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



        If pTipo Is GetType(FN.Empresas.DN.EntidadColaboradoraDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN()
            Dim alentidades As ArrayList

            mapSubInst = New InfoDatosMapInstClaseDN()
            alentidades = New ArrayList()
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpleadoDN)))
            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.SedeEmpresaDN)))
            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mEntidadAsociada"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst


            Return mapinst

        End If

        ''FINZONA: EmpresaDN ________________________________________________________________

        ''ZONA: PersonaDN ________________________________________________________________

        ''Dim pp As PersonasDN.PersonaDN
        ''Dim l As LocalizacionesDN.NifDN

        'If (pTipo.FullName = "PersonasDN.PersonaDN") Then
        '    Dim mapSubInst As New InfoDatosMapInstClaseDN

        '    mapSubInst.NombreCompleto = "LocalizacionesDN.NifDN"

        '    ParametrosGeneralesNoProcesar(mapSubInst)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNIF"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst
        'End If

        ''FINZONA: PersonaDN ________________________________________________________________


        'ZONA: PersonaDN ________________________________________________________________

        If pTipo Is GetType(FN.Personas.DN.PersonaDN) Then
            Dim mapSubInst As New InfoDatosMapInstClaseDN


            'ParametrosGeneralesNoProcesar(mapSubInst)

            'campodatos = New InfoDatosMapInstCampoDN
            'campodatos.InfoDatosMapInstClase = mapinst
            'campodatos.NombreCampo = "mNIF"
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            'campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)
            'campodatos.MapSubEntidad = mapSubInst





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
            campodatos.NombreCampo = "mNIF"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
            campodatos.MapSubEntidad = mapSubInst



            Return mapinst
        End If

        ''FINZONA: PersonaDN ________________________________________________________________


        ''ZONA: localizaciones ________________________________________________________________


        'If (pTipo.FullName = "LocalizacionesDN.IContactoElementoDN") Then
        '    Dim alentidades As New ArrayList

        '    alentidades.Add(New VinculoClaseDN("LocalizacionesDN", "LocalizacionesDN.DireccionNoUnicaDN"))
        '    mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    Return mapinst
        'End If

        'If pTipo Is GetType(CodigoPostalDN) Then
        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNombre"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "LocalizacionesDN.DatosContactoEntidadDN") Then
        '    Dim mapSubInst As InfoDatosMapInstClaseDN
        '    Dim alentidades As ArrayList

        '    mapSubInst = New InfoDatosMapInstClaseDN
        '    alentidades = New ArrayList
        '    alentidades.Add(New VinculoClaseDN(GetType(PersonaDN)))
        '    alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaDN)))
        '    mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mEntidadReferida"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst
        'End If



        If (pTipo Is GetType(FN.Localizaciones.DN.IEntidadFiscalDN)) Then
            Dim alentidades As New ArrayList

            alentidades.Add(New VinculoClaseDN(GetType(PersonaFiscalDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            alentidades.Add(New VinculoClaseDN(GetType(FN.Empresas.DN.EmpresaFiscalDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

            Return mapinst
        End If


        ''FINZONA: localizaciones(________________________________________________________________)


        ''ZONA: Procesos ________________________________________________________________________



        'If pTipo.FullName = "Framework.Procesos.ProcesosDN.ClientedeFachadaDN" Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mNombre"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "Framework.Procesos.ProcesosDN.GrupoDTSDN") Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValor"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If

        'If (pTipo.FullName = "Framework.Procesos.ProcesosDN.GrupoColDN") Then

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mValor"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenidaSerializada)

        '    Return mapinst
        'End If


        ''FINZONA: Procesos _____________________________________________________________________


        ''ZONA: FinancieroDN ________________________________________________________________

        'If pTipo.FullName = GetType(FN.Financiero.DN.CuentaBancariaDN).FullName Then
        '    Dim mapSubInst As InfoDatosMapInstClaseDN

        '    ' mapeado de la clase referida por el campo IBAN
        '    mapSubInst = New InfoDatosMapInstClaseDN()
        '    mapSubInst.NombreCompleto = GetType(FN.Financiero.DN.IBANDN).FullName

        '    ParametrosGeneralesNoProcesar(mapSubInst)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mIBAN"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    campodatos.MapSubEntidad = mapSubInst

        '    ' mapeado de la clase referida por el campo CCC
        '    mapSubInst = New InfoDatosMapInstClaseDN()
        '    mapSubInst.NombreCompleto = GetType(FN.Financiero.DN.CCCDN).FullName

        '    ParametrosGeneralesNoProcesar(mapSubInst)

        '    campodatos = New InfoDatosMapInstCampoDN
        '    campodatos.InfoDatosMapInstClase = mapinst
        '    campodatos.NombreCampo = "mCCC"
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)
        '    campodatos.ColCampoAtributo.Add(CampoAtributoDN.UnicoEnFuenteDatosoNulo)

        '    campodatos.MapSubEntidad = mapSubInst

        '    Return mapinst

        'End If


        ''FINZONA: EmpresaDN ________________________________________________________________

        ''FNZONA: AMVGDocs____________________________________________________________________________




        'FNZONA: PAGOS____________________________________________________________________________





        'If (pTipo Is GetType(FN.GestionPagos.DN.ApunteImpDDN)) Then


        '    Dim alentidades As ArrayList
        '    Dim mapSubInst As InfoDatosMapInstClaseDN

        '    'mapSubInst = New InfoDatosMapInstClaseDN
        '    'alentidades = New ArrayList

        '    'mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
        '    'alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))

        '    'mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


        '    'campodatos = New InfoDatosMapInstCampoDN
        '    'campodatos.InfoDatosMapInstClase = mapinst
        '    'campodatos.NombreCampo = "mAcreedora"
        '    'campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
        '    'campodatos.MapSubEntidad = mapSubInst

        '    'campodatos = New InfoDatosMapInstCampoDN
        '    'campodatos.InfoDatosMapInstClase = mapinst
        '    'campodatos.NombreCampo = "mHuellaIOrigenImpDebDN"
        '    'campodatos.ColCampoAtributo.Add(CampoAtributoDN.PersistenciaContenida)

        '    Return mapinst

        'End If

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

            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)))


            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.PagoDN)))
            mapinst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades

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


        If (pTipo Is GetType(FN.GestionPagos.DN.PagoDN)) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.ApunteImpDDN)))

            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColIEntidadDN"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst

        End If

        If (pTipo Is GetType(FN.GestionPagos.DN.OrigenIdevBaseDN)) Then


            Dim alentidades As ArrayList
            Dim mapSubInst As InfoDatosMapInstClaseDN

            mapSubInst = New InfoDatosMapInstClaseDN
            alentidades = New ArrayList

            mapSubInst.NombreCompleto = GetType(Framework.DatosNegocio.IEntidadDN).FullName
            alentidades.Add(New VinculoClaseDN(GetType(FN.GestionPagos.DN.ApunteImpDDN)))

            mapSubInst.ItemDatoMapeado(TiposDatosMapInstClaseDN.InterfaceImplementadaPor) = alentidades


            campodatos = New InfoDatosMapInstCampoDN
            campodatos.InfoDatosMapInstClase = mapinst
            campodatos.NombreCampo = "mColIEntidad"
            campodatos.ColCampoAtributo.Add(CampoAtributoDN.InterfaceImplementadaPor)
            campodatos.MapSubEntidad = mapSubInst

            Return mapinst

        End If
        Return mapinst

    End Function

End Class
