Namespace ControlesP
    'interfaz que implementan los controladores de IcontroladorCtrl (implementa el base)
    Public Interface IControladorCtrl
        Property Marco() As Motor.INavegador
        Property ControladorForm() As FormulariosP.IControladorForm
        Property Propietario() As FormulariosP.IFormularioP
    End Interface
End Namespace

