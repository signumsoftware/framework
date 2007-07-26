Imports MotorIU.Motor
Imports Framework.IU.IUComun
Imports PropiedadesControles

Public Class ModuloFicheros

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

        Navegador.Add("ListaRutasAlmacenamiento", New Destino(GetType(Framework.Ficheros.FicherosIU.RutaAlmacenamientoFrm), GetType(Framework.Ficheros.FicherosIU.ctrlRutaAlmacenamientoFrm))) ' "FicherosForm.RutaAlmacenamientoFrm", "FicherosForm.ctrlRutaAlmacenamientoFrm"))

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

        Navegar("ListaRutasAlmacenamiento", Me, Nothing, TipoNavegacion.Normal, Me.GenerarDatosIniciales, Nothing)
    End Sub

End Class