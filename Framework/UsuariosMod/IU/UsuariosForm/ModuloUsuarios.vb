Imports MotorIU.Motor
Imports PropiedadesControles

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
        PropiedadesBoton.ColorFondo = ColorTranslator.FromWin32(13434879)
        PropiedadesBoton.ColorOver = Color.Brown
        PropiedadesBoton.ForeColor = Color.Black
        PropiedadesBoton.ForeColorOver = Color.White

        'determinamos el aspecto para los controles
        PropiedadesCtrl = New PropiedadesControles.PropiedadesControlP
        PropiedadesCtrl.ColorFondo = ColorTranslator.FromWin32(13434879)
        PropiedadesCtrl.TituloForeColor = Color.Brown

        'determinamos el aspecto para los controles de entrada/salida
        PropiedadesES = New PropiedadesControles.PropiedadesControlP
        PropiedadesES.ColorEdicion = Color.White
        PropiedadesES.ColorConsulta = ColorTranslator.FromWin32(13434879)
        PropiedadesES.ForeColor = Color.Black
        PropiedadesES.ForeColorError = Color.Red
        PropiedadesES.ColorFondo = Color.White
        PropiedadesES.ColorOver = Color.White

        'determinamos las propiedades del formulario
        PropiedadesForm = PropiedadesCtrl


        'cargamos los datos para el navegador
        Navegador = New Hashtable

        Navegador.Add("AdminUsuarios", New Destino(GetType(frmAdminUsuarios), GetType(Controladores.ctrlAdminUsuariosForm))) ' "UsrForm.frmAdminUsuarios", "UsrControladores.ctrlAdminUsuariosForm"))
        Navegador.Add("ListaUsuarios", New Destino(GetType(frmListadoUsuarios), GetType(Controladores.ctrlAdminUsuariosForm))) ' "UsrForm.frmListadoUsuarios", "UsrControladores.ctrlAdminUsuariosForm"))
        'Navegador.Add("ListaEntidadUsuario", New Destino("ClienteAdmin.frmListaOperador", "ClienteAdmin.frmOperadorControlador"))
        'Navegador.Add("EntidadUsuario", New Destino("ClienteAdmin.frmOperador", "ClienteAdmin.frmOperadorControlador"))

        'habilitamos aspecto XP
        Application.EnableVisualStyles()

        'instanciamos la clase que va a llevar el motor de formularios
        cmarco = New Marco(Navegador, PropiedadesForm, PropiedadesES, PropiedadesBoton)

        Application.Run()

    End Sub

End Class


Public Class Marco
    Inherits NavegadorBase

    Public Sub New(ByVal pTablaNavegacion As Hashtable, ByVal pPropiedadesForm As PropiedadesControlP, ByVal pPropiedadesES As PropiedadesControlP, ByVal pPropiedadesBoton As PropiedadesControlP)
        MyBase.New(pTablaNavegacion, pPropiedadesForm, pPropiedadesES, pPropiedadesBoton)

        'cargamos los datosMarco
        Me.DatosMarco = New Hashtable
        '(...)

        Navegar("ListaUsuarios", Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosIniciales, Nothing)
    End Sub

End Class


Public Class CargadorMarco
    Implements MotorIU.Motor.IProveedorTablaNavegacion

    Public Sub CargarTablaNavegacion(ByVal navegador As MotorIU.Motor.INavegador) Implements MotorIU.Motor.IProveedorTablaNavegacion.CargarTablaNavegacion
        navegador.TablaNavegacion.Add("AdminUsuarios", New Destino(GetType(frmAdminUsuarios), GetType(Controladores.ctrlAdminUsuariosForm))) ' "UsrForm.frmAdminUsuarios", "UsrControladores.ctrlAdminUsuariosForm"))
        navegador.TablaNavegacion.Add("ListaUsuarios", New Destino(GetType(frmListadoUsuarios), GetType(Controladores.ctrlAdminUsuariosForm))) ' "UsrForm.frmListadoUsuarios", "UsrControladores.ctrlAdminUsuariosForm"))
        navegador.TablaNavegacion.Add("Login", New Destino(GetType(frmLogin), GetType(frmLoginCtrl)))
    End Sub
End Class