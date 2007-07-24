Imports AuxIU
Imports MotorBusquedaBasicasDN
Public Class frmMain

    Private Sub frmMain_Load(ByVal sender As Object, ByVal e As System.EventArgs) Handles Me.Load
        Try
            'Se comprueba si el usuario es un rol con permisos de administración
            Dim miPrincipal As Framework.Usuarios.DN.PrincipalDN
            miPrincipal = cMarco.DatosMarco.Item("Principal")

            If miPrincipal IsNot Nothing Then
                'If Not (miPrincipal.IsInRole("Admin Negocio") OrElse miPrincipal.IsInRole("Admin Ficheros") OrElse miPrincipal.IsInRole("Administrador Total")) Then
                '    Throw New ApplicationException("El usuario no es un administrador")
                'End If
            Else
                Throw New ApplicationException("Usuario no válido")
            End If

            If Not Framework.Configuracion.AppConfiguracion.DatosConfig.Contains("EnviarMail") Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Add("EnviarMail", False)
            ElseIf Not TypeOf Framework.Configuracion.AppConfiguracion.DatosConfig.Item("EnviarMail") Is Boolean Then
                Framework.Configuracion.AppConfiguracion.DatosConfig.Item("EnviarMail") = False
            End If

            ActivoToolStripMenuItem.Checked = Framework.Configuracion.AppConfiguracion.DatosConfig.Item("EnviarMail")

            ConfigurarAccesosxRol(miPrincipal)

        Catch ex As Exception
            MostrarError(ex)
            Application.Exit()
        End Try
    End Sub

    Private Sub SalirToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles SalirToolStripMenuItem.Click
        Try
            Application.Exit()
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ÁrbolDeEntidadesToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ÁrbolDeEntidadesToolStripMenuItem.Click
        Try
            Me.cMarco.Navegar("AdministracionArbol", Me, Me, MotorIU.Motor.TipoNavegacion.MonoInstancia, Me.GenerarDatosCarga, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub RutasAlmacenamientoToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles RutasAlmacenamientoToolStripMenuItem.Click
        Try
            Me.cMarco.Navegar("AdminRutaAlmacenamiento", Me, Me, MotorIU.Motor.TipoNavegacion.MonoInstancia, Me.GenerarDatosCarga, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub UsuariosToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles UsuariosToolStripMenuItem.Click
        Dim miPaquete As New Hashtable()
        Dim miParametroCargaEst As ParametroCargaEstructuraDN

        Try

            miParametroCargaEst = New ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwUsuariosxEntidadRefSel"
            miParametroCargaEst.NombreVistaVis = "vwUsuariosxEntidadRef"
            miParametroCargaEst.DestinoNavegacion = "AdminUsuarios"
            miParametroCargaEst.TipodeEntidad = GetType(Framework.Usuarios.DN.PrincipalDN)


            Dim miListaCampos As New List(Of String)()
            miListaCampos.Add("Rol")
            miParametroCargaEst.CamposaCargarDatos = miListaCampos

            miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

            miPaquete.Add("MultiSelect", False)
            miPaquete.Add("TipoNavegacion", MotorIU.Motor.TipoNavegacion.Modal)
            miPaquete.Add("Agregable", True)
            miPaquete.Add("EnviarDatatableAlNavegar", False)
            miPaquete.Add("Navegable", True)
            miPaquete.Add("AlternatingBackcolorFiltro", System.Drawing.Color.LightBlue)
            miPaquete.Add("AlternatingBackcolorResultados", System.Drawing.Color.LightBlue)

            miPaquete.Add("Titulo", "Administración de Usuarios")

            Me.cMarco.Navegar("Filtro", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub PermisosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PermisosToolStripMenuItem.Click
        Try
            Me.cMarco.Navegar("AdminPermisosUsuarios", Me, Me, MotorIU.Motor.TipoNavegacion.MonoInstancia, Me.GenerarDatosCarga, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub OperadoresToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OperadoresToolStripMenuItem1.Click
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwOperadoresVis"
            miParametroCargaEst.NombreVistaVis = "vwOperadoresVis"
            miParametroCargaEst.DestinoNavegacion = "EntidadUsuario"
            miParametroCargaEst.TipodeEntidad = GetType(AmvDocumentosDN.OperadorDN)
            miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

            miPaquete.Add("MultiSelect", False)
            miPaquete.Add("TipoNavegacion", MotorIU.Motor.TipoNavegacion.Modal)
            miPaquete.Add("Agregable", True)
            miPaquete.Add("EnviarDatatableAlNavegar", False)
            miPaquete.Add("Navegable", True)
            miPaquete.Add("AlternatingBackcolorFiltro", System.Drawing.Color.LightBlue)
            miPaquete.Add("AlternatingBackcolorResultados", System.Drawing.Color.LightBlue)

            miPaquete.Add("Titulo", "Administración de Operadores")

            Me.cMarco.Navegar("Filtro", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub EstadoDeFicherosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwRelacionesENF"
            miParametroCargaEst.NombreVistaVis = "vwRelacionesENF"
            miParametroCargaEst.DestinoNavegacion = "AdministracionEstadoDocumentos"

            Dim lista As New List(Of String)
            lista.Add("Estado_Operacion")
            miParametroCargaEst.CamposaCargarDatos = lista

            miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

            miPaquete.Add("MultiSelect", True)
            miPaquete.Add("TipoNavegacion", MotorIU.Motor.TipoNavegacion.Modal)
            miPaquete.Add("Navegable", True)
            miPaquete.Add("EnviarDatatableAlNavegar", True)
            miPaquete.Add("AlternatingBackcolorFiltro", System.Drawing.Color.LightBlue)
            miPaquete.Add("AlternatingBackcolorResultados", System.Drawing.Color.LightBlue)

            miPaquete.Add("Titulo", "Selección de Documentos")

            Me.cMarco.Navegar("Filtro", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub AcercaDeGestiónDeDocumentosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AcercaDeGestiónDeDocumentosToolStripMenuItem.Click
        Try
            Me.cMarco.Navegar("Acercade", Me, Me, MotorIU.Motor.TipoNavegacion.Modal, Me.GenerarDatosCarga, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub OperacionesFicheroToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwTrazaOperacionesVis"
            miParametroCargaEst.NombreVistaVis = "vwTrazaOperacionesVis"
            miParametroCargaEst.DestinoNavegacion = "ConsultaTrazaOperaciones"

            Dim lista As New List(Of String)
            lista.Add("Tipo_Operacion")
            lista.Add("Entidad_Negocio")
            lista.Add("Estado_Documento")
            miParametroCargaEst.CamposaCargarDatos = lista

            miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

            miPaquete.Add("MultiSelect", False)
            miPaquete.Add("TipoNavegacion", MotorIU.Motor.TipoNavegacion.Modal)
            miPaquete.Add("Navegable", True)
            miPaquete.Add("EnviarDatatableAlNavegar", False)
            miPaquete.Add("AlternatingBackcolorFiltro", System.Drawing.Color.LightBlue)
            miPaquete.Add("AlternatingBackcolorResultados", System.Drawing.Color.LightBlue)

            miPaquete.Add("Titulo", "Consulta de las operaciones realizadas")

            Me.cMarco.Navegar("Filtro", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ActivoToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ActivoToolStripMenuItem.Click
        Try
            Dim estado As Boolean
            estado = Not Framework.Configuracion.AppConfiguracion.DatosConfig.Item("EnviarMail")
            Framework.Configuracion.AppConfiguracion.DatosConfig.Item("EnviarMail") = estado
            ActivoToolStripMenuItem.Checked = estado
        Catch ex As Exception
            MostrarError(ex)
        End Try

    End Sub

    Private Sub MapeadosVisToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)






    End Sub

    Private Sub FormularioGToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        '    Dim miPaquete As New Hashtable()

        Try



            Me.cMarco.Navegar("FG", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try




    End Sub

    Private Sub BuscadorTiposToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New ParametroCargaEstructuraDN()

            ' miParametroCargaEst.TipodeEntidad = GetType(AmvDocumentosDN.CanalEntradaDocsDN)
            Throw New NotImplementedException





            Dim mipaquetecarga As New MotorBusquedaDN.PaqueteFormularioBusqueda
            mipaquetecarga.ParametroCargaEstructura = miParametroCargaEst
            mipaquetecarga.TipoNavegacion = MotorIU.Motor.TipoNavegacion.Normal
            mipaquetecarga.Agregable = True
            mipaquetecarga.AlternatingBackcolorFiltro = System.Drawing.Color.LightBlue
            mipaquetecarga.AlternatingBackcolorResultados = System.Drawing.Color.LightBlue
            mipaquetecarga.Titulo = "Buscador GENERICO"
            miPaquete.Add("PaqueteFormularioBusqueda", mipaquetecarga)

            Me.cMarco.Navegar("Filtro", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub MapeadosVis2ToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MapeadosVis2ToolStripMenuItem.Click

        Try
            Me.cMarco.Navegar("AdminMapVis", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try



    End Sub

    Private Sub CartasModeloToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CartasModeloToolStripMenuItem.Click
        Try
            'TODO: luis - aquí debería ir al buscador para seleccionar las plantillas que se quieran
            Me.cMarco.Navegar("PlantillaCartaModelo", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub BuscadorDePagosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles BuscadorDePagosToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.PagoDN))
    End Sub

    Private Sub NotificacionesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles NotificacionesToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.NotificacionPagoDN))
    End Sub

    Private Sub OrigenesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OrigenesToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.OrigenDN))
    End Sub

    Private Sub ConfigurarAccesosxRol(ByVal actor As Framework.Usuarios.DN.PrincipalDN)
        If actor IsNot Nothing Then
            If Not actor.IsInRole("Administrador Total") Then
                Me.PruebasSGToolStripMenuItem.Visible = False

                If Not actor.IsInRole("Responsable Negocio") AndAlso Not actor.IsInRole("Responsable contabilidad") AndAlso Not actor.IsInRole("Dirección empresa") Then
                    Me.EntidadesFinancierasToolStripMenuItem.Visible = False
                    Me.LocalizacionesToolStripMenuItem.Visible = False
                    Me.TrazaDePagosToolStripMenuItem.Visible = False
                    Me.NotificacionesToolStripMenuItem.Visible = False
                    If Not actor.IsInRole("Gestor impresión talones") Then
                        Me.AdministraciónPlantillasImpresiónToolStripMenuItem.Visible = False
                    End If
                End If

                If Not actor.IsInRole("Responsable contabilidad") AndAlso Not actor.IsInRole("Dirección empresa") Then
                    Me.PagosToolStripMenuItem1.Visible = False
                End If

                If Not actor.IsInRole("Dirección empresa") Then
                    Me.GestionEmpresasMenuItem1.Visible = False
                End If

                If Not actor.IsInRole("Gestor impresión talones") Then
                    Me.ImprimirTalonesToolStripMenuItem.Visible = False
                End If

                If Not actor.IsInRole("Gestor transferencias") AndAlso Not actor.IsInRole("Dirección empresa") Then
                    Me.TransferenciasToolStripMenuItem.Visible = False
                End If

                If Not actor.IsInRole("Gestor transferencias") Then
                    Me.AdjuntarPagosAFicherosToolStripMenuItem.Visible = False
                End If

            End If

        Else
            Me.ToolStripAdministracion.Visible = False
            Me.AdministrarToolStripMenuItem.Visible = False
            Me.BuscadorPagosToolStripMenuItem.Visible = False
            Me.PruebasSGToolStripMenuItem.Visible = False
        End If


        Me.PruebasSGToolStripMenuItem.Visible = True
    End Sub

    Private Sub ProveedoresToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ProveedoresToolStripMenuItem.Click
        Try
            Dim paquete As New Hashtable()
            paquete.Add("tipoImportacion", "Proveedores")
            Me.cMarco.Navegar("ImportacionFicheros", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, paquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub PagosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PagosToolStripMenuItem.Click
        Try
            Dim paquete As New Hashtable()
            paquete.Add("tipoImportacion", "Pagos")
            Me.cMarco.Navegar("ImportacionFicheros", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, paquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub TiposDeSedeToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TiposDeSedeToolStripMenuItem.Click
        '  MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Empresas.DN.TipoSedeDN))
    End Sub

    Private Sub PlantillasDeCartasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PlantillasDeCartasToolStripMenuItem.Click
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwPlantillaCartaSel"
            miParametroCargaEst.NombreVistaVis = "vwPlantillaCartaSel"
            ' miParametroCargaEst.TipodeEntidad = GetType(FN.GestionPagos.DN.PlantillaCartaDN)
            Throw New NotImplementedException



            miParametroCargaEst.DestinoNavegacion = "PlantillaCartaModelo"

            'Dim lista As New List(Of String)
            'lista.Add("Tipo_Operacion")
            'lista.Add("Entidad_Negocio")
            'lista.Add("Estado_Documento")
            'miParametroCargaEst.CamposaCargarDatos = lista

            miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

            Dim mipaqueteconf As New MotorBusquedaDN.PaqueteFormularioBusqueda
            mipaqueteconf.Agregable = True
            mipaqueteconf.EnviarDatatableAlNavegar = False
            mipaqueteconf.MultiSelect = False
            mipaqueteconf.TipoNavegacion = MotorIU.Motor.TipoNavegacion.Normal
            mipaqueteconf.Titulo = "Plantillas de Cartas Modelo"
            mipaqueteconf.Navegable = True
            mipaqueteconf.ParametroCargaEstructura = miParametroCargaEst

            miPaquete.Add("PaqueteFormularioBusqueda", mipaqueteconf)

            Me.cMarco.Navegar("Filtro", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)

        Catch ex As Exception
            MostrarError(ex)
        End Try

    End Sub

    Private Sub ConfiguraciónImpresiónToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ConfiguraciónImpresiónToolStripMenuItem.Click
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwConfiguracionImpresionTalonSel"
            miParametroCargaEst.NombreVistaVis = "vwConfiguracionImpresionTalonSel"
            ' miParametroCargaEst.TipodeEntidad = GetType(FN.GestionPagos.DN.PlantillaCartaDN)

            Throw New NotFiniteNumberException


            miParametroCargaEst.DestinoNavegacion = "ConfiguracionImpresionTalon"

            'Dim lista As New List(Of String)
            'lista.Add("Tipo_Operacion")
            'lista.Add("Entidad_Negocio")
            'lista.Add("Estado_Documento")
            'miParametroCargaEst.CamposaCargarDatos = lista

            miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

            Dim mipaqueteconf As New MotorBusquedaDN.PaqueteFormularioBusqueda
            mipaqueteconf.Agregable = True
            mipaqueteconf.EnviarDatatableAlNavegar = False
            mipaqueteconf.MultiSelect = False
            mipaqueteconf.TipoNavegacion = MotorIU.Motor.TipoNavegacion.Normal
            mipaqueteconf.Titulo = "Configuración de Impresión de Talones"
            mipaqueteconf.Navegable = True
            mipaqueteconf.ParametroCargaEstructura = miParametroCargaEst

            miPaquete.Add("PaqueteFormularioBusqueda", mipaqueteconf)

            Me.cMarco.Navegar("Filtro", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub OperacionesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OperacionesToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Procesos.ProcesosDN.OperacionDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub AutorizacionPorTipoOrigenToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AutorizacionPorTipoOrigenToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Usuarios.DN.AutorizacionRelacionalDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub LímitesDeFirmaToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LímitesDeFirmaToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Usuarios.DN.PermisoDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub LímitesDePagoToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LímitesDePagoToolStripMenuItem.Click
        Try
            ' MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.LimitePagoDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub AdjuntarPagosAFicherosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AdjuntarPagosAFicherosToolStripMenuItem.Click
        'Dim principal As Framework.Usuarios.DN.PrincipalDN = Me.cMarco.DatosMarco.Item("Principal")

        'Try
        '    If principal IsNot Nothing Then
        '        Dim op As Framework.Procesos.ProcesosDN.OperacionDN = principal.ColOperaciones.RecuperarxNombreVerbo("Adjuntar Fichero Transferencia")

        '        If op IsNot Nothing Then
        '            Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN
        '            colop.Add(op)
        '            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.PagoDN), Nothing, MotorIU.Motor.TipoNavegacion.Normal, "AdjuntarPagoFT", True, colop, Nothing)
        '        Else
        '            MessageBox.Show("El usuario actual no tiene autorizada la operación")
        '        End If

        '    End If

        'Catch ex As Exception
        '    MostrarError(ex)
        'End Try
    End Sub

    Private Sub PolizasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PolizasToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Seguros.Polizas.DN.PolizaDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub RiesgosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RiesgosToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.RiesgosVehiculos.DN.RiesgoMotorDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#Region "Menú Administración - Usuarios"

    Private Sub UsuariosToolStripMenuItem4_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UsuariosToolStripMenuItem4.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Usuarios.DN.PrincipalDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub RolesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles RolesToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Usuarios.DN.RolDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub CasosDeUsoToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CasosDeUsoToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Usuarios.DN.CasosUsoDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region

#Region "Ménú Administración - Trabajos"

    Private Sub AgentesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AgentesToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Trabajos.DN.AgenteDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ServiciosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ServiciosToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Trabajos.DN.ServicioDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub TiposDeServicioToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TiposDeServicioToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Trabajos.DN.TipoServicioDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region

#Region "Menú Administración - Pólizas"

    Private Sub CoberturasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CoberturasToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Seguros.Polizas.DN.CoberturaDN))
    End Sub

    Private Sub ProductosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ProductosToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Seguros.Polizas.DN.ProductoDN))
    End Sub

    Private Sub CompañíasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CompañíasToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Seguros.Polizas.DN.CompaniaDN))
    End Sub

#End Region

#Region "Administración - Riesgos Vehículos"

    Private Sub MarcasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles MarcasToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.RiesgosVehiculos.DN.MarcaDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ModelosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ModelosToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.RiesgosVehiculos.DN.ModeloDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub CategoríasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CategoríasToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.RiesgosVehiculos.DN.CategoriaDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region

#Region "Administración - Tarificador"

    Private Sub PrimasBaseToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PrimasBaseToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.RiesgosVehiculos.DN.PrimaBaseRVDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub CoeficientesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CoeficientesToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.RiesgosVehiculos.DN.ModuladorRVDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ImpuestosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ImpuestosToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.RiesgosVehiculos.DN.ImpuestoRVDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ComisionesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ComisionesToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.RiesgosVehiculos.DN.ComisionRVDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

#End Region


    Private Sub PeriodoDeRenovaciónToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PeriodoDeRenovaciónToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub TarifasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TarifasToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Seguros.Polizas.DN.TarifaDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub PresupuestosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PresupuestosToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Seguros.Polizas.DN.PresupuestoDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub TarificaciónToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TarificaciónToolStripMenuItem.Click
        Try
            Me.cMarco.Navegar("Cuestionario1", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga(), Me.GenerarDatosCarga(), Nothing)
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub PresupuestosActivosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PresupuestosActivosToolStripMenuItem.Click
        Try
            Dim pce As New ParametroCargaEstructuraDN
            pce.NombreVistaSel = "vwPresupActivoPositSel"
            pce.NombreVistaVis = "vwPresupuestoVis"
            pce.TipodeEntidad = GetType(FN.Seguros.Polizas.DN.PresupuestoDN)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, Nothing, pce)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub GenerarinfomreToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            Dim c As New GSAMVControladores.controladorInformes()
            Dim fi As System.IO.FileInfo = c.GenerarInformePresupuesto("1")
            System.Diagnostics.Process.Start(fi.FullName)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub TarificadorStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TarificadorStripMenuItem1.Click
        Try
            Me.cMarco.Navegar("Cuestionario1", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga(), Me.GenerarDatosCarga(), Nothing)
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub PolizasPagosIncidentadosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PolizasPagosIncidentadosToolStripMenuItem.Click
        Try
            Dim pce As New ParametroCargaEstructuraDN
            pce.NombreVistaSel = "vwPeridosRenovacionImpDebIncidentados"
            pce.NombreVistaVis = "vwPeridosRenovacionImpDebIncidentados"
            pce.TipodeEntidad = GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, Nothing, pce)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub PeriodosRenovacionActivosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PeriodosRenovacionActivosToolStripMenuItem.Click
        Try
            Dim pce As New ParametroCargaEstructuraDN
            pce.NombreVistaVis = "vwPeriodoRenovacionVisRapida"
            pce.NombreVistaSel = "vwPeriodosRenovacionActivoSel"
            pce.TipodeEntidad = GetType(FN.Seguros.Polizas.DN.PeriodoRenovacionPolizaDN)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, Nothing, pce)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub LiquidacionesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LiquidacionesToolStripMenuItem.Click


    End Sub

    Private Sub TodasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TodasToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.LiquidacionPagoDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub LiquidacionesDePólizasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LiquidacionesDePólizasToolStripMenuItem.Click
        Try
            Dim pce As New ParametroCargaEstructuraDN
            pce.NombreVistaVis = "vwLiquidacionesxPolizas"
            pce.NombreVistaSel = "vwLiquidacionesxPolizas"
            pce.TipodeEntidad = GetType(FN.GestionPagos.DN.LiquidacionPagoDN)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, Nothing, pce)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ClasificarDocToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ClasificarDocToolStripMenuItem.Click
        Try
            Me.cMarco.Navegar("GED-Clasificar", Me, Nothing, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga(), Me.GenerarDatosCarga(), Nothing)
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try
    End Sub

    Private Sub CDocsIdentificadosNoVincualdosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CDocsIdentificadosNoVincualdosToolStripMenuItem.Click
        Try
            Dim pce As New ParametroCargaEstructuraDN
            pce.Titulo = "Cajones documentos Identificados No Vinculados"
            pce.NombreVistaVis = "vwCajonDocumentoVis"
            pce.NombreVistaSel = "vwCDIdentificadosNoVinculados"
            pce.TipodeEntidad = GetType(Framework.Ficheros.FicherosDN.CajonDocumentoDN)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, Nothing, pce)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub DocsIdentificadosNoVincualdosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DocsIdentificadosNoVincualdosToolStripMenuItem.Click
        Try
            Dim pce As New ParametroCargaEstructuraDN
            pce.Titulo = "Ficheros Identificados No Vinculados"
            pce.NombreVistaVis = "vwHuellaFicheroVis"
            pce.NombreVistaSel = "vwHuellaFicheroIdentificadosNoVinculados"
            pce.TipodeEntidad = GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, Nothing, pce)

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub VincularCDocsToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles VincularCDocsToolStripMenuItem.Click

        Try
            Me.cMarco.Navegar("GDE-Vincualar", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga(), Me.GenerarDatosCarga(), Nothing)
        Catch ex As Exception
            MostrarError(ex, Me)
        End Try

    End Sub

    Private Sub DocumentosIdentificadosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DocumentosIdentificadosToolStripMenuItem.Click
        Try
            Dim pce As New ParametroCargaEstructuraDN
            pce.Titulo = "Buscador de Ficheros identificados"
            pce.NombreVistaVis = "vwHuellaFicheroIdentificadosVis"
            pce.NombreVistaSel = "vwHuellaFicheroIdentificadosVis"
            pce.TipodeEntidad = GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, Nothing, pce)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub CajonesDeDocumentoToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CajonesDeDocumentoToolStripMenuItem.Click
        Try
            Dim pce As New ParametroCargaEstructuraDN
            pce.Titulo = "Buscador de Cajones de Documento"
            pce.NombreVistaVis = "vwCajonDocumentoVis"
            pce.NombreVistaSel = "vwCajonDocumentoVis"
            pce.TipodeEntidad = GetType(Framework.Ficheros.FicherosDN.HuellaFicheroAlmacenadoIODN)
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, Nothing, pce)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub


    Private Sub TiposDeCanalesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TiposDeCanalesToolStripMenuItem.Click
        Dim miPaquete As Hashtable

        Try
            miPaquete = New Hashtable()

            miPaquete.Add("Tipo", GetType(AmvDocumentosDN.TipoCanalDN))
            miPaquete.Add("NombreForm", "Administración del tipo de canales")

            Me.cMarco.Navegar("AdminTipos", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

 
    Private Sub TrazaDePagosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrazaDePagosToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.PagoTrazaDN))
    End Sub

    Private Sub PruebasSGToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PruebasSGToolStripMenuItem.Click

    End Sub

    Private Sub CargarTarifiadorToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CargarTarifiadorToolStripMenuItem.Click
        Dim rvas As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS
        rvas.CargarGrafoTarificacion()
    End Sub

    Private Sub DescargarTarificadorToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DescargarTarificadorToolStripMenuItem.Click
        Dim rvas As New FN.RiesgosVehiculos.AS.RiesgosVehículosAS
        rvas.DesCargarGrafoTarificacion()

    End Sub

    Private Sub ProductosTiposDpcumentosRequeridosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ProductosTiposDpcumentosRequeridosToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Ficheros.FicherosDN.TipoDocumentoRequeridoDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub



    Private Sub CrearDocumentosPruebaToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CrearDocumentosPruebaToolStripMenuItem.Click
        Dim gdoctest As New GDocEntrantesFSTest.GDocEntrantesFSTest
        gdoctest.AltaDocumento()

    End Sub

    Private Sub CrearPresupuestoPruebasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CrearPresupuestoPruebasToolStripMenuItem.Click
        Dim gdoctest As New GestionSegurosAMV.TEST.utPresupuestos

        gdoctest.CrearPresupuesto()
    End Sub

    Private Sub CargarArchivoDeInternetToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CargarArchivoDeInternetToolStripMenuItem.Click
        Try
            Dim frm As New frmCargadorFicherosWeb
            frm.ShowDialog()
        Catch ex As Exception

        End Try
    End Sub
End Class