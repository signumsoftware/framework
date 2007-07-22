Namespace ControlesP
    Public Interface IControlP
        Inherits ControlesPBase.IControlPBase


#Region "propiedades"
        Property PropiedadesBoton() As PropiedadesControles.PropiedadesControlP
        Property PropiedadesES() As PropiedadesControles.PropiedadesControlP
        Property ErroresValidadores() As Framework.DatosNegocio.ArrayListValidable  'esta lista nos da el total de errores
        Property Marco() As Motor.INavegador 'referencia al motor de navegación
        Property Controlador() As IControladorCtrl 'el controlador de contrlP
#End Region

#Region "métodos"
        'función que recogerá el evento de error de cada uno de los controles validables q contenga,
        'o bien de los demás controlesP que contenga
        Sub ErrorValidando(ByVal sender As Object, ByVal e As EventArgs)
        Sub ComprobarValidaciones() 'nos dirá si hay o no errores
        Sub Validacion(ByVal sender As Object, ByVal e As EventArgs) 'es el sub que recoje los eventos de validaciónok de los ctrlsES
#End Region

#Region "eventos"
        'evento de error para que burbujee hacia arriba
        Event ErrorValidacion(ByVal sender As Object, ByVal e As EventArgs)
        'evento de no error para que burbujee hacia arriba
        Event Validado(ByVal sender As Object, ByVal e As EventArgs)
#End Region

#Region "inicializadores"
        Sub Inicializar()
        Sub InicializarEnCascada(ByVal pcontrol As Object)
#End Region

    End Interface
End Namespace
