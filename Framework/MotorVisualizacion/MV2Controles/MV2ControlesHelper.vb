Imports MotorBusquedaBasicasDN
Imports Framework.IU.IUComun

Public Class MV2ControlesHelper





    Public Shared Function SustituirParametrosPorValores(ByVal pElementoVinc As MV2DN.IVincElemento, ByVal pLista As List(Of ValorCampo), ByVal pHtDatosEsternos As Hashtable) As Boolean

        Return MotorBusquedaIuWinCtrl.ParametrosHelper.SustituirParamettrosPorValores(pElementoVinc, pLista, pHtDatosEsternos)

    End Function



    Public Shared Function Buscar(ByVal pIctrlDinamico As IctrlDinamico) As Framework.DatosNegocio.IEntidadBaseDN

        'Dim miControlP As MotorIU.ControlesP.BaseControlP = pIctrlDinamico
        'Dim tipoSeleccioando As System.Type

        'tipoSeleccioando = RecuperarTipoSeleccioando(pIctrlDinamico)


        'If tipoSeleccioando Is Nothing Then
        '    Throw New ApplicationException("El tipo seleccioando debe estar establecidoa este nivel")
        'End If



        '' una vez selecioando el tipo navego al busador 
        'Dim paquete As New Hashtable
        'Dim ParametroCargaEstructuraDN As ParametroCargaEstructuraDN = pIctrlDinamico.IGestorPersistencia.RecuperarParametroBusqueda(pIctrlDinamico.ElementoVinc, tipoSeleccioando)
        'Dim miPaqueteFormularioBusqueda As New MotorBusquedaDN.PaqueteFormularioBusqueda
        'miPaqueteFormularioBusqueda.ParametroCargaEstructura = ParametroCargaEstructuraDN
        'miPaqueteFormularioBusqueda.MultiSelect = False
        'miPaqueteFormularioBusqueda.Agregable = pIctrlDinamico.ElementoVinc.ElementoMap.Instanciable
        'miPaqueteFormularioBusqueda.DevolucionAutomatica = pIctrlDinamico.ElementoVinc.ElementoMap.DevolucionAutomatica
        'miPaqueteFormularioBusqueda.BusquedaAutomatica = pIctrlDinamico.ElementoVinc.ElementoMap.BusquedaAutomatica


        ''   miPaqueteFormularioBusqueda.EntidadReferidora = pIctrlDinamico.ElementoVinc.InstanciaVinc.DN
        '' sustituir los parametros por los correspondientes valores de los objetos
        'SustituirParametrosPorValores(pIctrlDinamico.ElementoVinc, miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores, miControlP.Marco.DatosMarco)

        '' la lsita de valores de condicones where que limian los que debe o no salir en el filtro

        'miPaqueteFormularioBusqueda.ListaValores = miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores

        Dim tipoSeleccioando, tipoEncapsuladorHuella As System.Type
        Dim miPaqueteFormularioBusqueda As New MotorBusquedaDN.PaqueteFormularioBusqueda
        tipoSeleccioando = DatosMapeadoToPaqueteFormularioBusqueda(pIctrlDinamico, miPaqueteFormularioBusqueda, tipoEncapsuladorHuella)
        miPaqueteFormularioBusqueda.MultiSelect = False

        Dim paquete As New Hashtable
        Dim miControlP As MotorIU.ControlesP.BaseControlP = pIctrlDinamico

        paquete.Add("PaqueteFormularioBusqueda", miPaqueteFormularioBusqueda)
        Dim fmid As System.Windows.Forms.Form
        If miControlP.ParentForm IsNot Nothing Then
            fmid = miControlP.ParentForm.MdiParent
        End If
        miControlP.Marco.Navegar("Filtro", miControlP.ParentForm, fmid, TipoNavegacion.Modal, miControlP.GenerarDatosCarga, paquete)

        If Not paquete Is Nothing Then
            If paquete.Contains("ID") Then
                ' convertimos el id en una huella
                Dim miId As String = paquete("ID")

                If Not String.IsNullOrEmpty(miId) Then

                    If tipoEncapsuladorHuella Is Nothing Then
                        ' devolvemos una huella generica
                        Dim ht As New Framework.DatosNegocio.HEDN(tipoSeleccioando, miId, "")
                        Return pIctrlDinamico.IGestorPersistencia.RecuperarInstancia(ht)

                    Else
                        ' como conocemos el tipo de huella encapsuladora devolvemos una isntacia de ella
                        Dim ht As Framework.DatosNegocio.HEDN = Activator.CreateInstance(tipoEncapsuladorHuella)
                        ht.AsignarDatosBasicos(ht.TipoEntidadReferida, miId, Nothing)
                        Return ht

                    End If

                Else
                    Return Nothing
                End If

            ElseIf paquete.Contains("DN") Then

                If tipoEncapsuladorHuella Is Nothing Then
                    Return paquete("DN")

                Else
                    Dim ht As Framework.DatosNegocio.HEDN = Activator.CreateInstance(tipoEncapsuladorHuella)
                    ht.AsignarEntidad(paquete("DN"))
                    Return ht

                End If



            End If

        End If

        Return Nothing


    End Function


    Public Shared Function CrearInstancia(ByVal pIctrlDinamico As IctrlDinamico) As Object


        Return CrearInstancia(RecuperarTipoSeleccioando(pIctrlDinamico))

    End Function


    Public Shared Function CrearInstancia(ByVal pTipoSeleccioando As System.Type) As Object


        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(pTipoSeleccioando) Then


            Dim huella As Framework.DatosNegocio.IHuellaEntidadDN = Activator.CreateInstance(pTipoSeleccioando)
            huella.AsignarEntidadReferida(Activator.CreateInstance(huella.TipoEntidadReferida))

            Return huella

        Else

            Return Activator.CreateInstance(pTipoSeleccioando)


        End If



    End Function


    Public Shared Function RecuperarTipoSeleccioando(ByVal pIctrlDinamico As IctrlDinamico) As System.Type


        Dim TipoATratar As System.Type
        '1 ver si es una col y obtener el tipo fijado
        TipoATratar = RecuperarTipoATratar(pIctrlDinamico.ElementoVinc)

        '2 ver si el tipo fijado es o no una interfacee
        If Not TipoATratar.IsInterface Then
            Return TipoATratar

        Else
            ' como es una interface habra que selecioanr el tipo

            Return (RecuperarTipoSeleccioando4(pIctrlDinamico))

        End If



    End Function
    Private Shared Function RecuperarTipoSeleccioando4(ByVal pIctrlDinamico As IctrlDinamico) As System.Type




        Dim tipoSeleccioando As System.Type

        Dim pmap As MV2DN.PropMapDN = pIctrlDinamico.ElementoVinc.ElementoMap




        Select Case pmap.ColNombresTiposComaptibles.Count
            Case Is = 0
                ' no hay ningun tipo compatible luego no se puede realizar la operacion



                ' ver si se trata de un submapeado y tine un mapeado que base que defina sus colde tipos compatibles
                Dim ivincReferida As MV2DN.InstanciaVinc = CType(pIctrlDinamico.ElementoVinc, MV2DN.PropVinc).InstanciaVincReferida

                If ivincReferida IsNot Nothing Then

                    ' llamar al emtodo de selecion de tipos

                    Return RecuperarTipoSeleccioandoSeleccion(pIctrlDinamico, ivincReferida.Map.ColNombresTiposComaptibles)

                Else
                    ' en el caso de no estar sub mapeado
                    ' intentaremos para el tipo selecioando buscar un mapeado base que defina sus colde tipos compatibles
                    tipoSeleccioando = RecuperarTipoATratar(pIctrlDinamico.ElementoVinc)

                    Dim miImap As MV2DN.InstanciaMapDN
                    Dim recuperador As MV2DN.IRecuperadorInstanciaMap = pIctrlDinamico.IRecuperadorInstanciaMap
                    miImap = recuperador.RecuperarInstanciaMap(tipoSeleccioando)



                    If miImap IsNot Nothing Then

                        Return RecuperarTipoSeleccioandoSeleccion(pIctrlDinamico, miImap.ColNombresTiposComaptibles)


                    Else


                        ' en este caso podemos recuperar del mapeado de persistencia  si tubiera tipos compatibles para esta interface
                        ' en el caso en que la interface no declarara los tipos compatibles sino que estos se hubieran mapeado en el campo, lo que diga en el campo nunca podria ser tenido en cuenta
                        ' y debiera mapearse en la propiedad del mapeado de visivilidad


                        'TODO: ALEX ...... que si es una interface pueda cargar los datos


                        '' si se recuperan mas de uno debe selecionar el tipo en el selector de tipos
                        'Dim infoMapDatosInst As Framework.TiposYReflexion.DN.InfoDatosMapInstClaseDN
                        ''  Dim gdmi As GestorMapPersistenciaCamposLN = FactoriaGestorMapPersistenciaCamposLN.ObtenerGestor
                        'infoMapDatosInst = gdmi.RecuperarMapPersistenciaCampos(pIctrlDinamico.ElementoVinc.InstanciaVinc.Tipo)

                        '' se verifica si hubiera inforamcion de mapeado de iunterface para el campo vinculado a la propiedad (por el atributo)
                        'Dim infoDatosMapInstCampo As Framework.TiposYReflexion.DN.InfoDatosMapInstCampoDN = Nothing
                        'infoDatosMapInstCampo = infoMapDatosInst.GetCampoXNombre(cv.Campo.Name)

                        'If (infoDatosMapInstCampo.ColCampoAtributo.Contains(Framework.TiposYReflexion.DN.CampoAtributoDN.InterfaceImplementadaPor)) Then


                        '    Dim colTiposImplementan As New Framework.TiposYReflexion.DN.ColVinculoClaseDN
                        '    Dim alDAtosInterface As ArrayList = Nothing
                        '    alDAtosInterface = infoDatosMapInstCampo.MapSubEntidad.ItemDatoMapeado(Framework.TiposYReflexion.DN.CampoAtributoDN.InterfaceImplementadaPor)
                        '    colTiposImplementan.AddRangeObject(alDAtosInterface)
                        '    Return RecuperarTipoSeleccioandoSeleccion(pIctrlDinamico, colTiposImplementan)
                        'End If


                        Dim colTiposImplementan As New Framework.TiposYReflexion.DN.ColVinculoClaseDN

                        Dim gbas As New MotorBusquedaAS.GestorBusquedaAS
                        colTiposImplementan.AddRangeObject(gbas.RecuperarTiposQueImplementan(pIctrlDinamico.ElementoVinc.InstanciaVinc.Tipo, CType(pIctrlDinamico.ElementoVinc, MV2DN.PropVinc).Map.NombreProp))


                        Select Case colTiposImplementan.Count

                            Case Is = 0
                                Throw New ApplicationException("no se logro recuperar informacion para resolver el tipo o tipos que implementan")
                            Case Is = 1
                                'tipoSeleccioando = pmap.ColNombresTiposComaptibles(0).TipoClase
                                Return colTiposImplementan(0).TipoClase
                            Case Is > 1

                                Return RecuperarTipoSeleccioandoSeleccion(pIctrlDinamico, colTiposImplementan)
                        End Select





                    End If


                End If

                Throw New ApplicationException("no se encontró informacion suficiente para maper el tipo")



            Case Is = 1
                ' solo un tipo luego no requiere navegacion a seleccion
                Return pmap.ColNombresTiposComaptibles(0).TipoClase

            Case Is > 1
                ' se debe de selecionar un tipo a si que se lanza el selector

                Return RecuperarTipoSeleccioandoSeleccion(pIctrlDinamico, pmap.ColNombresTiposComaptibles)

        End Select


        Return tipoSeleccioando
    End Function

    Private Shared Function RecuperarTipoSeleccioandoSeleccion(ByVal pIctrlDinamico As IctrlDinamico, ByVal pColTiposCompatibles As Framework.TiposYReflexion.DN.ColVinculoClaseDN) As System.Type

        Dim tipoSeleccioando As System.Type

        Select Case pColTiposCompatibles.Count
            Case Is = 0
                ' no hay ningun tipo compatible luego no se puede realizar la operacion


                tipoSeleccioando = RecuperarTipoATratar(pIctrlDinamico.ElementoVinc)


            Case Is = 1
                ' solo un tipo luego no requiere navegacion a seleccion
                tipoSeleccioando = pColTiposCompatibles(0).TipoClase

            Case Is > 1
                ' se debe de selecionar un tipo a si que se lanza el selector

                Dim mipaquete1 As New Hashtable
                Dim controlp As MotorIU.ControlesP.BaseControlP = pIctrlDinamico

                ' una vez recojidos los tipos hay que recuper los mapeados de visitbilidad para dichos tipos
                ' en contro los que tine a utorizados el empelado
                Dim milistaTipoYMapVisAsociadoDN As New List(Of MV2DN.TipoYMapVisAsociadoDN)

                For Each vc As Framework.TiposYReflexion.DN.VinculoClaseDN In pColTiposCompatibles

                    Dim miTipoYMapVisAsociadoDN As New MV2DN.TipoYMapVisAsociadoDN
                    miTipoYMapVisAsociadoDN.Tipo = vc.TipoClase
                    miTipoYMapVisAsociadoDN.MapVis = pIctrlDinamico.IRecuperadorInstanciaMap.RecuperarInstanciaMap(vc.TipoClase)
                    milistaTipoYMapVisAsociadoDN.Add(miTipoYMapVisAsociadoDN)
                Next


                mipaquete1.Add("LsitaTipoYMapVisAsociadoDN", milistaTipoYMapVisAsociadoDN)
                controlp.Marco.Navegar("SeleccionarTipo", controlp.ParentForm, Nothing, TipoNavegacion.Modal, controlp.GenerarDatosCarga, mipaquete1)
                Dim miTipoYMapVisAsociadoDNSelecioando As MV2DN.TipoYMapVisAsociadoDN = mipaquete1.Item("TipoYMapVisAsociadoDNSeleccioando")
                tipoSeleccioando = miTipoYMapVisAsociadoDNSelecioando.Tipo

        End Select
        Return tipoSeleccioando
    End Function



    Public Shared Function BuscarCol(ByVal pIctrlDinamico As IctrlDinamico) As Framework.DatosNegocio.ColIEntidadBaseDN

        Dim BaseControlP As MotorIU.ControlesP.BaseControlP = pIctrlDinamico

        'Dim tipoSeleccioando As System.Type

        'tipoSeleccioando = RecuperarTipoSeleccioando(pIctrlDinamico)

        '' si el tipo selecciando es una hurlla nos quedamos con su tipo referido
        'If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(tipoSeleccioando) Then
        '    Dim huella As Framework.DatosNegocio.IHuellaEntidadDN = Activator.CreateInstance(tipoSeleccioando)
        '    tipoSeleccioando = huella.TipoEntidadReferida
        'End If

        'If tipoSeleccioando Is Nothing Then
        '    Throw New ApplicationException("El tipo seleccioando debe estar establecidoa este nivel")
        'End If



        ' una vez selecioando el tipo navego al busador 
        'Dim paquete As New Hashtable
        'Dim ParametroCargaEstructuraDN As ParametroCargaEstructuraDN = pIctrlDinamico.IGestorPersistencia.RecuperarParametroBusqueda(pIctrlDinamico.ElementoVinc, tipoSeleccioando)
        'Dim miPaqueteFormularioBusqueda As New MotorBusquedaDN.PaqueteFormularioBusqueda
        'miPaqueteFormularioBusqueda.ParametroCargaEstructura = ParametroCargaEstructuraDN
        'miPaqueteFormularioBusqueda.MultiSelect = True
        'miPaqueteFormularioBusqueda.Agregable = True

        Dim tipoSeleccioando, tipoEncapsuladorHuella As System.Type

        Dim paquete As New Hashtable
        Dim miPaqueteFormularioBusqueda As New MotorBusquedaDN.PaqueteFormularioBusqueda
        tipoSeleccioando = DatosMapeadoToPaqueteFormularioBusqueda(pIctrlDinamico, miPaqueteFormularioBusqueda, tipoEncapsuladorHuella)
        miPaqueteFormularioBusqueda.MultiSelect = True
        miPaqueteFormularioBusqueda.Agregable = True





        paquete.Add("PaqueteFormularioBusqueda", miPaqueteFormularioBusqueda)

        Try
            BaseControlP.Marco.Navegar("Filtro", BaseControlP.FormularioPadre, BaseControlP.FormularioPadre.ParentForm, TipoNavegacion.Modal, BaseControlP.GenerarDatosCarga, paquete)

        Catch ex As Exception
            Return Nothing
        End Try

        ' convertimos el id en una huella
        Dim misIds As IList(Of String) = paquete("IDMultiple")

        If misIds IsNot Nothing AndAlso misIds.Count > 0 Then
            Dim colh As New Framework.DatosNegocio.ColIHuellaEntidadDN

            For Each miid As String In misIds
                colh.Add(New Framework.DatosNegocio.HEDN(tipoSeleccioando, miid, ""))
            Next
            ' recuperar la entidad a partir de la huella 

            Return pIctrlDinamico.IGestorPersistencia.RecuperarColInstancia(colh)

        ElseIf paquete.ContainsKey("DN") Then
            Dim dn As Framework.DatosNegocio.IEntidadBaseDN = paquete("DN")

            If dn IsNot Nothing Then
                Dim col As New Framework.DatosNegocio.ColIEntidadBaseDN
                col.Add(dn)
                Return col
            End If

        End If




        Return Nothing


    End Function

    Public Shared Function RecuperarTipoATratar(ByVal pPropVinc As MV2DN.PropVinc) As System.Type

        Return pPropVinc.TipoATratar

        ' '' seleccionar el tipo a buscar en el mapeado de persistencia
        'Dim tipoaTratar As System.Type
        'If pPropVinc.RepresentaTipoPorReferencia Then
        '    tipoaTratar = pPropVinc.TipoRepresentado
        'Else
        '    If pPropVinc.EsColeccion Then
        '        tipoaTratar = (pPropVinc.TipoFijadoColPropiedad)
        '    Else
        '        tipoaTratar = (pPropVinc.TipoPropiedad)
        '    End If
        'End If


        'Return tipoaTratar
    End Function


    Private Shared Function DatosMapeadoToPaqueteFormularioBusqueda(ByVal pIctrlDinamico As IctrlDinamico, ByVal miPaqueteFormularioBusqueda As MotorBusquedaDN.PaqueteFormularioBusqueda, ByRef ptipoEncapsuladorHuella As System.Type) As System.Type


        ptipoEncapsuladorHuella = Nothing
        Dim tipoSeleccioando As System.Type

        tipoSeleccioando = RecuperarTipoSeleccioando(pIctrlDinamico)

        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaNoTipada(tipoSeleccioando) Then
            Throw New NotImplementedException(" de debiera buscar la informacion en el mapeado de persistencia o un listado de tipos a buscar para vinculaciones genericas")
        End If


        ' si el tipo selecciando es una hurlla nos quedamos con su tipo referido
        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuellaTipada(tipoSeleccioando) Then
            ptipoEncapsuladorHuella = tipoSeleccioando
            Dim huella As Framework.DatosNegocio.IHuellaEntidadDN = Activator.CreateInstance(tipoSeleccioando)
            tipoSeleccioando = huella.TipoEntidadReferida
        End If

        If tipoSeleccioando Is Nothing Then
            Throw New ApplicationException("El tipo seleccioando debe estar establecidoa este nivel")
        End If


        Dim miControlP As MotorIU.ControlesP.BaseControlP = pIctrlDinamico
        Dim ParametroCargaEstructuraDN As ParametroCargaEstructuraDN = pIctrlDinamico.IGestorPersistencia.RecuperarParametroBusqueda(pIctrlDinamico.ElementoVinc, tipoSeleccioando)

        miPaqueteFormularioBusqueda.ParametroCargaEstructura = ParametroCargaEstructuraDN
        miPaqueteFormularioBusqueda.MultiSelect = False
        miPaqueteFormularioBusqueda.Agregable = pIctrlDinamico.ElementoVinc.ElementoMap.Instanciable
        miPaqueteFormularioBusqueda.DevolucionAutomatica = pIctrlDinamico.ElementoVinc.ElementoMap.DevolucionAutomatica
        miPaqueteFormularioBusqueda.BusquedaAutomatica = pIctrlDinamico.ElementoVinc.ElementoMap.BusquedaAutomatica
        miPaqueteFormularioBusqueda.OcultarAccionesxDefecto = pIctrlDinamico.ElementoVinc.ElementoMap.OcultarAccionesxDefecto
        miPaqueteFormularioBusqueda.Filtrable = pIctrlDinamico.ElementoVinc.ElementoMap.Filtrable
        miPaqueteFormularioBusqueda.FiltroVisible = pIctrlDinamico.ElementoVinc.ElementoMap.FiltroVisible


        '   miPaqueteFormularioBusqueda.EntidadReferidora = pIctrlDinamico.ElementoVinc.InstanciaVinc.DN
        ' sustituir los parametros por los correspondientes valores de los objetos
        SustituirParametrosPorValores(pIctrlDinamico.ElementoVinc, miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores, miControlP.Marco.DatosMarco)

        ' la lsita de valores de condicones where que limian los que debe o no salir en el filtro

        miPaqueteFormularioBusqueda.ListaValores = miPaqueteFormularioBusqueda.ParametroCargaEstructura.ListaValores

        Return tipoSeleccioando

    End Function


End Class
