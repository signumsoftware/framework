Imports System
Imports System.Text
Imports System.Collections.Generic
Imports Microsoft.VisualStudio.TestTools.UnitTesting

Imports Framework.AccesoDatos.MotorAD.LN
Imports Framework.DatosNegocio
Imports Framework
Imports Framework.TiposYReflexion.DN

Imports Framework.LogicaNegocios.Transacciones
Imports Framework.Operaciones.OperacionesDN

<TestClass()> Public Class CargaDatosTest
    Dim mRecurso As Framework.LogicaNegocios.Transacciones.IRecursoLN

#Region "Métodos Test"


    <TestMethod(), Timeout(5700000)> Public Sub CargarDocumentosRequeridos()

        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarDocumentosRequeridosP()
        End Using

    End Sub


    <TestMethod(), Timeout(5700000)> Public Sub CargarPrimasBase()

        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarPrimasBaseP()
        End Using

    End Sub

    <TestMethod()> Public Sub CargarModuladores()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarModuladoresP()

        End Using

    End Sub

    <TestMethod()> Public Sub CargarModuladores2Conductor()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarModuladores2ConductorP()

        End Using

    End Sub

    <TestMethod()> Public Sub CargarImpuesto()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarImpuestoP()

        End Using

    End Sub

    <TestMethod()> Public Sub CrearGrafoTarificacion()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CrearGrafoTarificacionP()
        End Using

    End Sub
    <TestMethod()> Public Sub TarificarPresupuesto()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)




            Using tr As New Transaccion
                Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))(0)
                Framework.Configuracion.AppConfiguracion.DatosConfig.Item(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN).Name) = opc

                Dim presup, presu2 As FN.Seguros.Polizas.DN.PresupuestoDN
                presup = ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.PresupuestoDN))(0)
                presup.FechaAltaSolicitada = presup.PeridoValidez.FInicio.AddDays(2)
                presup.CodColaborador = Nothing

                Dim lnc As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN
                presu2 = lnc.TarificarPresupuesto(presup)




                tr.Confirmar()


            End Using


        End Using

    End Sub
    Public Sub CargarPrimasBaseP()
        Dim colprimas As FN.RiesgosVehiculos.DN.ColPrimabaseRVSVDN
        Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD
        Dim colCategoriaMD As New FN.RiesgosVehiculos.DN.ColCategoriaModDatosDN()

        Using tr As New Transaccion(True)
            Dim miLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colCategoriaMD.AddRangeObjectUnico(miLN.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.CategoriaModDatosDN)))

            colprimas = ad.CargarPrimasBase(colCategoriaMD)
            tr.Confirmar()
        End Using
    End Sub

    <TestMethod()> Public Sub CargarComisiones()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarComisionesP()
        End Using

    End Sub

    <TestMethod()> Public Sub CargarFraccionamientos()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarFraccionamientosP()
        End Using

    End Sub

    <TestMethod()> Public Sub CargarCategoriaModDatos()
        ObtenerRecurso()

        Using New CajonHiloLN(mRecurso)
            CargarCategoriaModDatosP()
        End Using

    End Sub


    Public Sub CargarImpuestoP()

        Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN
        Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using tr As New Transaccion(True)

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            colcober.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            tr.Confirmar()

        End Using

        Using tr As New Transaccion

            Dim PrimasBaseLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PrimasBaseLN

            Dim ColImpuestoRVSV As New FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN
            Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD

            ColImpuestoRVSV = ad.CargarImpuestosModuladores(colcober)

            tr.Confirmar()

        End Using

    End Sub

    Public Sub CargarModuladores2ConductorP()

        Dim ColModuladorRVSV As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN()
        Dim colprimas As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN()
        '        Dim ColValorIntervalNumMap As New Framework.Tarificador.TarificadorDN.ColValorIntervalNumMapDN
        Dim ColCaracteristica As New Framework.Cuestionario.CuestionarioDN.ColCaracteristicaDN

        Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using tr As New Transaccion()

            Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD()

            Dim PrimasBaseLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PrimasBaseLN
            colprimas.AddRangeObject(PrimasBaseLN.RecuperarLista())

            'lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            'ColValorIntervalNumMap.AddRangeObject(lng.RecuperarLista(GetType(Framework.Tarificador.TarificadorDN.ValorIntervalNumMapDN)))

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            ColCaracteristica.AddRangeObject(lng.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CaracteristicaDN)))


            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            ColModuladorRVSV.AddRangeObject(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))

            ColModuladorRVSV.AddRangeObject(ad.CargarModuladoresMultiConductor(ColCaracteristica, colprimas, colprimas.RecuperarColCategoriasDN, colprimas.RecuperarColCoberturaDN))

            tr.Confirmar()

        End Using

    End Sub

    Public Sub CargarModuladoresP()

        Dim PrimasBaseLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PrimasBaseLN

        Dim ColModuladorRVSV As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
        Dim colprimas As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN
        Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD

        Using tr As New Transaccion

            colprimas.AddRangeObject(PrimasBaseLN.RecuperarLista())

            ColModuladorRVSV = ad.CargarModuladores(colprimas, colprimas.RecuperarColCategoriasDN, colprimas.RecuperarColCoberturaDN)

            tr.Confirmar()

        End Using

    End Sub

    Public Sub CargarComisionesP()
        Dim PrimasBaseLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PrimasBaseLN

        Dim colComisionesRVSV As New FN.RiesgosVehiculos.DN.ColComisionRVSVDN
        Dim colPrimas As New FN.RiesgosVehiculos.DN.ColPrimaBaseRVDN
        Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD

        Using tr As New Transaccion
            colPrimas.AddRangeObject(PrimasBaseLN.RecuperarLista())
            colComisionesRVSV = ad.CargarComisiones(colPrimas.RecuperarColCoberturaDN)
            tr.Confirmar()
        End Using

    End Sub

    Private Sub CargarCategoriaModDatosP()
        Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD()

        Using tr As New Transaccion()

            ad.CargarCategoriaModDatos()

            tr.Confirmar()

        End Using
    End Sub

    Public Sub CrearGrafoTarificacionP()
        Dim ColModuladorRVSV As New FN.RiesgosVehiculos.DN.ColModuladorRVSVDN
        Dim ColPrimabaseRVSV As New FN.RiesgosVehiculos.DN.ColPrimabaseRVSVDN
        Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using tr As New Transaccion()

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            ColPrimabaseRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.PrimabaseRVSVDN)))

            tr.Confirmar()

        End Using

        Using tr As New Transaccion()

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            ColModuladorRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ModuladorRVSVDN)))

            tr.Confirmar()

        End Using

        Dim ColImpuestoRVSV As New FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN()
        Using tr As New Transaccion()

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            ColImpuestoRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ImpuestoRVSVDN)))

            tr.Confirmar()

        End Using

        Dim colComisionesRVSV As New FN.RiesgosVehiculos.DN.ColComisionRVSVDN()
        Using tr As New Transaccion()

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            colComisionesRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.ComisionRVSVDN)))

            tr.Confirmar()

        End Using

        Dim colFracRVSV As New FN.RiesgosVehiculos.DN.ColFraccionamientoRVSVDN()
        Using tr As New Transaccion()

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colFracRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.FraccionamientoRVSVDN)))

            tr.Confirmar()

        End Using

        Dim colBonifRVSV As New FN.RiesgosVehiculos.DN.ColBonificacionRVSVDN()
        Using tr As New Transaccion()

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            colBonifRVSV.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.BonificacionRVSVDN)))

            tr.Confirmar()

        End Using

        Using tr As New Transaccion()
            ' creamos el flujo
            Dim colopRamaCober As New Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN
            Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
            Dim suministradorvPrimas As FN.RiesgosVehiculos.DN.PrimabaseRVSVDN
            Dim nombreCobertura As String
            Dim colCobert As FN.Seguros.Polizas.DN.ColCoberturaDN = ColPrimabaseRVSV.RecuperarColCoberturaDN()

            ''''''''''''''''''''''''''''''''''''''''''''''
            '       RAMA
            ''''''''''''''''''''''''''''''''''''''''''''''

            For Each cober As FN.Seguros.Polizas.DN.CoberturaDN In colCobert

                nombreCobertura = cober.Nombre

                ' recuperamos el objeto  que tine todas las primas base para una cobertura (para todas las categorias de esa cobertura)
                suministradorvPrimas = ColPrimabaseRVSV.RecuperarxNombreCobertura(nombreCobertura)
                ' la operacion de prima base
                op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
                op.Nombre = "opPB" & nombreCobertura
                op.Operando1 = New SumiValFijoDN(1) ' esto debe sustituirse por la operación vinculada a un suministrador de valor que sera un modulador
                op.Operando2 = suministradorvPrimas
                op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN
                op.DebeCachear = True

                op = GenerarOperacionModulador(ColModuladorRVSV, op, nombreCobertura, "ANTG", True)
                If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
                    Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (ANTG)")
                End If
                op = GenerarOperacionModulador(ColModuladorRVSV, op, nombreCobertura, "CARN", True)
                If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
                    Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (CARN)")
                End If
                op = GenerarOperacionModulador(ColModuladorRVSV, op, nombreCobertura, "CYLD", True)
                If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
                    Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (CYLD)")
                End If
                op = GenerarOperacionModulador(ColModuladorRVSV, op, nombreCobertura, "EDAD", True)
                If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
                    Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (EDAD)")
                End If
                op = GenerarOperacionModulador(ColModuladorRVSV, op, nombreCobertura, "ZONA", True)
                If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
                    Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (ZONA)")
                End If

                op = GenerarOperacionModulador(ColModuladorRVSV, op, nombreCobertura, "MCND", True)
                If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
                    Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (MCND)")
                End If

                If nombreCobertura <> "AV" AndAlso nombreCobertura <> "AC" Then
                    op = GenerarOperacionBonificion(colBonifRVSV, op, "Bonificación Siniestralidad", True)
                    If op.Operando1 Is Nothing OrElse op.Operando2 Is Nothing Then
                        Throw New ApplicationException("Los operandos de la operación no pueden ser nulos para la bonificación")
                    End If
                End If


                ' op = GenerarOperacionModulador(pColModuladores, op, nombreCobertura, "PROM", False)

                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                '  Operación truncar a 5 decimales
                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                op = GenerarOperacionTruncar(op, 5, False)


                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                '  comisiones: Operaciones de comisiones (excepto comisión fija AMV por gestión)
                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

                'Dim colOpComCob As Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN = GenerarOperacionComision(cober, op, colComisionesRVSV, nombreCobertura, "Comisión Fija", True)
                'Dim opResumenComisionCob As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = GeneracionOperacionSumaResumen("Suma Comisión Fija", op, colOpComCob)

                Dim opComisionCob As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = GenerarOperacionComision(cober, op, colComisionesRVSV, "Comisión Fija", True)

                If opComisionCob IsNot Nothing Then
                    If opComisionCob.Operando1 Is Nothing OrElse opComisionCob.Operando2 Is Nothing Then
                        Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (Suma comisiones)")
                    End If
                Else
                    opComisionCob = op
                End If



                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                '  fraccionamiento
                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

                opComisionCob = GenerarOperacionFraccionamiento(colFracRVSV, opComisionCob, nombreCobertura, True)


                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                '  Operación que cachea la prima que reparte la comisión
                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''

                opComisionCob = GenerarOperacionSuma(0, opComisionCob, nombreCobertura, True, "Prima Riesgo Comisiones")

                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                '  comisiones: comisión fija AMV por gestión

                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


                'Dim colOpComGest As Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN = GenerarOperacionComision(cober, op, colComisionesRVSV, nombreCobertura, "Gestión AMV", True)
                'Dim opResumenComision As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = GeneracionOperacionSumaResumen("Suma Comisión AMV", op, colOpComGest)

                Dim opComision As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = GenerarOperacionComision(cober, opComisionCob, colComisionesRVSV, "Gestión AMV", True)

                If opComision IsNot Nothing Then
                    If opComision.Operando1 Is Nothing OrElse opComision.Operando2 Is Nothing Then
                        Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (Suma comisiones)")
                    End If
                Else
                    opComision = op
                End If


                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                '  Operación de redondeo a 2 decimales
                '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
                opComision = GenerarOperacionRedondear(opComision, 2, False)

                '''''''''''''''''''''''''''''''''''
                '  impuestos
                '''''''''''''''''''''''''''''''''''

                Dim ColOperacionSimpleBase As Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN = GenerarColOperacionesDeImpuestos(cober, opComision, ColImpuestoRVSV)
                Dim opeResumen As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = GeneracionOperacionSumaResumen("Suma Impuestos/" & nombreCobertura, opComision, ColOperacionSimpleBase)
                opeResumen.DebeCachear = True
                If opeResumen.Operando1 Is Nothing OrElse opeResumen.Operando2 Is Nothing Then
                    Throw New ApplicationException("Los operandos de la operación no pueden ser nulos (Suma impuestos)")
                End If


                colopRamaCober.Add(opeResumen)

            Next

            Dim opeResumenTotal As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN = GeneracionOperacionSumaResumen("Suma Total", New SumiValFijoDN(0), colopRamaCober)
            opeResumenTotal.DebeCachear = True

            Dim opr As New Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN()
            opr.Nombre = "GrafoTotal"
            opr.IOperacionDN = opeResumenTotal
            Me.GuardarDatos(opr)

            tr.Confirmar()

        End Using

    End Sub

    Public Sub CargarDocumentosRequeridosP()
        Using tr As New Transaccion()
            '''''''''''''''''''''''''
            ' crear las relaciones de documentos requeridos y coberturas


            ' creción de los tipos de documentos
            Dim coltdoc As New Framework.Ficheros.FicherosDN.ColTipoFicheroDN
            Dim tipoDoc As Framework.Ficheros.FicherosDN.TipoFicheroDN

            tipoDoc = New Framework.Ficheros.FicherosDN.TipoFicheroDN
            tipoDoc.Nombre = "Ficha técnica"
            coltdoc.Add(tipoDoc)

            tipoDoc = New Framework.Ficheros.FicherosDN.TipoFicheroDN
            tipoDoc.Nombre = "Carnet conducir"
            coltdoc.Add(tipoDoc)

            tipoDoc = New Framework.Ficheros.FicherosDN.TipoFicheroDN
            tipoDoc.Nombre = "Certificado 125"
            coltdoc.Add(tipoDoc)

            tipoDoc = New Framework.Ficheros.FicherosDN.TipoFicheroDN
            tipoDoc.Nombre = "Presupuesto firmado"
            coltdoc.Add(tipoDoc)

            Me.GuardarDatos(coltdoc)



            ' creacion de los documentos requeridos asociados a coberturtas
            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN
            colcober.AddRangeObject(ln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            Dim coldocumentoRequerido As New Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN
            Dim documentoRequerido As Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN

            documentoRequerido = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
            documentoRequerido.TipoDoc = coltdoc.RecuperarPrimeroXNombre("Ficha técnica")
            documentoRequerido.ColEntidadesRequeridoras.AddHuellaPara(colcober.RecuperarPrimeroXNombre("RCO"))
            Dim plazoR As New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias()
            plazoR.Meses = 1
            documentoRequerido.Plazo = plazoR
            coldocumentoRequerido.Add(documentoRequerido)

            documentoRequerido = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
            documentoRequerido.TipoDoc = coltdoc.RecuperarPrimeroXNombre("Carnet conducir")
            documentoRequerido.ColEntidadesRequeridoras.AddHuellaPara(colcober.RecuperarPrimeroXNombre("RCO"))
            plazoR = New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias()
            plazoR.Meses = 1
            documentoRequerido.Plazo = plazoR
            coldocumentoRequerido.Add(documentoRequerido)

            documentoRequerido = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
            documentoRequerido.TipoDoc = coltdoc.RecuperarPrimeroXNombre("Presupuesto firmado")
            documentoRequerido.ColEntidadesRequeridoras.AddHuellaPara(colcober.RecuperarPrimeroXNombre("RCO"))
            plazoR = New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias()
            plazoR.Meses = 1
            documentoRequerido.Plazo = plazoR
            coldocumentoRequerido.Add(documentoRequerido)

            'Certificado 125 para SPORT
            Dim colCategorias As New FN.RiesgosVehiculos.DN.ColCategoriaDN()
            colCategorias.AddRangeObject(ln.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.CategoriaDN)))

            documentoRequerido = New Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN
            documentoRequerido.TipoDoc = coltdoc.RecuperarPrimeroXNombre("Certificado 125")
            documentoRequerido.ColEntidadesRequeridoras.AddHuellaPara(colCategorias.RecuperarPrimeroXNombre("Sport"))
            plazoR = New Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias()
            plazoR.Dias = 15
            documentoRequerido.Plazo = plazoR
            coldocumentoRequerido.Add(documentoRequerido)

            Me.GuardarDatos(coldocumentoRequerido)


            tr.Confirmar()
        End Using
    End Sub

    Public Sub CargarFraccionamientosP()
        Dim colcober As New FN.Seguros.Polizas.DN.ColCoberturaDN
        Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using tr As New Transaccion(True)

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            colcober.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.Seguros.Polizas.DN.CoberturaDN)))

            tr.Confirmar()

        End Using

        Using tr As New Transaccion

            Dim PrimasBaseLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.PrimasBaseLN

            Dim colFracRVSV As New FN.RiesgosVehiculos.DN.ColFraccionamientoRVSVDN
            Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD()

            colFracRVSV = ad.CargarFraccionamientos(colcober)

            tr.Confirmar()

        End Using
    End Sub

    Public Sub CargarBonificaciones()
        Dim colCatMD As New FN.RiesgosVehiculos.DN.ColCategoriaModDatosDN()
        Dim lng As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

        Using tr As New Transaccion(True)

            lng = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            colCatMD.AddRangeObjectUnico(lng.RecuperarLista(GetType(FN.RiesgosVehiculos.DN.CategoriaModDatosDN)))

            tr.Confirmar()

        End Using

        Using tr As New Transaccion

            Dim colBonifRVSV As New FN.RiesgosVehiculos.DN.ColBonificacionRVSVDN
            Dim ad As New FN.RiesgosVehiculos.AD.CargadorPrimasBaseAD()

            colBonifRVSV = ad.CargarBonificaciones(colCatMD)

            tr.Confirmar()

        End Using
    End Sub

