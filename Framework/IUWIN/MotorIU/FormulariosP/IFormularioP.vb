Namespace FormulariosP
    'ésta es la interfaz que deben implementar todos los formularios personalizados
    Public Interface IFormularioP

        Property PropiedadesControl() As PropiedadesControles.PropiedadesControlP
        Property PropiedadesBoton() As PropiedadesControles.PropiedadesControlP
        Property PropiedadesES() As PropiedadesControles.PropiedadesControlP
        Property Controlador() As IControladorForm
        Property Datos() As Hashtable
        Property cMarco() As Motor.INavegador
        Property Paquete() As Hashtable

        Sub Inicializar()
        Sub InicializarEnCascada(ByVal pcontrol As Object)
        ''' <summary>
        ''' Indica que el Navegador ya ha terminado de cargar el formulario
        ''' </summary>
        Sub PostInicializar()

        Sub EstablecerToolTip(ByVal sender As Object)

        Sub AgregarSubControles(ByVal sender As Object)

        Sub ErrorValidacion(ByVal sender As Object, ByVal e As EventArgs)
        Sub Validado(ByVal sender As Object, ByVal e As EventArgs)

    End Interface
End Namespace
