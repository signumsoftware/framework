Namespace ControlesP

    'clase base que heredan los controladoresCtrl
    Public Class ControladorControlBase
        Implements IControladorCtrl

#Region "campos"
        Private mMarco As Motor.INavegador
        Private mControladorForm As FormulariosP.IControladorForm
        Private mPropietario As FormulariosP.IFormularioP
        Private mControl As IControlP
#End Region

#Region "constructor"
        Public Sub New(ByVal pNavegador As Motor.INavegador, ByVal ControlAsociado As IControlP)
            Dim miformulario As FormulariosP.IFormularioP
            Dim micontrol As Windows.Forms.UserControl

            'establecemos el motor
            Me.Marco = pNavegador
            'le decimos el control al que está asociado
            Me.Control = ControlAsociado

            If Not Me.Control Is Nothing Then
                'buscamos el formularioP del que depende este control
                micontrol = Me.Control
                miformulario = micontrol.ParentForm
                'establecemos ese FormularioP como mi propietario
                Me.Propietario = miformulario
                'establecemos su controlador como ControladorForm
                'Me.ControladorForm = Me.Propietario.Controlador
                Me.ControladorForm = miformulario.Controlador

            End If
        End Sub
#End Region

#Region "propiedades"
        Public Property Control() As IControlP
            Get
                Return mControl
            End Get
            Set(ByVal Value As IControlP)
                mControl = Value
            End Set
        End Property

        Public Property ControladorForm() As FormulariosP.IControladorForm Implements IControladorCtrl.ControladorForm
            Get
                Return mControladorForm
            End Get
            Set(ByVal Value As FormulariosP.IControladorForm)
                mControladorForm = Value
            End Set
        End Property

        Public Property Marco() As Motor.INavegador Implements IControladorCtrl.Marco
            Get
                Return mMarco
            End Get
            Set(ByVal Value As Motor.INavegador)
                mMarco = Value
            End Set
        End Property

        Public Property Propietario() As FormulariosP.IFormularioP Implements IControladorCtrl.Propietario
            Get
                Return mPropietario
            End Get
            Set(ByVal Value As FormulariosP.IFormularioP)
                mPropietario = Value
            End Set
        End Property
#End Region
    End Class

End Namespace