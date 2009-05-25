Imports MotorIU
Imports MotorIU.Motor
Imports Framework.IU.IUComun
Imports PropiedadesControles


Module Module1

    Public Sub Main()
        Dim Navegador As Hashtable

        Dim PropiedadesBoton As PropiedadesControlP 'propiedades Botones
        Dim PropiedadesCtrl As PropiedadesControlP 'Propiedades formularios
        Dim PropiedadesES As PropiedadesControlP 'propiedades ctrles edición/consulta
        Dim PropiedadesForm As PropiedadesControlP 'propiedades formularios

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

        Application.EnableVisualStyles()

        'cargamos los datos para el navegador
        Navegador = New Hashtable

        Navegador.Add("ClienteSonda", New Destino(GetType(ClienteSonda.frmClienteSonda), GetType(ClienteSonda.ControladorGenerico))) ' "ClienteSonda.frmClienteSonda", "ClienteSonda.ControladorGenerico"))

        Dim mimotor As New MotorClienteSonda(Navegador, PropiedadesForm, PropiedadesES, PropiedadesBoton)

        Application.Run()
    End Sub


End Module

Public Class MotorClienteSonda
    Inherits Motor.NavegadorBase

    Public Sub New(ByVal pTablaNavegacion As Hashtable, ByVal pPropiedadesForm As PropiedadesControles.PropiedadesControlP, ByVal pPropiedadesControles As PropiedadesControles.PropiedadesControlP, ByVal pPropiedadesBoton As PropiedadesControles.PropiedadesControlP)
        MyBase.New(pTablaNavegacion, pPropiedadesForm, pPropiedadesControles, pPropiedadesBoton)

        'cargar configuracion

        'pantalla inicial
        Navegar("ClienteSonda", Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosIniciales, Nothing)
    End Sub
End Class

Public Class ControladorGenerico
    Inherits MotorIU.FormulariosP.ControladorFormBase

    Public Sub New()

    End Sub
End Class