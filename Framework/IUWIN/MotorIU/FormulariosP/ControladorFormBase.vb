Namespace FormulariosP

    'Clase base que heredan los ControladoresForm
    Public Class ControladorFormBase
        Implements FormulariosP.IControladorForm


#Region "campos"
        Private mMarco As Motor.INavegador
        Private mControladorCtrl As ControlesP.IControladorCtrl
        Private mFormularioContenedor As FormulariosP.FormularioBase
#End Region

#Region "constructor"
        Public Sub New()

        End Sub

        Public Sub New(ByVal pMarco As Motor.INavegador)
            Me.Marco = pMarco
        End Sub
#End Region

#Region "propiedades"
        Public Property Marco() As Motor.INavegador Implements IControladorForm.Marco
            Get
                Return mMarco
            End Get
            Set(ByVal Value As Motor.INavegador)
                mMarco = Value
            End Set
        End Property
#End Region

        Public Property ControladorCtrl() As ControlesP.IControladorCtrl Implements IControladorForm.ControladorCtrl
            Get
                Return mControladorCtrl
            End Get
            Set(ByVal Value As ControlesP.IControladorCtrl)
                mControladorCtrl = Value
            End Set
        End Property

        Public Property FormularioContenedor() As FormulariosP.FormularioBase Implements IControladorForm.FormularioContenedor
            Get
                Return mFormularioContenedor
            End Get
            Set(ByVal Value As FormulariosP.FormularioBase)
                mFormularioContenedor = Value
            End Set
        End Property
    End Class

End Namespace