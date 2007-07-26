Imports MotorIU.Motor
Imports PropiedadesControles
Imports Framework.IU.IUComun

Public Class modMarco

    Public Shared Sub Main()

        Dim Navegador As Hashtable

        Dim PropiedadesBoton As PropiedadesControlP 'propiedades Botones
        Dim PropiedadesCtrl As PropiedadesControlP 'Propiedades formularios
        Dim PropiedadesES As PropiedadesControlP 'propiedades ctrles edición/consulta
        Dim PropiedadesForm As PropiedadesControlP 'propiedades formularios

        Dim cmarco As Marco

        'determinamos el aspecto para los botones personalizados
        PropiedadesBoton = New PropiedadesControles.PropiedadesControlP
        PropiedadesBoton.ColorFondo = Color.Empty
        PropiedadesBoton.ColorOver = Color.Empty
        PropiedadesBoton.ForeColor = Color.Black
        PropiedadesBoton.ForeColorOver = Color.Empty

        'determinamos el aspecto para los controles
        PropiedadesCtrl = New PropiedadesControles.PropiedadesControlP
        PropiedadesCtrl.ColorFondo = Color.Empty
        PropiedadesCtrl.TituloForeColor = Color.Blue

        'determinamos el aspecto para los controles de entrada/salida
        PropiedadesES = New PropiedadesControles.PropiedadesControlP
        PropiedadesES.ColorEdicion = Color.White
        PropiedadesES.ColorConsulta = Color.Silver
        PropiedadesES.ForeColor = Color.Black
        PropiedadesES.ForeColorError = Color.Red
        PropiedadesES.ColorFondo = Color.White
        PropiedadesES.ColorOver = Color.White

        'determinamos las propiedades del formulario
        PropiedadesForm = PropiedadesCtrl


        'cargamos los datos para el navegador
        Navegador = New Hashtable

        Navegador.Add("GestorDocumentos", New Destino(GetType(GDocEntrantes.frmDocsEntrantes), GetType(Controladores.frmDocsEntrantesctrl))) ' "GDocEntrantes.frmDocsEntrantes", "Controladores.frmDocsEntrantesctrl"))
        Navegador.Add("Login", New Destino(GetType(GDocEntrantes.frmAutorizacion), GetType(Controladores.frmAutorizacionctrl))) '"GDocEntrantes.frmAutorizacion", "Controladores.frmAutorizacionctrl"))
        Navegador.Add("SeleccionArbol", New Destino(GetType(GDocEntrantes.frmArbol), GetType(Controladores.frmArbolctrl))) ' "GDocEntrantes.frmArbol", "Controladores.frmArbolctrl"))
        Navegador.Add("Archivo", New Destino(GetType(GDocEntrantes.frmArchivo), GetType(Controladores.frmArchivoctrl))) ' "GDocEntrantes.frmArchivo", "Controladores.frmArchivoctrl"))
        Navegador.Add("Filtro", New Destino(GetType(MotorBusquedaIuWin.frmFiltro), GetType(MotorBusquedaIuWinCtrl.frmFiltroctrl))) ' "MotorBusquedaIuWin.frmFiltro", "MotorBusquedaIuWinCtrl.frmFiltroctrl"))

        'habilitamos aspecto XP
        Application.EnableVisualStyles()
        CargarConfiguracion()

        'instanciamos la clase que va a llevar el motor de formularios
        cmarco = New Marco(Navegador, PropiedadesForm, PropiedadesES, PropiedadesBoton)

        Application.Run()

        'End Module
    End Sub

    Private Shared Sub CargarConfiguracion()
        Dim prop As System.Configuration.SettingsPropertyValue
        Dim a As Object
        a = My.Settings.LocalizacionServidor
        For Each prop In My.Settings.PropertyValues
            Framework.Configuracion.AppConfiguracion.DatosConfig.Add(prop.Name, prop.PropertyValue)
        Next

    End Sub

End Class


Public Class Marco
    Inherits NavegadorBase

    Public Sub New(ByVal pTablaNavegacion As Hashtable, ByVal pPropiedadesForm As PropiedadesControlP, ByVal pPropiedadesES As PropiedadesControlP, ByVal pPropiedadesBoton As PropiedadesControlP)
        MyBase.New(pTablaNavegacion, pPropiedadesForm, pPropiedadesES, pPropiedadesBoton)

        'cargamos los datosMarco
        Me.DatosMarco = New Hashtable
        '(...)

        Navegar("Login", Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosIniciales, Nothing)
    End Sub


End Class
