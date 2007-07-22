Namespace FormulariosP
    'interfaz que implementan los controladores de IcontroladorForm (implementa el base)
    Public Interface IControladorForm
        Property Marco() As Motor.INavegador
        Property ControladorCtrl() As ControlesP.IControladorCtrl
        Property FormularioContenedor() As FormularioBase
    End Interface
End Namespace


