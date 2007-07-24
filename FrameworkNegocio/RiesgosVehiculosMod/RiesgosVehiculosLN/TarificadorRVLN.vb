Imports Framework.LogicaNegocios.Transacciones

Public Class TarificadorRVLN

    Protected Shared mOperacionConfiguradaDN As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN



    Public Function RecuperarnuevoPosibleModeloDatosParaPoliza(ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN) As FN.RiesgosVehiculos.DN.ModeloDatosDN


        Using tr As New Transaccion


            Dim rm As RiesgosVehiculos.DN.RiesgoMotorDN = pTarifa.Riesgo
            Dim rvad As New RiesgosVehiculos.AD.RiesgosVehiculosAD
            Dim md As FN.RiesgosVehiculos.DN.ModeloDatosDN = rvad.RecuperarModeloDatos(rm.ModeloDatos.Modelo.Nombre, rm.ModeloDatos.Modelo.Marca.Nombre, rm.ModeloDatos.Matriculado, pTarifa.FEfecto)

            ' si el modelo datos recuperado es el mismo en ese caso queire decir que no hay un nuevo modelodatos  candidato para tarificar para el modelo
            If md.GUID = rm.ModeloDatos.GUID Then
                RecuperarnuevoPosibleModeloDatosParaPoliza = Nothing
            Else
                RecuperarnuevoPosibleModeloDatosParaPoliza = md
            End If


            tr.Confirmar()

        End Using


    End Function



    Private Sub TarificarComparandoTarifasConModelosMArcas(ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal pCuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal PosibleModeloDatosParaPoliza As FN.RiesgosVehiculos.DN.ModeloDatosDN, ByVal verificarProductosAplicables As Boolean)
        Dim importe1, importe2 As Double


        TarificarTarifaPrivado(pTarifa, pCuestionarioResuelto, verificarProductosAplicables)
        importe1 = pTarifa.Importe

        ' cambiar el modelodatos y prober nueva tarificacion
        Dim rm As RiesgosVehiculos.DN.RiesgoMotorDN = pTarifa.Riesgo
        Dim modelodatosPrevio As FN.RiesgosVehiculos.DN.ModeloDatosDN = rm.ModeloDatos
        rm.ModeloDatos = PosibleModeloDatosParaPoliza


        TarificarTarifaPrivado(pTarifa, pCuestionarioResuelto, verificarProductosAplicables)
        importe2 = pTarifa.Importe

        If importe2 <= importe1 Then
            ' vale el modelos de datos nuevo

        Else
            ' nos quedamos con el anterior
            rm.ModeloDatos = modelodatosPrevio
            TarificarTarifaPrivado(pTarifa, pCuestionarioResuelto, verificarProductosAplicables)

        End If

    End Sub


    Private Sub TarificarComparandoImporteConPrimaAnteriro(ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal pCuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal PosibleModeloDatosParaPoliza As FN.RiesgosVehiculos.DN.ModeloDatosDN, ByVal pCosteDiarioAnteriroPC As Double, ByVal verificarProductosAplicables As Boolean)




        ' cambiar el modelodatos y prober nueva tarificacion
        Dim rm As RiesgosVehiculos.DN.RiesgoMotorDN = pTarifa.Riesgo
        Dim modelodatosPrevio As FN.RiesgosVehiculos.DN.ModeloDatosDN = rm.ModeloDatos
        rm.ModeloDatos = PosibleModeloDatosParaPoliza


        TarificarTarifaPrivado(pTarifa, pCuestionarioResuelto, verificarProductosAplicables)

        If pTarifa.CalcualrImporteDia <= pCosteDiarioAnteriroPC Then
            ' vale el modelos de datos nuevo

        Else
            ' nos quedamos con el anterior
            rm.ModeloDatos = modelodatosPrevio
            TarificarTarifaPrivado(pTarifa, pCuestionarioResuelto, verificarProductosAplicables)

        End If

    End Sub

    Public Sub TarificarTarifa(ByVal pValorBonificacion As Double, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal pCuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal verificarProductosAplicables As Boolean)
        TarificarTarifa(pValorBonificacion, pTarifa, pCuestionarioResuelto, 0, verificarProductosAplicables)
    End Sub

    Public Sub TarificarTarifa(ByVal pValorBonificacion As Double, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal pCuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal pCosteDiarioAnteriroPC As Double, ByVal verificarProductosAplicables As Boolean)

        Using tr As New Transaccion


            pTarifa.DatosTarifa.ValorBonificacion = pValorBonificacion

            Dim PosibleModeloDatosParaPoliza As FN.RiesgosVehiculos.DN.ModeloDatosDN = RecuperarnuevoPosibleModeloDatosParaPoliza(pTarifa)

            If PosibleModeloDatosParaPoliza Is Nothing Then
                ' una sola tarificacion con el modelodatos de la poliza
                TarificarTarifaPrivado(pTarifa, pCuestionarioResuelto, verificarProductosAplicables)

            Else
                ' tarificar con el modelodatos que tenga el menor coeficiente
                ' modo A
                TarificarComparandoTarifasConModelosMArcas(pTarifa, pCuestionarioResuelto, PosibleModeloDatosParaPoliza, verificarProductosAplicables)


                ' activar si se decide que vale con compara con el precio anteriro
                'Modo B
                'If pCosteDiarioAnteriroPC > 0 Then
                '    TarificarComparandoImporteConPrimaAnteriro(pTarifa, pCuestionarioResuelto, PosibleModeloDatosParaPoliza, pCosteDiarioAnteriroPC)
                'End If


            End If

            tr.Confirmar()

        End Using

    End Sub





    Private Sub TarificarTarifaPrivado(ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal pCuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal pVerificarProductosAplicables As Boolean)

        Using tr As New Transaccion


            Dim rvm As RiesgosVehiculos.DN.RiesgoMotorDN = pTarifa.Riesgo


            Dim irec As FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN
            irec = New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RVIRecSumiValorLN

            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN

            'cargar los datos al irecuperador de valor
            irec.Tarifa = pTarifa
            irec.DataSoucers.Add(irec.Tarifa)
            irec.DataSoucers.Add(pCuestionarioResuelto)
            irec.DataSoucers.Add(rvm.ModeloDatos)


            ' verificar los productos alcanzables
            If pVerificarProductosAplicables Then
                VerificarProductosAplicables(pTarifa)
            End If



            ' asiganar el recuperador de valor a la operacion principal del grafo
            Dim opc As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN = RecuperarGrafo(pTarifa.FEfecto)
            opc.IOperacionDN.IRecSumiValorLN = irec

            ' solicitar la tarificacion
            Dim datosTa As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = pTarifa.DatosTarifa
            pTarifa.Importe = opc.IOperacionDN.GetValor()

            ' cargar los datos cache generados en la tarificacion
            datosTa.ColOperacionImpuestoRVCache.AddRangeObject(irec.RecuperarResultados(GetType(FN.RiesgosVehiculos.DN.OperacionImpuestoRVCacheDN)))
            datosTa.ColOperacionSumaRVCache.AddRangeObject(irec.RecuperarResultados(GetType(FN.RiesgosVehiculos.DN.OperacionSumaRVCacheDN)))
            datosTa.ColOperacionPrimaBaseRVCache.AddRangeObject(irec.RecuperarResultados(GetType(FN.RiesgosVehiculos.DN.OperacionPrimaBaseRVCacheDN)))
            datosTa.ColOperacionModuladorRVCache.AddRangeObject(irec.RecuperarResultados(GetType(FN.RiesgosVehiculos.DN.OperacionModuladorRVCacheDN)))
            datosTa.HeCuestionarioResuelto = New Framework.Cuestionario.CuestionarioDN.HeCuestionarioResueltoDN(pCuestionarioResuelto)

            tr.Confirmar()

        End Using

    End Sub
    Public Shared Sub VerificarProductosAplicables(ByVal tarifa As FN.Seguros.Polizas.DN.TarifaDN)

        Using tr As New Transaccion

            ' verificar los productos alcanzables

            Dim dt As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = tarifa.DatosTarifa
            Dim docln As New Framework.Ficheros.FicherosLN.CajonDocumentoLN
            ' recuperar col cajonesdocumentos
            Dim colCdVinculadosaProductos As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN = docln.RecuperarCajonesParaEntidadReferida(tarifa.GUID)
            dt.ActualizarProdutosAplicables(colCdVinculadosaProductos)




            tr.Confirmar()

        End Using



    End Sub
    Public Function RecuperarGrafo(ByVal pFechaEfecto As Date) As Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN
        'TODO: implementar un control de vigencia del grafo
        If mOperacionConfiguradaDN Is Nothing Then
            Dim ln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            mOperacionConfiguradaDN = ln.RecuperarGenerico("1", GetType(Framework.Operaciones.OperacionesDN.OperacionConfiguradaDN)) 'TODO: provisional esto tine que venir suministrado por el tarificador

        End If
        Return mOperacionConfiguradaDN
    End Function

End Class
