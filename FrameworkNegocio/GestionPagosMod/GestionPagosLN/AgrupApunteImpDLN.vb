


Imports Framework.LogicaNegocios.Transacciones


Public Class AgrupApunteImpDLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN

    Public Function CargarAgrupacionID(ByVal ag As FN.GestionPagos.DN.AgrupApunteImpDDN) As FN.GestionPagos.DN.AgrupApunteImpDDN


        Using tr As New Transaccion



            If ag.IImporteDebidoDN.Acreedora Is Nothing Then
                ag.IImporteDebidoDN.Acreedora = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.GestionPagos.DN.EntidadFiscalGenericaPrincipal).FullName)
            End If

            If ag.IImporteDebidoDN.Deudora Is Nothing Then
                ag.IImporteDebidoDN.Deudora = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.GestionPagos.DN.EntidadFiscalGenericaPrincipal).FullName)

            End If


            Dim miAgrupApunteImpDAD As New FN.GestionPagos.AD.AgrupApunteImpDAD
            ag.ColApunteImpDDN.AddRange(miAgrupApunteImpDAD.RecuperarApunteImpDebidoLibres(ag))
            ag.Actualizar()


            CargarAgrupacionID = ag

            tr.Confirmar()

        End Using





    End Function

    'Public Function CrearAgrupacionID(ByVal param As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN) As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN


    '    Using tr As New Transaccion



    '        If param.Acreedora Is Nothing Then
    '            param.Acreedora = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.GestionPagos.DN.EntidadFiscalGenericaPrincipal).FullName)
    '        End If

    '        If param.Deudora Is Nothing Then
    '            param.Deudora = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.GestionPagos.DN.EntidadFiscalGenericaPrincipal).FullName)

    '        End If

    '        Dim ag As New FN.GestionPagos.DN.AgrupApunteImpDDN
    '        ag.PermiteCompensar = param.PermiteCompensar
    '        ag.IImporteDebidoDN.Acreedora = param.Acreedora
    '        ag.IImporteDebidoDN.Deudora = param.Deudora

    '        Dim miAgrupApunteImpDAD As New FN.GestionPagos.AD.AgrupApunteImpDAD
    '        ag.ColApunteImpDDN.AddRange(miAgrupApunteImpDAD.RecuperarApunteImpDebidoLibres(param))
    '        ag.Actualizar()
    '        param.Agrupacion = ag

    '        CrearAgrupacionID = param

    '        tr.Confirmar()

    '    End Using





    'End Function


    Public Function BuscarImportesDebidosLibres(ByVal param As FN.GestionPagos.DN.ParEntFiscalGenericaParamDN) As DataSet


        Using tr As New Transaccion

            Dim miAgrupApunteImpDAD As New FN.GestionPagos.AD.AgrupApunteImpDAD
            BuscarImportesDebidosLibres = miAgrupApunteImpDAD.BuscarImportesDebidosLibres(param)

            tr.Confirmar()

        End Using





    End Function





    ''' <summary>
    ''' los id no pueden formar parte de otra agrupacion ni estar anulados, o modificarse tras al agrupacion
    ''' para poder modificar un id primero es necesario desagruparlo
    ''' </summary>
    ''' <param name="ColApunteImpDaAgrupar"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function CrearAgrupacion(ByVal ColApunteImpDaAgrupar As FN.GestionPagos.DN.ColApunteImpDDN) As FN.GestionPagos.DN.AgrupApunteImpDDN

        Using tr As New Transaccion

            Dim ag As New FN.GestionPagos.DN.AgrupApunteImpDDN

            ag.ColApunteImpDDN.AddRangeObjectUnico(ColApunteImpDaAgrupar)
            ag.Actualizar()
            Me.Guardar(Of FN.GestionPagos.DN.AgrupApunteImpDDN)(ag)

            CrearAgrupacion = ag

            tr.Confirmar()

        End Using

    End Function


    Public Function GuardarAgrupacion(ByVal pAgrupApunteImpD As FN.GestionPagos.DN.AgrupApunteImpDDN) As FN.GestionPagos.DN.AgrupApunteImpDDN

        Using tr As New Transaccion


            ' verificaciones de integridad
            ' es necesario dado que si se han eliminado importes debidos se les habrá eliminado el campo de guidAGRUPACION
            ' y deben ser guardados para que en la base de datos tambien se relaice la modificacion


            Dim aidEliminados As New FN.GestionPagos.DN.ColApunteImpDDN

            If pAgrupApunteImpD.EliminadoAlgunApunteID AndAlso Not String.IsNullOrEmpty(pAgrupApunteImpD.ID) Then

                ' recuperamos el original de la base de datos
                Dim AgrupApunteImpDbd As FN.GestionPagos.DN.AgrupApunteImpDDN = Me.Recuperar(Of FN.GestionPagos.DN.AgrupApunteImpDDN)(pAgrupApunteImpD.ID)

                ' recuperamos la col de elemetos eliminados
                aidEliminados.AddRangeObject(AgrupApunteImpDbd.ColApunteImpDDN.DiferenciaA(pAgrupApunteImpD.ColApunteImpDDN.ToListOFt, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos))

            End If


            Me.Guardar(Of FN.GestionPagos.DN.AgrupApunteImpDDN)(pAgrupApunteImpD)
            If aidEliminados.Count > 0 Then
                ' eliminar las referencias a al agrupacion
                For Each id As FN.GestionPagos.DN.ApunteImpDDN In aidEliminados
                    id.GUIDAgrupacion = Nothing
                Next

                Me.GuardarGenerico(aidEliminados)
            End If


            GuardarAgrupacion = pAgrupApunteImpD

            tr.Confirmar()

        End Using

    End Function

    ''' <summary>
    ''' desvincula los importes debidos vinculados y anula tanto la agrupacion como su importe debido producto
    ''' 
    ''' no puede realizarse si el importe debido producto esta referido porpagos no anulados, o por agrupaciones
    ''' </summary>
    ''' <param name="pAgrupApunteImpD"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function AnularAgrupacion(ByVal pAgrupApunteImpD As FN.GestionPagos.DN.AgrupApunteImpDDN, ByVal pFAnulacion As Date) As FN.GestionPagos.DN.AgrupApunteImpDDN

        Using tr As New Transaccion

            Dim mensaje As String


            ' verificar si por si mismos es anulable (forma parte de una agrupacion)
            If Not pAgrupApunteImpD.Anulable(mensaje) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
            End If

            '' verificar que no esta referido por pagos (se tiene en cuenta en la anulacion del importe debido)
            'Dim pgln As New FN.GestionPagos.LN.PagosLN
            'Dim colpago As FN.GestionPagos.DN.ColPagoDN = pgln.RecuperarGUIDImporteDebidoOrigen(pAgrupApunteImpD.IImporteDebidoDN.GUID)
            '' si hay pagos todos debieran estar anulados
            'For Each pago As FN.GestionPagos.DN.PagoDN In colpago
            '    If pago.FechaAnulacion <> Date.MinValue Then
            '        Throw New Framework.LogicaNegocios.ApplicationExceptionLN("La agrupacion de id " & pAgrupApunteImpD.ID & " no puede ser anulada dado que dispone de almenos un pago activo vinculado a su importe debido idpago:" & pago.ID)
            '    End If
            'Next




            ' se puede realizar la anulacion


            'modificar la ag liverando todos los aid agrupagos

            Dim col As FN.GestionPagos.DN.ColApunteImpDDN = pAgrupApunteImpD.EliminarAIDReferidos()
            pAgrupApunteImpD.Actualizar()

            ' anular el importedebido producto
            Dim mlq As New MotorLiquidacionLN
            mlq.AnularOrigenImpDeb(pAgrupApunteImpD, pFAnulacion)



            Me.GuardarGenerico(col)




            AnularAgrupacion = pAgrupApunteImpD

            tr.Confirmar()

        End Using

    End Function

End Class
