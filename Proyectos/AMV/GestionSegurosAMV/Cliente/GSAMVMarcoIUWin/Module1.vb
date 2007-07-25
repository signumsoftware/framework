Module Module1

    Public Sub Main()
        Application.EnableVisualStyles()

        Dim tablanavegacion As New Hashtable

        tablanavegacion.Add("Autorizacion", New MotorIU.Motor.Destino(GetType(GSAMVMarcoIUWin.frmAutorizacion), GetType(GSAMVMarcoIUWin.frmAutorizacionctrl))) ' "ClienteAdmin.frmAutorizacion", "ClienteAdmin.frmAutorizacionctrl"))
        tablanavegacion.Add("Main", New MotorIU.Motor.Destino(GetType(GSAMVMarcoIUWin.frmMain), Nothing)) ' "ClienteAdmin.frmMain", "ClienteAdmin.ControladorGenerico"))
        tablanavegacion.Add("Filtro", New MotorIU.Motor.Destino(GetType(MotorBusquedaIuWin.frmFiltro), GetType(MotorBusquedaIuWin.frmFiltroctrl))) ' "MotorBusquedaIuWin.frmFiltro", "MotorBusquedaIuWin.frmFiltroctrl"))
        tablanavegacion.Add("Acercade", New MotorIU.Motor.Destino(GetType(GSAMVMarcoIUWin.frmAcercaDe), Nothing)) ' "ClienteAdmin.frmAcercaDe", "ClienteAdmin.ControladorGenerico"))
        tablanavegacion.Add("AdminMapVis", New MotorIU.Motor.Destino(GetType(MV2ConfiguradorIUWin.Form1), Nothing)) ' "MV2ConfiguradorIUWin.Form1", Nothing))
        tablanavegacion.Add("SeleccionarTipo", New MotorIU.Motor.Destino(GetType(MV2Controles.frmSeleccionarTipo), Nothing)) '"MV2Controles.frmSeleccionarTipo", Nothing))
        tablanavegacion.Add("FG", New MotorIU.Motor.Destino(GetType(MV2Controles.frmFormularioGenerico), GetType(MotorIU.FormulariosP.ControladorFormBase))) ' "MV2Controles.frmFormularioGenerico", Nothing))

        tablanavegacion.Add("Cuestionario1", New MotorIU.Motor.Destino(GetType(frmCuestionario1), GetType(GSAMVControladores.ctrlCuestionarioFrm)))

        ' GDE - CLASIFICAR DOC
        tablanavegacion.Add("GED-Clasificar", New MotorIU.Motor.Destino(GetType(GDocEntrantes.frmDocsEntrantes), GetType(Controladores.frmDocsEntrantesctrl)))
        tablanavegacion.Add("GDE-Vincualar", New MotorIU.Motor.Destino(GetType(Framework.Ficheros.FicherosIU.FicherosVinculadosFrm), GetType(MotorIU.FormulariosP.ControladorFormBase)))
        tablanavegacion.Add("SeleccionArbol", New MotorIU.Motor.Destino(GetType(GDocEntrantes.frmArbol), GetType(Controladores.frmArbolctrl))) ' "GDocEntrantes.frmArbol", "Controladores.frmArbolctrl"))
        tablanavegacion.Add("Archivo", New MotorIU.Motor.Destino(GetType(GDocEntrantes.frmArchivo), GetType(Controladores.frmArchivoctrl))) ' "GDocEntrantes.frmArchivo", "Controladores.frmArchivoctrl"))

        'GDE - ADMIN
        tablanavegacion.Add("EntidadUsuario", New MotorIU.Motor.Destino(GetType(ClienteAdmin.frmOperador), GetType(ClienteAdmin.frmOperadorControlador)))
        tablanavegacion.Add("AdministracionArbol", New MotorIU.Motor.Destino(GetType(ClienteAdmin.frmAdministracionArbol), GetType(ClienteAdmin.frmAdministracionArbolctrl)))
        tablanavegacion.Add("AdminRutaAlmacenamiento", New MotorIU.Motor.Destino(GetType(Framework.Ficheros.FicherosIU.RutaAlmacenamientoFrm), GetType(Framework.Ficheros.FicherosIU.ctrlRutaAlmacenamientoFrm)))
        tablanavegacion.Add("AdminTipos", New MotorIU.Motor.Destino(GetType(ControlesPGenericos.frmAdministracionTipos), GetType(ControlesPGenericos.ctrlAdministracionTiposForm))) ' "ControlesPGenericos.frmAdministracionTipos", "ControlesPGenericos.ctrlAdministracionTiposForm"))

        ' USUARIOS
        tablanavegacion.Add("AdminUsuarios", New MotorIU.Motor.Destino(GetType(Framework.Usuarios.IUWin.Form.frmAdminUsuarios), GetType(Framework.Usuarios.IUWin.Controladores.ctrlAdminUsuariosForm)))
        tablanavegacion.Add("ListaUsuarios", New MotorIU.Motor.Destino(GetType(Framework.Usuarios.IUWin.Form.frmListadoUsuarios), GetType(Framework.Usuarios.IUWin.Controladores.ctrlAdminUsuariosForm))) ' "UsrForm.frmListadoUsuarios", "UsrControladores.ctrlAdminUsuariosForm"))



        'formularo de tarificación del presupuesto
        tablanavegacion.Add("TarificarPresupuesto", New MotorIU.Motor.Destino(GetType(FN.RiesgosVehiculos.IU.Formularios.frmPresupuesto), Nothing))

        'formulario de presupuesto
        tablanavegacion.Add("Presupuesto", New MotorIU.Motor.Destino(GetType(FN.RiesgosVehiculos.IU.Formularios.frmPresupuesto), Nothing))

        'formulario de tarifiación prueba
        tablanavegacion.Add("TarificarPrueba", New MotorIU.Motor.Destino(GetType(FN.RiesgosVehiculos.IU.Formularios.frmTarifa), GetType(FN.RiesgosVehiculos.IU.Controladores.frmTarifaCtrl)))



        Dim mimarco As New Marco(tablanavegacion)

        CargarConfiguracion()
        Application.Run()




    End Sub

    Private Sub CargarConfiguracion()

        ' cargar las configuraciones del seting

        Dim prop As System.Configuration.SettingsPropertyValue
        Dim a As Object
        a = My.Settings.LocalizacionServidor ' esto hay que ponerlo porque si no consultas al cabron no carga los datos, como tas quedao!!!
        For Each prop In My.Settings.PropertyValues
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add(prop.Name, prop.PropertyValue)
        Next

        ' cargar las configuraciones del app.config



        Dim colValConfigServer As System.Collections.Specialized.NameValueCollection

        colValConfigServer = System.Configuration.ConfigurationManager.AppSettings

        For f As Integer = 0 To colValConfigServer.Count - 1
            Dim clave As String
            clave = colValConfigServer.GetKey(a)
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add(clave, colValConfigServer.Item(clave))
        Next

    End Sub

End Module

Public Class Marco
    Inherits MotorIU.Motor.NavegadorBase

    Public Sub New(ByVal pTablaNavegacion As Hashtable)
        MyBase.New(pTablaNavegacion)

        'agregamos el color de fondo para los títulos
        Me.DatosMarco.Add("ColorTituloGD", Color.Lavender)

        Me.Navegar("Autorizacion", Me, Nothing, MotorIU.Motor.TipoNavegacion.Normal, Me.GenerarDatosIniciales(), Nothing)
    End Sub
End Class

Public Class ControladorGenerico
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub
End Class