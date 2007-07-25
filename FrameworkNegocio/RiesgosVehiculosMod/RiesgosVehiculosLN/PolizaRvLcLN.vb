
Imports FN.RiesgosVehiculos.DN

Imports Framework.LogicaNegocios.Transacciones
Public Class PolizaRvLcLN
    'Inherits FN.GestionPagos.LN.LiquidadorConcretoBaseLN
    Inherits Framework.ClaseBaseLN.BaseTransaccionConcretaLN





    Public Sub ModificarPoliza(ByVal periodoR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal tarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal fechaInicioPC As Date)

        Using tr As New Transaccion()

            If Not periodoR.Contiene(fechaInicioPC) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("La fecha del nuevo periodo de cobertura debe estar contenida dentro del periodo de renovación vigente")
            End If

            Dim ln As New PolizaRvLcLN()
            If Date.Compare(fechaInicioPC, Now()) < 0 Then
                ln.ModificarCondicionesCoberturaRetroactiva(periodoR, tarifa, cuestionarioR, fechaInicioPC, 10)
            Else
                ln.ModificarCondicionesCoberturaRetroactiva(periodoR, tarifa, cuestionarioR, fechaInicioPC, 10)
            End If

            tr.Confirmar()
        End Using

    End Sub

    ''' <summary>
    ''' este metodo no debe permitir modificar elemenetos de la poliza que intervengan en los datos de tarificacion
    ''' </summary>
    ''' <param name="pPR"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function ModificarPoliza(ByVal pPR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
        Using tr As New Transaccion

            ''''''''''''''''''''''''''
            ' PRECONDICIONES
            ''''''''''''''''''''''''''

            ' no pueden estar modificados elemetnos que interfirar en el calculo de la tarifa

            If Not pPR.Estado = Framework.DatosNegocio.EstadoDatosDN.SinModificar Then
                Throw New ApplicationException("se modificaron datos que influyen en los datos de tarificación, no esta permitido en esta operación")
            End If

            '' FIN ''''''''''''''''''''''

            '''''''''
            ' CUERPO
            ''''''''''

            ' ++ guardar
            Me.GuardarGenerico(pPR)

            tr.Confirmar()

            Return pPR

        End Using
    End Function


    ''' <summary>
    ''' 
    ''' precondiciones
    '''  no pueden haber dos polizas del mimo tipo para el mismo priesgo 
    '''  no pueden exitir dos tomadores para la misma entidad fiscal (integridad controlada por el tomardordn)
    ''' 
    ''' </summary>
    ''' <param name="pPR"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function AltaDePolizapp(ByVal pPR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pDebeExistirPresupuesto As Boolean) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN


        Using tr As New Transaccion


            ''''''''''''''''''''''''''
            ' PRECONDICIONES
            ''''''''''''''''''''''''''


            ' debe de exitir un riesgo valido
            Dim rm As FN.RiesgosVehiculos.DN.RiesgoMotorDN = pPR.PeridoCoberturaActivo.Tarifa.Riesgo
            If rm Is Nothing OrElse Not rm.RiesgoValidoPoliza() Then
                Throw New ApplicationException("el riesgo nos es valido para relaizar una póliza")
            End If




            ' no pueden haber dos polizas del mimo tipo para el mismo priesgo 
            Dim rmad As New FN.RiesgosVehiculos.AD.RiesgosVehiculosAD
            Dim colPR As IList(Of FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN) = rmad.RecuperarHuellasPeridosRenovacionActivosParaRiesgoMotor(rm.Matricula.ValorMatricula, rm.NumeroBastidor)
            If colPR.Count > 0 Then
                Dim ides As String

                For Each he As Framework.DatosNegocio.HEDN In colPR
                    ides += he.IdEntidadReferida & ", "
                Next

                Throw New ApplicationException("Ya  exiten los peridos de renovación activos de ids: " & ides.Substring(0, ides.Length - 2))
            End If




            If pDebeExistirPresupuesto Then



                ' la fecha de alta del perido de renivación debe estar contenida en el perido de validad del presupuesto
                ' hay que recuperar el presupuesto para la tarifa referida
                Dim presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN
                Dim bln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
                presupuesto = bln.RecuperarObjetoReverso(pPR.PeridoCoberturaActivo.Tarifa, GetType(FN.Seguros.Polizas.DN.PresupuestoDN), "Tarifa")

                If presupuesto Is Nothing Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se recupero el presupuesto origen para el perido de renovacion de id " & pPR.ID)
                End If

                If Not presupuesto.PeridoValidez.Contiene(pPR.PeridoCoberturaActivo.FI) Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("la fecha de inicio del perido de renovación debe estar contenida en el perido de validez del presupuesto")
                End If

            End If
            '' FIN ''''''''''''''''''''''






            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' CUERPO
            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            Dim colClones As Framework.DatosNegocio.ColIEntidadDN

            ' establecer las fechas 
            pPR.FCreacion = Now
            pPR.FF = pPR.PeridoCoberturaActivo.Tarifa.AMD.IncrementarFecha(pPR.FI)
            pPR.PeridoCoberturaActivo.FI = pPR.FI
            pPR.Poliza.FechaAlta = pPR.FI


            ' ++ solicitar el nuemro de poliza
            colClones = Nothing
            pPR.ToHtGUIDs(Nothing, colClones)
            If colClones.Count > 0 Then
                Beep()
            End If

            ' ++ guardar
            Me.GuardarGenerico(pPR)





            ' ++ coleccion de pagos e importes debidos
            Dim colpagos As FN.GestionPagos.DN.ColPagoDN
            colpagos = GenerarCargosPara(Nothing, pPR, pPR.PeridoCoberturaActivo, 100)

            colClones = Nothing
            pPR.ToHtGUIDs(Nothing, colClones)
            If colClones.Count > 0 Then
                Beep()
            End If

            ' ++ Crear Revincular los cajones documento del presupuesto
            VincularCajonesDocumento(pPR)




            colClones = Nothing
            pPR.ToHtGUIDs(Nothing, colClones)
            If colClones.Count > 0 Then
                Beep()
            End If


            ' ++ Crear el documento apra la emisión de poliza








            tr.Confirmar()


            Return pPR

        End Using
    End Function


    Public Function VincularCajonesDocumento(ByVal pPResupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN



        ' crear los cajones documetnos
        ' si se crean, entonces recuperar los cd que ya estubieran vinculados a la poliza
        ' podar los cd recien creados que refieran a tipos de documentos ya vinculados a la poliza por cd recuperados

        ' de los preexistentes, podar aquellos que no esten en los cajones creados originales

        ' vincular los eixtentes que hayan sobrevividoa la poda a la tarifa




        Using tr As New Transaccion


            Dim colClones As Framework.DatosNegocio.ColIEntidadDN

            ' helementos a referir por los cd

            Dim colhe As New Framework.DatosNegocio.ColHEDN
            colhe.AddHuellaPara(pPResupuesto)
            colhe.AddHuellaPara(pPResupuesto.Tarifa)


            ' guid de los elementos que pueden requerir un cd
            Dim colguid As List(Of String) = Framework.TiposYReflexion.LN.ListHelper(Of String).Convertir(pPResupuesto.Tarifa.ToHtGUIDs(Nothing, colClones).Keys)
            If colClones.Count > 4 Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(" Exiten " & colClones.Count & " clones para " & pPResupuesto.ToString)
            End If



            ' el guid de la categoria
            Dim rvln As New RiesgosVehiculosLN
            Dim rv As FN.RiesgosVehiculos.DN.RiesgoMotorDN = pPResupuesto.Tarifa.Riesgo
            Dim h As Framework.DatosNegocio.HEDN = rvln.RecuperarHuellaCategoria(rv.Modelo, rv.Matriculado)
            colguid.Add(h.GUIDReferida)


            Dim ColTipoDocumentoRequerido As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN
            Dim colCDVinculados As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
            colCDVinculados = VincularCajonesDocumento(New Framework.DatosNegocio.HEDN(pPResupuesto, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir), colguid, colhe, ColTipoDocumentoRequerido)


            ' identificar el cajon documento que indique el texto del contrato
            IdentificacionCD(pPResupuesto, colCDVinculados)

            Dim DatosTarifa As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = pPResupuesto.Tarifa.DatosTarifa
            ' DatosTarifa.ColCajonDocumento = colCDVinculados
            DatosTarifa.ColTipoDocumentoRequerido = ColTipoDocumentoRequerido


            Me.GuardarGenerico(colCDVinculados)




            VincularCajonesDocumento = colCDVinculados


            tr.Confirmar()

        End Using




    End Function



    Private Sub IdentificacionCD(ByVal pPResupuesto As FN.Seguros.Polizas.DN.PresupuestoDN, ByVal colCDVinculados As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN)
        Dim TextoClaveTipoDocPresupuesto As String = Framework.Configuracion.AppConfiguracion.DatosConfig("TextoClaveTipoDocPresupuesto")

        Dim cdPresupuesto As Framework.Ficheros.FicherosDN.CajonDocumentoDN = colCDVinculados.RecuperarPrimeroXNombre(TextoClaveTipoDocPresupuesto)

        If cdPresupuesto Is Nothing Then
            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("no se recupero ningun cajon documneto para la clave de presupuesto")
        End If

        If cdPresupuesto.TipoDocumento Is Nothing Then
            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("cdPresupuesto.TipoDocumento no puede ser nulo en el proceso de identificacion")
        End If

        Dim iddoc As New Framework.Ficheros.FicherosDN.IdentificacionDocumentoDN
        iddoc.TipoFichero = cdPresupuesto.TipoDocumento
        iddoc.Identificacion = pPResupuesto.GUID.Substring(0, 8) ' el codigo unico del presupuesto
        cdPresupuesto.IdentificacionDocumento = iddoc

    End Sub


    Private Sub VincularCajonesDocumento(ByVal pPR As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)
        ' crear los cajones documetnos
        ' si se crean, entonces recuperar los cd que ya estubieran vinculados a la poliza
        ' podar los cd recien creados que refieran a tipos de documentos ya vinculados a la poliza por cd recuperados

        ' de los preexistentes, podar aquellos que no esten en los cajones creados originales

        ' vincular los eixtentes que hayan sobrevividoa la poda a la tarifa

        Using tr As New Transaccion


            ' 

            Dim colclones As Framework.DatosNegocio.ColIEntidadDN


            ' helementos a referir por los cd

            Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = pPR.PeridoCoberturaActivo.Tarifa

            Dim colhe As New Framework.DatosNegocio.ColHEDN
            colhe.AddHuellaPara(pPR.Poliza)
            colhe.AddHuellaPara(pPR)
            colhe.AddHuellaPara(tarifa)


            ' guid de los elementos que pueden requerir un cd
            Dim colguid As List(Of String) = Framework.TiposYReflexion.LN.ListHelper(Of String).Convertir(pPR.PeridoCoberturaActivo.Tarifa.ColLineaProducto.RecuperarColProductos.ToHtGUIDs(Nothing, colclones).Keys)
            If colclones.Count <> 0 Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("se recuperaron clones " & colguid.Count & " para " & pPR.ToString)
            End If

            ' el guid de la categoria
            Dim rvln As New RiesgosVehiculosLN
            Dim rv As FN.RiesgosVehiculos.DN.RiesgoMotorDN = tarifa.Riesgo
            Dim h As Framework.DatosNegocio.HEDN = rvln.RecuperarHuellaCategoria(rv.Modelo, rv.Matriculado)
            colguid.Add(h.GUIDReferida)


            Dim ColTipoDocumentoRequerido As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN
            Dim colCDVinculados As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
            colCDVinculados = VincularCajonesDocumento(New Framework.DatosNegocio.HEDN(pPR.PeridoCoberturaActivo.Tarifa, Framework.DatosNegocio.HuellaEntidadDNIntegridadRelacional.relacionDebeExixtir), colguid, colhe, ColTipoDocumentoRequerido)

            Dim DatosTarifa As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = tarifa.DatosTarifa
            '  DatosTarifa.ColCajonDocumento = colCDVinculados
            DatosTarifa.ColTipoDocumentoRequerido = ColTipoDocumentoRequerido

            Me.GuardarGenerico(colCDVinculados)


            tr.Confirmar()

        End Using




    End Sub


    Public Function VincularCajonesDocumento(ByVal heEntidadPreincipal As Framework.DatosNegocio.HEDN, ByVal colguid As List(Of String), ByVal pColHeEntidadesReferidasCD As Framework.DatosNegocio.ColHEDN, ByRef pColTipoDocumentoRequerido As Framework.Ficheros.FicherosDN.ColTipoDocumentoRequeridoDN) As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN


        Using tr As New Transaccion

            Dim cdln As New Framework.Ficheros.FicherosLN.CajonDocumentoLN

            ' creacion de los cajones documento



            Dim colCDCreados, colCDCreadosoriginales, colCDResultantes As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
            Dim colCDPreexistentes, colCDPreexistentesPodados As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN

            colCDResultantes = New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
            colCDCreados = cdln.GenerarCajonesParaObjetos(pColHeEntidadesReferidasCD, colguid, pColTipoDocumentoRequerido)
            colCDCreadosoriginales = colCDCreados.Clone ' superficial

            ' recuperacion de los cd preexitentes si se generaron cajones de documento y si la poliza exitia previamente
            If Not String.IsNullOrEmpty(heEntidadPreincipal.IdEntidadReferida) AndAlso colCDCreados.Count > 0 Then

                colCDPreexistentes = cdln.RecuperarCajonesParaEntidadReferida(heEntidadPreincipal.GUIDReferida)



                ' podar los cd creados en base a los cd que ya existen
                For Each cd As Framework.Ficheros.FicherosDN.CajonDocumentoDN In colCDPreexistentes
                    colCDCreados.PodarCol(cd.TipoDocumento)
                Next


                ' de los preexintentes podar aquerllos tipos que no esten entre los credos originales
                colCDPreexistentesPodados = New Framework.Ficheros.FicherosDN.ColCajonDocumentoDN
                If colCDPreexistentes IsNot Nothing Then
                    For Each cd As Framework.Ficheros.FicherosDN.CajonDocumentoDN In colCDCreadosoriginales
                        colCDPreexistentesPodados.AddRangeObjectUnico(colCDPreexistentes.PodarCol(cd.TipoDocumento))
                    Next
                End If


                ' relacionar los preexistentes Podados  con las entidades a relacionar
                If colCDPreexistentesPodados.Count > 0 Then
                    For Each cd As Framework.Ficheros.FicherosDN.CajonDocumentoDN In colCDPreexistentesPodados
                        For Each he As Framework.DatosNegocio.HEDN In pColHeEntidadesReferidasCD
                            cd.HuellasEntidadesReferidas.AddUnicoHuellaPara(he)
                        Next
                    Next
                    'Me.GuardarGenerico(colCDPreexistentesPodados)
                End If


            End If

            'Me.GuardarGenerico(colCDCreados)



            colCDResultantes.AddRangeObjectUnico(colCDCreados)
            colCDResultantes.AddRangeObjectUnico(colCDPreexistentesPodados)

            tr.Confirmar()

            Return colCDResultantes

        End Using

    End Function


    Private Function tratamientoTomador(ByVal pPresupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.TomadorDN

        '''''''''''''''''''''''''''''''''''''''''
        '' Vinculación del tomador
        ''''''''''''''''''''''''''''''''''''''''

        Dim polLN As New FN.Seguros.Polizas.LN.PolizaLN
        Dim ft As FN.Seguros.Polizas.DN.FuturoTomadorDN = pPresupuesto.FuturoTomador
        If ft Is Nothing Then
            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El presupuesto debe tener un futuro tomador")
        End If
        Dim tomador As FN.Seguros.Polizas.DN.TomadorDN = polLN.RecuperarCrearTomador(ft.NIFCIFFuturoTomador)
        Dim dt As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = pPresupuesto.Tarifa.DatosTarifa
        'Dim cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = Me.Recuperar(Of Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN)(dt.HeCuestionarioResuelto.IdEntidadReferida)

        If String.IsNullOrEmpty(tomador.ID) Then
            ' dado que el tomador no existe en el sistema procedemos a crear uno nuevo a partir de los datos del cuestionario
            Dim mensaje As String = String.Empty

            If FN.Localizaciones.DN.NifDN.ValidaNif(ft.NIFCIFFuturoTomador, mensaje) Then
                ' creamos una persona fiscal
                Dim pef As FN.Personas.DN.PersonaFiscalDN = tomador.EntidadFiscalGenerica.IentidadFiscal

                pef.Persona.Nombre = pPresupuesto.FuturoTomador.Nombre
                pef.Persona.Apellido = pPresupuesto.FuturoTomador.Apellido1FuturoTomador
                pef.Persona.Apellido2 = pPresupuesto.FuturoTomador.Apellido2FuturoTomador
                pef.DomicilioFiscal = pPresupuesto.FuturoTomador.direccion
                pef.Persona.FechaNacimiento = pPresupuesto.FuturoTomador.FI

            ElseIf FN.Localizaciones.DN.CifDN.ValidaCif(ft.NIFCIFFuturoTomador, mensaje) Then
                ' creamos una empresa fiscal
                Throw New NotImplementedException

            End If

        Else
            ' el tomador exsite en el sistema luego hay que verificar la coincidencia de los datos y si tine alguna exclusión

            pPresupuesto.FuturoTomador.Tomador = tomador

            'Dim pf As FN.Personas.DN.PersonaFiscalDN = tomador.EntidadFiscalGenerica.IentidadFiscal
            'If pf.Persona.FechaNacimiento <> pPresupuesto.FuturoTomador.FI Then
            '    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("las fechas de nacimiento no coinciden entre el tomador del sistema y los datos suministrados")
            'End If

            If tomador.EsImpago AndAlso pPresupuesto.CondicionesPago.ModalidadDePago <> GestionPagos.DN.ModalidadPago.Tranferencia Then
                ' las condiciones de pago deben ser forzosamente tranferencia
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El tomador esta en estado de impago, solo se le permite tranferencia como modalidad de pago")
            End If

            If tomador.Vetado Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(" la entidad fiscal  no puede  ser tomador esta vetado ")
            End If

        End If

        Return tomador

    End Function


    Private Function VerificacionDeRiesgos(ByVal rmsistema As FN.RiesgosVehiculos.DN.RiesgoMotorDN, ByVal rmcuestioanrio As FN.RiesgosVehiculos.DN.RiesgoMotorDN) As Framework.OperProg.OperProgDN.ColAlertaDN

        Dim colalertas As New Framework.OperProg.OperProgDN.ColAlertaDN

        If rmcuestioanrio Is Nothing Then
            Throw New ApplicationException(" debia exitir un riesgo de presupuesto")
        End If


        If rmsistema Is Nothing Then
            ' como no exite, no hay nada que comparar
        Else

            If Not rmsistema.Matricula Is Nothing AndAlso Not rmcuestioanrio.Matricula Is Nothing Then

                If rmsistema.Matricula.ValorMatricula.ToLower = rmcuestioanrio.Matricula.ValorMatricula.ToLower Then
                    ' iguales matriculas
                    ' accion referir a la matricula del sistema
                    rmcuestioanrio.Matricula = rmsistema.Matricula

                    If Not String.IsNullOrEmpty(rmsistema.NumeroBastidor) AndAlso Not String.IsNullOrEmpty(rmcuestioanrio.NumeroBastidor) AndAlso rmsistema.NumeroBastidor.ToLower <> rmcuestioanrio.NumeroBastidor.ToLower Then
                        ' iguales matriculas pero distientos numeros de bastidor
                        ' accion: ALERTAR
                        colalertas.Add(AlertarRiesgos(rmsistema, rmcuestioanrio, " Dos riesgos disponen de igual matrícula pero distintos números de bastidor"))

                    Else
                        ' iguales matrigulas y numeros de bastidos o alguino no  dispone
                        ' accion no hacer nada


                        If String.IsNullOrEmpty(rmcuestioanrio.NumeroBastidor) Then
                            ' como el que no dispone es el nuevo co obtenemos del existente
                            rmcuestioanrio.NumeroBastidor = rmsistema.NumeroBastidor
                        Else

                            ' accion no hacer nada
                        End If

                    End If

                Else
                    ' las matriculas son distientas
                    colalertas.Add(AlertarRiesgos(rmsistema, rmcuestioanrio, " Dos riesgos disponen de igual número de bastidor , pero de matrículas distintas"))

                End If

            Else
                ' alguno de los dos riesgos no dispone de matricula
                If Not rmsistema.Matricula Is Nothing Then
                    ' el risgo de sistema no tiene matricula
                    ' accion nada o alertar (por determinar)
                    ' la poliza del sistema se realizó contra un riesgo  no matriculado
                Else
                    ' el riesgo del presupuesto no tine matricula pero dado que coincide el numero de bastidor y el riesgo sistema si tiene matrigula
                    ' se copia la matricula del sistema
                    rmcuestioanrio.Matricula = rmsistema.Matricula

                End If


            End If

            If rmsistema.Modelo.GUID <> rmcuestioanrio.Modelo.GUID Then
                ' en este caso no coinciden los datos y habie a que alertar
                colalertas.Add(AlertarRiesgos(rmsistema, rmcuestioanrio, " Dos riesgos disponen de distintos datos de Modelo"))
            End If

            If rmsistema.Cilindrada <> rmcuestioanrio.Cilindrada Then
                colalertas.Add(AlertarRiesgos(rmsistema, rmcuestioanrio, " Dos riesgos disponen de distintos datos de Cilindrada"))
            End If

        End If

        Return colalertas

    End Function


    Private Function AlertarRiesgos(ByVal rmsistema As FN.RiesgosVehiculos.DN.RiesgoMotorDN, ByVal rmcuestioanrio As FN.RiesgosVehiculos.DN.RiesgoMotorDN, ByVal mensaje As String) As Framework.OperProg.OperProgDN.AlertaDN

        Dim alerta As New Framework.OperProg.OperProgDN.AlertaDN
        alerta.ColIHEntidad.AddUnicoHuellaPara(rmsistema)
        alerta.ColIHEntidad.AddUnicoHuellaPara(rmcuestioanrio)
        alerta.Nombre = "Inconsistancia de datos"
        alerta.comentario = mensaje
        alerta.FEjecProgramada = Now

        Return alerta
    End Function


    'Public Sub ActualizarProdutosAplicables(ByVal tarifa As FN.Seguros.Polizas.DN.TarifaDN)



    '    Dim cdln As Framework.Ficheros.FicherosLN.CajonDocumentoLN

    '    Dim colCdVinculadosaProductos As Framework.Ficheros.FicherosDN.ColCajonDocumentoDN = cdln.RecuperarCajonesParaEntidadReferida(tarifa.ColLineaProducto.col)

    '    ActualizarProdutosAplicables(tarifa, colCdVinculadosaProductos)



    'End Sub




    '    Public Function AltaDePolizap(ByVal ptomador As FN.Seguros.Polizas.DN.TomadorDN, ByVal pEmisora As FN.Seguros.Polizas.DN.EmisoraPolizasDN, ByVal ptarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal fi As Date, ByVal ff As Date) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN
    Public Function AltaDePolizap(ByVal pPresupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN


        Using tr As New Transaccion

            ' '''''''''''''''''''''''''''
            ' precondiciones
            '''''''''''''
            Dim mensaje As String


            ' el presupuesto debe alcanzar estado de poliza
            If Not pPresupuesto.AlcanzaestadoPoliza(mensaje) Then
                Throw New ApplicationException(mensaje)
            End If

            '' la fecha de alta del perido de renivación debe estar contenida en el perido de validad del presupuesto
            If Not pPresupuesto.PeridoValidez.Contiene(pPresupuesto.FechaAltaSolicitada) Then
                Throw New ApplicationException("la fecha de inicio del perido de renovación debe estar contenida en el perido de validez del presupuesto")
            End If


            '''''''''''''''''''''''''''
            ' cuerpo
            Me.VerificarDatosPresupuesto(pPresupuesto)
            Me.GuardarGenerico(pPresupuesto)

            Dim toma As FN.Seguros.Polizas.DN.TomadorDN = Me.tratamientoTomador(pPresupuesto)



            '''''''''''''''''''''''''''''''''''''''''
            '' tratameinto del riesgo
            ''''''''''''''''''''''''''''''''''''''''
            Dim colalertas As Framework.OperProg.OperProgDN.ColAlertaDN
            Dim rvLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN
            Dim rmsistema, rmcuestioanrio As FN.RiesgosVehiculos.DN.RiesgoMotorDN
            rmcuestioanrio = pPresupuesto.Tarifa.Riesgo
            rmsistema = rvLN.RecuperarRiesgoMotorActivo(rmcuestioanrio.Matricula.ValorMatricula, rmcuestioanrio.NumeroBastidor)
            colalertas = VerificacionDeRiesgos(rmsistema, rmcuestioanrio)


            ' creacion de los objetos
            Dim prp As New FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN()
            prp.FI = pPresupuesto.FechaAltaSolicitada  'la pone el usuario en la pantalla
            prp.FF = pPresupuesto.Tarifa.AMD.IncrementarFecha(prp.FI)


            '''''''''''''''''''''''
            ' creación de la poliza
            Dim pol As New FN.Seguros.Polizas.DN.PolizaDN
            prp.Poliza = pol
            pol.Tomador = toma
            pol.EmisoraPolizas = pPresupuesto.Emisora
            pol.CodColaborador = pPresupuesto.CodColaborador


            '''''''''''''''''''''''''''''''''''''''''''''''
            ' recuperacion del codigo del colaborador el guid del colaborador 

            Dim codColaborador As String = pPresupuesto.CodColaborador

            If Not String.IsNullOrEmpty(codColaborador) Then
                Dim empad As New FN.Empresas.AD.EmpresaAD
                Dim ghudcolab As String = empad.RecuperarGUIDeIDColaborador(codColaborador)
                If String.IsNullOrEmpty(ghudcolab) Then
                    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("No se recupero ningun guid de colaborador para el codigo de colaborador: " & codColaborador)
                End If
                pol.GuidColaborador = ghudcolab

            End If

            '' FIN ''''''''''''''''''''''



            ''''''''''''''''''''''''''''''''''''''
            ' perido de cobertura
            Dim pc As New FN.Seguros.Polizas.DN.PeriodoCoberturaDN
            prp.ColPeriodosCobertura.Add(pc)
            pc.FI = prp.FI
            pc.Tarifa = pPresupuesto.Tarifa
            pc.CondicionesPago = pPresupuesto.CondicionesPago.CloneSuperficialSinIdentidad
            ' pc.CondicionesPago.PlazoEjecucion = pPresupuesto.CondicionesPago.PlazoEjecucion.CloneSuperficialSinIdentidad
            ' recuperar el plazo de ejecucion y se asigna
            Dim pead As New FN.GestionPagos.AD.PlazoEfectoAD
            Dim plazo As FN.GestionPagos.DN.PlazoEfectoDN = pead.Recuperar(pc.CondicionesPago, pc.Tarifa.FEfecto)
            If plazo Is Nothing Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN("debió haberse recuperado un plazo de ejecución de recivo")
            End If
            pc.CondicionesPago.PlazoEjecucion = plazo.PlazoEjecucion.CloneSuperficialSinIdentidad




            Me.AltaDePolizapp(prp, True)
            Me.GuardarGenerico(colalertas)
            Me.GuardarGenerico(pPresupuesto)

            AltaDePolizap = prp

            tr.Confirmar()

        End Using




    End Function


    Public Function VerificarDatosPresupuesto(ByVal pPresupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.PresupuestoDN

        Using tr As New Transaccion

            ' '''''''''''''''''''''''''''
            ' precondiciones
            '''''''''''''

            ' la fecha de alta del perido de renivación debe estar contenida en el perido de validad del presupuesto
            If Not pPresupuesto.PeridoValidez.Contiene(pPresupuesto.FechaAltaSolicitada) Then
                Throw New ApplicationException("la fecha de inicio del perido de renovación debe estar contenida en el perido de validez del presupuesto")
            End If


            '''''''''''''''''''''''''''
            ' cuerpo

            Dim toma As FN.Seguros.Polizas.DN.TomadorDN = Me.tratamientoTomador(pPresupuesto)
            pPresupuesto.FuturoTomador.Tomador = toma


            '''''''''''''''''''''''''''''''''''''''''
            '' tratameinto del riesgo
            ''''''''''''''''''''''''''''''''''''''''
            Dim colalertas As Framework.OperProg.OperProgDN.ColAlertaDN
            Dim rvLN As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.RiesgosVehiculosLN
            Dim rmsistema, rmcuestioanrio As FN.RiesgosVehiculos.DN.RiesgoMotorDN
            rmcuestioanrio = pPresupuesto.Tarifa.Riesgo
            rmsistema = rvLN.RecuperarRiesgoMotorActivo(rmcuestioanrio.Matricula.ValorMatricula, rmcuestioanrio.NumeroBastidor)
            colalertas = VerificacionDeRiesgos(rmsistema, rmcuestioanrio)


            ' creacion de los objetos


            Me.GuardarGenerico(colalertas)

            tr.Confirmar()


            Return pPresupuesto

        End Using

    End Function


    ' Public Function GuardarPresupuestoYAsociarDocumento(ByVal pemisora As FN.Seguros.Polizas.DN.EmisoraPolizasDN, ByVal tarifaTarificada As FN.Seguros.Polizas.DN.TarifaDN, ByVal cuestionario As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal tiempoValidez As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias) As FN.Seguros.Polizas.DN.DocAsociadoPolizaDN
    Public Function GuardarPresupuestoYAsociarDocumento(ByVal pemisora As FN.Seguros.Polizas.DN.EmisoraPolizasDN, ByVal tarifaTarificada As FN.Seguros.Polizas.DN.TarifaDN, ByVal cuestionario As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal tiempoValidez As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias) As Framework.Ficheros.FicherosDN.CajonDocumentoDN
        'ByVal futuroTomador As FN.Localizaciones.DN.EntidadFiscalGenericaDN, 

        Using tr As New Transaccion
            GuardarPresupuestoYAsociarDocumento = AsociarDocumentoPresupuesto(GuardarPresupuesto(pemisora, tarifaTarificada, cuestionario, tiempoValidez))

            tr.Confirmar()
        End Using


    End Function
    ' Public Function AsociarDocumentoPresupuesto(ByVal pPresupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.DocAsociadoPolizaDN


    Public Function AsociarDocumentoPresupuesto(ByVal pPresupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As Framework.Ficheros.FicherosDN.CajonDocumentoDN

        Using tr As New Transaccion

            Me.VincularCajonesDocumento(pPresupuesto)

            'Dim doc As New FN.Seguros.Polizas.DN.DocAsociadoPolizaDN

            'doc.DocAsociado = New Framework.Ficheros.FicherosDN.CajonDocumentoDN
            'doc.Presupuesto = pPresupuesto
            'doc.DocAsociado.HuellasEntidadesReferidas.AñadirHuellaPara(pPresupuesto)

            'Me.GuardarGenerico(doc)
            'AsociarDocumentoPresupuesto = doc
            tr.Confirmar()

        End Using

    End Function

    ' ByVal futuroTomador As FN.Localizaciones.DN.EntidadFiscalGenericaDN,
    Public Function GuardarPresupuesto(ByVal pemisora As FN.Seguros.Polizas.DN.EmisoraPolizasDN, ByVal tarifaTarificada As FN.Seguros.Polizas.DN.TarifaDN, ByVal cuestionario As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal tiempoValidez As Framework.DatosNegocio.Localizaciones.Temporales.AnyosMesesDias) As FN.Seguros.Polizas.DN.PresupuestoDN

        Using tr As New Transaccion

            Dim presupuesto As New FN.Seguros.Polizas.DN.PresupuestoDN
            ' presupuesto.FuturoTomador = futuroTomador
            presupuesto.Tarifa = tarifaTarificada
            'presupuesto.CuestionarioResuelto = cuestionario
            presupuesto.Emisora = pemisora
            presupuesto.PeridoValidez.FInicio = tarifaTarificada.FEfecto
            presupuesto.PeridoValidez.FFinal = tiempoValidez.IncrementarFecha(presupuesto.PeridoValidez.FInicio)

            Me.GuardarGenerico(presupuesto)
            GuardarPresupuesto = presupuesto
            tr.Confirmar()

        End Using

    End Function


    Public Function RenovacionPoliza(ByVal pfechaRenovacion As Date, ByVal pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByRef pColpagos As FN.GestionPagos.DN.ColPagoDN) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN

        ' no se puede renovar una poliza cuyo cliente prente impagos para otra poliza
        ' la tarifa presenta las mimas caracteristicas que la anterior pero con fecha de efecto distienta

        Using tr As New Transaccion

            ' verificar que se puede renovar  en base a la fecha
            If Not pfechaRenovacion >= pr.FF Then
                Throw New ApplicationException("no es posible renovar la poliza dado que todavia esta e vigor el perido de renovacion")
            End If


            ' verificar que el tomador esta al corriente de pagos
            Dim apln As New FN.GestionPagos.LN.ApunteImpDLN
            Dim saldo2 As Double = apln.Saldo(pr.Poliza.EmisoraPolizas.EnidadFiscalGenerica, pr.Poliza.Tomador.EntidadFiscalGenerica, pr.FF)
            System.Diagnostics.Debug.WriteLine(saldo2)

            If saldo2 > 0 Then
                Throw New ApplicationException("La poliza no puede renovarse porque existe un saldo pendiente por parte del tomador")
            End If

            Dim nuevopr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN = pr.Renovacion
            Me.GuardarGenerico(pr)
            Me.GuardarGenerico(nuevopr)
            RenovacionPoliza = nuevopr

            ''''''''''''''''''''''''''''''''''''''''''''
            ' tarificar la nueva tarifa
            ''''''''''''''''''''''''''''''''''''''''''''''''




            ' actualizar los valores de bonificacion del tomador
            Dim tarifa As FN.Seguros.Polizas.DN.TarifaDN = nuevopr.ColPeriodosCobertura.Item(0).Tarifa
            Dim tomador As FN.Seguros.Polizas.DN.TomadorDN = nuevopr.Poliza.Tomador
            ActualizarValorBonificacionTomador(tomador, pr, tarifa)



            ' asignar los valores de renovacion al del tomador a la tarifa
            tarifa.DatosTarifa.ValorBonificacion = tomador.ValorBonificacion


            ' recuperar cuestionario resuelto de esta tarifa

            Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
            Dim hecr As Framework.Cuestionario.CuestionarioDN.HeCuestionarioResueltoDN = CType(tarifa.DatosTarifa, FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN).HeCuestionarioResuelto
            miln.RecuperarGenerico(hecr)


     

            Dim cr As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = hecr.EntidadReferida
            Dim trln As New FN.RiesgosVehiculos.LN.RiesgosVehiculosLN.TarificadorRVLN
            trln.TarificarTarifa(pr.Poliza.Tomador.ValorBonificacion, tarifa, cr, pr.PeridoCoberturaActivo.Tarifa.CalcualrImporteDia, False)




            pColpagos = GenerarCargosPara(Nothing, nuevopr, nuevopr.ColPeriodosCobertura.Item(0), 100)


            ' ++ Crear Revincular los cajones documento del presupuesto
            VincularCajonesDocumento(pr)

            ' ++ Crear el documento apra la emisión de poliza

            tr.Confirmar()

        End Using

    End Function


    Public Function Ultimos3añosconMayorNivelBonificacion() As Boolean
        'el nivel de bonificacion durante los tres años era el maximo de cada año

    End Function


    Public Function SiniestrosEnLosUltimos3AñosDeCulpa() As FN.Seguros.Polizas.DN.ColSiniestroDN

        Return New FN.Seguros.Polizas.DN.ColSiniestroDN
    End Function

    Public Sub ActualizarValorBonificacionTomador(ByVal tm As FN.Seguros.Polizas.DN.TomadorDN, ByVal pr As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal tr As FN.Seguros.Polizas.DN.TarifaDN)
        ' reglas si el tomador tine 




        Dim siniestros As FN.Seguros.Polizas.DN.ColSiniestroDN = SiniestrosEnLosUltimos3AñosDeCulpa()
        Dim colConstatesConfigurablesSeguros As FN.Seguros.Polizas.DN.ColConstatesConfigurablesSegurosDN = Framework.Configuracion.AppConfiguracion.DatosConfig.Item(GetType(FN.Seguros.Polizas.DN.ColConstatesConfigurablesSegurosDN).FullName)
        Dim colConstatesConfigurablesSegurosRecuperados As New FN.Seguros.Polizas.DN.ColConstatesConfigurablesSegurosDN
        colConstatesConfigurablesSegurosRecuperados.AddRangeObject(colConstatesConfigurablesSeguros.RecuperarContienenFecha(tr.FEfecto))

        If colConstatesConfigurablesSegurosRecuperados.Count <> 1 Then
            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("numero de elementos de configuracion recuperado incorrecto: " & colConstatesConfigurablesSegurosRecuperados.Count)
        End If


        Dim CosntatesConfigurablesSeguros As FN.Seguros.Polizas.DN.ConstatesConfigurablesSegurosDN = colConstatesConfigurablesSegurosRecuperados.Item(0)


        ' ////////////////////////////REGLAS PRE CALCULO

        ''''''''''''''''''''''''''''
        ' REGLA A
        ''''''''''''''''''''''''''''
        If tm.ValorBonificacion = 0.5 Then
            ' posible aplicacion de la regla A

            If Ultimos3añosconMayorNivelBonificacion() Then

            End If

            Dim fechaHaceTresAños As Date = pr.FI.AddYears(-3)
            Dim ultimosSiniestros As FN.Seguros.Polizas.DN.ColSiniestroDN = siniestros.RecuperarDesdeFechaOcurrencia(fechaHaceTresAños)


            If ultimosSiniestros.Count < 2 Then
                ' en este el cleine mantiene su nivel maximo de bonificacion

                ' REGLA A (no confirmada )se permite un siniestro en los ultimos tres años, si el nivel de bonificacion durante los tres años era el maximo de cada año
                Exit Sub

            End If

        End If



        ' /////////////////////////////////CALCULO
        Dim intf As New Framework.DatosNegocio.Localizaciones.Temporales.IntervaloFechasDN
        intf.FInicio = pr.FI.AddYears(-1).AddMonths(-2)
        intf.FFinal = pr.FI.AddMonths(-2)
        Dim ultimosSiniestrosparaCalculo As FN.Seguros.Polizas.DN.ColSiniestroDN = siniestros.RecuperarDesdeFechaOcurrencia(intf)

        If ultimosSiniestrosparaCalculo.Count = 0 Then
            tm.ValorBonificacion = tm.ValorBonificacion * CosntatesConfigurablesSeguros.ValorBonificacionSiniestros
        Else
            tm.ValorBonificacion = tm.ValorBonificacion * CosntatesConfigurablesSeguros.ValorMalificacionSiniestros ^ ultimosSiniestrosparaCalculo.Count
        End If





        ' /////////////////////////////REGLAS POST CALCULO

        ''''''''''''''''''''''''''''
        ' REGLA B
        ''''''''''''''''''''''''''''

        If tm.ValorBonificacion > 1 Then

            Dim fechaHaceDosAños As Date = pr.FI.AddYears(-2)
            Dim ultimosSiniestros As FN.Seguros.Polizas.DN.ColSiniestroDN = siniestros.RecuperarDesdeFechaOcurrencia(fechaHaceDosAños)

            If ultimosSiniestros.Count = 0 Then
                tm.ValorBonificacion = 1
            End If


        End If





    End Sub





    Public Function BajaDePoliza(ByVal hePeridoRenovacion As FN.Seguros.Polizas.DN.HEPeriodoRenovacionPolizaDN, ByVal pFechaEfectoBaja As Date) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN

        Dim pPeriodoRenovacionPolizaDN As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN


        Using tr As New Transaccion

            ' la tarifa debe estrar tarificada

            'If hePeridoRenovacion.TipoEntidadReferida Is GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN) Then
            '    Throw New Framework.LogicaNegocios.ApplicationExceptionLN("Tipo incompatible")
            'End If

            ' 1º anular el origen de importe debido actual
            Dim ColPeriodoRenovacionPolizaOid As FN.RiesgosVehiculos.DN.ColPeriodoRenovacionPolizaOidDN
            Dim ad As New FN.RiesgosVehiculos.AD.PeriodoRenovacionPolizaOidAD
            ColPeriodoRenovacionPolizaOid = ad.Recuperar(hePeridoRenovacion)

            If ColPeriodoRenovacionPolizaOid.RecuperarActivos.Count <> 1 Then
                Throw New ApplicationException("A este nivel solo debiera exitir un unico origen de importe debido activo")
            End If

            Dim mensaje As String

            Dim pPeriodoRenovacionPolizaOidPrevio As FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN = ColPeriodoRenovacionPolizaOid.RecuperarNoAnulado  ' hay que recuperar el que no este anulado
            ' sustituimos el objeto por el recuperado de la bd
            pPeriodoRenovacionPolizaDN = pPeriodoRenovacionPolizaOidPrevio.PeriodoRenovacionPoliza

            ' efectuamos la baja en servidor
            If Not pPeriodoRenovacionPolizaDN.BajaAFecha(pFechaEfectoBaja, mensaje) Then
                Throw New Framework.LogicaNegocios.ApplicationExceptionLN(mensaje)
            End If


            ' anulamos el origen de importe debidos !!!!pero sin compensar los pagos !!!!
            '   Me.AnularOrigenImpDebSinCompensarPagosEfectuados(pPeriodoRenovacionPolizaOidPrevio, pFechaEfectoBaja)
            Dim MotorLiquidacion As New FN.GestionPagos.LN.MotorLiquidacionLN
            MotorLiquidacion.AnularOrigenImpDebSinCompensarPagosEfectuados(pPeriodoRenovacionPolizaOidPrevio, pFechaEfectoBaja)

            '  Me.GuardarGenerico(pPeriodoRenovacionPolizaOidPrevio)


            ' +++ eliminar los cajones de documentos que estubieran pendientes

            tr.Confirmar()

        End Using


        Return pPeriodoRenovacionPolizaDN

    End Function


    ''' <summary>
    '''  en este caso no se trata de una anulacion de la pliza sino de un CONSUMO de la poliza en la indemnización generada por  un siniestro
    ''' o por un robo si tiene contratada robo o incendio
    ''' 
    ''' luego no se anulan los importes debidos que fueron generados por la poliza
    ''' </summary>
    ''' <param name="pPeriodoRenovacionPolizaDN"></param>
    ''' <param name="pPeriodoCobertura"></param>
    ''' <param name="pFechaEfectoBaja"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function BajaDePolizaPorSiniestro(ByVal pPeriodoRenovacionPolizaDN As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pPeriodoCobertura As FN.Seguros.Polizas.DN.PeriodoCoberturaDN, ByVal pFechaEfectoBaja As Date) As GestionPagos.DN.ColIImporteDebidoDN

    End Function


    Public Function ModificarCondicionesCoberturaRetroactiva(ByVal pPeriodoRenovacionPoliza As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal pCuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal pFechaInicioNuevoPC As Date, ByVal pNumeroMaximoPagos As Int16) As GestionPagos.DN.ColIImporteDebidoDN


        Using tr As New Transaccion

            ' la tarifa debe estrar tarificada

            If Not pFechaInicioNuevoPC >= pTarifa.FEfecto Then
                Throw New ApplicationException("fechas incorrectas")
            End If

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            '0º recuperar el perido de cobertura anteriro que va a ser modificado
            Dim PeriodoCoberturaAModificar As FN.Seguros.Polizas.DN.PeriodoCoberturaDN
            PeriodoCoberturaAModificar = pPeriodoRenovacionPoliza.ColPeriodosCobertura.RecuperarPrimeroContengaFecha(pFechaInicioNuevoPC)

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' 1º anular el origen de importe debido actual
            Dim ColPeriodoRenovacionPolizaOid As FN.RiesgosVehiculos.DN.ColPeriodoRenovacionPolizaOidDN
            Dim ad As New FN.RiesgosVehiculos.AD.PeriodoRenovacionPolizaOidAD
            ColPeriodoRenovacionPolizaOid = ad.Recuperar(PeriodoCoberturaAModificar)

            'If ColPeriodoRenovacionPolizaOid.Count <> 1 Then ' todo esto no es cierto pueden haber varios si se realizan varias modificaciones en las condiciones de la poliza
            '    Throw New ApplicationException("A este nivel solo debiera exitir un unico origen de importe debido, existen: " & ColPeriodoRenovacionPolizaOid.Count)
            'End If


            If ColPeriodoRenovacionPolizaOid.RecuperarActivos.Count <> 1 Then
                Throw New ApplicationException("A este nivel solo debiera exitir un unico origen de importe debido activo")
            End If


            Dim pPeriodoRenovacionPolizaOidPrevio As FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN = ColPeriodoRenovacionPolizaOid.RecuperarNoAnulado  ' hay que recuperar el que no este anulado
            ' anulamos el origen de importe debidos !!!!pero sin compensar los pagos !!!!
            ' Me.AnularOrigenImpDebSinCompensarPagosEfectuados(pPeriodoRenovacionPolizaOidPrevio, pFechaInicioNuevoPC.Subtract(New TimeSpan(1, 0, 0, 0)))
            Dim MotorLiquidacion As New FN.GestionPagos.LN.MotorLiquidacionLN
            MotorLiquidacion.AnularOrigenImpDebSinCompensarPagosEfectuados(pPeriodoRenovacionPolizaOidPrevio, pFechaInicioNuevoPC.Subtract(New TimeSpan(1, 0, 0, 0)))


            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ''2º crear el nuevo periodo de cobertua
            'Dim NuevoPeriodoCobertura As New FN.Seguros.Polizas.DN.PeriodoCoberturaDN
            'NuevoPeriodoCobertura.FI = pFechaInicioNuevoPC
            'NuevoPeriodoCobertura.FF = pPeriodoRenovacionPoliza.FF
            'NuevoPeriodoCobertura.Tarifa = pTarifa



      

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            '3º modificar la fecha y genera el nuevo origen de importes debidos para el perido de cobertura  a modificar
            ' PeriodoCoberturaAModificar.FF = NuevoPeriodoCobertura.FI.Subtract(New TimeSpan(1, 0, 0, 0))
            ' la tarifa anteriro no se tarifica dado que es la msiama solo que su periodo de cobertura ha cambiado su aplitud temporal

            Dim miTarificadorRVLN As New TarificadorRVLN
            miTarificadorRVLN.TarificarTarifa(pPeriodoRenovacionPoliza.Poliza.Tomador.ValorBonificacion, pTarifa, pCuestionarioResuelto, True)
            PeriodoCoberturaAModificar.AsignarNuevaTarifa(pTarifa)
            GenerarCargosPara(Nothing, pPeriodoRenovacionPoliza, PeriodoCoberturaAModificar, 100)

            ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ''4º crear los cargos para el nuevo periodo de cobertuta
            'pPeriodoRenovacionPoliza.ColPeriodosCobertura.Add(NuevoPeriodoCobertura)

            'Dim miTarificadorRVLN As New TarificadorRVLN
            'miTarificadorRVLN.TarificarTarifa(pTarifa, pCuestionarioResuelto)
            ''GenerarCargosPara(pPeriodoRenovacionPolizaOidPrevio, pPeriodoRenovacionPoliza, NuevoPeriodoCobertura)
            'GenerarCargosPara(Nothing, pPeriodoRenovacionPoliza, NuevoPeriodoCobertura, pNumeroMaximoPagos)

            ' ++ Crear Revincular los cajones documento del presupuesto
            VincularCajonesDocumento(pPeriodoRenovacionPoliza)


            ' ++ Crear el documento apra la emisión de poliza


            tr.Confirmar()

        End Using

    End Function


    Public Function ModificarCondicionesCoberturaNoRetroactiva(ByVal pPeriodoRenovacionPoliza As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pTarifa As FN.Seguros.Polizas.DN.TarifaDN, ByVal pCuestionarioResuelto As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN, ByVal pFechaInicioNuevoPC As Date, ByVal pNumeroMaximoPagos As Int16) As GestionPagos.DN.ColIImporteDebidoDN


        Using tr As New Transaccion

            ' la tarifa debe estrar tarificada

            If Not pFechaInicioNuevoPC >= pTarifa.FEfecto Then
                Throw New ApplicationException("fechas incorrectas")
            End If

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            '0º recuperar el perido de cobertura anteriro que va a ser modificado
            Dim PeriodoCoberturaAModificar As FN.Seguros.Polizas.DN.PeriodoCoberturaDN
            PeriodoCoberturaAModificar = pPeriodoRenovacionPoliza.ColPeriodosCobertura.RecuperarPrimeroContengaFecha(pFechaInicioNuevoPC)

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            ' 1º anular el origen de importe debido actual
            Dim ColPeriodoRenovacionPolizaOid As FN.RiesgosVehiculos.DN.ColPeriodoRenovacionPolizaOidDN
            Dim ad As New FN.RiesgosVehiculos.AD.PeriodoRenovacionPolizaOidAD
            ColPeriodoRenovacionPolizaOid = ad.Recuperar(PeriodoCoberturaAModificar)

            If ColPeriodoRenovacionPolizaOid.Count <> 1 Then
                Throw New ApplicationException("A este nivel solo debiera exitir un unico origen de importe debido, existen: " & ColPeriodoRenovacionPolizaOid.Count)
            End If

            Dim pPeriodoRenovacionPolizaOidPrevio As FN.RiesgosVehiculos.DN.PeriodoRenovacionPolizaOidDN = ColPeriodoRenovacionPolizaOid.Item(0)
            ' anulamos el origen de importe debidos !!!!pero sin compensar los pagos !!!!
            ' Me.AnularOrigenImpDebSinCompensarPagosEfectuados(pPeriodoRenovacionPolizaOidPrevio, pFechaInicioNuevoPC.Subtract(New TimeSpan(1, 0, 0, 0)))
            Dim MotorLiquidacion As New FN.GestionPagos.LN.MotorLiquidacionLN
            MotorLiquidacion.AnularOrigenImpDebSinCompensarPagosEfectuados(pPeriodoRenovacionPolizaOidPrevio, pFechaInicioNuevoPC.Subtract(New TimeSpan(1, 0, 0, 0)))


            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            '2º crear el nuevo periodo de cobertua
            Dim NuevoPeriodoCobertura As New FN.Seguros.Polizas.DN.PeriodoCoberturaDN
            NuevoPeriodoCobertura.FI = pFechaInicioNuevoPC
            NuevoPeriodoCobertura.FF = pPeriodoRenovacionPoliza.FF
            NuevoPeriodoCobertura.Tarifa = pTarifa

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            '3º modificar la fecha y genera el nuevo origen de importes debidos para el perido de cobertura  a modificar
            PeriodoCoberturaAModificar.FF = NuevoPeriodoCobertura.FI.Subtract(New TimeSpan(1, 0, 0, 0))
            ' la tarifa anteriro no se tarifica dado que es la msiama solo que su periodo de cobertura ha cambiado su aplitud temporal
            GenerarCargosPara(Nothing, pPeriodoRenovacionPoliza, PeriodoCoberturaAModificar, 1)

            '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
            '4º crear los cargos para el nuevo periodo de cobertuta
            pPeriodoRenovacionPoliza.ColPeriodosCobertura.Add(NuevoPeriodoCobertura)


            Dim miTarificadorRVLN As New TarificadorRVLN
            miTarificadorRVLN.TarificarTarifa(pPeriodoRenovacionPoliza.Poliza.Tomador.ValorBonificacion, pTarifa, pCuestionarioResuelto, 0, True)

            'GenerarCargosPara(pPeriodoRenovacionPolizaOidPrevio, pPeriodoRenovacionPoliza, NuevoPeriodoCobertura)
            GenerarCargosPara(Nothing, pPeriodoRenovacionPoliza, NuevoPeriodoCobertura, pNumeroMaximoPagos)


            ' ++ Crear Revincular los cajones documento del presupuesto
            VincularCajonesDocumento(pPeriodoRenovacionPoliza)


            ' ++ Crear el documento apra la emisión de poliza

            tr.Confirmar()

        End Using

    End Function


    ''' <summary>
    ''' Encargado de generar los apuntes de importes debidos que el nuevo perido de cobertura implican
    ''' 
    ''' debe tener encuenta los importes debidos delos pedridos de cobertura anteriores
    ''' </summary>
    ''' <param name="pEntidadOrigen"></param>
    ''' <param name="pPeriodoCobertura"></param>
    ''' <returns></returns>
    ''' <remarks></remarks>
    Public Function GenerarCargosPara(ByVal pPeriodoRenovacionPolizaOidPrevio As PeriodoRenovacionPolizaOidDN, ByVal pPeriodoRenovacionPolizaOrigen As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pPeriodoCobertura As FN.Seguros.Polizas.DN.PeriodoCoberturaDN, ByVal pNumeroMaximoPagos As Int16) As GestionPagos.DN.ColPagoDN

        ' reglas de creacion de importes debidos

        ' Aspectos()
        '   financiacion
        '   extornos
        '   importes debidos previos


        ' 1º recopger el importe total de la tarifa
        ' 2º verificar si se ha solicitado financiación y si se cumplen los importes minimos de finanaciación
        ' 3º verificar si hay importes debidos previos con pagos pendietes de abonar
        ' 4º calcular el importe definitivo del importe debido
        ' 5º verificar si el resultado se trata de un posible extorno
        ' 6º calcular y programar los pagos resultantes
        ' 4º calcular el importe definitivo del importe debido
        ' 5º calcular y programar los pagos resultantes

        Using tr As New Transaccion

            '0º contruir el origen de datos 
            Dim origen As PeriodoRenovacionPolizaOidDN = Me.GenerarOrigen(pPeriodoRenovacionPolizaOidPrevio, pPeriodoRenovacionPolizaOrigen, pPeriodoCobertura)


            ' 1º recopger el importe total de la tarifa
            'Dim datosTarif As DatosTarifaVehiculosDN
            'datosTarif = origen.PeriodoCobertura.Tarifa.DatosTarifa
            Dim importeTarifa As Double = CalcularImportePeridoCobertura(origen)


            ' 4º calcular el importe definitivo del importe debido
            Dim importeCompensadoTarifa As Double = CalcualrimporteCompensadoTarifa(origen)

            ' 5º verificar si el resultado se trata de un posible extorno
            Select Case importeCompensadoTarifa

                Case Is = 0
                    ' el importe debido quedó compensado con alguno anterior
                    ' se guarda el importe debido pero no se generan pagos
                    Me.Guardar(Of PeriodoRenovacionPolizaOidDN)(origen)

                Case Is < 0
                    ' es un extorno
                    ' se guarda el importe debido pero no se generan pagos
                    Me.Guardar(Of PeriodoRenovacionPolizaOidDN)(origen)

                Case Is > 0
                    ' debe generar los pagos anteriores
                    Me.Guardar(Of PeriodoRenovacionPolizaOidDN)(origen)
                    Dim colp As GestionPagos.DN.ColPagoDN = GenerarLosPagosConFinanciación(origen, importeCompensadoTarifa, origen.PeriodoCobertura.Tarifa.DatosTarifa, pNumeroMaximoPagos)
                    GenerarCargosPara = colp
            End Select

            tr.Confirmar()

        End Using

    End Function


    Private Function CalcularImportePeridoCobertura(ByVal pOrigenID As PeriodoRenovacionPolizaOidDN) As Double
        Dim datosTarif As DatosTarifaVehiculosDN
        datosTarif = pOrigenID.PeriodoCobertura.Tarifa.DatosTarifa
        Dim importeTarifa As Double = pOrigenID.PeriodoCobertura.Tarifa.Importe

        '1º calcular los dias de este año
        Dim diasDelPEriodoRenovacion, DiasDelPeridoDeCobertura As Int16
        diasDelPEriodoRenovacion = pOrigenID.PeriodoRenovacionPoliza.Periodo.FF.Subtract(pOrigenID.PeriodoRenovacionPoliza.Periodo.FI).Days
        If pOrigenID.PeriodoCobertura.Periodo.FF = Date.MinValue Then
            DiasDelPeridoDeCobertura = pOrigenID.PeriodoRenovacionPoliza.Periodo.FF.Subtract(pOrigenID.PeriodoCobertura.Periodo.FI).Days

        Else
            DiasDelPeridoDeCobertura = pOrigenID.PeriodoCobertura.Periodo.FF.Subtract(pOrigenID.PeriodoCobertura.Periodo.FI).Days

        End If

        importeTarifa = (pOrigenID.PeriodoCobertura.Tarifa.Importe / diasDelPEriodoRenovacion) * DiasDelPeridoDeCobertura

        Return importeTarifa

    End Function

    Private Function GenerarLosPagos(ByVal origen As PeriodoRenovacionPolizaOidDN, ByVal importeCompensadoTarifa As Double, ByVal datosTarif As DatosTarifaVehiculosDN) As GestionPagos.DN.ColPagoDN
        Throw New NotImplementedException
        ' debe generar los pagos anteriores
        ' Xº verificar si se ha solicitado financiación y si se cumplen los importes minimos de finanaciación
        Dim numerodePagos As Int16 = CalcualrNumeroDePagos(origen.PeriodoCobertura, importeCompensadoTarifa)

        ' 6º calcular y programar los pagos resultantes

        ' calcular las fechas programadas de emision 
        ' SON la primera inmediatamente, y las restantes en los periodos que queden hasta el final de period de cobertura anual
        Dim colfechas As List(Of Date) = CalcualrFechasProgramadasEmision(origen.PeriodoCobertura.Tarifa.FEfecto, origen.PeriodoRenovacionPoliza.FF, numerodePagos)


        For a As Int16 = 0 To numerodePagos - 1

            Dim pago As FN.GestionPagos.DN.PagoDN
            pago = New FN.GestionPagos.DN.PagoDN
            pago.ApunteImpDOrigen = origen.IImporteDebidoDN
            pago.Importe = importeCompensadoTarifa / numerodePagos ' hojo con los redondeos en este puento
            pago.FechaProgramadaEmision = colfechas(a)
            Me.Guardar(Of FN.GestionPagos.DN.PagoDN)(pago)

        Next

    End Function


    Private Function GenerarLosPagosConFinanciación(ByVal origen As PeriodoRenovacionPolizaOidDN, ByVal importeCompensadoTarifa As Double, ByVal datosTarif As DatosTarifaVehiculosDN, ByVal pNumeroMaximoPagos As Int16) As GestionPagos.DN.ColPagoDN
        ' debe generar los pagos anteriores
        ' Xº verificar si se ha solicitado financiación y si se cumplen los importes minimos de finanaciación
        Dim numerodePagos As Int16 = CalcualrNumeroDePagos(origen.PeriodoCobertura, importeCompensadoTarifa)
        If numerodePagos > pNumeroMaximoPagos Then
            numerodePagos = pNumeroMaximoPagos
        End If

        If numerodePagos < 1 And importeCompensadoTarifa > 0 Then
            Throw New ApplicationException("exite impporte para pagos pero el numero de pagos permitidos es <1")
        End If



        ' Xº calcular y programar los pagos resultantes
        ' calcular las fechas programadas de emision para los pagos 
        ' SON la primera inmediatamente, y las restantes en los periodos que queden hasta el final de period de cobertura anual
        Dim colfechas As List(Of Date) = CalcualrFechasProgramadasEmision(origen.PeriodoCobertura.Tarifa.FEfecto, origen.PeriodoRenovacionPoliza.FF, numerodePagos)
        Dim cilfechasPagos As New List(Of Date)

        ' eliminar de la coleccion de fechas las anteriores a fecha de efecto
        For Each fecha As Date In colfechas
            If fecha >= origen.PeriodoCobertura.Tarifa.FEfecto Then
                cilfechasPagos.Add(fecha)
            End If
        Next


        If cilfechasPagos.Count = 0 Then
            cilfechasPagos.Add(origen.PeriodoCobertura.Tarifa.FEfecto)
        End If

        numerodePagos = cilfechasPagos.Count
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''''


        Dim importeFinanciable, importeNoFinanciable As Double
        Dim importeImpuestosFinanciable, importeImpuestosNoFinanciable As Double
        datosTarif.ImporteFinaciablesImpuestos(importeImpuestosFinanciable, importeImpuestosNoFinanciable)
        importeFinanciable = importeCompensadoTarifa - importeImpuestosNoFinanciable
        importeNoFinanciable = importeImpuestosNoFinanciable


        Dim ImportePrimerPago, ImportePagosSucesivos As Double
        ImportePagosSucesivos = Math.Round(importeFinanciable / numerodePagos, 2) ' ojo con los redondeos en este puento
        ImportePrimerPago = importeCompensadoTarifa - (ImportePagosSucesivos * (numerodePagos - 1))

        If Not (ImportePagosSucesivos * (numerodePagos - 1)) + ImportePrimerPago = importeCompensadoTarifa Then
            Throw (New ApplicationException("error en el claculo valores no cuaran "))
        End If

        If ImportePrimerPago < 0 OrElse ImportePrimerPago < 0 Then
            Throw (New ApplicationException("error en el claculo generacion de importe negativo"))
        End If

        Dim colp As New FN.GestionPagos.DN.ColPagoDN

        For a As Int16 = 0 To numerodePagos - 1

            Dim pago As FN.GestionPagos.DN.PagoDN
            pago = New FN.GestionPagos.DN.PagoDN

            If a = 0 Then
                pago.PosicionPago = GestionPagos.DN.PosicionPago.Primero
            ElseIf a = numerodePagos - 1 Then
                pago.PosicionPago = GestionPagos.DN.PosicionPago.Ultimo
            Else
                pago.PosicionPago = GestionPagos.DN.PosicionPago.Intermedio
            End If


            pago.ApunteImpDOrigen = origen.IImporteDebidoDN
            colp.Add(pago)

            If a = 0 Then
                pago.Importe = ImportePrimerPago
            Else
                pago.Importe = ImportePagosSucesivos
            End If

            If pago.Importe > 0 Then
                pago.FechaProgramadaEmision = colfechas(a)
                pago.FechaEfectoEsperada = origen.PeriodoCobertura.CondicionesPago.PlazoEjecucion.IncrementarFecha(pago.FechaProgramadaEmision)
                Me.Guardar(Of FN.GestionPagos.DN.PagoDN)(pago)
            Else
                ' generacion de un pago con importe 0
                Beep()
            End If


        Next


        ProcesoCuadreImportesFraccioandosPagos(colp, colp(0)) ' ajustamos en el priemr pago
        GenerarLosPagosConFinanciación = colp


    End Function


    Private Sub ProcesoCuadreImportesFraccioandosPagos(ByVal colp As FN.GestionPagos.DN.ColPagoDN, ByVal pago As FN.GestionPagos.DN.PagoDN)

        If Not colp.Contains(pago) Then
            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El pago objetivo del ajuste no está contenido en la coleccion")
        End If



        Dim importeTotalImporteDebido As Double = pago.ApunteImpDOrigen.Importe
        Dim importeTotalPagos As Double = colp.ImporteTotal()
        Dim ajuste As Double = importeTotalImporteDebido - importeTotalPagos
        If ajuste <> 0 Then
            pago.Importe += ajuste
        End If



    End Sub



    Private Function GenerarOrigen(ByVal pPeriodoRenovacionPolizaOidPrevio As PeriodoRenovacionPolizaOidDN, ByVal pPeriodoRenovacionPoliza As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pPeriodoCobertura As FN.Seguros.Polizas.DN.PeriodoCoberturaDN) As PeriodoRenovacionPolizaOidDN

        If Not pPeriodoRenovacionPoliza.ColPeriodosCobertura.Contiene(pPeriodoCobertura, Framework.DatosNegocio.CoincidenciaBusquedaEntidadDN.Todos) Then
            Throw New ApplicationException("Error el perido de renovacion no contine al poerido de cobertura")
        End If

        Dim origen As New PeriodoRenovacionPolizaOidDN
        origen.PeriodoRenovacionPoliza = pPeriodoRenovacionPoliza
        origen.PeriodoCobertura = pPeriodoCobertura
        ' origen.PRPolizaOidPrevio = pPeriodoRenovacionPolizaOidPrevio 'odo: revisar esto



        ' recuperamos todos los origenes de importes debido para un perido de cobertura dado
        ' todos deben referir a tarifas con el mismo tipo de coberturas pero que pueden tener modificaciones en otros datos pero no en las coberturtas
        ' ya que sino se daria de alta un nuevo periodo de cobertura
        Dim pcaoidad As New FN.RiesgosVehiculos.AD.PeriodoRenovacionPolizaOidAD
        Dim colPeriodoRenovacionPolizaOid As FN.RiesgosVehiculos.DN.ColPeriodoRenovacionPolizaOidDN = pcaoidad.Recuperar(origen.PeriodoCobertura)
        Select Case colPeriodoRenovacionPolizaOid.Count

            Case Is = 1
                origen.PRPolizaOidPrevio = colPeriodoRenovacionPolizaOid.Item(0)
            Case Is = 0

            Case Else
                ' es posible siempre que las coberturas no cambien 
                'de este modo un perido de cobertura puede tener varias tarifas a sociadas y portanto varios origenes de importes debidos


                Throw New ApplicationException

        End Select


        '' si se trata de una modificacion de un perido de cobertura, este tendra ya un origen de importe debido previo
        'If origen.PRPolizaOidPrevio Is Nothing Then
        '    ' solo debiera de ser nothing si es el primer y unico pc de la coleccion 
        '    'If Not origen.PeriodoRenovacionPoliza.ColPeriodosCobertura.Count = 1 Then
        '    '    Throw (New ApplicationException("Error de integridad en la base de datos, la unica posiviliad para no existir un PCAnualOidPrevio es que sea el primer Perido de cobertura y hay <>1 "))
        '    'End If



        'Else

        '    Dim PagosLN As New FN.GestionPagos.LN.PagosLN
        '    Dim colPagos As FN.GestionPagos.DN.ColPagoDN = PagosLN.RecuperarGUIDImporteDebidoOrigen(origen.PRPolizaOidPrevio.IImporteDebidoDN.GUID)
        '    origen.PRPolizaOidPrevio.PRPolizaOidPrevioColPago.AddRange(colPagos)


        'End If

        Dim miln As New Framework.ClaseBaseLN.BaseTransaccionConcretaLN
        Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName) = miln.RecuperarLista(GetType(FN.Seguros.Polizas.DN.EmisoraPolizasDN))(0)
        Dim correduria As FN.Seguros.Polizas.DN.EmisoraPolizasDN = Framework.Configuracion.AppConfiguracion.DatosConfig(GetType(FN.RiesgosVehiculos.DN.AcreedoraTarifasConf).FullName)

        Dim acreedoras As FN.Localizaciones.DN.IEntidadFiscalDN = correduria.EnidadFiscalGenerica.IentidadFiscal

        If acreedoras Is Nothing Then
            Throw New ApplicationException("debia existir una etidad acredora emisora de las polizas")
        End If

        origen.IImporteDebidoDN = New GestionPagos.DN.ApunteImpDDN(origen)
        origen.IImporteDebidoDN.Acreedora = acreedoras.EntidadFiscalGenerica  ' 
        origen.IImporteDebidoDN.Deudora = pPeriodoRenovacionPoliza.Poliza.Tomador.EntidadFiscalGenerica  ' EL TOMADOR
        origen.IImporteDebidoDN.FEfecto = pPeriodoCobertura.FI


        'Dim datosTarif As DatosTarifaVehiculosDN
        'datosTarif = origen.PeriodoCobertura.Tarifa.DatosTarifa
        'Dim importeTarifa As Double = datosTarif.Importe


        origen.IImporteDebidoDN.Importe = Me.CalcularImportePeridoCobertura(origen)
        Return origen

    End Function


    Private Function CalcualrFechasProgramadasEmision(ByVal pFechaInicioPagos As Date, ByVal pFechaRenovacionPoliza As Date, ByVal pNumeroPagoas As Int16) As IList(Of Date)

        Dim dias As Int64 = pFechaRenovacionPoliza.Subtract(pFechaInicioPagos).TotalDays
        Dim Cadencia As Int64 = dias / pNumeroPagoas

        Dim colfechas As New List(Of Date)
        For a As Int16 = 0 To pNumeroPagoas - 1
            colfechas.Add(pFechaInicioPagos.AddDays(Cadencia * a))
        Next

        Return colfechas
    End Function


    Private Function CalcualrimporteCompensadoTarifa(ByVal pPeriodoRenovacionPolizaOid As PeriodoRenovacionPolizaOidDN) As Double

        ' tiene encuenta los importes debidos  y sus pagos   del perido de cobertuta precedente  e intenta realizar las operaciones que procedan para calcuar el importe de la tarifa
        ' las acciones pueden conllevar 
        '   * la anulacion de los pagos pendientes no ejecutados
        '   * restara del importe de la tarifa el importe de los pagos abonados
        Dim datosTarif As DatosTarifaVehiculosDN = pPeriodoRenovacionPolizaOid.PeriodoCobertura.Tarifa.DatosTarifa

        Dim mensaje As String
        Dim colpagosPeridoCoberturaPrecedente, colpagosAAnular, colpagosNoAnulables As FN.GestionPagos.DN.ColPagoDN
        colpagosAAnular = New FN.GestionPagos.DN.ColPagoDN
        colpagosNoAnulables = New FN.GestionPagos.DN.ColPagoDN

        'If Not pOrigen.PRPolizaOidPrevio Is Nothing Then
        '    colpagosPeridoCoberturaPrecedente = pOrigen.PRPolizaOidPrevio.PRPolizaOidPrevioColPago
        'End If

        ' recupera los pagos para esta poliza cullos origen es de importes debidos esten anulados

        'Dim guids As New List(Of String)
        'For Each pc As FN.Seguros.Polizas.DN.PeriodoCoberturaDN In pPeriodoRenovacionPolizaOid.PeriodoRenovacionPoliza.ColPeriodosCobertura
        '    guids.Add(pc.GUID)
        'Next

        Dim ad As New FN.RiesgosVehiculos.AD.PeriodoRenovacionPolizaOidAD
        colpagosPeridoCoberturaPrecedente = ad.RecuperarPagosActivos(pPeriodoRenovacionPolizaOid.PeriodoRenovacionPoliza, True)

        If Not colpagosPeridoCoberturaPrecedente Is Nothing Then
            For Each pago As FN.GestionPagos.DN.PagoDN In colpagosPeridoCoberturaPrecedente

                If pago.Anulable(mensaje) Then
                    colpagosAAnular.Add(pago)
                Else
                    colpagosNoAnulables.Add(pago)
                End If
            Next
        End If


        ' intento de anulacion de los pagos anulables


        For Each pago As FN.GestionPagos.DN.PagoDN In colpagosAAnular

            If Not pago.AnularPago(mensaje, Now) Then
                Throw New ApplicationException(mensaje)
            End If
            Me.Guardar(Of FN.GestionPagos.DN.PagoDN)(pago)

        Next


        Dim ImporteResultante As Double = Me.CalcularImportePeridoCobertura(pPeriodoRenovacionPolizaOid) - colpagosNoAnulables.ImporteTotal
        Return ImporteResultante

    End Function


    Private Function CalcualrNumeroDePagos(ByVal pPC As FN.Seguros.Polizas.DN.PeriodoCoberturaDN, ByVal importeCompensadoTarifa As Double) As Double
        ' hay que recupear los datos que configuran los intervlalos de financiacion

        ' hay que verificar si se perite la financiación solicitada en el cuestionario

        ' de mometo suponemos que si y que son 4 pagos

        Return pPC.CondicionesPago.NumeroRecibos
        Throw New NotImplementedException
    End Function


    'Private Function tratamientoTomador(ByVal pPresupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.TomadorDN



    '    '''''''''''''''''''''''''''''''''''''''''
    '    '' Vinculación del tomador
    '    ''''''''''''''''''''''''''''''''''''''''


    '    Dim polLN As New FN.Seguros.Polizas.LN.PolizaLN
    '    Dim ft As FN.Seguros.Polizas.DN.FuturoTomadorDN = pPresupuesto.FuturoTomador
    '    If ft Is Nothing Then
    '        Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El presupuesto debe tener un futuro tomador")
    '    End If
    '    Dim tomador As FN.Seguros.Polizas.DN.TomadorDN = polLN.RecuperarCrearTomador(ft.NIFCIFFuturoTomador)
    '    Dim dt As FN.RiesgosVehiculos.DN.DatosTarifaVehiculosDN = pPresupuesto.Tarifa.DatosTarifa
    '    Dim cuestionarioR As Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN = Me.Recuperar(Of Framework.Cuestionario.CuestionarioDN.CuestionarioResueltoDN)(dt.HeCuestionarioResuelto.IdEntidadReferida)

    '    If String.IsNullOrEmpty(tomador.ID) Then
    '        ' dado que el timador no existe en el sistema procedemos a crear uno nuevo a partir de los datos del cuestionario
    '        Dim mensaje As String = String.Empty

    '        If FN.Localizaciones.DN.NifDN.ValidaNif(ft.NIFCIFFuturoTomador, mensaje) Then
    '            ' creamos una persona fiscal
    '            Dim pef As FN.Personas.DN.PersonaFiscalDN = tomador.EntidadFiscalGenerica.IentidadFiscal

    '            pef.Persona.Nombre = cuestionarioR.ColRespuestaDN.RecuperarRespuestaaxPregunta("Nombre").IValorCaracteristicaDN.Valor
    '            pef.Persona.Apellido = cuestionarioR.ColRespuestaDN.RecuperarRespuestaaxPregunta("Apellido1").IValorCaracteristicaDN.Valor
    '            pef.Persona.Apellido2 = cuestionarioR.ColRespuestaDN.RecuperarRespuestaaxPregunta("Apellido2").IValorCaracteristicaDN.Valor
    '            pef.DomicilioFiscal = cuestionarioR.ColRespuestaDN.RecuperarRespuestaaxPregunta("DireccionEnvio").IValorCaracteristicaDN.Valor
    '            pef.Persona.FechaNacimiento = cuestionarioR.ColRespuestaDN.RecuperarRespuestaaxPregunta("FechaNacimiento").IValorCaracteristicaDN.Valor

    '        ElseIf FN.Localizaciones.DN.CifDN.ValidaCif(ft.NIFCIFFuturoTomador, mensaje) Then
    '            ' creamos una empresa fiscal
    '            Throw New NotImplementedException

    '        End If


    '    Else
    '        ' el tomador exsite en el sistema luego hay que verificar la coincidencia de los datos y si tine alguna exclusión
    '        Dim pf As FN.Personas.DN.PersonaFiscalDN = tomador.EntidadFiscalGenerica.IentidadFiscal
    '        If pf.Persona.FechaNacimiento = (cuestionarioR.ColRespuestaDN.RecuperarRespuestaaxPregunta("FechaNacimiento").IValorCaracteristicaDN.Valor()) Then
    '            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("las fechas de nacimiento no coinciden entre el tomador del sistema y los datos suministrados")

    '        End If

    '        If tomador.EsImpago AndAlso pPresupuesto.CondicionesPago.ModalidadDePago <> GestionPagos.DN.ModalidadPago.Tranferencia Then
    '            ' las condiciones de pago deben ser forzosamente tranferencia
    '            Throw New Framework.LogicaNegocios.ApplicationExceptionLN("El tomador esta en estado de impago, solo se le permite tranferencia como modalidad de pago")
    '        End If

    '        If tomador.Vetado Then
    '            Throw New Framework.LogicaNegocios.ApplicationExceptionLN(" la entidad fiscal  no puede  ser tomador esta vetado ")
    '        End If

    '    End If


    '    Return tomador


    'End Function

    '   Public Function BajaDePoliza(ByRef pPeriodoRenovacionPolizaDN As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN, ByVal pPeriodoCobertura As FN.Seguros.Polizas.DN.PeriodoCoberturaDN, ByVal pFechaEfectoBaja As Date) As GestionPagos.DN.ColIImporteDebidoDN

    'Public Function AltaDePolizaDesdePresupuesto(ByVal presupuesto As FN.Seguros.Polizas.DN.PresupuestoDN) As FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN


    '    Using tr As New Transaccion


    '        ' recuperar o crear el tomador
    '        Dim toma As FN.Seguros.Polizas.DN.TomadorDN
    '        Dim ad As New FN.Seguros.Polizas.AD.PolizasAD

    '        toma = ad.RecuperarTomador(presupuesto.FuturoTomador.ValorCifNif)
    '        If toma Is Nothing Then
    '            toma = New FN.Seguros.Polizas.DN.TomadorDN
    '            toma.EntidadFiscalGenerica = presupuesto.FuturoTomador
    '        End If
    '        '''''''''''''''''''''''''''''''''''''''''''


    '        ' CLACULO DE FECHAS
    '        Dim fi, ff As Date
    '        fi = Now
    '        ff = fi.AddDays(presupuesto.Tarifa.Dias)
    '        ff = ff.AddYears(presupuesto.Tarifa.Años)


    '        AltaDePolizaDesdePresupuesto = AltaDePolizap(toma, presupuesto.Emisora, presupuesto.Tarifa, fi, ff)



    '        tr.Confirmar()

    '    End Using




    'End Function


    'Public Overrides Function GuardarNuevoapunteImporteDebido(ByVal origen As GestionPagos.DN.IOrigenIImporteDebidoDN) As GestionPagos.DN.ColIImporteDebidoDN
    '    Return GuardarNuevoapunteImporteDebidoPoliza(origen)
    'End Function
End Class


