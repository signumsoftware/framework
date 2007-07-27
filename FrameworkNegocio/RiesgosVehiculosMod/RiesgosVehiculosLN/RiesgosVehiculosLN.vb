Imports Framework.LogicaNegocios.Transacciones

Imports FN.RiesgosVehiculos.DN
Imports FN.Seguros.Polizas.DN

Public Class RiesgosVehiculosLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN


    Public Sub VerificarCajonDocumento(ByVal pCajonDocumento As Framework.Ficheros.FicherosDN.CajonDocumentoDN)




        Using tr As New Transaccion


            pCajonDocumento.VerificarDocumentoEnlazado()


            Dim colcd As New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
            colcd.Add(pCajonDocumento)

            Dim ad As New FN.RiesgosVehiculos.AD.RiesgosVehiculosAD
            Dim colt As FN.Seguros.Polizas.DN.ColTarifaDN = ad.RecuperarTarifasRefierenCDs(colcd)


            For Each tarifa As FN.Seguros.Polizas.DN.TarifaDN In colt
                TarificadorRVLN.VerificarProductosAplicables(tarifa)
                Me.GuardarGenerico(tarifa)
            Next

            tr.Confirmar()

        End Using




    End Sub

    Public Function RecuperarRiesgoMotorActivo(ByVal pMatricula As String, ByVal pNumeroBastidor As String) As FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Dim RiesgoMotor As FN.RiesgosVehiculos.DN.RiesgoMotorDN
        Dim RVAD As RiesgosVehiculos.AD.RiesgosVehiculosAD

        Using tr As New Transaccion()

            RVAD = New RiesgosVehiculos.AD.RiesgosVehiculosAD()
            RiesgoMotor = RVAD.RecuperarRiesgoMotorActivo(pMatricula, pNumeroBastidor)

            tr.Confirmar()

            Return RiesgoMotor

        End Using
    End Function

    Public Function RecuperarModeloDatos(ByVal nombreModelo As String, ByVal nombreMarca As String, ByVal matriculada As Boolean, ByVal fecha As Date) As ModeloDatosDN
        Dim modeloDatos As ModeloDatosDN = Nothing
        Dim RVAD As RiesgosVehiculos.AD.RiesgosVehiculosAD

        Using tr As New Transaccion()

            RVAD = New RiesgosVehiculos.AD.RiesgosVehiculosAD()
            modeloDatos = RVAD.RecuperarModeloDatos(nombreModelo, nombreMarca, matriculada, fecha)

            tr.Confirmar()

            Return modeloDatos

        End Using
    End Function



    Public Function RecuperarModelosPorMarca(ByVal pMarca As MarcaDN) As List(Of ModeloDN)
        Dim RVAD As RiesgosVehiculos.AD.RiesgosVehiculosAD

        Using tr As New Transaccion()
            RVAD = New RiesgosVehiculos.AD.RiesgosVehiculosAD()
            Dim lista As List(Of ModeloDN) = RVAD.RecuperarModelosPorMarca(pMarca)
            tr.Confirmar()
            Return lista
        End Using

    End Function

    Public Sub TarificarPresupuestoOp(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal pParametros As Object)

        Using tr As New Transaccion()
            Dim presupuesto As PresupuestoDN = CType(pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion, PresupuestoDN)

            pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion = Me.TarificarPresupuesto(CType(pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion, PresupuestoDN))

            tr.Confirmar()

        End Using

    End Sub

    Public Function TarificarPresupuesto(ByVal presupuesto As PresupuestoDN) As PresupuestoDN

        Using tr As New Transaccion()


            Dim fecha As Date = Now
            Dim tarifaP As TarifaDN = Me.TarificarTarifa(presupuesto.Tarifa, Nothing, presupuesto.FuturoTomador, True, True)
            Debug.WriteLine("todoEl MEtodo" & Now.Subtract(fecha).TotalSeconds)




            presupuesto.Tarifa = tarifaP

            Me.GuardarGenerico(presupuesto)

            Dim miLN As New PolizaRvLcLN()
            Dim colcd As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN = miLN.VincularCajonesDocumento(presupuesto)
            '     miLN.ActualizarProdutosAplicables(presupuesto.Tarifa, colcd)

            'Me.GuardarGenerico(presupuesto)
            'Dim presupuestoBD As PresupuestoDN
            'presupuestoBD = Me.RecuperarGenerico(presupuesto.ID, GetType(PresupuestoDN))

            tr.Confirmar()

            'Return presupuestoBD

            Return presupuesto

        End Using

    End Function

    Public Function TarificarTarifa(ByVal tarifa As TarifaDN, ByVal tipoFraccionamiento As FN.GestionPagos.DN.FraccionamientoDN, ByVal tomador As FN.Seguros.Polizas.DN.ITomador, ByVal tarificarporOferttados As Boolean, ByVal pVerificarProductosAplicables As Boolean) As TarifaDN
        Using tr As New Transaccion()
            Dim btLN As Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim colLPEliminadas As New ColLineaProductoDN()
            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = CType(Framework.Configuracion.AppConfiguracion.DatosConfig.Item(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN).Name), Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN)

            If opc Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se ha podido recuperar la operación configurada del grafo de tarificación actual")
            End If

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' preparamos el recuperador de suministraodres de valor que usa el gafo para recuperar valores y poner los resultados
            Dim irec As RVIRecSumiValorLN

            If opc.IOperacionDN.IRecSumiValorLN Is Nothing Then
                irec = New RVIRecSumiValorLN()
            Else
                irec = opc.IOperacionDN.IRecSumiValorLN
            End If



            irec.DataSoucers.Clear()
            irec.DataResults.Clear()
            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''



            ' verificamos la integridad de la tarifa donde se añaden las linias de tarifa

            tarifa.CompletarProductosDependientes()

            'Dim mensaje As String = String.Empty
            'If tarifa.EstadoIntegridad(mensaje) <> Framework.DatosNegocio.EstadoIntegridadDN.Consistente Then
            '    Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
            'End If



            'añade a a la coleccion de lineas a eliminar lsa lineas de productos que no son tarificadas
            For Each lp As LineaProductoDN In tarifa.ColLineaProducto
                If tarificarporOferttados Then
                    If lp.Ofertado = False Then
                        colLPEliminadas.Add(lp)
                    End If
                Else
                    If lp.Establecido = False Then
                        colLPEliminadas.Add(lp)
                    End If
                End If
            Next



            For Each lp As LineaProductoDN In colLPEliminadas
                tarifa.ColLineaProducto.EliminarEntidadDN(lp, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos)
            Next

            Dim recNivelBonif As New RecNivelBonificacionLN()
            recNivelBonif.Tomador = tomador

            irec.Tarifa = tarifa
            irec.DataSoucers.Add(irec.Tarifa)
            irec.DataSoucers.Add(recNivelBonif)

            Dim rm As FN.RiesgosVehiculos.DN.RiesgoMotorDN = irec.Tarifa.Riesgo
            irec.DataSoucers.Add(rm.ModeloDatos)

            Dim dTarifaV As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN
            dTarifaV = CType((tarifa).DatosTarifa, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN) '.Clone()

            If dTarifaV.HeCuestionarioResuelto.EntidadReferida Is Nothing Then
                btLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                dTarifaV.HeCuestionarioResuelto.EntidadReferida = btLN.RecuperarGenerico(dTarifaV.HeCuestionarioResuelto)
            End If


            irec.DataSoucers.Add(dTarifaV.HeCuestionarioResuelto.EntidadReferida)

            opc.IOperacionDN.IRecSumiValorLN = irec

            Dim gf As New FN.GestionPagos.DN.GrupoFraccionamientosDN()
            Dim colGPF As New FN.GestionPagos.DN.ColGrupoPagosFraccionadosDN()
            Dim gpf As FN.GestionPagos.DN.GrupoPagosFraccionadosDN


            Dim fecha As Date

            If tipoFraccionamiento Is Nothing Then
                btLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                Dim colFrac As New FN.GestionPagos.DN.ColFraccionamientoDN
                colFrac.AddRangeObjectUnico(btLN.RecuperarLista(GetType(FN.GestionPagos.DN.FraccionamientoDN)))

                For Each fr As FN.GestionPagos.DN.FraccionamientoDN In colFrac.ListaOrdenada()
                    tarifa.Fraccionamiento = fr

                    If fr.NumeroPagos = 1 Then
                        tipoFraccionamiento = fr
                    Else
                        irec.DataResults.Clear()
                        fecha = Now
                        tarifa.Importe = opc.IOperacionDN.GetValor()
                        Debug.WriteLine(Now.Subtract(fecha).TotalSeconds)
                        gpf = ObtenerGrupoPagosFraccionados(irec, fr)
                        colGPF.Add(gpf)

                    End If

                Next

            End If

            tarifa.Fraccionamiento = tipoFraccionamiento
            irec.DataResults.Clear()

            fecha = Now
            tarifa.Importe = opc.IOperacionDN.GetValor()
            Debug.WriteLine(Now.Subtract(fecha).TotalSeconds)


            opc.IOperacionDN.Limpiar()

            tarifa.ColLineaProducto.AddRange(colLPEliminadas)

            gpf = ObtenerGrupoPagosFraccionados(irec, tipoFraccionamiento)

            btLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim ColLimitesFrac As New FN.GestionPagos.DN.ColLimiteMinFraccionamientoDN
            ColLimitesFrac.AddRangeObjectUnico(btLN.RecuperarLista(GetType(FN.GestionPagos.DN.LimiteMinFraccionamientoDN)))

            gf.ColGrupoPagosF = colGPF.RecuperaGruposPagosFracSuperanLimite(ColLimitesFrac, tarifa.Importe)

            gf.ColGrupoPagosF.Add(gpf)

            tarifa.GrupoFraccionamientos = gf

            'se guarda y se recupera el presupuesto para garantizar que no tiene referenciadas más entidades de las necesarias
            irec.ClearAll()
            opc.IOperacionDN.Limpiar()

            If pVerificarProductosAplicables Then
                TarificadorRVLN.VerificarProductosAplicables(tarifa)
            End If

            'Me.GuardarGenerico(tarifa)
            'Dim miLN As New PolizaRvLcLN()
            'Dim tarifaBD As TarifaDN
            'tarifaBD = Me.RecuperarGenerico(tarifa.ID, GetType(TarifaDN))

            tr.Confirmar()

            ' Return tarifaBD
            Return tarifa
        End Using

    End Function


    'Public Function longitud(ByVal objeto As Object) As Double
    '    Dim serializador As New System.Runtime.Serialization.Formatters.Binary.BinaryFormatter
    '    Dim ms As New System.IO.MemoryStream
    '    serializador.Serialize(ms, objeto)
    '    longitud = ms.Length
    '    System.Diagnostics.Debug.WriteLine(longitud)



    'End Function

    Public Sub DesCargarGrafoTarificacion()
        Using tr As New Transaccion()

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
            opc = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))(0)

            If Framework.Configuracion.AppConfiguracion.DatosConfig.ContainsKey(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN).Name) Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Item(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN).Name) = Nothing
            End If

            tr.Confirmar()

        End Using
    End Sub

    Public Sub CargarGrafoTarificacion()
        Using tr As New Transaccion()
            'Cargamos en memoria el grafo de tarificación
            'TODO: Habría que cargar el grafo actual por la fecha
            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
            opc = ln.RecuperarLista(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN))(0)

            If Framework.Configuracion.AppConfiguracion.DatosConfig.Item(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN).Name) Is Nothing Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN).Name, opc)
            Else
                Framework.Configuracion.AppConfiguracion.DatosConfig.Item(GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN).Name) = opc
            End If

            tr.Confirmar()

        End Using
    End Sub

    Public Function RecuperarCategoria(ByVal modelo As ModeloDN, ByVal matriculada As Boolean) As CategoriaDN
        Dim heCat As HECategoriaModDatosDN
        Dim categoria As CategoriaDN = Nothing
        Dim categoriaMD As CategoriaModDatosDN = Nothing

        Using tr As New Transaccion()
            heCat = Me.RecuperarHuellaCategoria(modelo, matriculada)

            If heCat IsNot Nothing Then
                Me.RecuperarGenerico(heCat)
                categoriaMD = heCat.EntidadReferida
                categoria = categoriaMD.Categoria
            End If

            tr.Confirmar()

            Return categoria

        End Using
    End Function

    Public Function RecuperarHuellaCategoria(ByVal modelo As ModeloDN, ByVal matriculada As Boolean) As HECategoriaModDatosDN
        Dim miAD As FN.RiesgosVehiculos.AD.RiesgosVehiculosAD

        Using tr As New Transaccion()
            miAD = New FN.RiesgosVehiculos.AD.RiesgosVehiculosAD()
            RecuperarHuellaCategoria = miAD.RecuperarHuellaCategoria(modelo, matriculada)

            tr.Confirmar()

        End Using
    End Function

    Public Function RecuperarProductosModelo(ByVal modelo As ModeloDN, ByVal matriculada As Boolean, ByVal fecha As Date) As ColProductoDN
        Dim categoria As CategoriaDN
        Dim miAD As FN.RiesgosVehiculos.AD.RiesgosVehiculosAD

        Using tr As New Transaccion()
            categoria = Me.RecuperarCategoria(modelo, matriculada)

            miAD = New FN.RiesgosVehiculos.AD.RiesgosVehiculosAD()

            RecuperarProductosModelo = miAD.RecuperarProductosModelo(categoria, fecha)

            tr.Confirmar()
        End Using
    End Function

    Private Function ObtenerGrupoPagosFraccionados(ByVal iRecRV As RVIRecSumiValorLN, ByVal fraccionamiento As FN.GestionPagos.DN.FraccionamientoDN) As FN.GestionPagos.DN.GrupoPagosFraccionadosDN
        Dim valorP1, valorPX As Double
        Dim tarifa As TarifaDN
        Dim datosTarifaV As DatosTarifaVehiculosDN
        Dim gpf As FN.GestionPagos.DN.GrupoPagosFraccionadosDN

        Using tr As New Transaccion()

            tarifa = iRecRV.Tarifa

            datosTarifaV = CType(iRecRV.Tarifa.DatosTarifa, DatosTarifaVehiculosDN)
            datosTarifaV.AsignarResultadosTarifa(iRecRV.RecuperarColOpImpuestos(), iRecRV.RecuperarColOpModulador(), iRecRV.RecuperarColOpPB(), iRecRV.RecuperarColOpSuma(), iRecRV.RecuperarColOpFraccionamiento(), iRecRV.RecuperarColOpComisiones(), iRecRV.RecuperarColOpBonificaciones())

            gpf = New FN.GestionPagos.DN.GrupoPagosFraccionadosDN()
            gpf.TipoFraccionamiento = fraccionamiento

            datosTarifaV.CalcularImportePagos(fraccionamiento.NumeroPagos, valorP1, valorPX)

            Dim colPF As New FN.GestionPagos.DN.ColPagoFraccionadoDN()
            For cont As Integer = 1 To fraccionamiento.NumeroPagos
                Dim pago As New FN.GestionPagos.DN.PagoFraccionadoDN()
                If cont = 1 Then
                    pago.Importe = valorP1
                Else
                    pago.Importe = valorPX
                End If
                pago.NumOrdenPago = cont

                colPF.Add(pago)
            Next

            gpf.ColPagoFraccionadoDN = colPF

            tr.Confirmar()

            Return gpf

        End Using


    End Function

    Public Function CalcularNivelBonificacion(ByVal valorBonificacion As Double, ByVal categoria As CategoriaDN, ByVal bonificacion As BonificacionDN, ByVal fecha As Date) As String
        Dim rvAD As FN.RiesgosVehiculos.AD.RiesgosVehiculosAD

        Using tr As New Transaccion()
            rvAD = New FN.RiesgosVehiculos.AD.RiesgosVehiculosAD()

            If bonificacion Is Nothing Then
                Dim lista As IList = Me.RecuperarLista(GetType(BonificacionDN))

                If lista IsNot Nothing Then
                    'TODO: la bonificación no debería ser nula, de momento se recupera puesto que solo existe una bonificación posible
                    bonificacion = lista.Item(0)
                End If
            End If

            CalcularNivelBonificacion = rvAD.CalcularNivelBonificacion(valorBonificacion, categoria, bonificacion, fecha)

            tr.Confirmar()

        End Using
    End Function

    'Public Function ClonarPresupuesto(ByVal presupuesto As PresupuestoDN, ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal fechaE As Date) As PresupuestoDN
    '    Dim presupuestoClonado As PresupuestoDN = Nothing
    '    Dim bLN As Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    '    Using tr As New Transaccion()
    '        If presupuesto IsNot Nothing Then
    '            'Se clona el presupuesto
    '            presupuestoClonado = presupuesto.ClonarPresupuesto()

    '            'Hay que obtener el cuestionario resueslto para la tarifa del presupuesto
    '            bLN = New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
    '            Dim colC As New Framework.Cuestionario.CuestionarioDN.ColCuestionarioDN()
    '            colC.AddRangeObject(bLN.RecuperarLista(GetType(Framework.Cuestionario.CuestionarioDN.CuestionarioDN)))
    '            Dim cuesrionarioActual As Framework.Cuestionario.CuestionarioDN.CuestionarioDN = colC.RecuperarCuestionarioxFecha(fechaE)

    '            If presupuesto.Tarifa IsNot Nothing AndAlso presupuesto.Tarifa.DatosTarifa IsNot Nothing Then
    '                Dim dtv As DatosTarifaVehiculosDN = CType(presupuestoClonado.Tarifa.DatosTarifa, DatosTarifaVehiculosDN)

    '                If dtv.HeCuestionarioResuelto IsNot Nothing AndAlso dtv.HeCuestionarioResuelto.EntidadReferida Is Nothing Then
    '                    Dim cuestRClon As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN
    '                    cuestRClon = cuestionarioR.ClonarCuestionarioRxC(cuesrionarioActual)
    '                    dtv.HeCuestionarioResuelto = New Framework.Cuestionario.CuestionarioDN.HeCuestionarioResueltoDN(cuestionarioR)

    '                End If

    '            End If

    '        End If

    '        tr.Confirmar()

    '        Return presupuestoClonado
    '    End Using
    'End Function

End Class


