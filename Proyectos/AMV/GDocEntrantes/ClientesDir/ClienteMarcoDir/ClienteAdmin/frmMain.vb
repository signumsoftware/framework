Imports AuxIU
Imports FN.Personas.DN
Imports FN.Localizaciones.DN
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

    Private Sub ÁrbolDeEntidadesToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Me.cMarco.Navegar("AdministracionArbol", Me, Me, MotorIU.Motor.TipoNavegacion.MonoInstancia, Me.GenerarDatosCarga, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub RutasAlmacenamientoToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs)
        Try
            Me.cMarco.Navegar("AdminRutaAlmacenamiento", Me, Me, MotorIU.Motor.TipoNavegacion.MonoInstancia, Me.GenerarDatosCarga, Nothing)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub UsuariosToolStripMenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles UsuariosToolStripMenuItem.Click
        Dim miPaquete As New Hashtable()
        Dim miParametroCargaEst As MotorBusquedaBasicasDN.ParametroCargaEstructuraDN

        Try

            miParametroCargaEst = New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwUsuariosxEntidadRefSel"
            miParametroCargaEst.NombreVistaVis = "vwUsuariosxEntidadRef"
            miParametroCargaEst.DestinoNavegacion = "AdminUsuarios"

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
            Dim miParametroCargaEst As New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwOperadoresVis"
            miParametroCargaEst.NombreVistaVis = "vwOperadoresVis"
            miParametroCargaEst.DestinoNavegacion = "EntidadUsuario"

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

    Private Sub TiposDeCanalesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Dim miPaquete As Hashtable

        Try
            miPaquete = New Hashtable()

            miPaquete.Add("TipoEntidad", GetType(AmvDocumentosDN.TipoCanalDN))
            miPaquete.Add("NombreForm", "Administración del tipo de canales")

            Me.cMarco.Navegar("AdminTipos", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub EstadoDeFicherosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs)
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN()

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
            Dim miParametroCargaEst As New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN()

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
            Dim miParametroCargaEst As New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN()

            'miParametroCargaEst.NombreVistaSel = "tlCanalEntradaDocsDN"
            ' miParametroCargaEst.NombreVistaVis = "tlCanalEntradaDocsDN"
            '   miParametroCargaEst.DestinoNavegacion = "AdministracionEstadoDocumentos"
            miParametroCargaEst.TipodeEntidad = GetType(AmvDocumentosDN.CanalEntradaDocsDN)
            '  miParametroCargaEst.NombreInstanciaMapVis = "" ' el nombre de una manera en concreto de verlo de no haber se elie por defecto





            'Dim lista As New List(Of String)
            'lista.Add("Estado_Operacion")
            'miParametroCargaEst.CamposaCargarDatos = lista


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

    Private Sub ImprimirTalonesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ImprimirTalonesToolStripMenuItem.Click
        ' MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.PagoDN))

        Dim pinciopal As Framework.Usuarios.DN.PrincipalDN = Me.cMarco.DatosMarco.Item("Principal")
        If pinciopal IsNot Nothing Then
            Dim op As Framework.Procesos.ProcesosDN.OperacionDN = pinciopal.ColOperaciones.RecuperarxNombreVerbo("Impresión")

            If op IsNot Nothing Then

                Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN
                colop.Add(op)
                '      MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.PagoDN), Nothing, MotorIU.Motor.TipoNavegacion.Normal, "PreImpresionTalones", True, colop, Nothing)
                MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.PagoDN), Nothing, MotorIU.Motor.TipoNavegacion.Normal, "PreImpresionTalones", True, colop, Nothing, Nothing, Nothing)

            Else
                MessageBox.Show("El usuario actual no tiene autorizada la operación")
            End If

        End If

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

    Private Sub TrazaDePagosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrazaDePagosToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.PagoTrazaDN))
    End Sub


    Private Sub TiposOrigenesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TiposOrigenesToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.TipoEntidadOrigenDN))
    End Sub

    Private Sub EmpresasToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EmpresasToolStripMenuItem1.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Empresas.DN.EmpresaFiscalDN))
    End Sub

    Private Sub PersonasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PersonasToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(PersonaFiscalDN))
    End Sub

    Private Sub CuentasBancariasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles CuentasBancariasToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Financiero.DN.CuentaBancariaDN))
    End Sub

    Private Sub EspacialesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EspacialesToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(PaisDN))
    End Sub

    Private Sub ProvinciasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ProvinciasToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(ProvinciaDN))
    End Sub

    Private Sub LocalidadesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles LocalidadesToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(LocalidadDN))
    End Sub

    Private Sub EmpleadosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EmpleadosToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Empresas.DN.EmpleadoYPuestosRDN))
    End Sub

    Private Sub UsuariosToolStripMenuItem1_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles UsuariosToolStripMenuItem1.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Usuarios.DN.PrincipalDN))
    End Sub

    Private Sub EmpresasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles EmpresasToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Empresas.DN.EmpresaDN))
    End Sub

    Private Sub SedesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles SedesToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Empresas.DN.SedeEmpresaDN))
    End Sub

    Private Sub DepartamentosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles DepartamentosToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Empresas.DN.DepartamentoDN))
    End Sub

    Private Sub TiposEmpresaToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TiposEmpresaToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Empresas.DN.TipoEmpresaDN))
    End Sub

    Private Sub TiposDeSedeToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TiposDeSedeToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.Empresas.DN.TipoSedeDN))
    End Sub

    Private Sub PlantillasDeCartasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles PlantillasDeCartasToolStripMenuItem.Click
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwPlantillaCartaSel"
            miParametroCargaEst.NombreVistaVis = "vwPlantillaCartaSel"
            miParametroCargaEst.TipodeEntidad = GetType(FN.GestionPagos.DN.PlantillaCartaDN)

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
            Dim miParametroCargaEst As New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwConfiguracionImpresionTalonSel"
            miParametroCargaEst.NombreVistaVis = "vwConfiguracionImpresionTalonSel"
            miParametroCargaEst.TipodeEntidad = GetType(FN.GestionPagos.DN.PlantillaCartaDN)

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


    Private Sub ReemplazosModelosCartaToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ReemplazosModelosCartaToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.ReemplazosTextoCartasDN))
    End Sub

    Private Sub OperacionesToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles OperacionesToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Procesos.ProcesosDN.OperacionDN))
    End Sub

    Private Sub AutorizacionPorTipoOrigenToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AutorizacionPorTipoOrigenToolStripMenuItem.Click
        MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(Framework.Usuarios.DN.AutorizacionRelacionalDN))
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
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.LimitePagoDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub AdjuntarPagosAFicherosToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles AdjuntarPagosAFicherosToolStripMenuItem.Click
        Dim principal As Framework.Usuarios.DN.PrincipalDN = Me.cMarco.DatosMarco.Item("Principal")

        Try
            If principal IsNot Nothing Then
                Dim op As Framework.Procesos.ProcesosDN.OperacionDN = principal.ColOperaciones.RecuperarxNombreVerbo("Adjuntar Fichero Transferencia")

                If op IsNot Nothing Then
                    Dim colop As New Framework.Procesos.ProcesosDN.ColOperacionDN
                    colop.Add(op)
                    MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.PagoDN), Nothing, MotorIU.Motor.TipoNavegacion.Normal, "AdjuntarPagoFT", True, colop, Nothing, Nothing, Nothing)
                Else
                    MessageBox.Show("El usuario actual no tiene autorizada la operación")
                End If

            End If

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub FicherosDeTransferenciasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles FicherosDeTransferenciasToolStripMenuItem.Click
        Try
            MotorBusquedaIuWinCtrl.NavegadorHelper.NavegarABuscador(Me, GetType(FN.GestionPagos.DN.FicheroTransferenciaDN))
        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub TrazaFicherosTransferenciasToolStripMenuItem_Click(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TrazaFicherosTransferenciasToolStripMenuItem.Click
        Try
            Dim miPaquete As New Hashtable()
            Dim miParametroCargaEst As New MotorBusquedaBasicasDN.ParametroCargaEstructuraDN()

            miParametroCargaEst.NombreVistaSel = "vwTrazaFicherosTransferencias"
            miParametroCargaEst.NombreVistaVis = "vwTrazaFicherosTransferencias"
            miParametroCargaEst.TipodeEntidad = GetType(FN.GestionPagos.DN.FicheroTransferenciaDN)

            miPaquete.Add("ParametroCargaEstructura", miParametroCargaEst)

            Dim mipaqueteconf As New MotorBusquedaDN.PaqueteFormularioBusqueda()
            mipaqueteconf.Agregable = False
            mipaqueteconf.EnviarDatatableAlNavegar = False
            mipaqueteconf.MultiSelect = False
            mipaqueteconf.TipoNavegacion = MotorIU.Motor.TipoNavegacion.Normal
            mipaqueteconf.Titulo = "Traza de los ficheros de transferencias"
            mipaqueteconf.Navegable = True
            mipaqueteconf.ParametroCargaEstructura = miParametroCargaEst

            miPaquete.Add("PaqueteFormularioBusqueda", mipaqueteconf)

            Me.cMarco.Navegar("Filtro", Me, Me, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosCarga, miPaquete)

        Catch ex As Exception
            MostrarError(ex)
        End Try
    End Sub

    Private Sub ÁrbolDeEntidadesToolStripMenuItem_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles ÁrbolDeEntidadesToolStripMenuItem.Click

    End Sub

    Private Sub TiposDeCanalesToolStripMenuItem_Click_1(ByVal sender As System.Object, ByVal e As System.EventArgs) Handles TiposDeCanalesToolStripMenuItem.Click

    End Sub
End Class