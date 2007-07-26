Imports Framework.IU.IUComun

Namespace Motor
    Public Interface INavegador 'Interface que deben implementar los navegadores (Marcos)
        Property DatosMarco() As Hashtable

        ''' <summary>
        ''' Navega al primer formulario de la aplicación generando los datos necesarios
        ''' de PropiedadesControles de manera adecuada
        ''' </summary>
        ''' <param name="funcion">La función de navegación a la que se quiere navegar
        ''' al comenzar la aplicación</param>
        Sub NavegarInicial(ByVal funcion As String)

        ''' <summary>
        ''' Navegación completa que devuelve una referencia al formulario creado
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="Padre">El nombre del formulario MDI que contandrá al formulario al que se navega</param>
        ''' <param name="pTipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Datos">Los datos de carga que contienen los Formatos de aspecto y comportamiento</param>
        ''' <param name="paquete">Contiene los datos que se le quieren pasar al formulario</param>
        ''' <param name="NuevoForm">Devuelve byref el nuevo formulario que se ha creado</param>
        ''' <remarks></remarks>
        Overloads Sub Navegar(ByVal Funcion As String, ByVal Sender As Object, ByVal Padre As Form, ByVal pTipoNavegacion As TipoNavegacion, ByVal Datos As Hashtable, ByRef paquete As Hashtable, ByRef NuevoForm As FormulariosP.IFormularioP)

        ''' <summary>
        ''' Navega a un formulario con un MDI padre
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="TipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Paquete">Hashtable que contiene los datos para inicializar en el formulario de destino</param>
        ''' <param name="Padre">El nombre del formulario MDI que contandrá al formulario al que se navega</param>
        Overloads Sub Navegar(ByVal Funcion As String, ByVal Sender As Form, ByVal Padre As Form, ByVal TipoNavegacion As TipoNavegacion, ByRef Paquete As Hashtable)

        ''' <summary>
        ''' Navega a un formulario sin un MDI padre
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="TipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Paquete">Hashtable que contiene los datos que se le quieren pasar al formulario de destino</param>
        Overloads Sub Navegar(ByVal Funcion As String, ByVal Sender As Form, ByVal TipoNavegacion As TipoNavegacion, ByRef Paquete As Hashtable)

        ''' <summary>
        ''' Navegación Completa
        ''' </summary>
        ''' <param name="Funcion">El nombre de la función a la que se quiere navegar</param>
        ''' <param name="Sender">El formulario desde el que se navega</param>
        ''' <param name="Padre">El nombre del formulario MDI que contandrá al formulario al que se navega</param>
        ''' <param name="pTipoNavegacion">Tipo de navegación a realizar</param>
        ''' <param name="Datos">Los datos de carga que contienen los Formatos de aspecto y comportamiento</param>
        ''' <param name="paquete">Contiene los datos que se le quieren pasar al formulario</param>
        ''' <remarks></remarks>
        Overloads Sub Navegar(ByVal Funcion As String, ByVal Sender As Object, ByVal Padre As Form, ByVal pTipoNavegacion As TipoNavegacion, ByVal Datos As Hashtable, ByRef paquete As Hashtable)

        Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal sender As Windows.Forms.Control)

        Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal sender As Windows.Forms.Form)

        Overloads Sub MostrarError(ByVal excepcion As System.Exception, ByVal titulo As String)

        Sub MostrarAdvertencia(ByVal mensaje As String, ByVal titulo As String)

        Sub MostrarInformacion(ByVal mensaje As String, ByVal titulo As String)

        ReadOnly Property Principal() As Object

        Property TablaNavegacion() As Hashtable

    End Interface

    ''' <summary>
    ''' Deben implementarla todas las clases que deban crellenar los datos de un
    ''' navegador para los módulos específicos
    ''' </summary>
    ''' <remarks></remarks>
    Public Interface IProveedorTablaNavegacion
        ''' <summary>
        ''' Rellena la TablaNavegacion del navegador con los destinos
        ''' de navegación correspondientes al módulo al que pertenezca
        ''' </summary>
        ''' <param name="navegador"></param>
        ''' <remarks></remarks>
        Sub CargarTablaNavegacion(ByVal navegador As INavegador)
    End Interface
End Namespace
