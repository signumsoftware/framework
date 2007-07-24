Imports MV2ControlesBasico
Public Class frmFormularioGenerico

    Protected mColTransiciones As Framework.Procesos.ProcesosDN.ColTransicionRealizadaDN

    Private Sub frmFormularioGenerico_Leave(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Leave
        If Not Me.Paquete Is Nothing Then
    
            If Not Me.Paquete.ContainsKey("Resultado") Then
                Me.Paquete.Add("Resultado", Resultado.Cancelar)
            End If

        End If
    End Sub

    Private Sub frmFormularioGenerico_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load

        OculatarMostrarBotones()
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' cargar los datos
        ' recupera del paquete la entidad dn o crea una a partir del tipo
        Dim entidad As Framework.DatosNegocio.IEntidadBaseDN = ExtraerDatos()


        ''''''''''''''''''''''''''''''''''''''''''''''''''''''
        CargarEntidad(entidad)

    End Sub

    Public Overrides Sub Inicializar()
        MyBase.Inicializar()

        'OculatarMostrarBotones()
        ''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        '' cargar los datos
        '' recupera del paquete la entidad dn o crea una a partir del tipo
        'Dim entidad As Framework.DatosNegocio.IEntidadBaseDN = ExtraerDatos()


        '''''''''''''''''''''''''''''''''''''''''''''''''''''''
        'CargarEntidad(entidad)
    End Sub

    Public Sub CargarEntidad(ByVal entidad As Framework.DatosNegocio.IEntidadBaseDN)

        If entidad Is Nothing Then
            MostrarError(New ApplicationException("No se ha podido recuperar la entidad solicitada o no existe"))
            Me.Close()
        End If

        ' obtine el tipo de la intacia creada o recuperada
        Dim tipo As System.Type = CType(entidad, Object).GetType
        Me.CtrlGD1.TipoEntidad = tipo



        '''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' si el paquete de navegación determina un mapado de visivilidad de le asigna al control, el paquete manda
        Dim nombreMapeadoVis As String = Nothing

        If Me.Paquete.Contains("NombreInstanciaMapVis") Then
            nombreMapeadoVis = Me.Paquete("NombreInstanciaMapVis")
            Me.CtrlGD1.NombreInstanciaMap = nombreMapeadoVis
        End If


        ' pongo el recuperador de mapeados al ctrlgd
        Dim recmap As New MV2DN.RecuperadorMapeadoXFicheroXMLAD(Framework.Configuracion.AppConfiguracion.DatosConfig("RutaCargaMapVis"))
        Me.CtrlGD1.RecuperadorMap = recmap


        ' si en el paquete no venia determinado un mapeado coje el por defecto que diga el recuperador
        'If String.IsNullOrEmpty(nombreMapeadoVis) Then
        '    Me.CtrlGD1.InstanciaMap = recmap.RecuperarInstanciaMap(tipo)
        'Else
        '    Me.CtrlGD1.InstanciaMap = recmap.RecuperarInstanciaMap(nombreMapeadoVis)
        '    If Me.CtrlGD1.InstanciaMap Is Nothing Then
        '        Me.CtrlGD1.InstanciaMap = Me.CtrlGD1.GenerarMapeadoBasicoEntidadDN(tipo) ' TODO:repasar
        '    End If
        'End If

        Me.CtrlGD1.CargarInstanciaMapSiNoExiste()
        ' poner en modo no editable si lo solicita el origen de navegacion
        If Me.Paquete.Contains("Editable") Then
            Me.CtrlGD1.InstanciaMap.Editable = Me.Paquete("Editable")
        End If



        ' crear los botones en la barra de herramientas

        PoblarBarraHerramientasFormulario(Me.CtrlGD1.InstanciaMap, entidad)

        ' impedir que se modifique la entidad dn si las operaciones no permiten modificar la entidad y el mapeado de visibilidad tampoc


        ' el nombre del formulario
        If Me.CtrlGD1.InstanciaMap IsNot Nothing Then
            Me.Text = "Formulario de: " & Me.CtrlGD1.InstanciaMap.NombreVis
        End If

        Me.Height = 0

        If Me.CtrlGD1.InstanciaMap IsNot Nothing Then
            If Me.CtrlGD1.InstanciaMap.Alto <> -1 Then
                Me.Height = Me.CtrlGD1.InstanciaMap.Alto
            End If
            If Me.CtrlGD1.InstanciaMap.Ancho <> -1 Then
                Me.Width = Me.CtrlGD1.InstanciaMap.Ancho
            End If
        End If

        Me.CtrlGD1.Poblar() ' TODO: posiblemente esto no es necesario porque se realiza al asignar la entidad
        Me.CtrlGD1.DN = entidad


        If Me.CtrlGD1.InstanciaMap IsNot Nothing AndAlso Me.CtrlGD1.InstanciaMap.ArbolNavegacionVisible Then

            If Me.CtrlGD1.InstanciaMap.NavegacionesAutomaticas Then
                Me.ctrlArbolNavD21.VincularEntidad(entidad)
            End If


            ' asiganr las entradas a buscadores del arbol
            Me.ctrlArbolNavD21.VincularBusquedaATipo(Me.CtrlGD1.InstanciaMap.ColEntradaMapNavBuscadorDN)
            Me.ctrlArbolNavD21.TreeView1.ExpandAll()
        Else
            Me.SplitContainer1.Panel1Collapsed = True
        End If



        ' Me.CtrlGD1.BackColor = Color.BurlyWood

        Me.Height = Me.CtrlGD1.TamañoResultanteVinculacionDN + 80
        '  Me.TableLayoutPanel1.Height = Me.CtrlGD1.TamañoResultanteVinculacionDN

    End Sub


    Private Function ExtraerDatos() As Framework.DatosNegocio.IEntidadBaseDN
        Dim tipo As System.Type = Nothing
        Dim entidad As Framework.DatosNegocio.IEntidadDN = Nothing

        If Me.Paquete Is Nothing Then

            Throw New ApplicationException("no se pasó ningún paquete desde el origen de navegación")

        Else


            If Me.Paquete.Contains("TipoEntidad") Then
                tipo = Me.Paquete("TipoEntidad")
            End If


            If Me.Paquete.Contains("DN") Then
                entidad = Me.Paquete("DN")
                If entidad Is Nothing Then
                    Throw New ApplicationException("el paquete contiene una entrada de dn nothing")
                End If
                If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(CType(entidad, Object).GetType) Then

                    Dim h As Framework.DatosNegocio.IHuellaEntidadDN = entidad

                    If h.EntidadReferida Is Nothing Then

                        Dim mias As New Framework.AS.MV2AS
                        entidad = mias.RecuperarDNGenerico(h) ' TODO: homogenizar con lo de luis y tipar el paquete
                    Else
                        entidad = h.EntidadReferida
                    End If

                Else
                    tipo = CType(entidad, Object).GetType

                End If
            Else

                If tipo Is Nothing Then
                    Throw New ApplicationException("en el paquete tipo Is Nothing o no se paso una DN ")
                Else

                    If Me.Paquete.Contains("ID") Then
                        Dim identidad As String = Me.Paquete("ID")
                        ' se recupera la entidad del sistema 
                        Dim mias As New Framework.AS.MV2AS
                        entidad = mias.RecuperarDNGenerico(New Framework.DatosNegocio.HEDN(tipo, identidad, Nothing))

                    ElseIf Me.Paquete.Contains("GUID") Then
                        Dim guid As String = Me.Paquete("GUID")
                        ' se recupera la entidad del sistema 
                        Dim mias As New Framework.AS.MV2AS
                        entidad = mias.RecuperarDNGenerico(New Framework.DatosNegocio.HEDN(tipo, Nothing, guid))


                    Else

                        ' verificar si la entidad referido sabe itnanciar su entidad referida

                        If Me.Paquete.Contains("EntidadReferidora") AndAlso Me.Paquete("EntidadReferidora") IsNot Nothing AndAlso TypeOf Me.Paquete("EntidadReferidora") Is Framework.DatosNegocio.IEntidadDN Then
                            Dim entidadReferidora As Framework.DatosNegocio.IEntidadDN = Me.Paquete("EntidadReferidora")

                            If Me.Paquete.Contains("PropiedadReferidora") AndAlso Me.Paquete("PropiedadReferidora") IsNot Nothing AndAlso TypeOf Me.Paquete("PropiedadReferidora") Is Reflection.PropertyInfo Then
                                entidad = entidadReferidora.InstanciarEntidad(tipo, Me.Paquete("PropiedadReferidora"))
                            Else
                                entidad = entidadReferidora.InstanciarEntidad(tipo)
                            End If

                        End If

                        ' si no sabe sera labor del usuario a sique la intaicoamos utilizando el contructuro vacio
                        If entidad Is Nothing Then
                            entidad = Activator.CreateInstance(tipo)
                        End If

                    End If

                End If
            End If
        End If



        Return entidad

    End Function



    Public Function CargarEntidad(ByVal entidad As Framework.DatosNegocio.IEntidadDN) As Framework.DatosNegocio.IEntidadDN

        If entidad Is Nothing Then
            Throw New ApplicationException("se pasó una entidad   dn nothing")
        End If

        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(CType(entidad, Object).GetType) Then

            Dim h As Framework.DatosNegocio.IHuellaEntidadDN = entidad

            If h.EntidadReferida Is Nothing Then

                Dim mias As New Framework.AS.MV2AS
                entidad = mias.RecuperarDNGenerico(h) ' TODO: homogenizar con lo de luis y tipar el paquete
            Else
                entidad = h.EntidadReferida
            End If

        Else
            ' tipo = CType(entidad, Object).GetType

        End If

        Return entidad
    End Function


    Private Sub PoblarBarraHerramientasFormulario(ByVal pInstanciaMapDN As MV2DN.InstanciaMapDN, ByVal pDN As Framework.DatosNegocio.IEntidadDN)

        ' eliminar los botones actuales
        Me.ToolStrip1.Items.Clear()
        If pInstanciaMapDN Is Nothing Then
            Exit Sub
        End If


        ' si se viene  de una navegacion modal añadir el boton aceptar

        If Me.Modal Then
            Dim img As Image = ProveedorImagenes.ObtenerImagen("Aceptar")
            Me.ToolStrip1.Items.Add(New ToolStripButton("Aceptar", img, Nothing, "AceptarForm"))
        Else
            If (pInstanciaMapDN.Instanciable OrElse (pInstanciaMapDN.Editable AndAlso Not String.IsNullOrEmpty(pDN.ID))) AndAlso Not pInstanciaMapDN.OcultarAccionesxDefecto Then


                Dim img As Image
                'img = MV2Controles.ProveedorImagenes.ObtenerImagen("CrearNueva")
                'Me.ToolStrip1.Items.Add(New ToolStripButton("Crear Nueva", img, Nothing, "CrearNueva"))

                'img = MV2Controles.ProveedorImagenes.ObtenerImagen("RechazarCambios")
                'Me.ToolStrip1.Items.Add(New ToolStripButton("Rechazar Cambios", img, Nothing, "RechazarCambios"))


                'img = MV2Controles.ProveedorImagenes.ObtenerImagen("DesHacer")
                'Me.ToolStrip1.Items.Add(New ToolStripButton("DesHacer", img, Nothing, "DesHacer"))

                'img = MV2Controles.ProveedorImagenes.ObtenerImagen("ReHacer")
                'Me.ToolStrip1.Items.Add(New ToolStripButton("ReHacers", img, Nothing, "ReHacer"))



                img = ProveedorImagenes.ObtenerImagen("Guardar")
                Me.ToolStrip1.Items.Add(New ToolStripButton("Guardar", img, Nothing, "GuardarForm"))

            End If
        End If


        ' los ComandoMapDN introducidas por el mapeado
        For Each operacion As MV2DN.ComandoMapDN In pInstanciaMapDN.ColComandoMap

            Dim img As Image = ProveedorImagenes.ObtenerImagen(operacion.Ico)
            If String.IsNullOrEmpty(operacion.NombreVis) Then
                Dim tsB As New ToolStripButton("Sin Nombre", img, Nothing, operacion.GUID)
                tsB.DisplayStyle = ToolStripItemDisplayStyle.Image
                Me.ToolStrip1.Items.Add(tsB)
            Else
                Dim tsB As New ToolStripButton(operacion.NombreVis, img, Nothing, operacion.GUID)
                tsB.DisplayStyle = ToolStripItemDisplayStyle.Image
                Me.ToolStrip1.Items.Add(tsB)
            End If

        Next


        ' las operaciones introducidas por el gestor de flujos
        If Not pDN Is Nothing Then

            ' Dim miOperacionesAS As New Framework.Procesos.ProcesosAS.OperacionesAS
            ' Dim he As Framework.DatosNegocio.HEDN
            ' he = New Framework.DatosNegocio.HEDN(pDN)
            '  mColTransiciones = miOperacionesAS.RecuperarOperacionesAutorizadasSobre(he)

            Dim procLNC As New Framework.Procesos.ProcesosLNC.ProcesoLNC
            mColTransiciones = procLNC.RecuperarOperacionesAutorizadasSobreLNC(pDN)
            If mColTransiciones Is Nothing Then

            Else

                For Each transicion As Framework.Procesos.ProcesosDN.TransicionRealizadaDN In mColTransiciones
                    'If Not transicion.Transicion.Automatica AndAlso Not transicion.EsFinalizacion Then
                    Dim img As Image = ProveedorImagenes.ObtenerImagen(transicion.Transicion.OperacionDestino.RutaIcono)
                    Dim tsB As New ToolStripButton(transicion.Transicion.OperacionDestino.Nombre, img, Nothing, transicion.GUID)
                    tsB.DisplayStyle = ToolStripItemDisplayStyle.Image
                    Me.ToolStrip1.Items.Add(tsB)

                    If transicion.OperacionRealizadaOrigen IsNot Nothing AndAlso transicion.OperacionRealizadaOrigen.ObjetoIndirectoOperacion IsNot Nothing Then
                        If transicion.OperacionRealizadaOrigen.ObjetoIndirectoOperacion.GUID = pDN.GUID Then
                            transicion.OperacionRealizadaOrigen.ObjetoIndirectoOperacion = pDN
                        Else
                            Throw New Framework.DatosNegocio.ApplicationExceptionDN("No coincide la entidad pasada con el objeto indirecto de la operacion")
                        End If

                    End If


                    ' End If
                Next

            End If
        End If

    End Sub







    Private Sub OculatarMostrarBotones()

        'If Me.Modal Then
        '    Me.btnGuardar.Visible = False
        'Else
        '    Me.btnAceptar.Visible = False

        'End If


    End Sub



#Region "Botones"

    Private Sub ToolStrip1_ItemClicked(ByVal sender As Object, ByVal e As System.Windows.Forms.ToolStripItemClickedEventArgs) Handles ToolStrip1.ItemClicked

        Me.CtrlGD1.IUaDNgd()
        Dim pInstanciaMap As MV2DN.InstanciaMapDN = Me.CtrlGD1.InstanciaMap






        Dim boton As ToolStripButton = e.ClickedItem

        ' todas las operaciones tienen iu a dn menos si se pulso o si la instancia era read only
        If Me.CtrlGD1.InstanciaVinc.Map.Editable Then
            Me.CtrlGD1.IUaDNgd()
        End If


        Select Case boton.Name

            Case "CrearNueva"

            Case "RechazarCambios"
            Case "DesHacer"
            Case "ReHacer"

            Case "AceptarForm"
                Me.AceptarYVolver()
            Case "GuardarForm"
                Me.GuardarGenerico()

            Case Else



                ' encontrar la operacion solicitada
                Dim Comando As MV2DN.ComandoMapDN = pInstanciaMap.ColComandoMap.RecuperarXGUID(boton.Name)


                If Not Comando Is Nothing Then


                    EjecutarCoamndo(Comando)

                Else


                    EjecutarOperacion(boton)
                End If



        End Select









    End Sub



    Private Sub GuardarGenerico()
        Me.CtrlGD1.IUaDNgd()
        ' cofigo listo para guardar

        Dim mensaje As String
        Dim edn As Framework.DatosNegocio.IDatoPersistenteDN = Me.CtrlGD1.DN
        If edn.EstadoIntegridad(mensaje) = Framework.DatosNegocio.EstadoIntegridadDN.Consistente Then
            Try
                Dim mias As New Framework.AS.MV2AS
                Me.CtrlGD1.DN = mias.GuardarDNGenerico2(edn, Me)
            Catch ex As Exception
                Me.MostrarError(ex)
            End Try

        Else
            MessageBox.Show(mensaje)


        End If

    End Sub

    Private Sub EjecutarCoamndo(ByVal Comando As MV2DN.ComandoMapDN)
        '  se trata de un comando de mapeado de mapeado




        Dim miIRecuperadorEjecutoresDeCliente As New Framework.Procesos.ProcesosAS.RecuperadorEjecutoresDeClienteAS

        Try
            Me.CtrlGD1.DN = miIRecuperadorEjecutoresDeCliente.EjecutarVinculoMetodo(Me.CtrlGD1, Me.CtrlGD1.DN, Comando.VinculoMetodo)
            PoblarBarraHerramientasFormulario(Me.CtrlGD1.InstanciaMap, Me.CtrlGD1.DN)

        Catch ex As Exception
            If ex.InnerException Is Nothing Then
                Me.MostrarError(ex)
            Else
                Me.MostrarError(ex.InnerException)
            End If

        End Try





        ' Dim miIRecuperadorEjecutoresDeCliente As New Framework.Procesos.ProcesosAS.RecuperadorEjecutoresDeClienteAS
        '' ejecutar el metodo del controlador

        '' ojo esta es una operacion simbple que no puede tener sub transiciones




        'Dim tranR As New Framework.Procesos.ProcesosDN.TransicionRealizadaDN
        'Dim tr As New Framework.Procesos.ProcesosDN.TransicionDN
        'Dim opr As New Framework.Procesos.ProcesosDN.OperacionRealizadaDN
        'opr.Operacion = operacion
        'tranR.OperacionRealizadaDestino = opr
        'tranR.Transicion = tr
        'tranR.Transicion.OperacionDestino = opr.Operacion
        'opr.Operacion.VerboOperacion.Nombre = opr.Operacion.Nombre
        'Try

        '    Me.CtrlGD1.DN = miIRecuperadorEjecutoresDeCliente.EjecutarMethodInfo("Cliente1", tranR, Me.CtrlGD1.DN)

        'Catch ex As Exception
        '    Me.MostrarError(ex.InnerException)
        'End Try

    End Sub

    Private Sub EjecutarOperacion(ByVal boton As ToolStripButton)





        Try
            ' recuperar la transicion de la coleccion de transiciones cacheada
            Dim tranR As Framework.Procesos.ProcesosDN.TransicionRealizadaDN = mColTransiciones.RecuperarXGUID(boton.Name)

            Dim procLNC As New Framework.Procesos.ProcesosLNC.ProcesoLNC
            Dim entidad As Object = procLNC.EjecutarOperacionLNC(Me.cMarco.Principal, tranR, Me.CtrlGD1.DN, Me)


            PoblarBarraHerramientasFormulario(Me.CtrlGD1.InstanciaMap, entidad)
            'Me.CtrlGD1.Poblar()
            Me.CtrlGD1.DN = entidad

        Catch ex As Exception
            If ex.InnerException Is Nothing Then
                Me.MostrarError(ex)
            Else
                Me.MostrarError(ex.InnerException)
            End If


        End Try



    End Sub






    Private Sub AceptarYVolver()


        Me.CtrlGD1.IUaDNgd()
        If Not Me.Paquete Is Nothing Then
            If Me.Paquete.ContainsKey("DN") Then
                Me.Paquete.Item("DN") = Me.CtrlGD1.DN

            Else
                Me.Paquete.Add("DN", Me.CtrlGD1.DN)

            End If

            If Me.Paquete.ContainsKey("Resultado") Then
                Me.Paquete.Item("Resultado") = Resultado.Aceptar
            Else
                Me.Paquete.Add("Resultado", Resultado.Aceptar)
            End If

        End If
        Me.Close()
    End Sub

#End Region



    Private Sub CtrlGD1_DNaIUgdFInalizado(ByVal sender As Object, ByVal e As System.EventArgs) Handles CtrlGD1.DNaIUgdFInalizado
        Me.Height = Me.CtrlGD1.Height + 100

    End Sub

    Private Sub SplitContainer1_Panel1_Paint(ByVal sender As System.Object, ByVal e As System.Windows.Forms.PaintEventArgs) Handles SplitContainer1.Panel1.Paint

    End Sub

    Private Sub ctrlArbolNavD21_NodoDobleClik(ByVal sender As Object, ByVal e As MNavegacionDatosIUWin.ctrlArbolNavD2EventArgs) Handles ctrlArbolNavD21.NodoDobleClik
        OculatarMostrarBotones()
        '''''''''''''''''''''''''''''''''''''''''''''''''''''''
        ' cargar los datos

        Dim entidad As Framework.DatosNegocio.IEntidadBaseDN = e.huella

        ''''''''''''''''''''''''''''''''''''''''''''''''''''''


        If Framework.TiposYReflexion.LN.InstanciacionReflexionHelperLN.EsHuella(CType(entidad, Object).GetType) Then

            Dim h As Framework.DatosNegocio.IHuellaEntidadDN = entidad

            If h.EntidadReferida Is Nothing Then

                Dim mias As New Framework.AS.MV2AS
                entidad = mias.RecuperarDNGenerico(h)
            Else
                entidad = h.EntidadReferida
            End If

        End If


        Me.CtrlGD1.Clear()


        CargarEntidad(entidad)
    End Sub



End Class




Public Enum Resultado
    Aceptar
    Cancelar
End Enum