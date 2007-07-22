Imports Framework.LogicaNegocios.Transacciones

Imports FN.GestionPagos.DN

Public Class PagosLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN

#Region "Métodos"

    Public Function RecuperarPagosActivosGUIDsOrigenImporteDebidoOrigen(ByVal colGUID As List(Of String), ByVal pImporteDebidoAnulado As Boolean) As ColPagoDN

        Using tr As New Transaccion

            Dim ad As New GestionPagos.AD.GestionPagosAD

            RecuperarPagosActivosGUIDsOrigenImporteDebidoOrigen = ad.RecuperarPagosActivosGUIDsOrigenImporteDebidoOrigen(colGUID, pImporteDebidoAnulado)

            tr.Confirmar()

        End Using


    End Function



    Public Function RecuperarGUIDImporteDebidoOrigen(ByVal pGUIDImporteDebido As String) As FN.GestionPagos.DN.ColPagoDN




        Using tr As New Transaccion

            Dim ad As New GestionPagos.AD.GestionPagosAD

            RecuperarGUIDImporteDebidoOrigen = ad.RecuperarGUIDImporteDebidoOrigen(pGUIDImporteDebido)

            tr.Confirmar()

        End Using






    End Function





    Public Function RecuperarLimitePago() As FN.GestionPagos.DN.LimitePagoDN
        Using tr As New Transaccion()

            Dim lista As IList

            Dim dbln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            lista = dbln.RecuperarLista(GetType(FN.GestionPagos.DN.LimitePagoDN))

            ' recupera el de menor limite de validacion si hubiera mas de uno
            'For Each objeto As Object In lista
            '    Dim limitep As FN.GestionPagos.DN.LimitePagoDN
            '    limitep.LimiteValidacion

            'Next

            If lista.Count <> 1 Then
                Throw New ApplicationException("Error de integridad en la base de datos Número de LimitePagoDN:" & lista.Count)
            End If

            RecuperarLimitePago = lista.Item(0)
            tr.Confirmar()


        End Using
    End Function

    Public Function AltaModProveedor(ByVal cuentaContable As String, ByVal NombreEmpresa As String, ByVal Domicilio As String, ByVal idFiscal As String, ByVal codp As String, ByVal localidad As String, ByVal provincia As String, ByVal telefono As String, ByRef id As String, ByRef Mensaje As String) As Boolean

        Using tr As New Transaccion(True)
            Mensaje = ""

            If String.IsNullOrEmpty(idFiscal) Then
                Mensaje = "el identificador fiscal no puede ser nulo"
                tr.Confirmar()
                Return False
            End If

            Dim EsCif As Boolean = FN.Localizaciones.DN.CifDN.ValidaCif(idFiscal, Mensaje)

            If Not EsCif AndAlso Not FN.Localizaciones.DN.NifDN.ValidaNif(idFiscal, Mensaje) Then
                Mensaje = "el identificador fiscal no es ni un Cif ni un Nif Valido: " & idFiscal
                tr.Confirmar()
                Return False
            End If

            Dim dir As New FN.Localizaciones.DN.DireccionNoUnicaDN

            If Not String.IsNullOrEmpty(codp) Then
                Dim objLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
                Dim lista As IList
                lista = objLN.RecuperarListaCondicional(GetType(FN.Localizaciones.DN.LocalidadDN), New Framework.AccesoDatos.MotorAD.AD.ConstructorBusquedaCampoStringAD("vwLocalidadesxCodPostal", "CodPostal", Trim(codp)))

                If lista IsNot Nothing AndAlso lista.Count > 0 Then
                    If lista.Count = 1 Then
                        dir.Localidad = lista.Item(0)
                    Else
                        For Each loc As FN.Localizaciones.DN.LocalidadDN In lista
                            If loc.Nombre = localidad Then
                                dir.Localidad = loc
                                Exit For
                            End If
                        Next
                    End If
                End If

                If dir.Localidad Is Nothing Then
                    Mensaje = "No se ha podido recuperar una localidad válida para el proveedor"
                    tr.Confirmar()
                    Return False
                End If

            End If

            dir.Via = Domicilio
            dir.CodPostal = Trim(codp)

            Dim edn As Framework.DatosNegocio.IDatoPersistenteDN

            If EsCif Then
                ' se recupera la empresa fiscal por cif
                Dim empresa As Empresas.DN.EmpresaDN = Nothing
                Dim empf As FN.Empresas.DN.EmpresaFiscalDN = Nothing

                Dim empLN As New FN.Empresas.LN.EmpresaLN()
                empf = empLN.RecuperarEmpresaFiscalxCIF(idFiscal)

                If empf Is Nothing Then
                    Mensaje = "Alta Empresa"
                    empf = New FN.Empresas.DN.EmpresaFiscalDN()
                    empf.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN(idFiscal)
                    empresa = New Empresas.DN.EmpresaDN()
                    empresa.EntidadFiscal = empf.EntidadFiscalGenerica
                Else
                    Mensaje = "Modificación Empresa"
                End If

                empf.NombreComercial = NombreEmpresa
                empf.RazonSocial = NombreEmpresa
                empf.DomicilioFiscal = dir

                If empresa Is Nothing Then
                    edn = empf
                Else
                    edn = empresa
                End If


            Else
                ' se recupera la persona fiscal por nif 
                Dim autonomo As FN.Personas.DN.PersonaFiscalDN = Nothing

                Dim persLN As New FN.Personas.LN.PersonaLN()
                autonomo = persLN.RecuperarPersonaFiscalxNIF(idFiscal)

                If autonomo Is Nothing Then
                    Mensaje = "Alta Persona"
                    autonomo = New FN.Personas.DN.PersonaFiscalDN()
                    autonomo.Persona = New FN.Personas.DN.PersonaDN()
                    autonomo.IdentificacionFiscal = New FN.Localizaciones.DN.NifDN(idFiscal)
                Else
                    Mensaje = "Modificación Persona"
                End If

                autonomo.NombreComercial = NombreEmpresa
                autonomo.Persona.Nombre = NombreEmpresa
                autonomo.DomicilioFiscal = dir

                edn = autonomo

            End If

            If Not edn.EstadoIntegridad(Mensaje) = Framework.DatosNegocio.EstadoIntegridadDN.Consistente Then
                tr.Confirmar()
                Return False
            End If

            Try
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

                gi.Guardar(edn)

                tr.Confirmar()
                Return True

            Catch ex As Exception
                Mensaje = ex.Message
                tr.Confirmar()
                Return False
            End Try

            tr.Confirmar()

        End Using


    End Function

    Public Function AltaModificacionProveedores(ByVal pDts As GestionPagos.DN.dtsGestionPagos) As Data.DataSet

        Dim resultado As Boolean
        Dim id, mensaje As String

        ' recuperar las localizaciones
        'Dim collocs As New FN.Localizaciones.DN.ColLocalidadDN
        'collocs.AddRangeObject(Me.RecuperarLista(Of FN.Localizaciones.DN.LocalidadDN)())

        ' intanciar el dts de respuesta
        Dim dtsRespuesta As New Data.DataSet
        Dim ttb As New DataTable("Resultados")

        dtsRespuesta.Tables.Add(ttb)

        ttb.Columns.Add(New DataColumn("CuentaContable"))
        ttb.Columns.Add(New DataColumn("idFiscal"))
        ttb.Columns.Add(New DataColumn("idSistema"))
        ttb.Columns.Add(New DataColumn("Resultado"))
        ttb.Columns.Add(New DataColumn("Mensaje"))



        For Each fila As GestionPagos.DN.dtsGestionPagos.EntidadesFiscalesRow In pDts.EntidadesFiscales.Rows
            resultado = AltaModProveedor(fila.CuentaContable, fila.NombreEmpresa, fila.Domicilio, fila.idFiscal, fila.codp, fila.Localidad, fila.Provincia, fila.Telefono, id, mensaje)

            Dim valoras(4) As Object
            valoras(0) = fila.CuentaContable
            valoras(1) = fila.idFiscal
            valoras(2) = id
            valoras(3) = resultado
            valoras(4) = mensaje
            ttb.Rows.Add(valoras)

        Next

        Return dtsRespuesta

    End Function

    Public Function AltaModPago(ByVal importe As Single, ByVal codOrigen As String, ByVal tipoEntOrigen As FN.GestionPagos.DN.TipoEntidadOrigenDN, ByVal nombreBeneficiario As String, ByVal idFiscal As String, ByRef mensaje As String, ByVal actor As Framework.Usuarios.DN.PrincipalDN, ByVal operacion As Framework.Procesos.ProcesosDN.OperacionDN) As Boolean

        Using tr As New Transaccion(True)
            mensaje = ""

            If String.IsNullOrEmpty(idFiscal) Then
                mensaje = "El identificador fiscal no puede ser nulo"
                tr.Confirmar()
                Return False
            End If

            Dim EsCif As Boolean = FN.Localizaciones.DN.CifDN.ValidaCif(idFiscal, mensaje)

            If Not EsCif AndAlso Not FN.Localizaciones.DN.NifDN.ValidaNif(idFiscal, mensaje) Then
                mensaje = "el identificador fiscal no es ni un Cif ni un Nif Valido:" & idFiscal
                tr.Confirmar()
                Return False
            End If

            ' Origen del pago
            Dim origenPago As FN.GestionPagos.DN.OrigenDN = Nothing

            If tipoEntOrigen IsNot Nothing Then
                origenPago = RecuperarOrigenxIdEntidadyTipo(codOrigen, tipoEntOrigen.ID)
            End If

            If origenPago Is Nothing Then
                origenPago = New FN.GestionPagos.DN.OrigenDN()
                origenPago.TipoEntidadOrigen = tipoEntOrigen
            End If

            origenPago.IDEntidad = codOrigen

            ' Beneficiario del pago
            Dim beneficiario As FN.Localizaciones.DN.IEntidadFiscalDN = Nothing

            If EsCif Then
                ' se recupera la empresa fiscal por cif
                Dim empresa As Empresas.DN.EmpresaDN = Nothing
                Dim empf As FN.Empresas.DN.EmpresaFiscalDN = Nothing

                Dim empLN As New FN.Empresas.LN.EmpresaLN()
                empf = empLN.RecuperarEmpresaFiscalxCIF(idFiscal)

                If empf Is Nothing Then
                    empf = New FN.Empresas.DN.EmpresaFiscalDN()
                    empf.IdentificacionFiscal = New FN.Localizaciones.DN.CifDN(idFiscal)
                    empresa = New Empresas.DN.EmpresaDN()
                    empresa.EntidadFiscal = empf.EntidadFiscalGenerica
                End If

                empf.NombreComercial = nombreBeneficiario
                empf.RazonSocial = nombreBeneficiario

                If empresa IsNot Nothing Then
                    Try
                        Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                        gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Nothing, Recurso.Actual)

                        gi.Guardar(empresa)
                    Catch ex As Exception
                        mensaje = "Los datos de la empresa no son válidos: " & ex.Message
                        tr.Confirmar()
                        Return False
                    End Try

                End If

                beneficiario = empf

            Else
                ' se recupera la persona fiscal por nif 
                Dim personaf As FN.Personas.DN.PersonaFiscalDN = Nothing

                Dim persLN As New FN.Personas.LN.PersonaLN()
                personaf = persLN.RecuperarPersonaFiscalxNIF(idFiscal)

                If personaf Is Nothing Then
                    personaf = New FN.Personas.DN.PersonaFiscalDN()
                    personaf.Persona = New FN.Personas.DN.PersonaDN()
                    personaf.IdentificacionFiscal = New FN.Localizaciones.DN.NifDN(idFiscal)
                End If

                If Not nombreBeneficiario.Contains(",") Then
                    mensaje = "No puede cargarse una persona sin nombre y apellidos válidos"
                    tr.Confirmar()
                    Return False
                End If

                personaf.Persona.Nombre = nombreBeneficiario.Split(",")(1)
                personaf.Persona.Apellido = nombreBeneficiario.Split(",")(0)

                beneficiario = personaf

            End If

            ' si se recuperar un pago para el mismo destinatario y el mismo origen, no se genera el pago
            If Not String.IsNullOrEmpty(origenPago.ID) AndAlso Not String.IsNullOrEmpty(beneficiario.ID) AndAlso RecuperarPagoxOrigenxDestinatario(origenPago, beneficiario).Count > 0 Then
                mensaje = "Ya existe un pago para este origen, con el mismo beneficiario"
                tr.Confirmar()
                Return False
            End If

            Dim edn As Framework.DatosNegocio.IDatoPersistenteDN

            ' Pago
            Dim pago As New FN.GestionPagos.DN.PagoDN()
            pago.Origen = origenPago
            pago.Importe = importe
            pago.Destinatario = beneficiario
            pago.FechaProgramadaEmision = New Date(Now().Year, Now().Month, Now().Day)

            edn = pago

            If Not edn.EstadoIntegridad(mensaje) = Framework.DatosNegocio.EstadoIntegridadDN.Consistente Then
                tr.Confirmar()
                Return False
            End If

            Try

                Dim prln As New Framework.Procesos.ProcesosLN.OperacionesLN()
                Dim colTrnI As New Framework.Procesos.ProcesosDN.ColTransicionDN()
                Dim trR As New Framework.Procesos.ProcesosDN.TransicionRealizadaDN()

                colTrnI = prln.RecuperarTransicionesDeInicio(GetType(PagoDN))

                For Each trn As Framework.Procesos.ProcesosDN.TransicionDN In colTrnI
                    If trn.OperacionDestino.EsIgualRapido(operacion) Then
                        trR.Transicion = trn
                        Exit For
                    End If
                Next

                Dim operacionR As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN()
                operacionR.Operacion = operacion

                Dim goprLN As New Framework.Procesos.ProcesosLN.GestorOPRLN()
                goprLN.EjecutarOperacion(edn, Nothing, actor, trR)
                'goprLN.EjecutarOperacion(edn, actor, operacionR)

                'Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                'gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

                'gi.Guardar(edn)

                mensaje = operacion.Nombre & ": " & tipoEntOrigen.Nombre

                tr.Confirmar()
                Return True

            Catch ex As Exception
                mensaje = ex.Message
                tr.Confirmar()
                Return False
            End Try

        End Using

    End Function

    Public Function CargarPagos(ByVal pDts As GestionPagos.DN.dtsGestionPagos, ByVal actor As Framework.Usuarios.DN.PrincipalDN, ByVal tipoOrigen As FN.GestionPagos.DN.TipoEntidadOrigenDN, ByVal operacion As Framework.Procesos.ProcesosDN.OperacionDN) As Data.DataSet

        Dim resultado As Boolean
        Dim mensaje As String = ""


        ' instanciar el dts de respuesta
        Dim dtsRespuesta As New Data.DataSet
        Dim ttb As New DataTable("Resultados")

        dtsRespuesta.Tables.Add(ttb)
        ' *******************************************************************************************************
        ' crear un dts tipado de respuesta que no debe estar en las dn sino en las ln porque son parte del proceso
        ttb.Columns.Add(New DataColumn("TipoOrigen"))
        ttb.Columns.Add(New DataColumn("idOrigen"))
        ttb.Columns.Add(New DataColumn("Importe"))
        ttb.Columns.Add(New DataColumn("NombreBeneficiario"))
        ttb.Columns.Add(New DataColumn("idFiscalBeneficiario"))
        ttb.Columns.Add(New DataColumn("Resultado"))
        ttb.Columns.Add(New DataColumn("Mensaje"))



        For Each fila As GestionPagos.DN.dtsGestionPagos.PagosConChequeRow In pDts.PagosConCheque.Rows
            resultado = AltaModPago(fila.Importe, fila.CodSiniestro, tipoOrigen, fila.NombreBeneficiario, fila.CifBeneficiario, mensaje, actor, operacion)

            Dim valores(6) As Object
            valores(0) = "Siniestro"
            valores(1) = fila.CodSiniestro
            valores(2) = fila.Importe
            valores(3) = fila.NombreBeneficiario
            valores(4) = fila.CifBeneficiario
            valores(5) = resultado
            valores(6) = mensaje

            ttb.Rows.Add(valores)

        Next

        Return dtsRespuesta

    End Function

    Public Function GuardarTalonDN(ByVal pTalon As FN.GestionPagos.DN.TalonDN) As FN.GestionPagos.DN.TalonDN
        Using tr As New Transaccion()

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            gi.Guardar(pTalon)

            tr.Confirmar()

            Return pTalon

        End Using
    End Function

    Public Function GuardarTalonDoc(ByVal pTdoc As FN.GestionPagos.DN.TalonDocumentoDN) As FN.GestionPagos.DN.TalonDocumentoDN
        Using tr As New Transaccion()

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            gi.Guardar(pTdoc)

            tr.Confirmar()

            Return pTdoc

        End Using
    End Function

    Public Function GuardarConfiguracionImpresionTalon(ByVal pci As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN) As FN.GestionPagos.DN.ConfiguracionImpresionTalonDN
        Using tr As New Transaccion()

            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)

            gi.Guardar(pci)

            tr.Confirmar()

            Return pci

        End Using
    End Function

    Public Function GuardarPlantillaCarta(ByVal pPlantillaCarta As FN.GestionPagos.DN.PlantillaCartaDN) As FN.GestionPagos.DN.PlantillaCartaDN

        Using tr As New Transaccion()
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN

            'TODO: Se fuerza que la plantilla esté modificada para guardar. Revisar, parece que huellaRTF no
            'informa de que el objeto ha sido modificado
            If pPlantillaCarta.HuellaRTF IsNot Nothing AndAlso pPlantillaCarta.HuellaRTF.EntidadReferida IsNot Nothing AndAlso Not pPlantillaCarta.HuellaRTF.IdEntidadReferida = 0 Then
                Dim cRTF As FN.GestionPagos.DN.ContenedorRTFDN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                cRTF = gi.Recuperar(Of FN.GestionPagos.DN.ContenedorRTFDN)(pPlantillaCarta.HuellaRTF.IdEntidadReferida)

                cRTF.ArrayString = CType(pPlantillaCarta.HuellaRTF.EntidadReferida, FN.GestionPagos.DN.ContenedorRTFDN).ArrayString

                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                gi.Guardar(cRTF)
            End If

            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            gi.Guardar(pPlantillaCarta)

            tr.Confirmar()

            Return pPlantillaCarta

        End Using

    End Function

    Public Sub ValidarPagoAsignado(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal sender As Object)
        Dim pago As FN.GestionPagos.DN.PagoDN

        Using tr As New Transaccion

            pago = pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion

            ' precondiciones
            If pago.Estado <> Framework.DatosNegocio.EstadoDatosDN.SinModificar Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No es posible modificar el pago en el estado actual del Pago")
            End If

            If pago.CuentaOrigenPago Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No es posible validar un pago que no tiene una cuenta de origen asociada")
            End If

            If pago.Talon Is Nothing AndAlso pago.Transferencia Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No es posible validar un pago que no tiene una modalidad de pago talón o tranferencia asociada")
            End If


            If Not pago.Talon Is Nothing AndAlso Not pago.Transferencia Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No es posible validar un pago que tiene ambas  modalidades de pago talón y tranferencia asociada")
            End If



            ' verificar las condiciones de limite
            Dim limitePago As FN.GestionPagos.DN.LimitePagoDN = RecuperarLimitePago()

            Dim procLN As Framework.Procesos.ProcesosLN.GestorEjecutoresLN
            procLN = New Framework.Procesos.ProcesosLN.GestorEjecutoresLN()
            procLN.GuardarGenerico(pago, pTransicionRealizada, Nothing)


            If pago.Importe > limitePago.LimiteAviso Then
                ' generar una aviso y guardarlo tras guardar el pago

                Dim notific As New FN.GestionPagos.DN.NotificacionPagoDN(Nothing, pago, DN.OrigenNotificacion.Automatica, "Aviso importe elevado:" & pago.Importe, Nothing)
                Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                gi.Guardar(notific)
            End If

            'Se valida si existe más de un pago para el mismo origen, y el mismo destinatario
            If pago.Origen IsNot Nothing Then
                Dim colP As FN.GestionPagos.DN.ColPagoDN
                colP = RecuperarPagoxOrigenxDestinatario(pago.Origen, pago.Destinatario)
                If colP IsNot Nothing AndAlso colP.Count > 1 Then
                    Dim notificOD As New FN.GestionPagos.DN.NotificacionPagoDN(Nothing, pago, DN.OrigenNotificacion.Automatica, "Aviso número de pagos para el mismo origen y destinatario: " & colP.Count, Nothing)
                    Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
                    gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
                    gi.Guardar(notificOD)
                End If
            End If

            tr.Confirmar()

        End Using
    End Sub

    Public Function RecuperarTalonDN(ByVal pID As String) As GestionPagos.DN.TalonDN
        Using tr As New Transaccion()
            Dim gi As Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN
            gi = New Framework.AccesoDatos.MotorAD.LN.GestorInstanciacionLN(Transaccion.Actual, Recurso.Actual)
            RecuperarTalonDN = gi.Recuperar(Of GestionPagos.DN.TalonDN)(pID)

            tr.Confirmar()
        End Using
    End Function

    Public Function RecuperarTipoOrigenxNombre(ByVal tipoOrigen As String) As FN.GestionPagos.DN.TipoEntidadOrigenDN
        Dim pgAD As FN.GestionPagos.AD.GestionPagosAD

        Using tr As New Transaccion()
            pgAD = New FN.GestionPagos.AD.GestionPagosAD()

            RecuperarTipoOrigenxNombre = pgAD.RecuperarTipoOrigenxNombre(tipoOrigen)

            tr.Confirmar()

        End Using
    End Function

    Public Function RecuperarOrigenxIdEntidadyTipo(ByVal idEntOrigen As String, ByVal idTipoEntOrigen As String) As FN.GestionPagos.DN.OrigenDN
        Dim pgAD As FN.GestionPagos.AD.GestionPagosAD

        Using tr As New Transaccion()
            pgAD = New FN.GestionPagos.AD.GestionPagosAD()

            RecuperarOrigenxIdEntidadyTipo = pgAD.RecuperarOrigenxIdEntidadyTipo(idEntOrigen, idTipoEntOrigen)

            tr.Confirmar()

        End Using
    End Function

    Public Function RecuperarPagoxOrigenxDestinatario(ByVal origen As FN.GestionPagos.DN.OrigenDN, ByVal destinatario As FN.Localizaciones.DN.IEntidadFiscalDN) As FN.GestionPagos.DN.ColPagoDN
        Dim pgAD As FN.GestionPagos.AD.GestionPagosAD

        Using tr As New Transaccion()
            pgAD = New FN.GestionPagos.AD.GestionPagosAD()

            RecuperarPagoxOrigenxDestinatario = pgAD.RecuperarPagoxOrigenxDestinatario(origen, destinatario)

            tr.Confirmar()

        End Using
    End Function

    Public Sub FirmarPagoAsignado(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal sender As Object)
        Dim pago As FN.GestionPagos.DN.PagoDN
        Dim actor As Framework.Usuarios.DN.PrincipalDN
        Dim entidadU As Framework.DatosNegocio.IEntidadDN = Nothing

        Using tr As New Transaccion

            pago = pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion
            actor = pTransicionRealizada.OperacionRealizadaDestino.SujetoOperacion

            If actor Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Para firmar el pago es necesario un usuario válido")
            End If

            ' verificar las condiciones de limite
            '1º Se recupera la colección de permisos del usuario
            Dim colPermisosEmp As Framework.Usuarios.DN.ColPermisoDN
            colPermisosEmp = actor.ColPermisos

            '2º Se recupera el permiso para el límite de firma del pago
            Dim colPermisoLF As Framework.Usuarios.DN.ColPermisoDN = Nothing
            If colPermisosEmp IsNot Nothing AndAlso colPermisosEmp.Count > 0 Then
                colPermisoLF = colPermisosEmp.RecuperarPermisoxTipo("LimiteFirmaPago")
            End If

            '3º Se comprueba el que el importe del pago esté por debajo del límite para la firma del pago
            If colPermisoLF Is Nothing OrElse colPermisoLF.Count = 0 Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Para firmar el pago es necesario un permiso para el límite de pago")
            End If

            '4º Se comprueban las condiciones del límite de pago recuperado
            Dim limitePago As Single
            For Each perm As Framework.Usuarios.DN.PermisoDN In colPermisoLF
                Dim limitePagoAux As Single
                If perm.EsRef OrElse Not Single.TryParse(perm.DatoVal, limitePagoAux) Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El permiso recuperado para el límite de pago no es válido")
                End If
                If limitePagoAux > limitePago Then
                    limitePago = limitePagoAux
                End If
            Next


            ' Esta validación se realiza en el proceso de firma
            If pago.Importe > limitePago Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("el importe del pago" & pago.Importe & " supera el límite que puede validar el usuario")
            End If

            tr.Confirmar()

        End Using
    End Sub

    Public Function RecuperarFicherosTransferenciasActivos() As FN.GestionPagos.DN.ColFicheroTransferenciaDN
        Dim pgAD As FN.GestionPagos.AD.GestionPagosAD

        Using tr As New Transaccion()
            pgAD = New FN.GestionPagos.AD.GestionPagosAD()

            RecuperarFicherosTransferenciasActivos = pgAD.RecuperarFicherosTransferenciasActivos()

            tr.Confirmar()

        End Using

    End Function

    Public Sub AdjuntarPagoFT(ByVal objeto As Object, ByVal pTransicionRealizada As Framework.Procesos.ProcesosDN.TransicionRealizadaDN, ByVal sender As Object)
        Dim pago As PagoDN
        Dim ft As FicheroTransferenciaDN

        Using tr As New Transaccion()
            '1º Se obtiene el pago a partir de la transición, y el fichero de transferencias
            pago = pTransicionRealizada.OperacionRealizadaDestino.ObjetoIndirectoOperacion

            Dim baseLN As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN()
            ft = baseLN.Recuperar(GetType(FicheroTransferenciaDN), pago.IdFicheroTransferencia)

            '2º se comprueba que el pago no se encuentre asignado a otro fichero de transferencias
            'TODO: No debería lanzar una excepción
            Dim miAD As New FN.GestionPagos.AD.GestionPagosAD()
            If miAD.RecuperarFicheroTransferenciasxPago(pago) IsNot Nothing OrElse ft.ColPagos.Contiene(pago, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El pago ya está asociado a un fichero de transferencias")
            End If

            '3º se agrega el pago a la colección de pagos del FT
            ft.ColPagos.Add(pago)

            '4º se guarda el fichero de transferencias
            Me.Guardar(Of FicheroTransferenciaDN)(ft)

            tr.Confirmar()

        End Using

    End Sub

#End Region


End Class
