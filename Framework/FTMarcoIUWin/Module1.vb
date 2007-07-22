Module Module1

    Public Sub Main()
        Application.EnableVisualStyles()

        Dim tablanavegacion As New Hashtable

        tablanavegacion.Add("Autorizacion", New MotorIU.Motor.Destino(GetType(FTMarcoIUWin.frmAutorizacion), GetType(FTMarcoIUWin.frmAutorizacionctrl))) ' "ClienteAdmin.frmAutorizacion", "ClienteAdmin.frmAutorizacionctrl"))
        tablanavegacion.Add("Main", New MotorIU.Motor.Destino(GetType(FTMarcoIUWin.frmMain), Nothing)) ' "ClienteAdmin.frmMain", "ClienteAdmin.ControladorGenerico"))
        tablanavegacion.Add("Filtro", New MotorIU.Motor.Destino(GetType(MotorBusquedaIuWin.frmFiltro), GetType(MotorBusquedaIuWin.frmFiltroctrl))) ' "MotorBusquedaIuWin.frmFiltro", "MotorBusquedaIuWin.frmFiltroctrl"))
        tablanavegacion.Add("Acercade", New MotorIU.Motor.Destino(GetType(FTMarcoIUWin.frmAcercaDe), Nothing)) ' "ClienteAdmin.frmAcercaDe", "ClienteAdmin.ControladorGenerico"))
        tablanavegacion.Add("AdminMapVis", New MotorIU.Motor.Destino(GetType(MV2ConfiguradorIUWin.Form1), Nothing)) ' "MV2ConfiguradorIUWin.Form1", Nothing))
        tablanavegacion.Add("SeleccionarTipo", New MotorIU.Motor.Destino(GetType(MV2Controles.frmSeleccionarTipo), Nothing)) '"MV2Controles.frmSeleccionarTipo", Nothing))
        tablanavegacion.Add("FG", New MotorIU.Motor.Destino(GetType(MV2Controles.frmFormularioGenerico), GetType(MotorIU.FormulariosP.ControladorFormBase))) ' "MV2Controles.frmFormularioGenerico", Nothing))




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