#End Region

#Region "Métodos Privados"

    Private Function GeneracionOperacionSumaResumen(ByVal pNombreOperacion As String, ByVal pOpPrimaNeta As Framework.Operaciones.OperacionesDN.ISuministradorValorDN, ByVal pColOperacionSimpleBase As Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        Select Case pColOperacionSimpleBase.Count

            Case Is = 0

                Return Nothing

            Case Is = 1
                Dim opsumPrecendete As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
                opsumPrecendete = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
                opsumPrecendete.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
                opsumPrecendete.Operando1 = pOpPrimaNeta
                opsumPrecendete.Operando2 = pColOperacionSimpleBase.Item(0) ' esta es la operacion que resume prima neta
                opsumPrecendete.Nombre = pNombreOperacion
                opsumPrecendete.DebeCachear = True
                Return opsumPrecendete

            Case Is > 1
                Dim opsumNueva, opsumPrecendete As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
                'opsumPrecendete = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
                'opsumPrecendete.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
                'opsumPrecendete.Operando1 = pColOperacionSimpleBase.Item(0)
                'opsumPrecendete.Operando2 = pColOperacionSimpleBase.Item(1)
                'opsumPrecendete.Nombre = CType(opsumPrecendete.Operando1, Object).ToString & "-s-" & CType(opsumPrecendete.Operando2, Object).ToString
                opsumPrecendete = pColOperacionSimpleBase.Item(0)
                'opsumPrecendete.Nombre = CType(opsumPrecendete.Operando1, Object).ToString & "-s-" & CType(opsumPrecendete.Operando2, Object).ToString

                For a As Integer = 1 To pColOperacionSimpleBase.Count - 1
                    opsumNueva = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
                    opsumNueva.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
                    opsumNueva.Operando1 = opsumPrecendete
                    opsumNueva.Operando2 = pColOperacionSimpleBase.Item(a)
                    opsumPrecendete = opsumNueva
                    opsumPrecendete.Nombre = CType(opsumPrecendete.Operando1, Object).ToString & "-SS-" & CType(opsumPrecendete.Operando2, Object).ToString

                Next


                opsumNueva = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
                opsumNueva.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
                opsumNueva.Operando1 = opsumPrecendete 'esta es la operacion que corresponde a la suma de todos los impuestos
                opsumNueva.Operando2 = pOpPrimaNeta ' esta es la operacion que resume prima neta
                opsumNueva.Nombre = pNombreOperacion
                opsumNueva.DebeCachear = True


                Return opsumNueva

        End Select

    End Function

    Private Function GenerarColOperacionesDeImpuestos(ByVal cober As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pOpPrimaNeta As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pColImpuestoRVSV As FN.RiesgosVehiculos.DN.ColImpuestoRVSVDN) As Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN

        Dim col As New Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN
        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        Dim opR As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        ' obtener la cobertura que es base en la rama

        'Dim ModuladorRVSV As FN.RiesgosVehiculos.DN.ModuladorRVSVDN = pOpPrimaNeta.Operando2
        'Dim cober As FN.Seguros.Polizas.DN.CoberturaDN = ModuladorRVSV.Cobertura

        For Each imp As FN.RiesgosVehiculos.DN.ImpuestoRVSVDN In pColImpuestoRVSV
            If imp.Cobertura.GUID = cober.GUID Then
                op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN()
                op.DebeCachear = True

                op.Operando2 = imp ' el impuesto siempre se posiciona en el operando 2

                Select Case imp.Operadoraplicable
                    Case "+"
                        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN
                        op.Operando1 = New SumiValFijoDN(0)
                        op.Nombre = CType(op.Operando1, Object).ToString & "-+-" & CType(op.Operando2, Object).ToString

                    Case "*"
                        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN
                        op.Operando1 = pOpPrimaNeta
                        op.Nombre = CType(op.Operando1, Object).ToString & "-*-" & CType(op.Operando2, Object).ToString

                    Case Else
                        Throw New ApplicationException("operador aplicable no reconocido")
                End Select

                opR = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN()
                opR.DebeCachear = False

                opR.IOperadorDN = New Framework.Operaciones.OperacionesDN.RedondeoOperadorDN()
                opR.Operando1 = op
                opR.Operando2 = New SumiValFijoDN(2)
                opR.Nombre = CType(op.Operando1, Object).ToString & "-R2-"

                col.Add(opR)
            End If
        Next


        Return col
    End Function

    Private Function GenerarOperacionModulador(ByVal pColModuladores As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN, ByVal pOpPrecedente As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pNombreCobertura As String, ByVal pNombreModulador As String, ByVal pdebeCachear As Boolean) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        Dim col As FN.RiesgosVehiculos.DN.ColModuladorRVSVDN = pColModuladores.Recuperar(pNombreCobertura, pNombreModulador) ' para todas las categorias


        Select Case col.Count


            Case Is = 0
                'Throw New ApplicationException("al menos debia haberse recuperado un modulador")
                Return pOpPrecedente
            Case Is = 1


            Case Else
                Throw New ApplicationException("solo debia haberse recuperado un modulador")
        End Select

        ' la operacion de coeficientes
        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.Nombre = "op-" & pNombreCobertura & "-" & pNombreModulador
        op.Operando1 = pOpPrecedente
        op.Operando2 = col.Item(0)
        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN()
        op.DebeCachear = pdebeCachear


        Return op

    End Function

    Private Function GenerarOperacionBonificion(ByVal pColBonificaciones As FN.RiesgosVehiculos.DN.ColBonificacionRVSVDN, ByVal pOpPrecedente As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pNombreBonificacion As String, ByVal pdebeCachear As Boolean) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        Dim col As FN.RiesgosVehiculos.DN.ColBonificacionRVSVDN = pColBonificaciones ' para todas las categorias

        Select Case col.Count
            Case Is = 0
                'Throw New ApplicationException("al menos debia haberse recuperado un modulador")
                Return pOpPrecedente
            Case Is = 1

            Case Else
                Throw New ApplicationException("solo debia haberse recuperado una bonificacion")
        End Select

        ' la operacion de coeficientes
        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.Nombre = "op-" & pNombreBonificacion
        op.Operando1 = pOpPrecedente
        op.Operando2 = col.Item(0)
        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN()
        op.DebeCachear = pdebeCachear


        Return op

    End Function

    Private Function GenerarOperacionComision(ByVal cober As FN.Seguros.Polizas.DN.CoberturaDN, ByVal pOpPrecedente As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pColComisionesRVSV As FN.RiesgosVehiculos.DN.ColComisionRVSVDN, ByVal nombreComision As String, ByVal debeCachear As Boolean) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        'Dim col As New Framework.Operaciones.OperacionesDN.ColOperacionSimpleBaseDN()
        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        'Dim opR As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        Dim col As FN.RiesgosVehiculos.DN.ColComisionRVSVDN = pColComisionesRVSV.RecuperarxCobertura(nombreComision, cober.Nombre) ' para todas las categorias

        Select Case col.Count
            Case Is = 0
                'Throw New ApplicationException("al menos debia haberse recuperado un modulador")
                Return pOpPrecedente
            Case Is = 1


            Case Else
                Throw New ApplicationException("solo debia haberse recuperado un modulador")
        End Select

        ' la operacion de coeficientes
        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.Nombre = "op-" & cober.Nombre & "-" & nombreComision
        op.Operando1 = pOpPrecedente
        op.Operando2 = col.Item(0)
        Select Case col.Item(0).Operadoraplicable
            Case "+"
                op.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN()
                'op.Operando1 = New SumiValFijoDN(0)
                op.Nombre = CType(op.Operando1, Object).ToString & "-+-" & CType(op.Operando2, Object).ToString

            Case "*"
                op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN()
                'op.Operando1 = pOpPrecedente
                op.Nombre = CType(op.Operando1, Object).ToString & "-*-" & CType(op.Operando2, Object).ToString

            Case Else
                Throw New ApplicationException("operador aplicable no reconocido")
        End Select

        op.DebeCachear = debeCachear

        Return op

    End Function

    Private Function GenerarOperacionSuma(ByVal valorSuma As Double, ByVal opPrecedente As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal nombreCobertura As String, ByVal debeCachear As Boolean, ByVal nombreOperacion As String) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        Dim op As New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN()

        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.Nombre = nombreOperacion & "/" & nombreCobertura
        op.Operando1 = opPrecedente
        op.Operando2 = New SumiValFijoDN(valorSuma)
        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN()
        op.DebeCachear = debeCachear

        Return op

    End Function

    Private Function GenerarOperacionRedondear(ByVal pOpPrecedente As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal numDecimales As Integer, ByVal debeCachear As Boolean) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.DebeCachear = debeCachear

        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.RedondeoOperadorDN()
        op.Operando1 = pOpPrecedente
        op.Operando2 = New SumiValFijoDN(numDecimales)
        op.Nombre = CType(op.Operando1, Object).ToString & "-R-" & numDecimales.ToString

        Return op

    End Function

    Private Function GenerarOperacionTruncar(ByVal pOpPrecedente As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal numDecimales As Integer, ByVal debeCachear As Boolean) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN
        op.DebeCachear = debeCachear

        op.IOperadorDN = New Framework.Operaciones.OperacionesDN.TruncarOperadorDN()
        op.Operando1 = pOpPrecedente
        op.Operando2 = New SumiValFijoDN(numDecimales)
        op.Nombre = CType(op.Operando1, Object).ToString & "-T-" & numDecimales.ToString

        Return op

    End Function

    Private Function GenerarOperacionFraccionamiento(ByVal pColFraccionamientos As FN.RiesgosVehiculos.DN.ColFraccionamientoRVSVDN, ByVal pOpPrecedente As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN, ByVal pNombreCobertura As String, ByVal pdebeCachear As Boolean) As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        Dim op As Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN

        Dim col As FN.RiesgosVehiculos.DN.ColFraccionamientoRVSVDN = pColFraccionamientos.Recuperar(pNombreCobertura)

        Select Case col.Count

            Case Is = 0
                Return pOpPrecedente
            Case Is = 1

            Case Else
                Throw New ApplicationException("solo debia haberse recuperado un modulador")
        End Select

        ' la operacion de coeficientes
        op = New Framework.Operaciones.OperacionesDN.OperacionSimpleBaseDN()
        op.Nombre = "op-" & pNombreCobertura & "-Fraccionamiento"
        op.Operando1 = pOpPrecedente
        op.Operando2 = col.Item(0)

        If col.Item(0).Operadoraplicable = "+" Then
            op.IOperadorDN = New Framework.Operaciones.OperacionesDN.SumaOperadorDN()
        ElseIf col.Item(0).Operadoraplicable = "*" Then
            op.IOperadorDN = New Framework.Operaciones.OperacionesDN.MultiplicacionOperadorDN()
        End If

        op.DebeCachear = pdebeCachear

        Return op

    End Function

    Private Sub GuardarDatos(ByVal objeto As Object)
        Using tr As New Transaccion
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(objeto)

            tr.Confirmar()

        End Using
    End Sub

    Private Sub ObtenerRecurso()

        Dim connectionstring As String
        Dim htd As New Dictionary(Of String, Object)

        connectionstring = "server=localhost;database=SSPruebasFN;user=sa;pwd='sa'"
        htd.Add("connectionstring", connectionstring)
        mRecurso = New Framework.LogicaNegocios.Transacciones.RecursoLN("1", "Conexion a MND1", "sqls", htd)

        'Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New Framework.AccesoDatos.MotorAD.LN.GestorMapPersistenciaCamposAMVDocsEntrantesLN
        Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN.GestorMapPersistenciaCampos = New GestorMapPersistenciaCamposMotosTest()

    End Sub

#End Region


End Class